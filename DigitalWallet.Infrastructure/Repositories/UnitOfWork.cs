using DigitalWallet.Core.Interfaces;
using DigitalWallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections;


namespace DigitalWallet.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();
        private bool _disposed;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class
        {
            var entityType = typeof(TEntity);

            if (_repositories.TryGetValue(entityType, out var repo))
            {
                return (IGenericRepository<TEntity>)repo;
            }

            var repositoryType = typeof(GenericRepository<>);
            var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(entityType), _context);

            _repositories.Add(entityType, repositoryInstance!);

            return (IGenericRepository<TEntity>)repositoryInstance!;
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
  