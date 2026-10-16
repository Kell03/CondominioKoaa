using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Amazon.S3.Util.S3EventNotification;

namespace Condominio.Infrastructure.Repositories
{
    public class GastosRepository : GenericRepository<Gasto>
    {

        private readonly MovimientosRepository _movimientoRepo;   // ← NUEVO

        public GastosRepository(AppDbContext context) : base(context) { }

        public GastosRepository(
    AppDbContext context,
    MovimientosRepository movimientoRepo) : base(context)  // ← NUEVO
        {
            _movimientoRepo = movimientoRepo;   // ← NUEVO
        }


        public override async Task<IEnumerable<Gasto>> GetAllAsync()
        {
            _context.ChangeTracker.Clear();

            return await _dbSet.Include(x => x.FacturaMesHijo).Include(x => x.CuotaEspecial).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }

        public override async Task AddAsync(Gasto entity)
        {


            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {

                if(entity.CuotaEspecialId > 0)
                {
                    var cuota = await _context.CuotaEspecial.FirstOrDefaultAsync(x => x.Id == entity.CuotaEspecialId);
                    cuota.Pagado = true;

                }else if(entity.FacturaMesHijoId > 0)
                {
                    var factura = await _context.FacturaMesHijo.FirstOrDefaultAsync(x => x.Id == entity.FacturaMesHijoId);
                    factura.Pagado = true;
                }


                // 2. Agregar el gasto
                await _dbSet.AddAsync(entity);

                // ✅ 3. GUARDAR PRIMERO para que el gasto obtenga Id
                await _context.SaveChangesAsync();

                // ✅ 4. AHORA el gasto ya tiene Id → guardar el movimiento
                await _movimientoRepo.SaveMovimiento(
                    monto: entity.Monto,
                    montoBs: entity.MontoBs,
                    tasa: entity.Tasa,
                    gasto: entity            // ← ahora entity.Id tiene valor
                );

                // ✅ 5. Guardar el movimiento
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            { // ❌ REVERTIR TRANSACCIÓN (ROLLBACK)
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Error en AddAsync: {ex.Message}");
                throw;
            }
        }


    }
}
