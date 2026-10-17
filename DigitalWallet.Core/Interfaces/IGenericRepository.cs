using DigitalWallet.Core.Specifications;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWallet.Core.Interfaces
{
    public interface  IGenericRepository<T>
    where T : class
{
    
        Task<T?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<T?> GetEntityWithSpec(ISpecification<T> spec);
        Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec);
        Task<int> CountAsync(ISpecification<T> spec);

        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}
