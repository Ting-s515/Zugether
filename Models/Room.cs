using System;
using System.Collections.Generic;

namespace Zugether.Models;

public partial class Room
{
    public short room_id { get; set; }

    public string room_title { get; set; } = null!;

    public string address { get; set; } = null!;

    public short rent { get; set; }

    public short? management_fee { get; set; }

    public byte floor { get; set; }

    public byte room_size { get; set; }

    public string room_type { get; set; } = null!;

    public string? bed_type { get; set; }

    public DateOnly? post_date { get; set; }

    public byte roommate_num { get; set; }

    public string? roommate_description { get; set; }

    public short? album_id { get; set; }

    public short? device_list_id { get; set; }

    public short? member_id { get; set; }

    public short? landlord_id { get; set; }

    public bool? isEnabled { get; set; }

    public short deposit { get; set; }

    public string lease_type { get; set; } = null!;

    public string pay_type { get; set; } = null!;

    public string prefer_jobtime { get; set; } = null!;

    public bool? is_completed { get; set; }

    public string? city { get; set; }

    public string? area { get; set; }

    public virtual ICollection<Favorites> Favorites { get; set; } = new List<Favorites>();

    public virtual ICollection<Message_Board> Message_Board { get; set; } = new List<Message_Board>();

    public virtual Album? album { get; set; }

    public virtual Device_List? device_list { get; set; }

    public virtual Landlord? landlord { get; set; }

    public virtual Member? member { get; set; }
}
