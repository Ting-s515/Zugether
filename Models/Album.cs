using System;
using System.Collections.Generic;

namespace Zugether.Models;

public partial class Album
{
    public short album_id { get; set; }

    public virtual ICollection<Photo> Photo { get; set; } = new List<Photo>();

    public virtual ICollection<Room> Room { get; set; } = new List<Room>();
}
