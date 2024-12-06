using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Zugether.DTO;
using Zugether.Models;
using static Zugether.DTO.ArticleDTO;


namespace Zugether.Controllers
{
    public class AdminController : Controller
    {
        private ZugetherContext _context;
        public AdminController(ZugetherContext context)
        {
            _context = context;
        }

        // 登入畫面
        public IActionResult Index()
        {
            ViewBag.show = false;
            return View();
        }
        // 送出使用者名稱和密碼
        [HttpPost]
        public async Task<IActionResult> Index(string username , string password)
        {
            var res = await (from a in _context.Admin
                       where a.userName == username && a.password == password
                       select a).SingleOrDefaultAsync();
            if (res == null) {
                ViewBag.show = true;
                ViewBag.color = "danger";
                ViewBag.message = "登入失敗";
                return View();
            }

            // 登入成功，設定 isLogin 為 true
            HttpContext.Session.SetString("admin_isLogin", "true");
            return RedirectToAction("Members");
            
        }

        // 登出
        [HttpPost]
        public IActionResult Logout()
        {
            // 移除登入狀態的 Session 資訊
            HttpContext.Session.Remove("admin_isLogin");

            // 重定向至登入頁面
            return Redirect("/Admin/Index");
        }

        // 判斷是否登入 function
        public bool isLogin()
        {
            return HttpContext.Session.GetString("admin_isLogin") == "true";
        }
        
        // 會員列表
        public IActionResult Members(string sortOrder)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index"); ;
            }
            sortOrder = string.IsNullOrEmpty(sortOrder) ? "mId_asc" : sortOrder;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.SortBymId = sortOrder == "mId_desc" ? "mId_asc" : "mId_desc";
            ViewBag.SortByGender = sortOrder == "gender_desc" ? "gender_asc" : "gender_desc";
            var res = from members in _context.Member
                      select members;
            // 根據排序條件調整
            switch (sortOrder)
            {
                case "mId_asc":
                    res = res.OrderBy(r => r.member_id);
                    break;
                case "mId_desc":
                    res = res.OrderByDescending(r => r.member_id);
                    break;
                case "gender_asc":
                    res = res.OrderBy(r => r.gender);
                    break;
                case "gender_desc":
                    res = res.OrderByDescending(r=>r.gender);
                    break;
                default:
                    res = res.OrderBy(r => r.member_id); // 預設按房間編號升序
                    break;
            }
            List<Member> memberList = res.ToList();
            return View(memberList);
        }
        //編輯會員頁面
        public IActionResult MemberEdit(short id)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            var member = _context.Member.Find(id);
            return View(member);
        }
        // 送出編輯資料
        [HttpPost]
        public IActionResult ConfirmMemberEdit(short member_id,string gender,DateOnly birthday)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            var member = _context.Member.Find(member_id);
            member!.gender = gender;
            member.birthday = birthday;
            _context.SaveChanges();
            return Redirect("/Admin/Members");
        }
        // 刪除會員頁面
        public IActionResult MemberDelete(short id)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            var member = _context.Member.Find(id);
            return View(member);
        }
        // 刪除會員資料
        [HttpPost]
        public async Task<IActionResult> ConfirmMemberDelete(short member_id)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            var member = _context.Member.Find(member_id);
            var favorList = _context.Favor_List.Where(f=>f.member_id == member_id);
            if (favorList.Any())
            {
                _context.Favor_List.RemoveRange(favorList);
            }
            _context.Member.Remove(member!);
            await _context.SaveChangesAsync();
            return Redirect("/Admin/Members");
        }
        // 房間列表
        public IActionResult Rooms(string sortOrder)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            sortOrder = string.IsNullOrEmpty(sortOrder) ? "enabled_asc" : sortOrder;
            ViewBag.CurrentSort = sortOrder; 
            ViewBag.SortByRoomId = sortOrder == "roomId_desc" ? "roomId_asc" : "roomId_desc";
            ViewBag.SortByEnabled = string.IsNullOrEmpty(sortOrder) || sortOrder == "enabled_desc" ? "enabled_asc" : "enabled_desc";
            var res = from rooms in _context.Room
                         select rooms;
            // 根據排序條件調整
            switch (sortOrder)
            {
                case "enabled_asc":
                    res = res.OrderBy(r => r.isEnabled);
                    break;
                case "enabled_desc":
                    res = res.OrderByDescending(r => r.isEnabled);
                    break;
                case "roomId_asc":
                    res = res.OrderBy(r => r.room_id);
                    break;
                case "roomId_desc":
                    res = res.OrderByDescending(r => r.room_id);
                    break;
                default:
                    res = res.OrderBy(r => r.room_id); // 預設按房間編號升序
                    break;
            }
            List<Room> roomList = res.ToList();
            return View(roomList);
        }
        // 編輯房間頁面
        public IActionResult RoomEdit(short id)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }

            var res = ((from r in _context.Room
                       join l in _context.Landlord on r.landlord_id equals l.landlord_id
                       where r.room_id == id
                       select new
                       {
                           r.room_id,
                           r.room_title,
                           r.post_date,
                           r.isEnabled,
                           l.consent_photo
                       }).Distinct()).SingleOrDefault();

            if (res == null) {
                ViewBag.Message = "尚無房東資料";
                return View();
            }

            if (res?.consent_photo != null)
            {
                var base64Image = Convert.ToBase64String(res.consent_photo);
                ViewBag.consentImage = $"data:image/png;base64,{base64Image}";
            }
            var viewModel = new RoomEditViewModel
            {
                room_id = res.room_id,
                room_title = res.room_title,
                post_date = res.post_date,
                isEnabled = res.isEnabled,
                consentImage = ViewBag.consentImage
            };
            return View(viewModel);
        }

        // 送出編輯資料
        [HttpPost]
        public IActionResult RoomEdit(short room_id, bool isEnabled)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            var room = _context.Room.Find(room_id);
            room!.isEnabled = isEnabled ? true:false;
            _context.SaveChanges();
            return Redirect("/Admin/Rooms");
        }

        // 刪除房間頁面
        public IActionResult RoomDelete(short id)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            var room = _context.Room.Find(id);
            return View(room);
        }
        // 刪除房間資料
        [HttpPost]
        public async Task<IActionResult> ConfirmRoomDelete(short room_id)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            var room = _context.Room.Find(room_id);
            _context.Room.Remove(room);
            await _context.SaveChangesAsync();
            return Redirect("/Admin/Rooms");
        }

        // 聯絡我們列表
        public IActionResult Contacts(string sortOrder)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index"); ;
            }
            sortOrder = string.IsNullOrEmpty(sortOrder) ? "cId_desc" : sortOrder;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.SortBycId = sortOrder == "cId_desc" ? "cId_asc" : "cId_desc";
           
            var res = from contacts in _context.Contact_us
                      select contacts;
            // 根據排序條件調整
            switch (sortOrder)
            {
                case "cId_asc":
                    res = res.OrderBy(r => r.contact_id);
                    break;
                case "cId_desc":
                    res = res.OrderByDescending(r => r.contact_id);
                    break;
                default:
                    res = res.OrderBy(r => r.contact_id); // 預設按房間編號升序
                    break;
            }
            List<Contact_us> contactList = res.ToList();
            return View(contactList);
        }
        //聯絡我們頁面
        public IActionResult ContactView(short id)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            var contact = _context.Contact_us.Find(id);
            return View(contact);
        }
        // 刪除聯絡我們頁面
        public IActionResult ContactDelete(short id)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            var contact = _context.Contact_us.Find(id);
            return View(contact);
        }
        // 刪除房間資料
        [HttpPost]
        public async Task<IActionResult> ConfirmContactDelete(short contact_id)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            var contact = _context.Contact_us.Find(contact_id);
            _context.Contact_us.Remove(contact);
            await _context.SaveChangesAsync();
            return Redirect("/Admin/Contacts");
        }

        // 統計
        public IActionResult Analytics()
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index"); ;
            }
            List<Member> MemberList = (from members in _context.Member
                                       select members).ToList();
            return View(MemberList);
        }

        // 文章列表 start
        public IActionResult Articles(string sortOrder)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index"); ;
            }
            sortOrder = string.IsNullOrEmpty(sortOrder) ? "aId_desc" : sortOrder;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.SortByaId = sortOrder == "aId_desc" ? "aId_asc" : "aId_desc";

            var res = from articles in _context.Article
                      select new ArticleList
                      {
                          ArticleId = articles.article_id,
                          Title = articles.title,
                          Create_at = articles.created_at,
                          Update_at = articles.updated_at,
                      };
            // 根據排序條件調整
            switch (sortOrder)
            {
                case "aId_asc":
                    res = res.OrderBy(r => r.ArticleId);
                    break;
                case "aId_desc":
                    res = res.OrderByDescending(r => r.ArticleId);
                    break;
                default:
                    res = res.OrderBy(r => r.ArticleId); // 預設按房間編號升序
                    break;
            }
            var articleList = res.ToList();
            return View(articleList);

        }
        //編輯文章頁面
        public IActionResult ArticleEdit(short id)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            var res = _context.Article.Find(id);
            if (res.photo != null)
            {
                var base64Image = Convert.ToBase64String(res.photo);
                ViewBag.articleImage = $"data:image/png;base64,{base64Image}";
            }
            var article = new ArticleViewModel
            {
                ArticleId = res.article_id,
                Title = res.title,
                Content = res.content
            };

            return View(article);
        }
        // 送出編輯資料
        [HttpPost]
        public async Task<IActionResult> ConfirmArticleEdit(short article_id, Article edit, IFormFile photo)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            var article = _context.Article.Find(article_id);

            article.title = edit.title;
            article.content = edit.content;
            article.updated_at = DateTime.Now;

            if (photo != null)
            {
                using (var ms = new MemoryStream())
                {
                    await photo.CopyToAsync(ms);
                    article.photo = ms.ToArray();
                }
            }


            await _context.SaveChangesAsync();
            return Redirect("/Admin/Articles");
        }
        // 刪除文章頁面
        public IActionResult ArticleDelete(short id)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            var res = _context.Article.Find(id);
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
        // 刪除文章資料
        [HttpPost]
        public async Task<IActionResult> ConfirmArticleDelete(short article_id)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            }
            var article = _context.Article.Find(article_id);
            _context.Article.Remove(article!);
            await _context.SaveChangesAsync();
            return Redirect("/Admin/Articles");
        }

        public IActionResult ArticleCreate()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ArticleCreate(Article create, IFormFile photo)
        {
            if (!isLogin())
            {
                // 若未登入，返回重定向至 Index
                return Redirect("/Admin/Index");
            };
            using (var ms = new MemoryStream())
            {
                await photo.CopyToAsync(ms);
                create.photo = ms.ToArray();
            }
            _context.Article.Add(create);
            await _context.SaveChangesAsync();
            return Redirect("/Admin/Articles");
        }
    }
}
