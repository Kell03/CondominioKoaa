using Condominio.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Interfaces.Repositories
{
    public interface IFacturaGastoRepository
    {
        Task<int> AgregarAsync(ImagenesFacturas factura);
        Task<List<ImagenesFacturas>> ObtenerTodasAsync();
        Task<ImagenesFacturas?> ObtenerPorIdAsync(int id);
        Task EliminarAsync(int id);
    }
}
