using System;
using System.Collections.Generic;

namespace Zugether.Models;

public partial class Admin
{
    public int admin_id { get; set; }

    public string userName { get; set; } = null!;

    public string password { get; set; } = null!;
}
