namespace Zugether.Models
{
    public class PartialViewModel
    {
    }

    public class PartialAlert
    {
        public string? Color { get; set; }
        public string? AlertText { get; set; }
        public bool Show { get; set; }
        public int Time { get; set; }
    }

    public class PartialLoading
    {
        public bool IsLoading { get; set; }
        public int Time { get; set; }
    }
}
