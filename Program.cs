using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Zugether.Hubs;
using Zugether.Models;

var builder = WebApplication.CreateBuilder(args);
// 加載 .env 文件
Env.Load();
// 添加身份驗證服務
builder.Services.AddAuthentication(options =>
{
	options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(googleOptions =>
{
	string? googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
	string? googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
	if (string.IsNullOrEmpty(googleClientId) || string.IsNullOrEmpty(googleClientSecret))
	{
		Console.WriteLine("Google Client ID 或 Client Secret 未設置");
		Environment.Exit(1); // 停止啟動，避免應用程序出錯
	}
	googleOptions.ClientId = googleClientId;
	googleOptions.ClientSecret = googleClientSecret;
	var scopes = new[] { "openid", "profile", "email", "https://www.googleapis.com/auth/gmail.send" };
	foreach (var scope in scopes)
	{
		googleOptions.Scope.Add(scope);
	}
	// 指定google api callback path
	googleOptions.CallbackPath = new PathString("/signin-google");
});
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ZugetherContext>(
				options => options.UseSqlServer(builder.Configuration.GetConnectionString("Connstring")));

// 添加 SignalR 服務
builder.Services.AddSignalR();
// 添加 Session 支援
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
	options.Cookie.HttpOnly = true; // 設置為 HttpOnly
	options.Cookie.IsEssential = true; // 將 Session 設置為必要 Cookie
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
// 啟用 Session
app.UseSession();
// 配置 SignalR 的路由
app.MapHub<ChatHub>("/chatHub");
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
