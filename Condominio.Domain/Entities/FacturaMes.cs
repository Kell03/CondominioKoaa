using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Condominio.Domain.Entities
{
    public class FacturaMes:BaseEntity
    {

       public int Mes {  get; set; }
       public int Year {  get; set; }
       public bool IsActive { get; set; } = false;
       public bool Enviado { get; set; } = false;
       public double MontoTotal { get; set; } = 0;

        public decimal? MontoRecaudado { get; set; } = 0;// Nullable


        public ICollection<FacturaMesHijo> FacturaMesHijos { get; set; } = new List<FacturaMesHijo>();

        [NotMapped]
        public string NombreMes => ObtenerNombreMes(this.Mes);

        // ✅ PROPIEDAD CALCULADA PARA AÑO (OPCIONAL)
     

        private string ObtenerNombreMes(int mes)
        {
            string[] meses = { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                               "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
            return mes >= 1 && mes <= 12 ? meses[mes - 1] : "Desconocido";
        }

    }
}
