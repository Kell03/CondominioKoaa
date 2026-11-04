using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Repositories
{
    public class PaymentRepository : GenericRepository<Payments>
    {
        public PaymentRepository(IDbContextFactory<AppDbContext> factory) : base(factory) { }

        public override async Task<IEnumerable<Payments>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<Payments>()
                .Include(x => x.FacturaMesCasa)
                .Include(x => x.CuotaEspecialCasa)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Payments>> GetAllPaymentFacturaAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<Payments>()
                .Where(x => x.FacturaMesCasaId > 0)
                .Include(x => x.FacturaMesCasa).ThenInclude(x => x.FacturaMes)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Payments>> GetAllPaymentCuotasAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<Payments>()
                .Where(x => x.CuotaEspecialCasaId > 0)
                .Include(x => x.CuotaEspecialCasa).ThenInclude(x => x.CuotaEspecial)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
    }
}