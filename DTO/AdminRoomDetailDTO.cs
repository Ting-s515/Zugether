namespace Zugether.DTO
{
    public class AdminRoomDetailDTO
    {
    }
    public class RoomEditViewModel
    {
        public short room_id { get; set; }
        public string room_title { get; set; }
        public DateOnly? post_date { get; set; }
        public bool? isEnabled { get; set; }
        public string consentImage { get; set; }
    }
}
