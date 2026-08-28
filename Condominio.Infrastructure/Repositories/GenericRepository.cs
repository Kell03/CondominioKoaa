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
        protected readonly AppDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public GenericRepository(AppDbContext context)
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

        public virtual void ClearTracker()
        {
            try
            {
                // ✅ LIMPIAR EL CHANGE TRACKER
                _context.ChangeTracker.Clear();
                Console.WriteLine("✅ ChangeTracker limpiado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al limpiar ChangeTracker: {ex.Message}");
                throw;
            }
        }

        public virtual async Task AddAsync(TEntity entity)
        {
            try
            {
                ClearTracker();


                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                // ✅ VERIFICAR EL ID
                var idProperty = typeof(TEntity).GetProperty("Id");
                if (idProperty != null)
                {
                    var id = (int)idProperty.GetValue(entity);
                    Console.WriteLine($"📊 Id de la entidad: {id}");

                    // ✅ SI EL ID ES 0, AGREGAR
                    if (id == 0)
                    {
                        await _dbSet.AddAsync(entity);
                        Console.WriteLine("✅ Entidad agregada como Added");
                    }
                    else
                    {
                        // ❌ SI EL ID NO ES 0, NO SE PUEDE AGREGAR
                        Console.WriteLine($"⚠️ La entidad ya tiene Id: {id}. No se puede agregar.");
                        throw new Exception($"La entidad ya tiene Id: {id}. Use Update en lugar de Add.");
                    }
                }
                else
                {
                    await _dbSet.AddAsync(entity);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en AddAsync: {ex.Message}");
                throw;
            }
        }

        public virtual void Update(TEntity entity)
        {
            try
            {

                ClearTracker();

                // 1. OBTENER EL ID
                var idProperty = typeof(TEntity).GetProperty("Id");
                if (idProperty == null)
                    throw new Exception("La entidad no tiene propiedad Id");

                var id = (int)idProperty.GetValue(entity);

                // 2. BUSCAR LA ENTIDAD EXISTENTE EN LA BASE DE DATOS
                var existingEntity = _dbSet.Find(id);

                if (existingEntity == null)
                    throw new Exception($"Entidad con ID {id} no encontrada");

                // 3. ACTUALIZAR LOS VALORES DE LA ENTIDAD EXISTENTE
                _context.Entry(existingEntity).CurrentValues.SetValues(entity);
                _context.Entry(existingEntity).State = EntityState.Modified;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al actualizar: {ex.Message}");
                throw;
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
