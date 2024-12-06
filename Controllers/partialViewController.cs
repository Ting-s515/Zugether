using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zugether.Models;
namespace Zugether.Controllers
{
    public class partialViewController : Controller
    {
        private readonly ZugetherContext _context;

        public partialViewController(ZugetherContext context)
        {
            _context = context;
        }
        //房間卡片返回給部分檢視
        public IActionResult RoomCard()
        {
            return PartialView("_PartialRoomCard");
        }
        //檢查
        [HttpPost]
        public async Task<IActionResult> checkFavoriteRoom(short memberID)
        {
            IQueryable<short> query = from x in _context.Favor_List
                                      where x.member_id == memberID
                                      join y in _context.Favorites
                                      on x.favor_list_id equals y.favor_list_id
                                      select y.room_id;
            var result = await query.ToListAsync();
            return Json(result);//返回room_id
        }
        //Loading動畫
        public IActionResult Loading(bool isLoading, int time)
        {
            PartialLoading model = new PartialLoading
            {
                IsLoading = isLoading,
                Time = time
            };
            return PartialView("_PartialLoading", model);
        }
    }
}
