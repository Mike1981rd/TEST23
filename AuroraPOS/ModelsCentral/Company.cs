using System;
using System.Collections.Generic;

namespace AuroraPOS.ModelsCentral;

public partial class Company
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string Database { get; set; } = null!;

    public virtual ICollection<UserCompany> UserCompanies { get; } = new List<UserCompany>();
}
