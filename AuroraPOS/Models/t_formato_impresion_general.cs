using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuroraPOS.Models
{
	public class t_formato_impresion_general
	{
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int f_id { get; set; }
        public string? f_descripcion { get; set; }
        public int? f_limite { get; set; }
        public string? f_cometario { get; set; }
        public int? f_orden { get; set; }
        public string? f_archivo { get; set; }
        public string? f_nombre_archivo { get; set; }
        public int? f_secuencia_no_control { get; set; }
        public int? f_formato_impresion { get; set; }
        public bool? f_impresion_por_pantalla { get; set; }
        public string? f_impresora { get; set; }
        public string? f_archivo1 { get; set; }
    }
}

