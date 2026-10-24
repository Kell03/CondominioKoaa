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
    public class PaymentRepository : GenericRepository<Payments>
    {

        public PaymentRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<Payments>> GetAllAsync()
        {
            return await _dbSet.Include(x => x.FacturaMesCasa).Include(x => x.CuotaEspecialCasa).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }


        public  async Task<List<Payments>> GetAllPaymentFacturaAsync()
        {
            return await _dbSet.Where(x => x.FacturaMesCasaId > 0).Include(x => x.FacturaMesCasa).ThenInclude(x => x.FacturaMes).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }
    }
}
