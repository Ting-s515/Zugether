using System;
using System.Collections.Generic;

namespace Zugether.Models;

public partial class Favor_List
{
    public short favor_list_id { get; set; }

    public short member_id { get; set; }

    public virtual ICollection<Favorites> Favorites { get; set; } = new List<Favorites>();

    public virtual Member member { get; set; } = null!;
}
