using Zugether.Models;
namespace Zugether.DTO
{
	public class RoomViewModel
	{
		public Room? Room { get; set; }
		public Device_List? DeviceList { get; set; }
		public Landlord? Landlord { get; set; }
		public Member? Member { get; set; }
		public Message? Message { get; set; }
		public Message_Board? MessageBoard { get; set; }
		public Album? Album { get; set; }
		public Photo? Photo { get; set; }
		public PartialViewModel? PartialViewModel { get; set; }
		public List<RoomMessage>? roomMessages { get; set; }
		public List<RoomImages>? roomImages { get; set; }
		public List<DeviceList>? deviceList { get; set; }
	}
	public class RoomMessage
	{
		public string? reply_member_content { get; set; }
		public string? message_content { get; set; }
		public string? member_name { get; set; }
		public string? reply_member_name { get; set; }
		public string? post_time { get; set; }
		public string? message_basement { get; set; }
		public byte[]? avatar { get; set; }
		public string avatarSrc => avatar != null
	  ? $"data:image/jpeg;base64,{Convert.ToBase64String(avatar)}"
	  : "/images/peopleImg.png"; // 預設圖片路徑
		public short? member_id { get; set; }
	}
	public class RoomImages
	{
		public byte[]? room_photo { get; set; }
		public string? photo_type { get; set; }
	}
	public class DeviceList
	{
		public bool? canPet { get; set; }
		public bool? canSmoking { get; set; }
	}
}
