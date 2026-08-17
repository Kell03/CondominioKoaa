using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Condominio.Infrastructure.Repositories
{
    public class FacturaMesHijoRepository : GenericRepository<FacturaMesHijo>
    {
        public FacturaMesHijoRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<FacturaMesHijo>> GetAllAsync()
        {
            return await _dbSet.Include(x => x.facturaMes).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }



        public override async Task AddAsync(FacturaMesHijo entity)
        {

            // 1. Agregar el detalle (hijo)
            await _dbSet.AddAsync(entity);

            // 2. Buscar la factura padre
            FacturaMes factura = await _context.Set<FacturaMes>()
                .FirstOrDefaultAsync(f => f.Id == entity.FacturaMesId);

            if (factura != null)
            {
                // 3. Actualizar el total de la factura padre
                factura.MontoTotal += entity.Monto;

                // 4. Marcar la factura como modificada (usando Set<FacturaMes>())
                _context.Set<FacturaMes>().Update(factura);
            }

            // 5. Guardar TODOS los cambios (el hijo + la actualización del padre)
            await _context.SaveChangesAsync();

        }


        public  async Task DeleteFacturaHijo(FacturaMesHijo entity)
        {
            _dbSet.Remove(entity);

            FacturaMes factura = await _context.Set<FacturaMes>()
                .FirstOrDefaultAsync(f => f.Id == entity.FacturaMesId);

            if (factura != null)
            {
                factura.MontoTotal =- entity.Monto;

                _context.Set<FacturaMes>().Update(factura);
            }

            await _context.SaveChangesAsync();
        }
    }
}
