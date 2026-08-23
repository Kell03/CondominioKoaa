using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
namespace Condominio.Infrastructure.Repositories
{
    public class UserRepository: GenericRepository<Users>
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<Users>> GetAllAsync()
        {
            return await _dbSet.Include(u => u.House).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }



        // ✅ LOGIN CON VERIFICACIÓN DE CREDENCIALES
        public async Task<Users> LoginAsync(string email, string password)
        {
            var user = await _dbSet
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive == true);

            if (user == null)
                return null;

            // ✅ Verificar password (hash)
            if (!VerifyPassword(password, user.PasswordHash))
                return null;

            return user;
        }

        // ✅ VERIFICAR PASSWORD CON HASH
        private bool VerifyPassword(string password, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }

        // ✅ OBTENER CLAIMS DEL USUARIO
        public List<Claim> GetUserClaims(Users user)
        {
            return new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("IsActive", user.IsActive.ToString())
        };
        }


    }
}
