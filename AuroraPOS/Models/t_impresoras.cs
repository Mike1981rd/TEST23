using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuroraPOS.Models
{
	public class t_impresoras
	{
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int f_id { get; set; }
        public string? f_id_estacion { get; set; }
        public string? f_impresora { get; set; }
        
    }
}

