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
    public class ProveedorRepository : GenericRepository<Proveedor>
    {
        public ProveedorRepository(AppDbContext context) : base(context) { }

    }
}
