using AutoMapper;
using DigitalWallet.Application.DTOs;
using DigitalWallet.Core.Entities;
using DigitalWallet.Core.Interfaces;
using DigitalWallet.Core.Specifications;
using DigitalWallet.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace DigitalWallet.UnitTests.Services;

/// <summary>
/// Unit tests for WalletService following the Arrange-Act-Assert (AAA) pattern.
/// All dependencies are mocked with Moq — no database, no real transactions.
/// Every test runs in complete isolation; a fresh set of mocks is created per test.
/// </summary>
public class WalletServiceTests
{
    private const string UserId = "user-1";
    private const string Currency = "EGP";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IGenericRepository<Wallet>> _walletRepo = new();
    private readonly Mock<IGenericRepository<Transaction>> _transactionRepo = new();
    private readonly Mock<IGenericRepository<IdempotencyKey>> _idempotencyRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IDbContextTransaction> _dbTransaction = new();

    private readonly WalletService _sut;

    public WalletServiceTests()
    {
        // Route every generic Repository<T>() call to its dedicated Moq.
        _unitOfWork.Setup(u => u.Repository<Wallet>()).Returns(_walletRepo.Object);
        _unitOfWork.Setup(u => u.Repository<Transaction>()).Returns(_transactionRepo.Object);
        _unitOfWork.Setup(u => u.Repository<IdempotencyKey>()).Returns(_idempotencyRepo.Object);

        // A mock EF transaction: the SUT opens, commits, or rolls it back.
        _dbTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dbTransaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_dbTransaction.Object);

        _sut = new WalletService(_unitOfWork.Object, _mapper.Object);
    }

    // ------------------------------------------------------------------ #
    //  Helpers
    // ------------------------------------------------------------------ #

    private static Wallet MakeWallet(decimal balance = 100m, string currency = "EGP", bool isActive = true)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Balance = balance,
            Currency = currency,
            IsActive = isActive
        };

    /// <summary>Makes Repository&lt;Wallet&gt;.ListAsync(spec) return the given wallets.</summary>
    private void SetupUserWallets(params Wallet[] wallets)
        => _walletRepo
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Wallet>>()))
            .ReturnsAsync(wallets);

    /// <summary>Makes Repository&lt;Wallet&gt;.GetEntityWithSpec(spec) return the given wallet (receiver lookup in transfers).</summary>
    private void SetupReceiverWallet(Wallet? receiver)
        => _walletRepo
            .Setup(r => r.GetEntityWithSpec(It.IsAny<ISpecification<Wallet>>()))
            .ReturnsAsync(receiver);

    // ================================================================== #
    //  CreateWalletAsync
    // ================================================================== #

    [Fact]
    public async Task CreateWalletAsync_WhenUserHasNoWalletForCurrency_CreatesActiveWalletWithZeroBalance()
    {
        // Arrange
        SetupUserWallets(); // no existing wallets

        // Act
        var result = await _sut.CreateWalletAsync(UserId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(UserId);
        result.Currency.Should().Be(Currency);
        result.Balance.Should().Be(0.00m);
        result.IsActive.Should().BeTrue();

        _walletRepo.Verify(r => r.AddAsync(result), Times.Once);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateWalletAsync_WhenUserAlreadyHasWalletWithSameCurrency_ThrowsAndPersistsNothing()
    {
        // Arrange
        var existing = MakeWallet(currency: Currency);
        SetupUserWallets(existing);

        // Act
        var act = () => _sut.CreateWalletAsync(UserId, Currency);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("User already has a wallet with this currency.");
        _walletRepo.Verify(r => r.AddAsync(It.IsAny<Wallet>()), Times.Never);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    // ================================================================== #
    //  GetWalletBalanceAsync / GetUserWalletAsync
    // ================================================================== #

    [Fact]
    public async Task GetWalletBalanceAsync_WhenWalletExists_ReturnsWalletBalance()
    {
        // Arrange
        var wallet = MakeWallet(balance: 425.50m);
        SetupReceiverWallet(wallet); // GetWalletBalanceAsync goes through GetEntityWithSpec

        // Act
        var balance = await _sut.GetWalletBalanceAsync(UserId);

        // Assert
        balance.Should().Be(425.50m);
    }

    [Fact]
    public async Task GetWalletBalanceAsync_WhenWalletDoesNotExist_ReturnsZero()
    {
        // Arrange
        SetupUserWallets();

        // Act
        var balance = await _sut.GetWalletBalanceAsync(UserId);

        // Assert
        balance.Should().Be(0);
    }

    // ================================================================== #
    //  DepositAsync
    // ================================================================== #

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-99.99)]
    public async Task DepositAsync_WhenAmountIsNotPositive_ThrowsArgumentException(decimal amount)
    {
        // Arrange
        // Act
        var act = () => _sut.DepositAsync(UserId, amount, Currency, "");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Deposit amount must be greater than zero.");
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task DepositAsync_WhenIdempotencyKeyAlreadyProcessed_ThrowsAndStartsNoTransaction()
    {
        // Arrange
        _idempotencyRepo
            .Setup(r => r.GetEntityWithSpec(It.IsAny<ISpecification<IdempotencyKey>>()))
            .ReturnsAsync(new IdempotencyKey { Key = "dup-key", UserId = UserId });

        // Act
        var act = () => _sut.DepositAsync(UserId, 100m, Currency, "dup-key");

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("This request has already been processed.");
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task DepositAsync_WhenAmountIsValid_CreditsWalletAndPersistsCompletedDeposit()
    {
        // Arrange
        var wallet = MakeWallet(balance: 50m, currency: Currency);
        SetupUserWallets(wallet);

        // Act
        var result = await _sut.DepositAsync(UserId, 100m, Currency, "");

        // Assert
        wallet.Balance.Should().Be(150m);

        result.ReceiverWalletId.Should().Be(wallet.Id);
        result.Amount.Should().Be(100m);
        result.Currency.Should().Be(Currency);
        result.Type.Should().Be(TransactionType.Deposit);
        result.Status.Should().Be(TransactionStatus.Completed);

        _transactionRepo.Verify(r => r.AddAsync(result), Times.Once);
        _walletRepo.Verify(r => r.Update(wallet), Times.Once);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        _dbTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DepositAsync_WhenUserHasNoWalletInCurrency_AutoCreatesOneThenDeposits()
    {
        // Arrange — first ListAsync finds nothing, CreateWalletAsync lists again, then AddAsync appends.
        var createdWallet = MakeWallet(balance: 0m, currency: Currency, isActive: true);
        _walletRepo
            .SetupSequence(r => r.ListAsync(It.IsAny<ISpecification<Wallet>>()))
            .ReturnsAsync(Array.Empty<Wallet>())      // deposit lookup
            .ReturnsAsync(Array.Empty<Wallet>());     // duplicate check inside CreateWalletAsync
        _walletRepo
            .Setup(r => r.AddAsync(It.IsAny<Wallet>()))
            .Callback<Wallet>(w => { /* emulate EF: assign persistence artifacts */ })
            .Returns(Task.CompletedTask);

        // Simulate that after AddAsync, the deposit overwrites `wallet` with the created one
        // by making the third lookup (if any) return nothing and validating via behavior below.

        // Act
        var result = await _sut.DepositAsync(UserId, 75m, Currency, "");

        // Assert — new wallet was created and the deposit persisted on it.
        _walletRepo.Verify(r => r.AddAsync(It.Is<Wallet>(w =>
            w.UserId == UserId && w.Currency == Currency && w.IsActive)), Times.Once);
        result.ReceiverWalletId.Should().NotBeNull();
        result.Amount.Should().Be(75m);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Exactly(2)); // once in CreateWalletAsync + once in DepositAsync
        _dbTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DepositAsync_WhenWalletIsInactive_ThrowsAndRollsBack()
    {
        // Arrange
        var wallet = MakeWallet(balance: 100m, currency: Currency, isActive: false);
        SetupUserWallets(wallet);

        // Act
        var act = () => _sut.DepositAsync(UserId, 50m, Currency, "");

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Wallet is inactive.");
        _dbTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    // ================================================================== #
    //  WithdrawAsync
    // ================================================================== #

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task WithdrawAsync_WhenAmountIsNotPositive_ThrowsArgumentException(decimal amount)
    {
        // Arrange
        // Act
        var act = () => _sut.WithdrawAsync(UserId, amount, Currency, "");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task WithdrawAsync_WhenBalanceIsSufficient_DeductsAmountAndPersistsTransaction()
    {
        // Arrange
        var wallet = MakeWallet(balance: 200m, currency: Currency);
        SetupUserWallets(wallet);

        // Act
        var result = await _sut.WithdrawAsync(UserId, 80m, Currency, "");

        // Assert
        wallet.Balance.Should().Be(120m);
        result.SenderWalletId.Should().Be(wallet.Id);
        result.Amount.Should().Be(80m);
        result.Type.Should().Be(TransactionType.Withdrawal);
        result.Status.Should().Be(TransactionStatus.Completed);

        _walletRepo.Verify(r => r.Update(wallet), Times.Once);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        _dbTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WithdrawAsync_WhenBalanceIsInsufficient_ThrowsAndDoesNotCommit()
    {
        // Arrange
        var wallet = MakeWallet(balance: 50m, currency: Currency);
        SetupUserWallets(wallet);

        // Act
        var act = () => _sut.WithdrawAsync(UserId, 100m, Currency, "");

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Insufficient balance.");
        wallet.Balance.Should().Be(50m, "the in-memory deduction is rolled back by the rethrow before any read");
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
        _dbTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithdrawAsync_WhenNoWalletForCurrency_Throws()
    {
        // Arrange
        SetupUserWallets(MakeWallet(currency: "USD")); // user only has a USD wallet

        // Act
        var act = () => _sut.WithdrawAsync(UserId, 10m, Currency, "");

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Wallet not found for this currency.");
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    // ================================================================== #
    //  TransferAsync
    // ================================================================== #

    [Fact]
    public async Task TransferAsync_WhenEverythingIsValid_MovesFundsBetweenWallets()
    {
        // Arrange
        var sender = MakeWallet(balance: 300m, currency: Currency);
        var receiver = MakeWallet(balance: 100m, currency: Currency);
        SetupUserWallets(sender);
        SetupReceiverWallet(receiver);

        // Act
        var result = await _sut.TransferAsync(UserId, receiver.Id, 150m, Currency, "");

        // Assert
        sender.Balance.Should().Be(150m);
        receiver.Balance.Should().Be(250m);

        result.SenderWalletId.Should().Be(sender.Id);
        result.ReceiverWalletId.Should().Be(receiver.Id);
        result.Amount.Should().Be(150m);
        result.Type.Should().Be(TransactionType.Transfer);
        result.Status.Should().Be(TransactionStatus.Completed);

        _walletRepo.Verify(r => r.Update(sender), Times.Once);
        _walletRepo.Verify(r => r.Update(receiver), Times.Once);
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        _dbTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransferAsync_WhenReceiverWalletCurrencyDiffers_ThrowsCurrencyMismatch()
    {
        // Arrange
        var sender = MakeWallet(balance: 100m, currency: Currency);
        var receiver = MakeWallet(balance: 100m, currency: "USD");
        SetupUserWallets(sender);
        SetupReceiverWallet(receiver);

        // Act
        var act = () => _sut.TransferAsync(UserId, receiver.Id, 50m, Currency, "");

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Currency mismatch between sender and receiver wallets.");
        sender.Balance.Should().Be(100m);
        receiver.Balance.Should().Be(100m);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task TransferAsync_WhenReceiverIsTheSameWallet_Throws()
    {
        // Arrange
        var sender = MakeWallet(balance: 100m, currency: Currency);
        SetupUserWallets(sender);
        SetupReceiverWallet(sender); // receiver resolves to the sender's own wallet

        // Act
        var act = () => _sut.TransferAsync(UserId, sender.Id, 50m, Currency, "");

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Cannot transfer funds to the same wallet.");
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task TransferAsync_WhenReceiverWalletNotFound_Throws()
    {
        // Arrange
        var sender = MakeWallet(balance: 100m, currency: Currency);
        SetupUserWallets(sender);
        SetupReceiverWallet(null);

        // Act
        var act = () => _sut.TransferAsync(UserId, Guid.NewGuid(), 50m, Currency, "");

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Receiver wallet not found or inactive.");
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task TransferAsync_WhenSenderBalanceIsInsufficient_Throws()
    {
        // Arrange
        var sender = MakeWallet(balance: 20m, currency: Currency);
        var receiver = MakeWallet(balance: 0m, currency: Currency);
        SetupUserWallets(sender);
        SetupReceiverWallet(receiver);

        // Act
        var act = () => _sut.TransferAsync(UserId, receiver.Id, 50m, Currency, "");

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Insufficient balance.");
        sender.Balance.Should().Be(20m);
        receiver.Balance.Should().Be(0m);
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
    }

    // ================================================================== #
    //  GetUserTransactionsAsync
    // ================================================================== #

    [Fact]
    public async Task GetUserTransactionsAsync_WhenTransactionsExist_ReturnsMappedDtos()
    {
        // Arrange
        var transactions = new List<Transaction> { new(), new() };
        var expectedDtos = new List<TransactionDto> { new(), new() };
        _transactionRepo
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync((IReadOnlyList<Transaction>)transactions);
        _mapper
            .Setup(m => m.Map<IEnumerable<TransactionDto>>(It.IsAny<object>()))
            .Returns(expectedDtos.AsReadOnly());

        // Act
        var result = await _sut.GetUserTransactionsAsync(UserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mapper.Verify(m => m.Map<IEnumerable<TransactionDto>>(It.IsAny<object>()), Times.Once);
    }
}
