using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Repositories
{
    public class FacturaGastoRepository : IFacturaGastoRepository
    {
        private readonly AppDbContext _context;
        public FacturaGastoRepository(AppDbContext context) => _context = context;

        public async Task<int> AgregarAsync(ImagenesFacturas factura)
        {
            await _context.ImagenesFacturas.AddAsync(factura);
            await _context.SaveChangesAsync();
            return factura.Id;
        }

        public async Task<List<ImagenesFacturas>> ObtenerTodasAsync()
            => await _context.ImagenesFacturas.OrderByDescending(f => f.FechaSubida).ToListAsync();

        public async Task<ImagenesFacturas?> ObtenerPorIdAsync(int id)
            => await _context.ImagenesFacturas.FindAsync(id);

        public async Task EliminarAsync(int id)
        {
            var factura = await _context.ImagenesFacturas.FindAsync(id);
            if (factura != null)
            {
                _context.ImagenesFacturas.Remove(factura);
                await _context.SaveChangesAsync();
            }
        }


    }
}
