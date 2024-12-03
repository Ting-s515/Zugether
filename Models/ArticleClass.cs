namespace Zugether.Models
{
    public class ArticleClass
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Date { get; set; }
        public string? Image { get; set; }
        //public string? Content { get; set; }
        public List<string>? Content { get; set; }
    }
}
