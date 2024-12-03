using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Zugether.DTO;
using Zugether.Models;

namespace Zugether.Controllers
{
	public class SearchController : Controller
	{
		public readonly ZugetherContext _context;
		public SearchController(ZugetherContext context)
		{
			_context = context;
		}
		public IActionResult Index()
		{
			return View();
		}
		//GET
		public async Task<IActionResult> RoomList()
		{
			// 讀取查詢結果
			string? dataJson = HttpContext.Session.GetString("SearchResults");
			// 如果查詢結果為空，設置為空的 List<T>
			List<RoomViewModel>? data = await Task.Run(() => JsonConvert.DeserializeObject<List<RoomViewModel>>(dataJson ?? "[]"));
			ViewBag.Message = HttpContext.Session.GetString("Message");
			ViewBag.CityList = HttpContext.Session.GetString("CityList");
			return View(data);
		}
		[HttpPost]
		public async Task<IActionResult> SearchRoom(string cityList, string cityAreaList, short rent, string roomType,
			 string preferJobtime, bool pet = false, bool smoking = false)
		{
			IQueryable<Room> query = _context.Room.AsQueryable();
			//測試價格
			//rent = 30000;
			query = query.Where(x => x.isEnabled == true);
			// 檢查是否有符合 isEnabled 條件的房間
			if (!await query.AnyAsync())
			{
				HttpContext.Session.SetString("Message", "查無結果");
				HttpContext.Session.SetString("SearchResults", JsonConvert.SerializeObject(new List<Room>()));
				return RedirectToAction("RoomList");
			}
			//寵物 抽菸
			query = from x in query
					join y in _context.Device_List on x.device_list_id equals y.device_list_id
					where (pet ? y.keep_pet == true : y.keep_pet != true) &&
						  (smoking ? y.smoking == true : y.smoking != true)
					select x;
			switch (cityList.ToLower().Trim())
			{
				case "all":
					//全部縣市
					break;
				default:
					//指定縣市
					query = query.Where(x => x.city.Contains(cityList));
					break;
			}
			switch (cityAreaList.ToLower().Trim())
			{
				case "all":
					//全部區域
					break;
				default:
					//指定區域
					query = query.Where(x => x.area.Contains(cityAreaList));
					break;
			}
			switch (roomType.Trim())
			{
				case "all":
					break;
				default:
					query = query.Where(x => x.room_type == roomType);
					break;
			}
			switch (preferJobtime.ToLower().Trim())
			{
				case "all":
					break;
				default:
					query = query.Where(x => x.prefer_jobtime.Contains(preferJobtime));
					break;
			}
			//價格
			query = query.Where(x => x.rent <= rent);

			//排除會員自身刊登的房間
			string? isLogin = HttpContext.Session.GetString("isLogin");
			if (isLogin == "true")
			{
				string? memberID = HttpContext.Session.GetString("memberID");
				if (!string.IsNullOrEmpty(memberID))
				{
					query = from room in query
							where room.member_id.ToString() != memberID
							select room;
				}
			}

			// 查詢並加載房間與照片資料
			List<RoomViewModel> result = await query.Select(room => new RoomViewModel
			{
				Room = room,
				roomImages = (from x in _context.Room
							  where x.room_id == room.room_id
							  join y in _context.Photo on x.album_id equals y.album_id
							  select new RoomImages
							  {
								  room_photo = y.room_photo,
								  photo_type = y.photo_type
							  }).ToList(),
				deviceList = _context.Device_List
					.Where(x => x.device_list_id == room.device_list_id)
					.Select(x => new DeviceList
					{
						canPet = x.keep_pet,
						canSmoking = x.smoking
					}).ToList(),
			}).ToListAsync();
			string sessionKey = "SearchResults";
			try
			{
				if (result.Any())
				{
					HttpContext.Session.SetString("Message", "查詢成功");
					HttpContext.Session.SetString(sessionKey, JsonConvert.SerializeObject(result));
				}
				else
				{
					HttpContext.Session.SetString("Message", "查無結果");
					HttpContext.Session.SetString(sessionKey, JsonConvert.SerializeObject(new List<RoomViewModel>()));
				}

				HttpContext.Session.SetString("CityList", cityList + cityAreaList + roomType + rent);
			}
			catch (Exception ex)
			{
				HttpContext.Session.SetString("Message", "發生錯誤，請稍後再試：" + ex.Message);
				HttpContext.Session.SetString(sessionKey, JsonConvert.SerializeObject(new List<RoomViewModel>()));
			}
			return RedirectToAction("RoomList");
		}
		[Route("RoomID/{roomID}")]
		//房間卡片連結
		public async Task<IActionResult> Room(short roomID)
		{
			if (roomID <= 0)
			{
				return PartialView("_PartialNotFound");
			}
			Room room = await GetRoomID(roomID);
			if (room == null)
			{
				return PartialView("_PartialNotFound");
			}
			else if (room.isEnabled != true)
			{
				return PartialView("_PartialNotFound");
			}
			Member member = await GetMember(roomID);
			Landlord landlord = await GetLandlord(roomID);
			List<RoomMessage> message = await GetMessageArea(roomID);
			List<RoomImages> images = await GetRoomImages(roomID);
			RoomViewModel viewModel = new RoomViewModel
			{
				Room = room,
				Member = member,
				roomMessages = message,
				roomImages = images,
				Landlord = landlord
			};
			return View(viewModel);
		}
		public async Task<Room> GetRoomID(short roomID)
		{
			Room? query = await _context.Room.FirstOrDefaultAsync(x => x.room_id == roomID);
			return query;
		}
		//取出留言區資料
		[HttpPost]
		public async Task<List<RoomMessage>> GetMessageArea(short roomID)
		{
			try
			{
				List<RoomMessage> query = await (from x in _context.Message_Board
												 where x.room_id == roomID
												 join y in _context.Message on x.message_board_id equals y.message_board_id
												 join z in _context.Member on y.member_id equals z.member_id
												 join z1 in _context.Member on y.reply_member_id equals z1.member_id into replies
												 from reply in replies.DefaultIfEmpty() // 左連接
												 select new RoomMessage
												 {
													 reply_member_content = y.reply_member_content,
													 message_content = y.message_content,
													 member_name = z.name,
													 reply_member_name = reply != null ? reply.name : "",
													 post_time = y.post_time,
													 message_basement = y.message_basement.ToString(),
													 avatar = z.avatar,
													 member_id = y.member_id
												 }).ToListAsync();
				return query;
			}
			catch (Exception ex)
			{
				throw new ApplicationException("抓取留言失败", ex);
			}
		}
		public async Task<Member> GetMember(short roomID)
		{

			Member? query = await (from x in _context.Room
								   where x.room_id == roomID
								   join y in _context.Member on x.member_id equals y.member_id
								   select y).FirstOrDefaultAsync();
			return query;
		}
		public async Task<Landlord> GetLandlord(short roomID)
		{
			Landlord? query = await (from x in _context.Room
									 where x.room_id == roomID
									 join y in _context.Landlord on x.landlord_id equals y.landlord_id
									 select y).FirstOrDefaultAsync();
			return query;
		}
		public async Task<List<RoomImages>> GetRoomImages(short roomID)
		{
			List<RoomImages> query = await (from x in _context.Room
											where x.room_id == roomID
											join y in _context.Photo on x.album_id equals y.album_id
											select new RoomImages
											{
												room_photo = y.room_photo,
												photo_type = y.photo_type,
											}).ToListAsync();
			return query;
		}
		//房間設備
		[HttpPost]
		public async Task<IActionResult> GetRoomDevice(short roomID)
		{
			try
			{
				Device_List? room = await (from x in _context.Room
										   where x.room_id == roomID
										   join y in _context.Device_List on x.device_list_id equals y.device_list_id
										   select y).FirstOrDefaultAsync();
				return Json(new { state = true, data = room });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					errorMessage = ex.Message,
					innerException = ex.InnerException?.Message,
					stackTrace = ex.StackTrace
				});
			}
		}
		[HttpPost]
		public async Task<IActionResult> MessageMember(short memberID)
		{
			Member? member = await _context.Member.FirstOrDefaultAsync(x => x.member_id == memberID);
			RoomViewModel? viewModel = new RoomViewModel
			{
				Member = member
			};
			return PartialView("_PartialMemberModal", viewModel);
		}
		[HttpPost]
		public async Task<IActionResult> PostMessage(short roomID, short memberID, string messageTime,
			string replyMemberContent, string messageContent, string replyName)
		{
			short? replyMemberID = null;
			try
			{
				// 回覆者編號
				if (!string.IsNullOrEmpty(replyName))
				{
					replyMemberID = await _context.Member
						.Where(x => x.name == replyName)
						.Select(x => x.member_id)
						.FirstOrDefaultAsync();

					if (replyMemberID == 0)
					{
						throw new Exception($"找不到回覆者名稱 {replyName} 的會員編號");
					}
				}

				//對應的留言板編號
				var messageBoardID = await _context.Message_Board.Where(x => x.room_id == roomID).Select(x => x.message_board_id).FirstOrDefaultAsync();
				if (messageBoardID == 0)
				{
					throw new Exception($"找不到對應的留言板編號，房間 ID: {roomID}");
				}
				// 檢查 messageContent
				if (string.IsNullOrWhiteSpace(messageContent))
				{
					throw new Exception("留言內容不能為空");
				}
				Message message = new Message
				{
					message_board_id = messageBoardID,
					member_id = memberID,
					reply_member_content = string.IsNullOrWhiteSpace(replyMemberContent) ? null : replyMemberContent.Trim(),
					message_content = messageContent.Trim(),
					post_time = messageTime,
					reply_member_id = replyMemberID
				};

				await _context.Message.AddAsync(message);
				await _context.SaveChangesAsync();
				return Json(new { state = true, message = "POST留言成功" });
			}
			catch (Exception ex)
			{
				return Json(new
				{
					state = false,
					message = "POST留言失敗",
					errorMessage = ex.Message,
					innerException = ex.InnerException?.Message,
					stackTrace = ex.StackTrace,
					debugInfo = new
					{
						roomID,
						memberID,
						messageTime,
						replyMemberContent,
						messageContent,
						replyName,
						replyMemberID
					}
				});
			}
		}
		//點擊回覆modal
		[HttpPost]
		public async Task<IActionResult> PostReplyBasement(string replyBasement, short roomID)
		{
			try
			{
				// 查詢房間對應留言板編號
				short messageBoardID = await _context.Message_Board
					.Where(x => x.room_id == roomID)
					.Select(x => x.message_board_id)
					.FirstOrDefaultAsync();
				// 查詢留言內容
				List<RoomMessage> message = await (from x in _context.Message
												   where x.message_board_id == messageBoardID && x.message_basement.ToString() == replyBasement
												   join y in _context.Member on x.member_id equals y.member_id
												   join y1 in _context.Member on x.reply_member_id equals y1.member_id into replyGroup
												   from replyMember in replyGroup.DefaultIfEmpty()// 左連接
												   select new RoomMessage
												   {
													   reply_member_content = x.reply_member_content,
													   message_content = x.message_content,
													   member_name = y.name,
													   reply_member_name = replyMember != null ? replyMember.name : "",
													   message_basement = x.message_basement.ToString(),
													   post_time = x.post_time.ToString(),
													   avatar = y.avatar
												   }).ToListAsync();
				//if (message == null || !message.Any())
				//{
				//	return Json(new { state = false, message = "未找到對應的留言。", parameter = replyBasement, parameter1 = roomID });
				//}
				RoomViewModel viewModel = new RoomViewModel
				{
					roomMessages = message
				};
				return PartialView("_PartialReplyModal", viewModel);

			}
			catch (Exception ex)
			{
				return Json(new
				{
					state = false,
					message = "查詢回覆樓層失敗",
					errorMessage = ex.Message,
					innerException = ex.InnerException?.Message,
					stackTrace = ex.StackTrace
				});
			}
		}
		//EditMessage
		[HttpPost]
		public async Task<IActionResult> EditMessageToDB(short roomID, short memberID, string messageContent, byte basement, string editMessageTime)
		{
			try
			{
				// 查詢房間對應留言板編號
				short messageBoardID = await _context.Message_Board
					.Where(x => x.room_id == roomID)
					.Select(x => x.message_board_id)
					.FirstOrDefaultAsync();

				Message? messageText = await _context.Message
					.Where(x => x.message_board_id == messageBoardID && x.message_basement == basement)
					.FirstOrDefaultAsync();
				if (messageText == null)
				{
					return Json(new { state = false, message = "找不到對應的留言" });
				}

				messageText.message_content = messageContent.Trim();
				messageText.post_time = editMessageTime.Trim();
				await _context.SaveChangesAsync();
				return Json(new { state = true, message = "編輯留言成功" });
			}
			catch (Exception ex)
			{
				return Json(new
				{
					state = false,
					message = "編輯留言失敗",
					errorMessage = ex.Message,
					innerException = ex.InnerException?.Message,
					stackTrace = ex.StackTrace
				});
			}
		}
		//抓出會員圖片
		[HttpPost]
		public IActionResult GetMemberImage(string memberID)
		{
			try
			{
				if (!short.TryParse(memberID.Trim(), out short parsedMemberID))
				{
					return Json(new
					{
						success = false,
						message = "無效的會員 ID，無法進行處理",
						debugInfo = new { MemberID = memberID }
					});
				}
				var member = _context.Member.FirstOrDefault(x => x.member_id == parsedMemberID);
				if (member?.avatar == null)
				{
					// 如果沒有圖片，返回預設圖片
					var defaultImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "peopleImg.png");
					if (!System.IO.File.Exists(defaultImagePath))
					{
						throw new FileNotFoundException("預設圖片不存在", defaultImagePath);
					}
					var defaultImageBytes = System.IO.File.ReadAllBytes(defaultImagePath);
					return File(defaultImageBytes, "image/png");
				}
				// 返回會員的圖片
				return File(member.avatar, "image/jpeg");
			}
			catch (Exception ex)
			{
				// 僅返回 JSON 格式的錯誤信息
				return StatusCode(500, new
				{
					success = false,
					message = "無法獲取會員圖片",
					error = ex.Message,
					innerException = ex.InnerException?.Message,
					stackTrace = ex.StackTrace,
					debugInfo = new
					{
						MemberID = memberID
					}
				});
			}
		}
		//Alert動畫
		public IActionResult Alert(string color, string alertText, bool show, int time)
		{
			PartialAlert model = new PartialAlert
			{
				Color = color,
				AlertText = alertText,
				Show = show,
				Time = time
			};
			return PartialView("_PartialAlert", model);
		}
		[HttpPost]
		public async Task<IActionResult> disableFavoriteAndShareRoom(short roomID)
		{
			try
			{
				short? member = await _context.Room
					.Where(x => x.room_id == roomID)
					.Select(x => x.member_id)
					.FirstOrDefaultAsync();
				return Json(new { success = true, getMemberID = member });
			}
			catch (Exception ex)
			{
				return Json(new
				{
					success = false,
					message = "查詢會員編號時發生錯誤",
					errorMessage = ex.Message,
					innerException = ex.InnerException?.Message,
					stackTrace = ex.StackTrace
				});
			}
		}
		[HttpPost]
		public async Task<IActionResult> GetMemberToRoomID(short memberID)
		{
			try
			{
				List<short> roomID = await _context.Room
					.Where(x => x.member_id == memberID)
					.Select(x => x.room_id)
					.ToListAsync();
				return Json(new { success = true, getRoomID = roomID });
			}
			catch (Exception ex)
			{
				return Json(new
				{
					success = false,
					message = "查詢房間編號時發生錯誤",
					errorMessage = ex.Message,
					innerException = ex.InnerException?.Message,
					stackTrace = ex.StackTrace
				});
			}
		}


		//是否刊登房間-------------------------
		[HttpPost]
		public async Task<IActionResult> GetRoomMemberID(short memberID)
		{
			try
			{
				var roomMemberID = await _context.Room
					.AnyAsync(room => room.member_id == memberID);

				if (!roomMemberID)
				{
					return Json(new { success = false, message = "該成員並未刊登房間。" });
				}
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "該成員並未刊登房間。", details = ex.Message });
			}
		}

		[HttpPost("/Room/GetPublisher")]
		public async Task<IActionResult> GetRoomPublisher([FromBody] InviteRequest roomRequest)
		{
			// 確認接收到的 roomID
			var roomID = roomRequest.RoomID;

			// 查詢該房間對應的會員 ID
			var memberId = await _context.Room
				.Where(r => r.room_id == roomID)
				.Select(r => r.member_id)
				.FirstOrDefaultAsync();

			if (memberId == 0) // 如果查無資料
			{
				return NotFound(new { Message = "房間不存在或無會員資料" });
			}

			// 返回會員 ID 作為 JSON 格式
			return Ok(new { PublisherId = memberId });
		}

		[HttpPost("/Invite/SendInviteNotification")]
		public async Task<IActionResult> SendInviteNotification([FromBody] InviteNotificationRequest request)
		{
			// 確保資料正確
			if (request.InviterMemberId <= 0 || request.InviteeMemberId <= 0)
			{
				return BadRequest(new { Message = "請提供有效的 InviterMemberId 和 InviteeMemberId" });
			}

			// 顯示收到的資料
			Console.WriteLine($"登入的會員 ID: {request.InviterMemberId}");
			Console.WriteLine($"該房間的會員 ID (刊登者): {request.InviteeMemberId}");
			Console.WriteLine($"當前日期: {request.NotifyDate}");

			// 創建新的邀請通知實體
			var newNotification = new InviteNotification
			{
				inviter_member_id = (short)request.InviterMemberId,  // 轉換為資料庫類型 short
				invitee_member_id = (short)request.InviteeMemberId,  // 轉換為資料庫類型 short
				notify_status = request.NotifyStatus,
				is_finalized = request.IsFinalized,
				notify_date = request.NotifyDate
			};

			// 儲存資料到資料庫
			_context.InviteNotification.Add(newNotification);
			await _context.SaveChangesAsync();  // 保存變更

			return Ok(new { Message = "通知發送成功" });
		}


		//是否刊登房間----用於會員通知相關---------------------
	}
}
