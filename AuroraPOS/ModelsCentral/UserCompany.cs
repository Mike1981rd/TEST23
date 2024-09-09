using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AuroraPOS.ModelsCentral;

public partial class UserCompany
{   
    public long Id { get; set; }

    public long UserId { get; set; }

    public long CompanyId { get; set; }

    public virtual Company Company { get; set; } 

    public virtual User User { get; set; }
}
