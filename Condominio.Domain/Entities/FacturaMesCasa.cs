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


        public string Estado { get; set; } = "Pendiente"; // 'Pendiente', 'Parcial', 'Pagada', 'Vencida', 'Anulada'

        public string? Referencia { get; set; }

        public string? MetodoPago { get; set; }

        public DateTime? FechaPago { get; set; }

        public string? Comentario { get; set; }

        public FacturaMes FacturaMes { get; set; }

        public Houses House { get; set; }

    }
}
