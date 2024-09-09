using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuroraPOS.Models
{
	public class logs
	{
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_log { get; set; }
        public string ubicacion { get; set; }
        public string descripcion { get; set; }
        public DateTime fecha { get; set; }        
    }
}

