using System;
using System.Collections.Generic;

namespace Zugether.Models;

public partial class Message_Board
{
    public short message_board_id { get; set; }

    public short? room_id { get; set; }

    public virtual ICollection<Message> Message { get; set; } = new List<Message>();

    public virtual Room? room { get; set; }
}
