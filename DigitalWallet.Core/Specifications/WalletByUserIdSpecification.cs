using DigitalWallet.Core.Entities;

namespace DigitalWallet.Core.Specifications;

public class WalletByUserIdSpecification : BaseSpecification<Wallet>
{
    public WalletByUserIdSpecification(string userId)
        : base(w => w.UserId == userId)
    {
        // لو حابب تجيب معاها المعاملات (Transactions) مستقبلاً، تقدر تضيف Include هنا بالشكل ده:
        // AddInclude(w => w.Transactions);
    }
}