using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Repositories
{
    public class ConfiguracionMonedaRepository : GenericRepository<ConfiguracionMoneda>
    {
        public ConfiguracionMonedaRepository(AppDbContext context) : base(context) { }



    }
}
