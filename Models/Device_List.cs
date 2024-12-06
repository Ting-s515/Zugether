using System;
using System.Collections.Generic;

namespace Zugether.Models;

public partial class Device_List
{
    public short device_list_id { get; set; }

    public bool? bed { get; set; }

    public bool? wardrobe { get; set; }

    public bool? room_table { get; set; }

    public bool? chair { get; set; }

    public bool? kitchen { get; set; }

    public bool? private_washer { get; set; }

    public bool? extinguisher { get; set; }

    public bool? tv { get; set; }

    public bool? wifi { get; set; }

    public bool? monitor { get; set; }

    public bool? balcony { get; set; }

    public bool? elevator { get; set; }

    public bool? public_washer { get; set; }

    public bool? trash_cart { get; set; }

    public bool? keep_pet { get; set; }

    public bool? smoking { get; set; }

    public virtual ICollection<Room> Room { get; set; } = new List<Room>();
}
