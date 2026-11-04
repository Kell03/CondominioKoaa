using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Repositories
{
    public class FacturaGastoRepository : IFacturaGastoRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public FacturaGastoRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<int> AgregarAsync(ImagenesFacturas factura)
        {
            await using var db = await _factory.CreateDbContextAsync();
            db.ImagenesFacturas.Add(factura);
            await db.SaveChangesAsync();
            return factura.Id;
        }

        public async Task<List<ImagenesFacturas>> ObtenerTodasAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.ImagenesFacturas
                .AsNoTracking()
                .OrderByDescending(f => f.FechaSubida)
                .ToListAsync();
        }

        public async Task<ImagenesFacturas?> ObtenerPorIdAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.ImagenesFacturas.FindAsync(id);
        }

        public async Task EliminarAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var factura = await db.ImagenesFacturas.FindAsync(id);
            if (factura != null)
            {
                db.ImagenesFacturas.Remove(factura);
                await db.SaveChangesAsync();
            }
        }
    }
}