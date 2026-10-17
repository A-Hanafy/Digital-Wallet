using AutoMapper;
using DigitalWallet.Application.DTOs;
using DigitalWallet.Core.Entities;
using DigitalWallet.Core.Interfaces;
using DigitalWallet.Core.Specifications;


namespace DigitalWallet.Infrastructure.Services
{
    public class WalletService :IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public WalletService(IUnitOfWork unitOfWork,IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Wallet> CreateWalletAsync(string userId, string currency = "EGP")
        {
            // التأكد إن المستخدم مش عنده محفظة قديمة لنفس العملة
            var existingWallets = await _unitOfWork.Repository<Wallet>()
                .ListAsync(new WalletByUserIdSpecification(userId));

            if (existingWallets.Any(w => w.Currency == currency))
            {
                throw new Exception("User already has a wallet with this currency.");
            }

            var wallet = new Wallet
            {
                UserId = userId,
                Balance = 0.00m,
                Currency = currency,
                IsActive = true
            };

            await _unitOfWork.Repository<Wallet>().AddAsync(wallet);
            await _unitOfWork.CompleteAsync();

            return wallet;
        }

        public async Task<Wallet?> GetUserWalletAsync(string userId)
        {
            return await _unitOfWork.Repository<Wallet>()
                .GetEntityWithSpec(new WalletByUserIdSpecification(userId));
        }

        public async Task<decimal> GetWalletBalanceAsync(string userId)
        {
            var wallet = await GetUserWalletAsync(userId);
            return wallet?.Balance ?? 0;
        }

        public async Task<Transaction> DepositAsync(string userId, decimal amount, string currency, string idempotencyKey)
        {
            if (amount <= 0)
                throw new ArgumentException("Deposit amount must be greater than zero.");

            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var existingKey = await _unitOfWork.Repository<IdempotencyKey>()
                    .GetEntityWithSpec(new IdempotencyKeySpecification(idempotencyKey));

                if (existingKey != null)
                {
                    throw new Exception("This request has already been processed.");
                }
            }

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                var wallets = await _unitOfWork.Repository<Wallet>()
                    .ListAsync(new WalletByUserIdSpecification(userId));

                var wallet = wallets.FirstOrDefault(w => w.Currency == currency);
                if (wallet == null)
                {
                    wallet = await CreateWalletAsync(userId, currency);
                }

                if (!wallet.IsActive)
                    throw new Exception("Wallet is inactive.");

                wallet.Balance += amount;

                var transaction = new Transaction
                {
                    ReceiverWalletId = wallet.Id,
                    Amount = amount,
                    Currency = currency,
                    Type = TransactionType.Deposit,
                    Status = TransactionStatus.Completed
                };

                await _unitOfWork.Repository<Transaction>().AddAsync(transaction);
                _unitOfWork.Repository<Wallet>().Update(wallet);

                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    var keyEntity = new IdempotencyKey
                    {
                        Key = idempotencyKey,
                        UserId = userId,
                        ExpiryTime = DateTime.UtcNow.AddHours(24)
                    };
                    await _unitOfWork.Repository<IdempotencyKey>().AddAsync(keyEntity);
                }

                await _unitOfWork.CompleteAsync();
                await dbTransaction.CommitAsync();

                return transaction;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Transaction> WithdrawAsync(string userId, decimal amount, string currency, string idempotencyKey)
        {
            if (amount <= 0)
                throw new ArgumentException("Withdrawal amount must be greater than zero.");

            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var existingKey = await _unitOfWork.Repository<IdempotencyKey>()
                    .GetEntityWithSpec(new IdempotencyKeySpecification(idempotencyKey));

                if (existingKey != null)
                {
                    throw new Exception("This request has already been processed.");
                }
            }

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                var wallets = await _unitOfWork.Repository<Wallet>()
                    .ListAsync(new WalletByUserIdSpecification(userId));

                var wallet = wallets.FirstOrDefault(w => w.Currency == currency);
                if (wallet == null)
                {
                    throw new Exception("Wallet not found for this currency.");
                }

                if (!wallet.IsActive)
                    throw new Exception("Wallet is inactive.");

                if (wallet.Balance < amount)
                {
                    throw new Exception("Insufficient balance.");
                }

                wallet.Balance -= amount;

                var transaction = new Transaction
                {
                    SenderWalletId = wallet.Id,
                    Amount = amount,
                    Currency = currency,
                    Type = TransactionType.Withdrawal,
                    Status = TransactionStatus.Completed
                };

                await _unitOfWork.Repository<Transaction>().AddAsync(transaction);
                _unitOfWork.Repository<Wallet>().Update(wallet);

                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    var keyEntity = new IdempotencyKey
                    {
                        Key = idempotencyKey,
                        UserId = userId,
                        ExpiryTime = DateTime.UtcNow.AddHours(24)
                    };
                    await _unitOfWork.Repository<IdempotencyKey>().AddAsync(keyEntity);
                }

                await _unitOfWork.CompleteAsync();
                await dbTransaction.CommitAsync();

                return transaction;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Transaction> TransferAsync(string senderUserId, Guid receiverWalletId, decimal amount, string currency, string idempotencyKey)
        {
            if (amount <= 0)
                throw new ArgumentException("Transfer amount must be greater than zero.");

            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var existingKey = await _unitOfWork.Repository<IdempotencyKey>()
                    .GetEntityWithSpec(new IdempotencyKeySpecification(idempotencyKey));

                if (existingKey != null)
                {
                    throw new Exception("This request has already been processed.");
                }
            }

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                var senderWallets = await _unitOfWork.Repository<Wallet>()
                    .ListAsync(new WalletByUserIdSpecification(senderUserId));

                var senderWallet = senderWallets.FirstOrDefault(w => w.Currency == currency);
                if (senderWallet == null || !senderWallet.IsActive)
                    throw new Exception("Sender wallet not found or inactive.");

                if (senderWallet.Balance < amount)
                    throw new Exception("Insufficient balance.");

                var receiverWallet = await _unitOfWork.Repository<Wallet>()
     .GetEntityWithSpec(new WalletByIdSpecification(receiverWalletId));

                if (receiverWallet == null || !receiverWallet.IsActive)
                    throw new Exception("Receiver wallet not found or inactive.");

                if (receiverWallet.Currency != currency)
                    throw new Exception("Currency mismatch between sender and receiver wallets.");

                if (senderWallet.Id == receiverWallet.Id)
                    throw new Exception("Cannot transfer funds to the same wallet.");

                senderWallet.Balance -= amount;
                receiverWallet.Balance += amount;

                var transaction = new Transaction
                {
                    SenderWalletId = senderWallet.Id,
                    ReceiverWalletId = receiverWallet.Id,
                    Amount = amount,
                    Currency = currency,
                    Type = TransactionType.Transfer,
                    Status = TransactionStatus.Completed
                };

                await _unitOfWork.Repository<Transaction>().AddAsync(transaction);
                _unitOfWork.Repository<Wallet>().Update(senderWallet);
                _unitOfWork.Repository<Wallet>().Update(receiverWallet);

                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    var keyEntity = new IdempotencyKey
                    {
                        Key = idempotencyKey,
                        UserId = senderUserId,
                        ExpiryTime = DateTime.UtcNow.AddHours(24)
                    };
                    await _unitOfWork.Repository<IdempotencyKey>().AddAsync(keyEntity);
                }

                await _unitOfWork.CompleteAsync();
                await dbTransaction.CommitAsync();

                return transaction;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<TransactionDto>> GetUserTransactionsAsync(string userId)
        {
            var spec = new TransactionsForUserSpecification(userId, 0, 50);
            var transactions = await _unitOfWork.Repository<Transaction>().ListAsync(spec);

            return _mapper.Map<IEnumerable<TransactionDto>>(transactions);
        }
    }
}
