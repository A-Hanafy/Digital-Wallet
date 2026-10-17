using DigitalWallet.Core.Entities;
using DigitalWallet.Core.Specifications;

namespace DigitalWallet.Core.Specifications
{
    public class TransactionsForUserSpecification : BaseSpecification<Transaction>
    {
        public TransactionsForUserSpecification(string userId, int skip, int take)
              : base(t => (t.SenderWallet != null && t.SenderWallet.UserId == userId) ||
                          (t.ReceiverWallet != null && t.ReceiverWallet.UserId == userId))
        {
            AddInclude(t => t.SenderWallet!);
            AddInclude(t => t.ReceiverWallet!);
            ApplyPaging(skip, take);
            AddOrderByDescending(t => t.CreatedAt); 
        }
    }
}