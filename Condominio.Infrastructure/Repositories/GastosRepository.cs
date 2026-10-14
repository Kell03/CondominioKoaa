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
    public class GastosRepository : GenericRepository<Gasto>
    {
        public GastosRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<Gasto>> GetAllAsync()
        {
            _context.ChangeTracker.Clear();

            return await _dbSet.Include(x => x.FacturaMesHijo).Include(x => x.CuotaEspecial).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }


    }
}
