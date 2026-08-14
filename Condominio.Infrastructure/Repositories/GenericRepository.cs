using Condominio.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Condominio.Infrastructure.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : class

    {
        protected readonly DbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public GenericRepository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        // VIRTUAL → permite sobrescritura
        public virtual async Task<TEntity> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }


        public virtual async Task AddAsync(TEntity entity)
        {

            try
            {
                await _dbSet.AddAsync(entity);

            }
            catch (Exception ex)
            {
                ;


            }
        }

        public virtual void Update(TEntity entity)
        {
            try
            {
                _dbSet.Update(entity);

            }
            catch (Exception ex)
            {
                ;
            }
        }

        public virtual void Delete(TEntity entity)
        {
            _dbSet.Remove(entity);
        }



        public async Task<int> SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}
