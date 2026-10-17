using AutoMapper;
using DigitalWallet.Application.DTOs;
using DigitalWallet.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DigitalWallet.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WalletsController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly IMapper _mapper;

        public WalletsController(IWalletService _walletService,IMapper mapper)
        {
            this._walletService = _walletService;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> CreateWallet([FromQuery] string currency = "EGP")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var wallet = await _walletService.CreateWalletAsync(userId, currency);
                return Ok(wallet);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserWallet()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var wallet = await _walletService.GetUserWalletAsync(userId);

            if (wallet == null)
            {
                return NotFound(new { message = "Wallet not found for this user." });
            }

            return Ok(wallet);
        }

        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var transaction = await _walletService.DepositAsync(
                    userId,
                    request.Amount,
                    request.Currency,
                    request.IdempotencyKey
                );

                return Ok(new
                {
                    message = "Deposit successful",
                    transactionId = transaction.Id,
                    referenceNumber = transaction.ReferenceNumber,
                    createdAt = transaction.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawalRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var transaction = await _walletService.WithdrawAsync(
                    userId,
                    request.Amount,
                    request.Currency,
                    request.IdempotencyKey
                );

                return Ok(new
                {
                    message = "Withdrawal successful",
                    transactionId = transaction.Id,
                    referenceNumber = transaction.ReferenceNumber,
                    createdAt = transaction.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequestDto request)
        {
            var senderUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(senderUserId)) return Unauthorized();

            try
            {
                var transaction = await _walletService.TransferAsync(
                    senderUserId,
                    request.ReceiverWalletId, // استخدام الـ Guid الجديد
                    request.Amount,
                    request.Currency,
                    request.IdempotencyKey
                );

                return Ok(new
                {
                    message = "Transfer successful",
                    transactionId = transaction.Id,
                    referenceNumber = transaction.ReferenceNumber,
                    createdAt = transaction.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetUserTransactions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var transactionsDto = await _walletService.GetUserTransactionsAsync(userId);
                return Ok(transactionsDto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
