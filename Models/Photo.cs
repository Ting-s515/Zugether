using System;
using System.Collections.Generic;

namespace Zugether.Models;

public partial class Photo
{
    public int photo_id { get; set; }

    public byte[]? room_photo { get; set; }

    public short? album_id { get; set; }

    public string? photo_type { get; set; }

    public virtual Album? album { get; set; }
}
