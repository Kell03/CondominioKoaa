using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Entities
{
    public class MesModel
    {
        public int Value { get; set; }
        public string? Name { get; set; }

        // 📌 Método estático que devuelve todos los meses
        public static List<MesModel> GetMeses()
        {
            return new List<MesModel>
        {
            new MesModel { Value = 1, Name = "Enero" },
            new MesModel { Value = 2, Name = "Febrero" },
            new MesModel { Value = 3, Name = "Marzo" },
            new MesModel { Value = 4, Name = "Abril" },
            new MesModel { Value = 5, Name = "Mayo" },
            new MesModel { Value = 6, Name = "Junio" },
            new MesModel { Value = 7, Name = "Julio" },
            new MesModel { Value = 8, Name = "Agosto" },
            new MesModel { Value = 9, Name = "Septiembre" },
            new MesModel { Value = 10, Name = "Octubre" },
            new MesModel { Value = 11, Name = "Noviembre" },
            new MesModel { Value = 12, Name = "Diciembre" }
        };
        }
    }
}
