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

        public CategoriaRepository(AppDbContext context) : base(context) { }


        public override async Task<IEnumerable<CategoriasGasto>> GetAllAsync()
        {
            _context.ChangeTracker.Clear();

            return await _dbSet.Include(c => c.Proveedor).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }
    }
}
