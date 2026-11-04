using Condominio.Domain.DB;
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
        protected readonly IDbContextFactory<AppDbContext> _factory;

        public GenericRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }


        // VIRTUAL → permite sobrescritura
        public virtual async Task<TEntity> GetByIdAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.Set<TEntity>().FindAsync(id);
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.Set<TEntity>().AsNoTracking().ToListAsync();
        }

     

        public virtual async Task AddAsync(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            await using var db = await _factory.CreateDbContextAsync();

            var idProperty = typeof(TEntity).GetProperty("Id");
            if (idProperty != null)
            {
                var id = (int)idProperty.GetValue(entity)!;
                if (id != 0)
                    throw new InvalidOperationException(
                        $"La entidad ya tiene Id: {id}. Use Update en lugar de Add.");
            }

            db.Set<TEntity>().Add(entity);
            await db.SaveChangesAsync();
        }

        public virtual async Task Update(TEntity entity)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var idProperty = typeof(TEntity).GetProperty("Id")
                ?? throw new InvalidOperationException("La entidad no tiene propiedad Id");

            var id = (int)idProperty.GetValue(entity)!;

            var existing = await db.Set<TEntity>().FindAsync(id)
                ?? throw new InvalidOperationException($"Entidad con ID {id} no encontrada");

            db.Entry(existing).CurrentValues.SetValues(entity);
            await db.SaveChangesAsync();
        }

        public virtual async Task Delete(TEntity entity)
        {
            await using var db = await _factory.CreateDbContextAsync();
            db.Set<TEntity>().Remove(entity);
            await db.SaveChangesAsync();
        }




    }
}
