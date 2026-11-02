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
            _context.ChangeTracker.Clear();

            return await _dbSet.Include(u => u.House).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }


        public  async Task UpdateAsync(Users entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            try
            {
                // ✅ 1. Buscar el usuario actual en la BD
                var existingUser = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == entity.Id);

                if (existingUser == null)
                    throw new InvalidOperationException($"El usuario con Id {entity.Id} no existe");

                // ✅ 2. Verificar que el email no esté ocupado por otro usuario
                var emailDuplicado = await _dbSet
                    .AnyAsync(u => u.Email == entity.Email && u.Id != entity.Id);

                if (emailDuplicado)
                    throw new InvalidOperationException($"El email {entity.Email} ya está registrado por otro usuario");

                // ✅ 3. Hashear la contraseña SOLO si cambió
                //    - Si viene vacía o nula → conservar el hash anterior
                //    - Si es igual al hash almacenado → ya está hasheada, no rehashear
                //    - Si es texto plano distinto → hashear
                if (string.IsNullOrWhiteSpace(entity.PasswordHash))
                {
                    entity.PasswordHash = existingUser.PasswordHash;
                }
                else if (!entity.PasswordHash.StartsWith("$2") || entity.PasswordHash != existingUser.PasswordHash)
                {
                    // BCrypt siempre empieza con "$2a$", "$2b$" o "$2y$"
                    // Si no es un hash BCrypt válido, asumimos texto plano y hasheamos
                    if (!EsHashBCrypt(entity.PasswordHash))
                    {
                        entity.PasswordHash = HashPassword(entity.PasswordHash);
                    }
                }

                // ✅ 4. Actualizar
                _dbSet.Update(entity);
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
                && valor.Length >= 55;   // longitud típica de un hash BCrypt
        }

        public async Task<IEnumerable<Users>> GetAllByUserAsync(int id)
        {
            return await _dbSet.Where(x => x.Id == id).Include(u => u.House).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }


        public override async Task AddAsync(Users entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            try
            {
                // ✅ 1. Verificar que el email no exista
                var existingUser = await _dbSet
                    .FirstOrDefaultAsync(u => u.Email == entity.Email);

                if (existingUser != null)
                    throw new InvalidOperationException($"El email {entity.Email} ya está registrado");

                // ✅ 2. Hashear la contraseña antes de guardar
                if (!string.IsNullOrEmpty(entity.PasswordHash))
                {
                    entity.PasswordHash = HashPassword(entity.PasswordHash);
                }

                // ✅ 3. Agregar a la base de datos
                await _dbSet.AddAsync(entity);
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
