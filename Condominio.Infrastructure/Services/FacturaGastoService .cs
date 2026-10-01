using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces.Repositories;
using Condominio.Domain.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Services
{
    public class FacturaGastoService : IFacturaGastoService
    {
        private readonly UpstashBlobService _blob;
        private readonly IFacturaGastoRepository _repo;

        public FacturaGastoService(UpstashBlobService blob, IFacturaGastoRepository repo)
        {
            _blob = blob;
            _repo = repo;
        }

        public async Task<int> SubirFacturaAsync(
            Stream stream,
            string nombre,
            string contentType,
            long tamano,
            int mes,
            int anio,
            string? comentario)
        {
            // 1. Subir a Upstash
            var key = await _blob.SubirAsync(stream, nombre, contentType);

            // 2. Guardar en MySQL
            var factura = new ImagenesFacturas
            {
                Mes = mes,
                Anio = anio,
                Comentario = comentario,
                RutaAlmacenamiento = key,
                NombreOriginal = nombre,
                TipoMime = contentType,
                TamanoBytes = tamano
            };

            return await _repo.AgregarAsync(factura);
        }

        public async Task<List<ImagenesFacturas>> ObtenerTodasAsync()
            => await _repo.ObtenerTodasAsync();


        public async Task<string> ObtenerUrlFacturaAsync(int facturaId)
        {
            var factura = await _repo.ObtenerPorIdAsync(facturaId);
            if (factura == null) throw new Exception("Factura no encontrada");

            return await _blob.ObtenerUrlLecturaAsync(factura.RutaAlmacenamiento);
        }

        public async Task EliminarFacturaAsync(int facturaId)
        {
            // 1. Buscar la factura en MySQL
            var factura = await _repo.ObtenerPorIdAsync(facturaId);
            if (factura == null) throw new Exception("Factura no encontrada");

            // 2. Borrar el archivo de Upstash
            await _blob.EliminarAsync(factura.RutaAlmacenamiento);

            // 3. Borrar el registro de MySQL
            await _repo.EliminarAsync(facturaId);
        }
    }
}
