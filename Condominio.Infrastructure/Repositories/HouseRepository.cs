using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Condominio.Infrastructure.Repositories
{
    public class HouseRepository : GenericRepository<Houses>
    {
        public HouseRepository(IDbContextFactory<AppDbContext> factory) : base(factory) { }

        public override async Task<IEnumerable<Houses>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<Houses>()
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> CreateDefaultHousesAsync(int cantidad)
        {
            if (cantidad <= 0)
                return 0;

            await using var db = await _factory.CreateDbContextAsync();

            var existingNumbers = await db.Set<Houses>()
                .Select(h => h.Number)
                .ToListAsync();

            var newHouses = Enumerable.Range(1, cantidad)
                .Select(i => i.ToString())
                .Where(number => !existingNumbers.Contains(number))
                .Select(number => new Houses
                {
                    Number = number,
                    Street = null,
                    AddressComplement = null,
                    IsActive = true
                })
                .ToList();

            if (newHouses.Count == 0)
                return 0;

            db.Set<Houses>().AddRange(newHouses);
            await db.SaveChangesAsync();

            return newHouses.Count;
        }
    }
}