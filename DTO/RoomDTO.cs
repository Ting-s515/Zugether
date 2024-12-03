namespace Zugether.DTO
{
	public class RoomDTO
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
	}
}
