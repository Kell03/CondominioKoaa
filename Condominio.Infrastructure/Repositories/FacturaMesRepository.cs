using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Condominio.Infrastructure.Repositories
{
    public class FacturaMesRepository : GenericRepository<FacturaMes>
    {

        public FacturaMesRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<FacturaMes>> GetAllAsync()
        {
            return await _dbSet.OrderByDescending(x => x.CreatedAt).ToListAsync();
        }
    }
}
