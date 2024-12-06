using System;
using System.Collections.Generic;

namespace Zugether.Models;

public partial class Member
{
    public short member_id { get; set; }

    public string email { get; set; } = null!;

    public string? password { get; set; }

    public string name { get; set; } = null!;

    public string? nickname { get; set; }

    public DateOnly? birthday { get; set; }

    public string? gender { get; set; }

    public string? phone { get; set; }

    public string? introduce { get; set; }

    public byte[]? avatar { get; set; }

    public string? job { get; set; }

    public string? jobtime { get; set; }

    public virtual ICollection<Favor_List> Favor_List { get; set; } = new List<Favor_List>();

    public virtual ICollection<Message> Messagemember { get; set; } = new List<Message>();

    public virtual ICollection<Message> Messagereply_member { get; set; } = new List<Message>();

    public virtual ICollection<Room> Room { get; set; } = new List<Room>();
}
