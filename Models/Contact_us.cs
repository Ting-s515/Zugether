using System;
using System.Collections.Generic;

namespace Zugether.Models;

public partial class Contact_us
{
    public short contact_id { get; set; }

    public string name { get; set; } = null!;

    public string title { get; set; } = null!;

    public string email { get; set; } = null!;

    public string message { get; set; } = null!;

    public string? phone { get; set; }

    public DateOnly? create_at { get; set; }
}
