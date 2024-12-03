using System;
using System.Collections.Generic;

namespace Zugether.Models;

public partial class InviteNotification
{
    public int notify_id { get; set; }

    public short inviter_member_id { get; set; }

    public short invitee_member_id { get; set; }

    public string notify_status { get; set; } = null!;

    public bool is_finalized { get; set; }

    public DateTime? notify_date { get; set; }
}
