using System;
using System.Collections.Generic;

namespace Zugether.Models;

public partial class Message
{
    public short message_id { get; set; }

    public short message_board_id { get; set; }

    public string? reply_member_content { get; set; }

    public string message_content { get; set; } = null!;

    public short? member_id { get; set; }

    public short? reply_member_id { get; set; }

    public string post_time { get; set; } = null!;

    public byte? message_basement { get; set; }

    public virtual Member? member { get; set; }

    public virtual Message_Board message_board { get; set; } = null!;

    public virtual Member? reply_member { get; set; }
}
