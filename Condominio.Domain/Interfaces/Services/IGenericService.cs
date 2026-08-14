using System;
using System.Collections.Generic;
using System.Text;

namespace Condominio.Domain.Interfaces.Services
{
    // IGenericService.cs
    public interface IGenericService<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task<int> SaveChangesAsync();
    }
}
