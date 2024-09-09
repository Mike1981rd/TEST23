using System;
using System.Collections.Generic;

namespace AuroraPOS.ModelsCentral;

public partial class User
{
    public long Id { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Pin { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<UserCompany> UserCompanies { get; } = new List<UserCompany>();
}
