using Microsoft.AspNetCore.Mvc;
using Zugether.Models;
using static Zugether.DTO.ArticleDTO;

namespace Zugether.Controllers
{
    public class ArticleController : Controller
    {
        private ZugetherContext _context;
        public ArticleController(ZugetherContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var res = (from a in _context.Article
                       select new ArticlesViewModel
                       {
                           ArticleId = a.article_id,
                           Title = a.title,
                           PhotoBase64 = a.photo != null ? $"data:image/jpeg;base64,{Convert.ToBase64String(a.photo)}" : null
                       }).ToList();

            return View(res);
        }

        public IActionResult Content(int id)
        {
            var res = (from a in _context.Article
                       where a.article_id == id
                       select a).SingleOrDefault();
            if (res?.photo != null)
            {
                var base64Image = Convert.ToBase64String(res.photo);
                ViewBag.articleImage = $"data:image/png;base64,{base64Image}";
            }

            var article = new ArticleViewModel
            {
                ArticleId = res.article_id,
                Title = res.title,
                Content = res.content,
                Create_at = res.created_at,
            };

            return View(article);
        }
    }
}
