using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Condominio.Infrastructure.Repositories
{
    public class FacturaMesCasaRepository : GenericRepository<FacturaMesCasa>
    {

        public FacturaMesCasaRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<FacturaMesCasa>> GetAllAsync()
        {
            return await _dbSet.Include(x => x.House).Include(x => x.FacturaMes).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }


    }
}
