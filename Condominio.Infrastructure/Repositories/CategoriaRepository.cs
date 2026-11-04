using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Repositories
{
    public class CategoriaRepository : GenericRepository<CategoriasGasto>
    {
        public CategoriaRepository(IDbContextFactory<AppDbContext> factory) : base(factory) { }


        public override async Task<IEnumerable<CategoriasGasto>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<CategoriasGasto>()
                .Include(c => c.Proveedor)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
    }
}
