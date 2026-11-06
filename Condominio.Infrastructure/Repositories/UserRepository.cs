using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Condominio.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<Users>
    {
        public UserRepository(IDbContextFactory<AppDbContext> factory) : base(factory) { }

        public override async Task<IEnumerable<Users>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<Users>()
                .Include(u => u.House)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(Users entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await using var db = await _factory.CreateDbContextAsync();

            try
            {
                var existingUser = await db.Set<Users>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == entity.Id);

                if (existingUser == null)
                    throw new InvalidOperationException($"El usuario con Id {entity.Id} no existe");

                var emailDuplicado = await db.Set<Users>()
                    .AnyAsync(u => u.Email == entity.Email && u.Id != entity.Id);

                if (emailDuplicado)
                    throw new InvalidOperationException($"El email {entity.Email} ya está registrado por otro usuario");

                if (string.IsNullOrWhiteSpace(entity.PasswordHash))
                {
                    entity.PasswordHash = existingUser.PasswordHash;
                }
                else if (!entity.PasswordHash.StartsWith("$2") || entity.PasswordHash != existingUser.PasswordHash)
                {
                    if (!EsHashBCrypt(entity.PasswordHash))
                    {
                        entity.PasswordHash = HashPassword(entity.PasswordHash);
                    }
                }

                db.Set<Users>().Update(entity);
                await db.SaveChangesAsync();  // ← FALTABA
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al actualizar usuario: {ex.Message}");
                throw;
            }
        }

        private bool EsHashBCrypt(string valor)
        {
            return !string.IsNullOrWhiteSpace(valor)
                && valor.StartsWith("$2")
                && valor.Length >= 55;
        }

        public async Task<IEnumerable<Users>> GetAllByUserAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<Users>()
                .Where(x => x.Id == id)
                .Include(u => u.House)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
        public async Task<Users> GetUserHouseAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<Users>()
                .Where(x => x.Id == id)
                .Include(u => u.House)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public override async Task AddAsync(Users entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await using var db = await _factory.CreateDbContextAsync();

            try
            {
                var existingUser = await db.Set<Users>()
                    .FirstOrDefaultAsync(u => u.Email == entity.Email);

                if (existingUser != null)
                    throw new InvalidOperationException($"El email {entity.Email} ya está registrado");

                if (!string.IsNullOrEmpty(entity.PasswordHash))
                {
                    entity.PasswordHash = HashPassword(entity.PasswordHash);
                }

                db.Set<Users>().Add(entity);
                await db.SaveChangesAsync();  // ← FALTABA
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al agregar usuario: {ex.Message}");
                throw;
            }
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public async Task<Users?> LoginAsync(string email, string password)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var user = await db.Set<Users>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive == true);

            if (user == null)
                return null;

            if (!VerifyPassword(password, user.PasswordHash))
                return null;

            return user;
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }

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