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

        public HouseRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<Houses>> GetAllAsync()
        {
            return await _dbSet.OrderByDescending(x => x.CreatedAt).ToListAsync();
        }

        public async Task<int> CreateDefaultHousesAsync(int cantidad)
        {
            if (cantidad <= 0)
                return 0;

            // Evita duplicados: solo genera las que no existan ya por Number
            var existingNumbers = await _dbSet
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

            await _dbSet.AddRangeAsync(newHouses);
            await _context.SaveChangesAsync();

            return newHouses.Count;
        }

    }
}
