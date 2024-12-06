using System;
using System.Collections.Generic;

namespace Zugether.Models;

public partial class Landlord
{
    public short landlord_id { get; set; }

    public string? name { get; set; }

    public string? phone { get; set; }

    public string? gender { get; set; }

    public byte[]? consent_photo { get; set; }

    public virtual ICollection<Room> Room { get; set; } = new List<Room>();
}
