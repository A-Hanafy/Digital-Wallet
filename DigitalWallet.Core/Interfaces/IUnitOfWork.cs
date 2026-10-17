using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWallet.Core.Interfaces
{
    public interface IUnitOfWork
    {
        IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class;
        Task<int> CompleteAsync();

        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
