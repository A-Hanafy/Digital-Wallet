using DigitalWallet.Application.DTOs;
using DigitalWallet.Core.Entities;


namespace DigitalWallet.Core.Interfaces;

public interface IWalletService
{
    Task<Wallet> CreateWalletAsync(string userId, string currency = "EGP");
    Task<Wallet?> GetUserWalletAsync(string userId);
    Task<decimal> GetWalletBalanceAsync(string userId);

    Task<Transaction> DepositAsync(string userId, decimal amount, string currency, string idempotencyKey);
    Task<Transaction> WithdrawAsync(string userId, decimal amount, string currency, string idempotencyKey);
    Task<Transaction> TransferAsync(string senderUserId, Guid receiverWalletId, decimal amount, string currency, string idempotencyKey);

    Task<IEnumerable<TransactionDto>> GetUserTransactionsAsync(string userId);
}