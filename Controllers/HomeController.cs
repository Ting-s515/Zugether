using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using Zugether.Models;

namespace Zugether.Controllers
{
    public class HomeController : Controller
    {
        private readonly ZugetherContext _context;
        private readonly PasswordHasher<Member> _passwordHasher;
        public HomeController(ZugetherContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Member>();
        }
        public string? Userforget_Email { get; set; }

        public IActionResult Index()
        {
            ViewBag.memberName = HttpContext.Session.GetString("memberName") ?? HttpContext.Session.GetString("googleName") ?? "";
            ViewBag.memberID = HttpContext.Session.GetString("memberID") ?? HttpContext.Session.GetString("googleMemberID") ?? "";
            ViewBag.memberEmail = HttpContext.Session.GetString("memberEmail") ?? HttpContext.Session.GetString("googleEmail") ?? "";
            if (!string.IsNullOrEmpty(ViewBag.memberName) && !string.IsNullOrEmpty(ViewBag.memberID) && !string.IsNullOrEmpty(ViewBag.memberEmail))
            {
                HttpContext.Session.SetString("isLogin", "true");
            }
            else
            {
                HttpContext.Session.SetString("isLogin", "false");
            }

            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        // 聯絡我們
        public IActionResult Contact()
        {
            ViewBag.show = false;
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Contact(Contact_us contact)
        {
            _context.Contact_us.Add(contact);
            await _context.SaveChangesAsync();

            ViewBag.color = "success";
            ViewBag.show = true;
            ViewBag.message = "感謝您的留言，已將留言傳送給管理者";
            return View();
        }

        /* 註冊 */
        public IActionResult Register()
        {
            var googleName = HttpContext.Session.GetString("googleName");
            var googleEmail = HttpContext.Session.GetString("googleEmail");

            if (!string.IsNullOrEmpty(googleName) && !string.IsNullOrEmpty(googleEmail))
            {
                ViewBag.googleName = googleName;
                ViewBag.googleEmail = googleEmail;
                ViewBag.googleLogin = true;
            }
            ViewBag.show = false;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Member register)
        {
            var r = (from m in _context.Member
                     where m.email == register.email
                     select m).SingleOrDefault();
            //判斷信箱是否被註冊
            if (r != null)
            {
                ViewBag.color = "danger";
                ViewBag.show = true;
                ViewBag.message = "Email 已被註冊 !";
                ViewBag.registerfalse = false;
                return View(register);
            }
            if (register.password != null)
            {
                //將密碼轉成雜湊
                register.password = _passwordHasher.HashPassword(register, register.password!);
            }
            _context.Member.Add(register);
            await _context.SaveChangesAsync();
            ViewBag.color = "success";
            ViewBag.show = true;
            ViewBag.message = "註冊成功";
            return View();
        }
        //登入
        [HttpPost]
        public async Task<IActionResult> Login(Member userlogin)
        {
            Member? em = await _context.Member.FirstOrDefaultAsync(u => u.email == userlogin.email);
            //判斷信箱是否被註冊
            if (em == null)
            {
                TempData["logincolor"] = "danger";
                TempData["loginshow"] = true;
                TempData["loginmessage"] = "Email 未被註冊 !";
                return RedirectToAction("Index", "Home");
            }
            //判斷密碼是否正確(使用雜湊)
            var result = _passwordHasher.VerifyHashedPassword(em, em.password!, userlogin.password!);
            if (result == PasswordVerificationResult.Success)
            {
                TempData["UserName"] = em.name.ToString();

                //測試用
                TempData["testt"] = em.name.ToString();

                // 密碼驗證成功，進行登入
                HttpContext.Session.SetString("memberID", em.member_id.ToString());//使用者ID
                HttpContext.Session.SetString("memberName", em.name.ToString());//使用者名稱
                HttpContext.Session.SetString("memberEmail", em.email.ToString());//使用者Email

                TempData["logincolor"] = "success";
                TempData["loginshow"] = true;
                TempData["loginmessage"] = "登入成功 !";
                return RedirectToAction("Index", "Home");
            }
            //密碼輸入錯誤
            TempData["logincolor"] = "danger";
            TempData["loginshow"] = true;
            TempData["loginmessage"] = "密碼錯誤 !";

            return RedirectToAction("Index", "Home");
        }
        //登出
        public IActionResult Logout()
        {
            // 清除所有 Session 資料
            HttpContext.Session.Clear();

            // 重新導向至首頁
            return RedirectToAction("Index", "Home");
        }


        //忘記密碼驗證Email
        [HttpPost]
        public async Task<IActionResult> CheckEmail([FromBody] EmailRequest request)
        {
            var exists = await _context.Member.FirstOrDefaultAsync(m => m.email == request.Email);

            return Json(new { exists });
        }

        //忘記密碼重設密碼
        [HttpPost]
        public async Task<IActionResult> ForgetPassword(EmailRequest userforget)
        {

            var member_exist = (from m in _context.Member
                                where m.email == userforget.Email
                                select m).SingleOrDefault();

            if (member_exist != null)
            {
                member_exist.password = _passwordHasher.HashPassword(member_exist, userforget.ResetPassword!);
                _context.Update(member_exist);
                await _context.SaveChangesAsync();
                TempData["logincolor"] = "success";
                TempData["loginshow"] = true;
                TempData["loginmessage"] = "密碼變更成功 !";
                return RedirectToAction("Index", "Home");
            }
            TempData["logincolor"] = "danger";
            TempData["loginshow"] = true;
            TempData["loginmessage"] = "錯誤！";
            return RedirectToAction("Index", "Home");

        }
        public class EmailRequest
        {
            public string? Email { get; set; }
            public string? ResetPassword { get; set; }
            public string? CheckResetPw { get; set; }

        }
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Home");
            return Challenge(new AuthenticationProperties { RedirectUri = redirectUrl }, "Google");
        }
        public async Task<IActionResult> GoogleResponse()
        {
            // 獲取token
            var user = HttpContext.User;
            // 提取相關資訊
            var googleName = user.FindFirst(c => c.Type == ClaimTypes.Name)?.Value ?? "no name";
            var googleEmail = user.FindFirst(c => c.Type == ClaimTypes.Email)?.Value ?? "no email";
            var query = await _context.Member.FirstOrDefaultAsync(x => x.email == googleEmail.Trim());
            if (query != null)
            {
                HttpContext.Session.SetString("googleName", query.name);
                HttpContext.Session.SetString("googleEmail", query.email);
                HttpContext.Session.SetString("googleMemberID", query.member_id.ToString());
                HttpContext.Session.SetString("checkGoogleLogin", "true");
                return RedirectToAction("Index");
            }
            HttpContext.Session.SetString("googleName", googleName);
            HttpContext.Session.SetString("googleEmail", googleEmail);
            return RedirectToAction("Register", "Home");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        //Alert動畫
        public IActionResult Alert(string color, string alertText, bool show, int time)
        {
            var model = new PartialAlert
            {
                Color = color,
                AlertText = alertText,
                Show = show,
                Time = time
            };
            return PartialView("_PartialAlert", model);
        }
    }
}
