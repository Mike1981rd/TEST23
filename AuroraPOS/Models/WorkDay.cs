using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AuroraPOS.Models
{
    public class WorkDay : BaseEntity
    {               
        public DateTime Day { get; set; }
        public int IDSucursal { get; set; }
        
    }
}

