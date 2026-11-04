using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Condominio.Domain.Interfaces.Repositories
{
    public interface IGenericRepository<TEntity>
    where TEntity : class
    {
        Task<TEntity> GetByIdAsync(int id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task AddAsync(TEntity entity);
        Task Update(TEntity entity);
        Task Delete(TEntity entity);

    }
}
