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
    public class CuotaEspecialCasaRepository : GenericRepository<CuotaEspecialCasa>
    {

        public CuotaEspecialCasaRepository(AppDbContext context) : base(context) { }


        public override async Task<IEnumerable<CuotaEspecialCasa>> GetAllAsync()
        {
            return await _dbSet.Include(x => x.CuotaEspecial).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }

    }
}
