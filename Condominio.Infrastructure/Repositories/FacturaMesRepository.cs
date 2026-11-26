using Condominio.Domain.DB;
using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces;
using Condominio.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using static Amazon.S3.Util.S3EventNotification;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Condominio.Infrastructure.Repositories
{
    public class FacturaMesRepository : GenericRepository<FacturaMes>
    {
        private readonly NotificacionRepository _notifRepo;
        private readonly INotificacionRealTimeService _notificacionService;
        private readonly PdfGenerator _pdfGenerator;
        private readonly BrevoEmailService _emailService;

        private readonly IConfiguration _config;
        public FacturaMesRepository(
            IDbContextFactory<AppDbContext> factory,
            NotificacionRepository notifRepo,
            PdfGenerator pdfGenerator,
            BrevoEmailService emailService,
            INotificacionRealTimeService notificacionService,
            IConfiguration config) : base(factory)
        {
            _notifRepo = notifRepo;
            _notificacionService = notificacionService;
            _pdfGenerator = pdfGenerator;
            _emailService = emailService;
            _config = config;
        }




        public async Task<FacturaMes?> GetMesByIdAsync(int id)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var query = await db.Set<FacturaMes>()
                .Where(x => x.Id == id)
                .Include(x => x.FacturaMesHijos)
                    .ThenInclude(x => x.CategoriaGasto)
                        .ThenInclude(x => x.Proveedor)
                .FirstOrDefaultAsync();

            return query;
        }

        public async Task<(List<FacturaMes> Items, int Total)> GetPagedAsync(
      int skip, int take, string filtro = null, string orderBy = null)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var query = db.Set<FacturaMes>()
                .Include(x => x.FacturaMesHijos)
                    .ThenInclude(x => x.CategoriaGasto)
                        .ThenInclude(x => x.Proveedor)
                .AsNoTracking()
                .AsQueryable();

            query = query.Where(x => x.Motivo == null || x.Motivo == "");

            //if (!string.IsNullOrWhiteSpace(filtro))
            //{
            //    // Extraemos el valor limpio
            //    string valorBusqueda = filtro;
            //    if (filtro.Contains("Contains(\""))
            //    {
            //        var inicio = filtro.IndexOf("Contains(\"") + 10;
            //        var fin = filtro.IndexOf("\")", inicio);
            //        if (fin > inicio)
            //        {
            //            valorBusqueda = filtro.Substring(inicio, fin - inicio);
            //        }
            //    }
            //
            //    var f = valorBusqueda.ToLower();
            //
            //    // ✅ BUSCAMOS LOS MESES QUE COINCIDEN
            //    var mesesCoincidentes = ObtenerMesesCoincidentes(f);
            //
            //    query = query.Where(x =>
            //        x.Year.ToString().Contains(f) ||
            //        (x.Motivo != null && x.Motivo.ToLower().Contains(f)) ||
            //        x.Mes.ToString().Contains(f) ||
            //        (mesesCoincidentes.Any() && mesesCoincidentes.Contains(x.Mes)) // 👈 La magia aquí
            //    );
            //}

            // ✅ Usar directamente la cadena de filtro generada por Radzen
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                // ✅ INTERCEPTAR EL FILTRO DE MES
                if (filtro.Contains("NombreMes"))
                {
                    string textoBusqueda = "";

                    // 1. Extraer de forma segura el texto que escribió el usuario (ignorando el "" vacío)
                    var partes = filtro.Split('"');
                    foreach (var parte in partes)
                    {
                        // Buscamos la parte que no esté vacía, no tenga la flecha "=>" y no sea parte de la sintaxis
                        if (!string.IsNullOrWhiteSpace(parte) &&
                            !parte.Contains("=>") &&
                            !parte.Contains("NombreMes") &&
                            !parte.Contains(".Contains") &&
                            !parte.Contains("??"))
                        {
                            textoBusqueda = parte.Trim().ToLower();
                            break; // Ya encontramos el mes (ej: "marzo")
                        }
                    }

                    // 2. Traducir el texto a número de mes
                    int? numeroMes = ObtenerNumeroMes(textoBusqueda);

                    if (numeroMes.HasValue)
                    {
                        // 3. Construir el filtro limpio para EF Core
                        // En lugar de hacer Replace (que falla por mayúsculas/minúsculas), 
                        // simplemente forzamos un filtro directo a la columna "Mes" con el número.
                        filtro = $"Mes == {numeroMes.Value}";
                    }
                    else
                    {
                        // Si el usuario escribió algo que no es un mes, forzamos un filtro falso
                        filtro = "1 == 2";
                    }
                }

                // ✅ Pasar el filtro (ya traducido o el original) a EF Core
                query = query.Where(filtro);
            }
            var total = await query.CountAsync();

            // ... (el resto del código de orden y paginación se queda igual)

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, total);
        }


        private int? ObtenerNumeroMes(string texto)
        {
            var meses = new Dictionary<string, int>
    {
        { "enero", 1 }, { "febrero", 2 }, { "marzo", 3 }, { "abril", 4 },
        { "mayo", 5 }, { "junio", 6 }, { "julio", 7 }, { "agosto", 8 },
        { "septiembre", 9 }, { "setiembre", 9 }, { "octubre", 10 }, { "noviembre", 11 }, { "diciembre", 12 }
    };

            // Buscar si el texto contiene alguna parte del nombre del mes
            foreach (var mes in meses)
            {
                if (mes.Key.Contains(texto) || texto.Contains(mes.Key))
                    return mes.Value;
            }

            return null;
        }
        private List<int> ObtenerMesesCoincidentes(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return new List<int>();

            var meses = new Dictionary<int, string>
    {
        { 1, "enero" }, { 2, "febrero" }, { 3, "marzo" }, { 4, "abril" },
        { 5, "mayo" }, { 6, "junio" }, { 7, "julio" }, { 8, "agosto" },
        { 9, "septiembre" }, { 10, "octubre" }, { 11, "noviembre" }, { 12, "diciembre" }
    };

            var textoLower = texto.ToLower();

            // Buscamos todos los meses que contengan el texto (ej: "o" -> [1, 2, 5, 6, 7, 8, 10, 11])
            var coincidentes = meses
                .Where(m => m.Value.Contains(textoLower))
                .Select(m => m.Key)
                .ToList();

            return coincidentes;
        }

        public override async Task<IEnumerable<FacturaMes>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<FacturaMes>()
                .Include(x => x.FacturaMesHijos).ThenInclude(x => x.CategoriaGasto).ThenInclude(x => x.Proveedor)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<FacturaMes>> GetAllForGastosAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Set<FacturaMes>()
                .Include(x => x.FacturaMesHijos).ThenInclude(x => x.CategoriaGasto).ThenInclude(x => x.Proveedor)
                .AsNoTracking()
                .Where(x => x.Motivo == null || x.Motivo == "")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task DeleteWithHijos(FacturaMes entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var factura = await db.FacturaMes
                    .Include(f => f.FacturaMesHijos)
                    .FirstOrDefaultAsync(f => f.Id == entity.Id);

                if (factura == null)
                    throw new Exception("Factura no encontrada");

                if (factura.FacturaMesHijos != null && factura.FacturaMesHijos.Any())
                {
                    db.FacturaMesHijo.RemoveRange(factura.FacturaMesHijos);
                }

                db.FacturaMes.Remove(factura);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error al eliminar: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SaveWithFacturaHijo(FacturaMes item, List<FacturaMesHijo> children)
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                // 1. Guardamos la factura para obtener el ID
                db.FacturaMes.Add(item);
                await db.SaveChangesAsync();

                var categoriasPorcentaje = db.CategoriasGasto.Where(x => x.Porcentaje == true).ToList();

                // 2. Calculamos la base (solo los gastos fijos)
                decimal montoBase = children
     .Where(x => x.IdCategoria == null || !categoriasPorcentaje.Select(x => x.Id).Contains(x.IdCategoria.Value))
     .Sum(c => (categoriasPorcentaje.FirstOrDefault(x => x.Id == c.IdCategoria)?.EsResta == true) ? -(decimal)c.Monto : (decimal)c.Monto);

                decimal totalFactura = montoBase;

                // 3. Calculamos los gastos de porcentaje
                foreach (var child in children.Where(x => x.IdCategoria != null && categoriasPorcentaje.Select(x => x.Id).Contains(x.IdCategoria.Value)))
                {
                    // Calculamos el monto del porcentaje sobre la base
                    var monto = categoriasPorcentaje.Where(x => x.Id == child.IdCategoria).Select(x => x.Monto).FirstOrDefault();
                    decimal montoCalculado = montoBase * ((decimal)monto / 100.0m);
                    // ✅ AGREGAR ESTA LÍNEA (Si la categoría es EsResta, el porcentaje se vuelve negativo)
                    if (categoriasPorcentaje.FirstOrDefault(x => x.Id == child.IdCategoria)?.EsResta == true) montoCalculado = -montoCalculado;



                    // Asignamos el monto al hijo
                    child.Monto = (double)montoCalculado;

                    // Sumamos al total general
                    totalFactura += montoCalculado;
                }

                // 4. Asignamos el ID a todos los hijos
                foreach (var child in children)
                {
                    child.FacturaMesId = item.Id;
                }

                // 5. Insertamos los hijos
                db.FacturaMesHijo.AddRange(children);
                await db.SaveChangesAsync();

                // 6. Actualizamos el monto total de la factura maestra
                item.MontoTotal = (double)totalFactura;
                db.FacturaMes.Update(item);
                await db.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error al guardar: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveListWithFacturaHijo(FacturaMes itemOriginal, List<FacturaMesHijo> childrenOriginal, List<int> houseIds)
        {
            var listIdUsers = new List<int>();
            int guardadasExitosamente = 0;

            foreach (var idcasa in houseIds)
            {
                await using var db = await _factory.CreateDbContextAsync();
                await using var transaction = await db.Database.BeginTransactionAsync();

                try
                {
                    var item = new FacturaMes
                    {
                        Mes = itemOriginal.Mes,
                        Year = itemOriginal.Year,
                        Motivo = itemOriginal.Motivo,
                        IsActive = itemOriginal.IsActive,
                        EstadoPago = itemOriginal.EstadoPago
                    };

                    db.FacturaMes.Add(item);
                    await db.SaveChangesAsync();

                    var children = new List<FacturaMesHijo>();
                    foreach (var child in childrenOriginal)
                    {
                        children.Add(new FacturaMesHijo
                        {
                            FacturaMesId = item.Id,
                            Motivo = child.Motivo,
                            Monto = child.Monto,
                            IdCategoria = child.IdCategoria,
                            IdProveedor = child.IdProveedor,
                            Pagado = false
                        });
                    }

                    db.FacturaMesHijo.AddRange(children);
                    await db.SaveChangesAsync();

                    item.MontoTotal = children.Sum(c => c.Monto);
                    db.FacturaMes.Update(item);
                    await db.SaveChangesAsync();

                    var facturaCasa = new FacturaMesCasa
                    {
                        FacturaMesId = item.Id,
                        HouseId = idcasa,
                        MontoTotal = (decimal)item.MontoTotal,
                        SaldoPendiente = (decimal)item.MontoTotal,
                        MontoPagado = 0,
                        Estado = "Pendiente"
                    };

                    db.FacturaMesCasa.Add(facturaCasa);

                    var usuario = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.HouseId == idcasa);
                    if (usuario != null)
                    {
                        await db.Notificaciones.AddAsync(new Notificaciones
                        {
                            UserId = usuario.Id,
                            Titulo = "Nueva factura",
                            Mensaje = $"Factura de {item.Motivo} - {item.Year} disponible por ${item.MontoTotal:N2}",
                            Tipo = "Factura",
                            ReferenciaId = item.Id
                        });
                        listIdUsers.Add(usuario.Id);
                    }

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    guardadasExitosamente++;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error al guardar factura para la casa {idcasa}: {ex.Message}");
                }
            }

            if (listIdUsers.Any())
            {
                await _notificacionService.EnviarNotificacionMultipleAsync(listIdUsers);
            }

            return guardadasExitosamente > 0;
        }

        public async Task DistribuirFacturaEntreCasas(FacturaMes entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await using var db = await _factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var factura = await db.FacturaMes.Include(x => x.FacturaMesHijos).FirstOrDefaultAsync(f => f.Id == entity.Id);

                if (factura == null)
                    throw new Exception("Factura no encontrada");

                if (factura.MontoTotal == 0)
                    throw new Exception("El monto total de la factura es 0");

                var casasUsuarios = await db.Houses
          .Where(h => h.IsActive)
          .Select(h => new
          {
              HouseId = h.Id,
              Email = db.Users.Where(u => u.HouseId == h.Id).Select(u => u.Email).FirstOrDefault()
          })
          .ToListAsync();


                int totalCasas = casasUsuarios.Count;

                if (totalCasas == 0)
                    throw new Exception("No hay casas activas");



                decimal montoPorCasa = decimal.Round((decimal)factura.MontoTotal / totalCasas, 2);

                var facturasCasas = new List<FacturaMesCasa>(totalCasas);
                var listIdUsers = new List<int>();

                foreach (var houseId in casasUsuarios.Select(x => x.HouseId).ToList())
                {
                    facturasCasas.Add(new FacturaMesCasa
                    {
                        FacturaMesId = factura.Id,
                        HouseId = houseId,
                        MontoTotal = montoPorCasa,
                        SaldoPendiente = montoPorCasa,
                        CorreoEnviado = false,
                        Estado = "Pendiente"
                    });

                    var usuario = await db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.HouseId == houseId);

                    if (usuario != null)
                    {
                        await db.Notificaciones.AddAsync(new Notificaciones
                        {
                            UserId = usuario.Id,
                            Titulo = "Nueva factura",
                            Mensaje = $"Factura de {factura.NombreMes} {factura.Year} disponible por ${montoPorCasa:N2}",
                            Tipo = "Factura",
                            ReferenciaId = factura.Id
                        });
                        listIdUsers.Add(usuario.Id);
                    }
                }

                await db.FacturaMesCasa.AddRangeAsync(facturasCasas);

                factura.Enviado = true;
                db.FacturaMes.Update(factura);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                if (listIdUsers.Any())
                {
                    await _notificacionService.EnviarNotificacionMultipleAsync(listIdUsers);
                }




                Console.WriteLine($"✅ Factura {factura.Mes}/{factura.Year} distribuida entre {totalCasas} casas");
                Console.WriteLine($"📊 Cada casa paga: ${montoPorCasa}");

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Error al distribuir factura: {ex.Message}");
                throw;
            }
        }


        public async Task EnviarCorreosFacturaAsync(int facturaId)
        {
            await using var db = await _factory.CreateDbContextAsync();

            ConfiguracionCondominio configuracion = await db.ConfiguracionCondominio.FirstOrDefaultAsync();

            if (configuracion == null || !configuracion.EnvioCorreo)
            {
                Console.WriteLine("⚠️ El envío de correos está desactivado.");
                return;
            }

            var factura = await db.FacturaMes
                .Include(f => f.FacturaMesHijos).ThenInclude(fh => fh.CategoriaGasto)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null) throw new Exception("Factura no encontrada");

            var casasPendientes = await db.FacturaMesCasa
                .Where(f => f.FacturaMesId == facturaId && (f.CorreoEnviado == false || f.CorreoEnviado == null))
                .ToListAsync();

            if (!casasPendientes.Any())
            {
                Console.WriteLine("⚠️ No hay correos pendientes por enviar en esta factura.");
                return;
            }

            var houseIdsPendientes = casasPendientes.Select(c => c.HouseId).Distinct().ToList();

            var usuariosPorCasa = await db.Users
                .AsNoTracking()
                .Where(u => u.HouseId != null && houseIdsPendientes.Contains(u.HouseId.Value))
                .GroupBy(u => u.HouseId!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.First().Email);

            // Casas sin correo válido: las marcamos de una vez para no trabar pendientes
            var casasSinCorreo = casasPendientes
                .Where(c => !usuariosPorCasa.TryGetValue(c.HouseId, out var email) || string.IsNullOrWhiteSpace(email))
                .ToList();

            foreach (var casaSin in casasSinCorreo)
            {
                casaSin.CorreoEnviado = true;
                casaSin.FechaEnvioCorreo = DateTime.Now;
            }

            var casasConCorreo = casasPendientes.Except(casasSinCorreo).ToList();

            if (!casasConCorreo.Any())
            {
                await db.SaveChangesAsync();
                Console.WriteLine("⚠️ No había correos válidos en las casas pendientes.");
                return;
            }

            // Correo principal en "To" (puedes usar también _emailService.ObtenerRemitente() si lo agregaste)
            string correoPrincipalTo = "notificacionesensenada@solunarinf.com";

            var listaBcc = casasConCorreo
                .Select(c => usuariosPorCasa[c.HouseId]!.Trim())
                .Where(email => !email.Equals(correoPrincipalTo, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            decimal montoParaPdf = casasConCorreo.First().MontoTotal;
            var totalCasas = await db.Houses.CountAsync();
            byte[] pdfBytes = _pdfGenerator.GenerarAviso(factura, totalCasas);

            string nombreArchivo = $"Aviso_{factura.NombreMes}_{factura.Year}.pdf";
            string asunto = $"Aviso de Cobro - {factura.NombreMesCompleto}";
            string mensaje = $@"
<h3>Estimado propietario,</h3>
<p>Adjunto encontrará el aviso de cobro correspondiente al mes de <strong>{factura.NombreMesCompleto}</strong>.</p>
<p>El monto a cancelar por su inmueble es de <strong>${montoParaPdf:N2}</strong>.</p>
<br/>
<p>Saludos cordiales,<br/>Administración La Ensenada</p>";

            // Brevo permite máximo 99 destinatarios por envío (1 en To + 98 en Bcc)
            const int maxBccPorEnvio = 98;
            var lotesBcc = listaBcc.Chunk(maxBccPorEnvio).ToList();
            var fechaEnvio = DateTime.Now;

            foreach (var lote in lotesBcc)
            {
                var bccLoteList = lote.ToList();

                await _emailService.EnviarCorreoConAdjuntoAsync(
                    destinatarioPrincipal: correoPrincipalTo,
                    asunto: asunto,
                    mensajeHtml: mensaje,
                    pdfBytes: pdfBytes,
                    nombreArchivo: nombreArchivo,
                    correosOcultosBcc: bccLoteList
                );

                // Marcamos como enviadas solo las casas de este lote que ya salió bien
                var casasDelLote = casasConCorreo
                    .Where(c => bccLoteList.Contains(usuariosPorCasa[c.HouseId]!.Trim(), StringComparer.OrdinalIgnoreCase))
                    .ToList();

                foreach (var casaFactura in casasDelLote)
                {
                    casaFactura.CorreoEnviado = true;
                    casaFactura.FechaEnvioCorreo = fechaEnvio;
                }

                await db.SaveChangesAsync();
                Console.WriteLine($"📧 Lote enviado en copia oculta a {bccLoteList.Count} destinatarios.");

                if (lotesBcc.Count > 1)
                {
                    await Task.Delay(1000);
                }
            }

            // Caso borde: si el propio correoPrincipalTo estaba entre las casas con correo, también lo marcamos
            var casasCorreoPrincipal = casasConCorreo
                .Where(c => usuariosPorCasa[c.HouseId]!.Trim().Equals(correoPrincipalTo, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (casasCorreoPrincipal.Any())
            {
                foreach (var casa in casasCorreoPrincipal)
                {
                    casa.CorreoEnviado = true;
                    casa.FechaEnvioCorreo = fechaEnvio;
                }
                await db.SaveChangesAsync();
            }

            var quedanPendientes = await db.FacturaMesCasa
                .AnyAsync(f => f.FacturaMesId == facturaId && (f.CorreoEnviado == false || f.CorreoEnviado == null));

            if (!quedanPendientes)
            {
                factura.CorreosEnviados = true;
                db.FacturaMes.Update(factura);
                await db.SaveChangesAsync();
                Console.WriteLine("✅ Todos los correos enviados. Factura actualizada.");
            }

            Console.WriteLine("✅ Proceso de envío de correos finalizado.");
        }
    }
}