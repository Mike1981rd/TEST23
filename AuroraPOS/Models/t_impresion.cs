using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuroraPOS.Models
{
    public class t_impresion 
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_reporte { get; set; }
        public string nombre { get; set; }
        public string extencion { get; set; }
        public string cuerpo { get; set; }
        public string impresora { get; set; }
        public string id_estacion { get; set; }
        public int status_impresion { get; set; }
        public int numero_copias { get; set; }        
        public int IDSucursal { get; set; }
        public bool IsReprint { get; set; }
    }
}

