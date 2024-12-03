using Zugether.Models;
namespace Zugether.DTO
{
    public class AllRoomInfoDTO
    {
        public Room? room { get; set; }
        public Device_List? deviceList { get; set; }
        public Landlord? landlord { get; set; }
        public List<Photo>? photos { get; set; }
        public List<string> imageUrl { get; set; } = new List<string>();
        public string? consentUrl { get; set; }

        public void convertRoomPhoto()
        {
            if (photos != null && photos.Count > 0)
            {
                foreach (Photo p in photos)
                {
                    string base64Image = Convert.ToBase64String(p.room_photo!);
                    string Url = $"data:image/jpeg;base64,{base64Image}";
                    imageUrl.Add(Url);
                }
            }
        }
        public void convertConsentPhoto()
        {
            if (landlord!.consent_photo != null)
            {
                consentUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(landlord.consent_photo)}";
            }
            else
            {
                consentUrl = null;
            }
        }

    }
}
