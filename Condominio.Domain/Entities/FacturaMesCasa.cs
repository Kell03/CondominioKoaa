using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Condominio.Domain.Entities
{
    public class FacturaMesCasa: BaseEntity
    {

        public int FacturaMesId { get; set; }


        public int HouseId { get; set; }


        public decimal MontoTotal { get; set; }


        public string Estado { get; set; } = "Pendiente"; // 'En Revisión', 'Confirmada'

        public string? Referencia { get; set; }

        public string? MetodoPago { get; set; }

        public DateTime? FechaPago { get; set; }

        public string? Comentario { get; set; }

        public FacturaMes FacturaMes { get; set; }

        public Houses House { get; set; }


        [NotMapped]
        public Users? User { get; set; } // ✅ RELACIÓN CON USUARIO (DUEÑO DE LA CASA)

        public decimal? MontoBs { get; set; } 



        [NotMapped]
        public string NombreMes => ObtenerNombreMes(FacturaMes?.Mes ?? 0);

        // ✅ PROPIEDAD CALCULADA PARA AÑO (OPCIONAL)
        [NotMapped]
        public int Year => FacturaMes?.Year ?? 0;

        private string ObtenerNombreMes(int mes)
        {
            string[] meses = { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                               "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
            return mes >= 1 && mes <= 12 ? meses[mes - 1] : "Desconocido";
        }

    }
}
