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
            IQueryable<short> roomID = from fa_li in _context.Favor_List
                                       where fa_li.member_id == memberID
                                       join fa in _context.Favorites
                                       on fa_li.favor_list_id equals fa.favor_list_id
                                       select fa.room_id;
            List<short> result = await roomID.ToListAsync();
            return Json(result);
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
