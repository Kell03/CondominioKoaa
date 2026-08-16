using System;
using System.Collections.Generic;
using System.Text;
using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Condominio.Infrastructure.Repositories
{
    public class UserRepository: GenericRepository<Users>
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<Users>> GetAllAsync()
        {
            return await _dbSet.Include(u => u.House).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }



    }
}
