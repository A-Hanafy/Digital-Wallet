using DigitalWallet.Core.Entities;

namespace DigitalWallet.Core.Specifications
{
    public class IdempotencyKeySpecification : BaseSpecification<IdempotencyKey>
    {
        public IdempotencyKeySpecification(string idempotencyKey)
            : base(k => k.Key == idempotencyKey)
        {
        }
    }
}