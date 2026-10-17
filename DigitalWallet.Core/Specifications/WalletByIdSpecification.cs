using DigitalWallet.Core.Entities;

namespace DigitalWallet.Core.Specifications
{

        public class WalletByIdSpecification : BaseSpecification<Wallet>
        {
            public WalletByIdSpecification(Guid walletId)
                : base(w => w.Id == walletId)
            {
            }
        }
}