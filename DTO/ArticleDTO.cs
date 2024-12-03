namespace Zugether.DTO
{
	public class ArticleDTO
	{
        public class ArticleList
        {
            public int ArticleId { get; set; }
            public string Title { get; set; }
            public DateTime Create_at { get; set; }

			public DateTime Update_at { get; set; }
        }
        public class ArticlesViewModel
		{
			public int ArticleId { get; set; }
			public string Title { get; set; }
			public string PhotoBase64 { get; set; }
		}

		public class ArticleViewModel
		{
			public int ArticleId { get; set; }
			public string Title { get; set; }
			public string Content { get; set; }
			public DateTime Create_at { get; set; }
		}
	}
}
