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
    public class ConfiguracionCondominioRepository : GenericRepository<ConfiguracionCondominio>
    {
        public ConfiguracionCondominioRepository(IDbContextFactory<AppDbContext> factory) : base(factory) { }

        public async Task<ConfiguracionCondominio?> ObtenerAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.Set<ConfiguracionCondominio>()
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }
    }
}
