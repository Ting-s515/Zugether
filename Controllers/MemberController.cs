using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using System.Reflection;
using Zugether.DTO;
using Zugether.Models;
namespace Zugether.Controllers
{
	public class MemberController : Controller
	{
		private readonly ZugetherContext _context;
		private readonly PasswordHasher<Member> _passwordHasher;
		public MemberController(ZugetherContext context)
		{
			_context = context;
			_passwordHasher = new PasswordHasher<Member>();
		}
		private bool checkLogin()
		{
			string? isLogin = HttpContext.Session.GetString("isLogin");
			return isLogin == "true";
		}
		private string? getMemberEmail()
		{
			if (checkLogin())
			{
				return HttpContext.Session.GetString("memberEmail") ?? HttpContext.Session.GetString("googleEmail");
			}
			return null;
		}
		private string? getMemberName()
		{
			if (checkLogin())
			{
				return HttpContext.Session.GetString("memberName") ?? HttpContext.Session.GetString("googleName");
			}
			return null;
		}
		private string? getMemberID()
		{
			if (checkLogin())
			{
				return HttpContext.Session.GetString("memberID") ?? HttpContext.Session.GetString("googleMemberID");
			}
			return null;
		}

		public IActionResult Index()
		{
			return View();
		}

		//-----------------Notify部分--------------------
		// 負責查詢 登入會員email的對應 ID
		private short? FindLoggedInMemberId()
		{
			return (from i in _context.Member
					where i.email == getMemberEmail()
					select i.member_id).SingleOrDefault();
		}

		// 負責提供 登入會員的 ID 且排除null的狀況
		private short GetLoggedInMemberId()
		{
			//取得FindLoggedInMemberId查詢到的結果
			short? loggedInMemberId = FindLoggedInMemberId();

			if (loggedInMemberId == null)
			{
				throw new Exception("找不到目前登入的會員 ID。");
			}
			return loggedInMemberId.Value;
		}


		//Notify View
		// 11/24 新增 房間審查狀態
		public IActionResult Notify()
		{
			// 取得登入會員的 ID
			short loggedInMemberId = GetLoggedInMemberId();

			// 從 Session 取得會員名稱
			string? userName = getMemberName();
			if (userName == null)
			{
				return Redirect("/Home/Index");
			}

			// 一次性查詢所需所有資料
			var notifications = _context.InviteNotification
				.Where(invite => invite.inviter_member_id == loggedInMemberId || invite.invitee_member_id == loggedInMemberId)
				.Select(invite => new
				{
					invite.notify_id,
					invite.inviter_member_id,
					invite.invitee_member_id,
					invite.notify_status,
					invite.is_finalized,
					invite.notify_date, // 新增此行
					InviterName = _context.Member.Where(m => m.member_id == invite.inviter_member_id).Select(m => m.name).FirstOrDefault(),
					InviterAvatar = _context.Member.Where(m => m.member_id == invite.inviter_member_id).Select(m => m.avatar).FirstOrDefault(),
					InviteeName = _context.Member.Where(m => m.member_id == invite.invitee_member_id).Select(m => m.name).FirstOrDefault(),
					InviteeAvatar = _context.Member.Where(m => m.member_id == invite.invitee_member_id).Select(m => m.avatar).FirstOrDefault(),
					RoomExists = _context.Room.Any(r => r.member_id == loggedInMemberId),
					RoommateNum = _context.Room.Where(r => r.member_id == loggedInMemberId).Select(r => r.roommate_num).FirstOrDefault()
				})
				.ToList();

			ViewBag.show = false;
			Member? member = (from i in _context.Member
							  where i.member_id == GetLoggedInMemberId()
							  select i).SingleOrDefault();
			if (member.avatar != null)
			{
				// Convert avatar to base64 string
				var base64Avatar = Convert.ToBase64String(member.avatar);
				ViewBag.AvatarImage = $"data:image/png;base64,{base64Avatar}";
			}

			// 查詢房間狀態
			var room = _context.Room.FirstOrDefault(r => r.member_id == loggedInMemberId);
			string roomStatusMessage = "";
			bool? roomIsEnabled = null;
			bool? roomIsCompleted = null;

			if (room != null)
			{
				// 根據房間的狀態設置訊息
				if (room.isEnabled == false && room.is_completed == false)
				{
					roomStatusMessage = "待審核";
				}
				else if (room.isEnabled == true && room.is_completed == false)
				{
					roomStatusMessage = "已上架";
				}
				else
				{
					roomStatusMessage = "已下架";
				}

				roomIsEnabled = room.isEnabled;
				roomIsCompleted = room.is_completed;
			}
			else
			{
				roomStatusMessage = "沒有房間";
			}

			// 結構化資料，減少後續處理
			var notifyModel = new NotifyViewModel
			{
				Member = _context.Member.Find(loggedInMemberId),
				InviteNotification = notifications.Select(n => new InviteNotification
				{
					notify_id = n.notify_id,
					inviter_member_id = n.inviter_member_id,
					invitee_member_id = n.invitee_member_id,
					notify_status = n.notify_status,
					is_finalized = n.is_finalized,
					notify_date = n.notify_date // 傳遞通知日期
				}).ToList(),
				InviterMembers = notifications
					.GroupBy(n => n.inviter_member_id)
					.ToDictionary(
						g => g.Key,
						g => new Member
						{
							name = g.First().InviterName,
							avatar = g.First().InviterAvatar
						}
					),
				InviteeMembers = notifications
					.GroupBy(n => n.invitee_member_id)
					.ToDictionary(
						g => g.Key,
						g => new Member
						{
							name = g.First().InviteeName,
							avatar = g.First().InviteeAvatar
						}
					),
				RoomInfo = new RoomInfo
				{
					RoomExists = notifications.Any(n => n.RoomExists),
					RoommateNum = notifications.FirstOrDefault()?.RoommateNum ?? 0
				},
				AgreedCount = notifications.Count(n => n.notify_status == "已同意"),
				RoommateNum = notifications.FirstOrDefault()?.RoommateNum ?? 0,
				ShowConfirmButton = notifications.Count(n => n.notify_status == "已同意") >= (notifications.FirstOrDefault()?.RoommateNum ?? 0)
					&& !notifications.All(n => n.is_finalized),
				CanAgreeBtn = notifications.Count(n => n.notify_status == "已同意") < (notifications.FirstOrDefault()?.RoommateNum ?? 0),
				RoomStatusMessage = roomStatusMessage, // 房間狀態訊息
				RoomIsEnabled = roomIsEnabled, // 房間是否啟用
				RoomIsCompleted = roomIsCompleted // 房間是否完成
			};

			return View(notifyModel);
		}


		// 用以導入邀約者、被邀約者之彈跳視窗的詳細資訊及頭貼
		[HttpPost]
		public async Task<IActionResult> GetMemberModal(string memberName)
		{
			Member? memberId = await _context.Member.FirstOrDefaultAsync(x => x.name == memberName); // 根據 ID 取得成員資料
			RoomViewModel viewModel = new RoomViewModel
			{
				Member = memberId
			};
			if (viewModel == null)
			{
				return NotFound();
			}
			return PartialView("_PartialMemberModal", viewModel);
		}

		//11/22 提供資料調取用
		private dynamic GetNotificationDetails(short loggedInMemberId)
		{
			//內容有
			// 房間資訊roomm
			// 室友數量roommateNum
			// 同意數量agreedCount
			// 確認全部同意且已完成 allAgreedFinalized
			//  →判斷是否出現 最終同意按鈕 showConfirmButton
			//	判斷是否能夠點按同意按紐canAgreeBtn

			// 查詢需求房間
			var room = _context.Room
				.FirstOrDefault(r => r.member_id == loggedInMemberId);
			// 查出室友數量
			int? roommateNum = room?.roommate_num;

			// 計算同意人數
			int agreedCount = _context.InviteNotification
				.Where(n => n.invitee_member_id == loggedInMemberId && n.notify_status == "已同意")
				.Count();

			// 計算是否可繼續同意
			bool canAgreeBtn = agreedCount < roommateNum;

			// 確認是否所有 "已同意" 的通知都已最終確認
			bool allAgreedFinalized = _context.InviteNotification
				.Where(n => n.notify_status == "已同意" && n.invitee_member_id == loggedInMemberId)
				.All(n => n.is_finalized);

			// 判斷是否顯示按鈕
			bool showConfirmButton = !allAgreedFinalized && agreedCount >= roommateNum;

			return new
			{
				room,
				roommateNum,
				agreedCount,
				canAgreeBtn,
				allAgreedFinalized,
				showConfirmButton
			};
		}

		//11/22調整 回傳按鈕的狀態
		[HttpPost]
		public IActionResult UpdateNotificationStatus(int notificationId, string status)
		{
			// 找到要更新的通知
			InviteNotification? notification = _context.InviteNotification.Find(notificationId);
			if (notification == null)
			{
				return BadRequest("通知不存在");
			}

			// 更新狀態
			notification.notify_status = status;
			// 儲存更動
			_context.SaveChanges();

			// 判斷是否為 "已撤回"
			bool isWithdrawn = notification.notify_status == "已撤回";

			// 獲取登入會員 ID
			short loggedInMemberId = GetLoggedInMemberId();

			// 調用共用方法
			var notificationDetails = GetNotificationDetails(loggedInMemberId);

			return Json(new
			{
				notificationDetails.canAgreeBtn,
				notificationDetails.showConfirmButton,
				notificationDetails.agreedCount,
				notificationText = isWithdrawn ? "對方已撤回邀請" : "邀請通知",
				isWithdrawn
			});
		}

		// 11/18 新增「確定合租」的按鈕功能
		// 新增 ConfirmFinalizeInvitations 功能
		[HttpPost]
		public IActionResult ConfirmFinalizeInvitations()
		{
			// 取得目前登入會員 ID
			short loggedInMemberId = GetLoggedInMemberId();

			// 調用共用方法
			var notificationDetails = GetNotificationDetails(loggedInMemberId);
			// 有以下資訊：
			// 抓取房間資訊room
			// 室友數量roommateNum
			// 同意數量agreedCount
			// 以上若相等 判斷是否能夠點按同意按紐canAgreeBtn = false
			// 以上確認同意數相等，並且已完成 allAgreedFinalized
			//  →判斷是否出現 最終同意按鈕 showConfirmButton


			// 查詢該會員所有的通知中，notify_status 為 "已同意" 或 "待處理" 的通知 用以判斷將 is_finalized 設為 1
			var notificationsToUpdate = _context.InviteNotification
				.Where(n => n.invitee_member_id == loggedInMemberId &&
							(n.notify_status == "已同意" || n.notify_status == "待處理"))
				.ToList();

			// 篩選出所有需要刪除的通知
			var notificationsToRemove = _context.InviteNotification
				.Where(n => n.invitee_member_id == loggedInMemberId &&
							(n.notify_status == "已撤回"))
				.ToList();

			if (notificationDetails.agreedCount >= notificationDetails.roommateNum)
			{
				// 將所有符合條件的通知的 is_finalized 設為 1
				foreach (var notification in notificationsToUpdate)
				{
					notification.is_finalized = true;
					if (notification.notify_status == "待處理")
					{
						notification.notify_status = "已取消";
					}
				}
				// 批量刪除所有 "已撤回" 的通知
				_context.InviteNotification.RemoveRange(notificationsToRemove);

				// 更新房間的 is_completed 和 isEnabled 狀態
				if (notificationDetails.room != null)
				{
					notificationDetails.room.is_completed = true; // 已完成合租
					notificationDetails.room.isEnabled = false;   // 下架房間
				}

				// 保存變更到資料庫	  
				_context.SaveChanges();

				// 返回操作成功訊息
				return Json(new { success = true, message = "已完成合租！" });
			}

			// 若同意人數未達需求室友人數，返回提示訊息
			return Json(new { success = false, message = "同意人數未達需求室友人數" });
		}

		//11/17 抓取現在的狀態
		[HttpGet]
		public IActionResult GetMemberNotifications()
		{
			var loggedInMemberId = GetLoggedInMemberId(); // 登入系統

			// 查詢通知資料
			var notifications = _context.InviteNotification
				.Where(n => n.invitee_member_id == loggedInMemberId || n.inviter_member_id == loggedInMemberId)
				.Select(n => new
				{
					n.notify_id,
					n.inviter_member_id,
					n.invitee_member_id,
					n.notify_status,
					n.is_finalized
				})
				.ToList();

			// 調用共用方法
			var notificationDetails = GetNotificationDetails(loggedInMemberId);
			// 有以下資訊：
			// 同意數量agreedCount
			// 室友數量roommateNum
			// 以上若相等 判斷是否能夠點按同意按紐canAgreeBtn = false
			// 以上確認同意數相等，並且已完成 allAgreedFinalized
			//  →判斷是否出現 最終同意按鈕 showConfirmButton

			return Json(new
			{
				notifications,//通知資料
				notificationDetails.agreedCount,//同意數量
				notificationDetails.showConfirmButton//是否可以出現確認確認合租按鈕
			});
		}

		//刪除通知用
		[HttpPost]
		public IActionResult DeleteNotification(int notificationId)
		{
			try
			{
				// 尋找要刪除的通知
				InviteNotification? notification = _context.InviteNotification.Find(notificationId);
				if (notification == null)
				{
					return Json(new { success = false, message = "通知不存在。" });
				}

				// 删除通知
				_context.InviteNotification.Remove(notification);
				_context.SaveChanges();

				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				// 捕捉異常
				return Json(new { success = false, message = ex.Message });
			}
		}

		//-----------------Notify部分--------------------


		// 修改密碼
		public IActionResult EditPassword()
		{
			ViewBag.show = false;
			var member = (from i in _context.Member
						  where i.email == getMemberEmail()
						  select i).SingleOrDefault();
			return View(member);
		}

		[HttpPost]
		public async Task<IActionResult> EditPassword(string email, string password)
		{
			var member = (from i in _context.Member
						  where i.email == email
						  select i).SingleOrDefault();

			if (member == null)
			{
				ViewBag.color = "danger";
				ViewBag.show = true;
				ViewBag.message = "修改密碼失敗，請重新輸入";
				return View(member);
			};
			if (member.password != null)
			{
				//將密碼轉成雜湊
				member.password = _passwordHasher.HashPassword(member, password!);
			}
			_context.Update(member);
			await _context.SaveChangesAsync();
			ViewBag.color = "success";
			ViewBag.show = true;
			ViewBag.message = "修改密碼成功";
			return View(member);
		}

		// 修改會員資料
		public IActionResult EditInfo()
		{
			ViewBag.show = false;
			var member = (from i in _context.Member
						  where i.email == getMemberEmail()
						  select i).SingleOrDefault();
			if (member == null)
			{
				return NotFound("無法找到會員資料");
			}
			if (member.avatar != null)
			{
				// Convert avatar to base64 string
				var base64Avatar = Convert.ToBase64String(member.avatar);
				ViewBag.AvatarImage = $"data:image/png;base64,{base64Avatar}";
			}
			else
			{
				// Default image if no avatar exists
				ViewBag.AvatarImage = Url.Content("~/images/peopleImg.png");
			}
			return View(member);
		}
		[HttpPost]
		public async Task<IActionResult> EditInfo(Member edit, IFormFile avatar)
		{
			var member = (from i in _context.Member
						  where i.email == getMemberEmail()
						  select i).SingleOrDefault();
			if (member == null)
			{
				ViewBag.color = "danger";
				ViewBag.show = true;
				ViewBag.message = "修改會員資料失敗，請重新輸入";
				return View(member);
			}

			if (avatar != null)
			{
				using (var ms = new MemoryStream())
				{
					await avatar.CopyToAsync(ms);
					member.avatar = ms.ToArray();
					var base64Avatar = Convert.ToBase64String(member.avatar);
					ViewBag.AvatarImage = $"data:image/png;base64,{base64Avatar}";
				}
			}
			else
			{
				// 沒有上傳新頭像時，沿用舊的頭像
				if (member.avatar != null && member.avatar.Length > 0)
				{
					var base64Avatar = Convert.ToBase64String(member.avatar);
					ViewBag.AvatarImage = $"data:image/png;base64,{base64Avatar}";
				}
				else
				{
					// 沒有舊頭像，使用預設圖片
					ViewBag.AvatarImage = Url.Content("~/images//peopleImg.png");
				}
			}
			member.name = edit.name;
			member.nickname = edit.nickname;
			member.phone = edit.phone;
			member.job = edit.job;
			member.jobtime = edit.jobtime;
			member.introduce = edit.introduce;
			_context.Update(member);
			await _context.SaveChangesAsync();
			ViewBag.color = "success";
			ViewBag.show = true;
			ViewBag.message = "修改成功 !";
			ViewBag.name = edit.name;
			return View(member);
		}

		// --------------------------------房間相關頁面開始-------------------------------
		// 新增房間照片時，用來偵測照片格式如jpg, png等
		public string GetMimeTypeFromImage(byte[] imageBytes)
		{
			IImageFormat format = SixLabors.ImageSharp.Image.DetectFormat(imageBytes);  // 靜態方法調用
			if (format != null)
			{
				return format.DefaultMimeType;
			}
			return "unknown";  // 如果無法檢測到格式
		}

		public async Task<IActionResult> AddRoom()
		{
			string? memberID = getMemberID();
			if (string.IsNullOrEmpty(memberID) || !short.TryParse(memberID, out short userID))
			{
				return Redirect("/Home/Index");
			}

			Room? room = await _context.Room.FirstOrDefaultAsync(r => r.member_id == userID);
			if (room == null)
			{
				ViewBag.isAdd = true;
				return View();
			}

			return RedirectToAction("DeleteRoom", "Member");
		}

		[HttpPost]
		public async Task<IActionResult> AddRoom(Room room, Device_List list, Landlord owner, List<IFormFile> roomPhotos, IFormFile consentPhoto)
		{
			using (IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					// Device_List
					foreach (PropertyInfo prop in list.GetType().GetProperties())
					{
						// 過濾掉虛擬屬性以及主鍵
						if (prop.GetMethod!.IsVirtual || prop.Name == "device_list_id")
						{
							continue;
						}

						// 根據表單中是否提交該屬性來設定值(1 or 0)
						prop.SetValue(list, Request.Form.ContainsKey(prop.Name) ? true : false);
					}
					await _context.Device_List.AddAsync(list);
					// Landlord
					if (consentPhoto != null && consentPhoto.Length > 0)
					{
						using (MemoryStream memoryStream = new MemoryStream())
						{
							await consentPhoto.CopyToAsync(memoryStream);
							owner.consent_photo = memoryStream.ToArray();
						}
					}
					await _context.Landlord.AddAsync(owner);
					Album album = new Album();
					await _context.Album.AddAsync(album);
					// 儲存變更，以獲取外鍵 ID
					await _context.SaveChangesAsync();

					// 設定 Room 的外鍵，然後新增 Room
					room.device_list_id = list.device_list_id;
					room.landlord_id = owner.landlord_id;
					room.album_id = album.album_id;
					room.member_id = Convert.ToInt16(getMemberID());
					await _context.Room.AddAsync(room);
					await _context.SaveChangesAsync();

					Message_Board newBoard = new Message_Board { room_id = room.room_id };
					await _context.Message_Board.AddAsync(newBoard);

					// 插入 Room 的照片
					foreach (IFormFile file in roomPhotos)
					{
						if (file.Length > 0)
						{
							using (MemoryStream memoryStream = new MemoryStream())
							{
								await file.CopyToAsync(memoryStream);
								byte[] imageBytes = memoryStream.ToArray();
								await _context.Photo.AddAsync(new Photo
								{
									room_photo = imageBytes,
									album_id = room.album_id,
									photo_type = GetMimeTypeFromImage(imageBytes)
								});
							}
						}
					}

					// 最後保存照片並提交交易
					await _context.SaveChangesAsync();
					await transaction.CommitAsync();
					return Json(new
					{
						success = true,
						color = "success",
						alertText = "刊登成功，請等待審核通過，將導轉至瀏覽頁面。",
						show = true,
						time = 2000

					});

				}
				catch
				{
					await transaction.RollbackAsync();
					Console.WriteLine("Error occurred, returning Partial View.");
					return Json(new
					{
						success = false,
						color = "danger",
						alertText = "刊登失敗，請於稍後再次嘗試，謝謝。",
						show = true,
						time = 2000
					});

				}
			}
		}

		public IActionResult DeleteRoom()
		{
			string? memberID = getMemberID();
			if (string.IsNullOrEmpty(memberID) || !short.TryParse(memberID, out short userID))
			{
				return Redirect("/Home/Index");
			}
			AllRoomInfoDTO? userRoom = (from r in _context.Room
										join dl in _context.Device_List on r.device_list_id equals dl.device_list_id
										join l in _context.Landlord on r.landlord_id equals l.landlord_id
										join p in _context.Photo on r.album_id equals p.album_id into photogroup
										where r.member_id == userID
										select new AllRoomInfoDTO
										{
											room = r,
											deviceList = dl,
											landlord = l,
											photos = photogroup.ToList()
										}).FirstOrDefault();
			ViewBag.isAdd = (userRoom == null);
			if (ViewBag.isAdd)
			{
				return RedirectToAction("AddRoom", "Member");
			}
			else
			{
				if (userRoom != null)
				{
					userRoom.convertRoomPhoto();
				}
				return View("AddRoom", userRoom);
			}

		}

		[HttpDelete]
		public async Task<IActionResult> DeleteRoom(short roomId)
		{
			Console.WriteLine(roomId);
			using (IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					// 查找 Room 記錄，並確認其屬於當前用戶
					short userId = Convert.ToInt16(getMemberID());
					Room? room = await _context.Room.FirstOrDefaultAsync(r => r.room_id == roomId && r.member_id == userId);
					if (room == null)
					{
						return RedirectToAction("AddRoom", "Member");
					}

					// 根據 room 取得相關的 Device_List、Album 和 Landlord 記錄
					if (room!.device_list_id.HasValue)
					{
						Device_List? deviceList = await _context.Device_List.FindAsync(room.device_list_id);
						if (deviceList != null)
						{
							_context.Device_List.Remove(deviceList);
						}
					}

					if (room.album_id.HasValue)
					{
						Album? album = await _context.Album.FindAsync(room.album_id);
						if (album != null)
						{
							_context.Album.Remove(album);
						}
					}

					if (room.landlord_id.HasValue)
					{
						Landlord? landlord = await _context.Landlord.FindAsync(room.landlord_id);
						if (landlord != null)
						{
							_context.Landlord.Remove(landlord);
						}
					}

					// 最後刪除 Room 記錄
					_context.Room.Remove(room);
					await _context.SaveChangesAsync();
					await transaction.CommitAsync();
					return Json(new
					{
						success = true,
						color = "success",
						alertText = "刪除成功，頁面即將跳轉",
						show = true,
						time = 2000
					});
				}
				catch (Exception ex)
				{
					// 發生錯誤時回滾交易
					await transaction.RollbackAsync();

					// 可以將錯誤記錄到日誌，或者返回一個錯誤的提示
					Console.WriteLine($"Error occurred: {ex.Message}");
					return Json(new
					{
						success = false,
						color = "danger",
						alertText = "刪除失敗，請於稍後再次嘗試，謝謝。",
						show = true,
						time = 2000
					});
				}
			}
		}

		public IActionResult EditRoom()
		{
			string? memberID = getMemberID();
			if (string.IsNullOrEmpty(memberID) || !short.TryParse(memberID, out short userID))
			{
				return Redirect("/Home/Index");
			}
			AllRoomInfoDTO? userRoom = (from r in _context.Room
										join dl in _context.Device_List on r.device_list_id equals dl.device_list_id
										join l in _context.Landlord on r.landlord_id equals l.landlord_id
										join p in _context.Photo on r.album_id equals p.album_id into photogroup
										where r.member_id == userID
										select new AllRoomInfoDTO
										{
											room = r,
											deviceList = dl,
											landlord = l,
											photos = photogroup.ToList()
										}).FirstOrDefault();
			if (userRoom == null)
			{
				TempData["AlertModel"] = JsonConvert.SerializeObject(new PartialAlert
				{
					Color = "danger",
					AlertText = "目前尚未刊登房間，請於刊登後再前往此頁面。",
					Show = true,
					Time = 2000
				});

				return RedirectToAction("AddRoom");
			}

			userRoom.convertRoomPhoto();
			userRoom.convertConsentPhoto();
			return View(userRoom);
		}

		// 用來比對網頁請求夾帶參數與資料庫的值是否有差異，若有才會更新
		public void Save_Different(PropertyInfo prop, object oldObj, object newObj)
		{
			object? newValue = prop.GetValue(newObj);
			object? oldValue = prop.GetValue(oldObj);
			if (newValue != null && !Equals(newValue, oldValue))
			{
				prop.SetValue(oldObj, newValue);
			}
		}

		[HttpPut]
		public async Task<IActionResult> EditRoom(short roomId, Room room, Device_List list, Landlord owner, List<IFormFile> roomPhotos, IFormFile consentPhoto, List<string> existingPhotos, string existingConsent)
		{
			Console.WriteLine($"====================================={existingConsent}");
			using (IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					short userId = Convert.ToInt16(getMemberID());
					Room? existingRoom = await _context.Room.FirstOrDefaultAsync(r => r.room_id == roomId && r.member_id == userId);
					if (existingRoom == null)
					{
						return RedirectToAction("AddRoom", "Member");
					}
					// 獲取現有的資料
					existingRoom!.isEnabled = false;
					existingRoom.is_completed = false;
					if (existingRoom == null)
					{
						return Json(new
						{
							success = false,
							color = "danger",
							alertText = "找不到要更新的房間。",
							show = true,
							time = 2000
						});
					}
					foreach (PropertyInfo prop in room.GetType().GetProperties())
					{
						// 過濾掉虛擬屬性以及主鍵、外鍵
						if (prop.GetMethod!.IsVirtual || prop.Name == "room_id" || prop.Name == "album_id" || prop.Name == "device_list_id")
						{
							continue;
						}
						// 比對新舊值，不一樣才存進去
						Save_Different(prop, existingRoom, room);
					}

					// 更新 Device_List
					Device_List? existingList = await _context.Device_List.FindAsync(existingRoom.device_list_id);
					foreach (PropertyInfo prop in list.GetType().GetProperties())
					{
						if (prop.GetMethod!.IsVirtual || prop.Name == "device_list_id")
						{
							continue;
						}
						Save_Different(prop, existingList!, list);
					}

					// 更新 landlord
					Landlord? existingOwner = await _context.Landlord.FindAsync(existingRoom.landlord_id);
					foreach (PropertyInfo prop in owner.GetType().GetProperties())
					{
						// 過濾掉虛擬屬性以及主鍵、圖片
						if (prop.GetMethod!.IsVirtual || prop.Name == "landlord_id" || prop.Name == "consent_photo")
						{
							continue;
						}
						Save_Different(prop, existingOwner!, owner);
					}

					// 更新同意書照片
					if (consentPhoto != null && consentPhoto.Length > 0)
					{
						using (MemoryStream memoryStream = new MemoryStream())
						{
							await consentPhoto.CopyToAsync(memoryStream);
							existingOwner!.consent_photo = memoryStream.ToArray();
						}
					}
					else
					{
						// 讀取進來是null需要清空 (送到後端的 null 會被轉成"字串")
						if (existingConsent == "null")
						{
							existingOwner!.consent_photo = null;
						}
					}

					// 更新房間照片
					// 獲取現有的照片
					List<Photo> existingPhotosInDb = await _context.Photo
						.Where(p => p.album_id == existingRoom.album_id)
						.ToListAsync();

					foreach (Photo photo in existingPhotosInDb)
					{
						// 將照片從資料庫中的 byte[] 轉為 Base64 字串，並形成完整的 URL
						string photoUrl = $"data:{photo.photo_type};base64,{Convert.ToBase64String(photo.room_photo!)}";

						// 如果前端沒有傳回這張照片的 URL，則刪除這張照片
						if (!existingPhotos.Contains(photoUrl))
						{
							_context.Photo.Remove(photo);
						}
					}

					if (roomPhotos != null && roomPhotos.Count > 0)
					{
						foreach (IFormFile file in roomPhotos)
						{
							if (file.Length > 0)
							{
								using (MemoryStream memoryStream = new MemoryStream())
								{
									await file.CopyToAsync(memoryStream);
									byte[] imageBytes = memoryStream.ToArray();
									await _context.Photo.AddAsync(new Photo
									{
										room_photo = imageBytes,
										album_id = existingRoom.album_id,
										photo_type = GetMimeTypeFromImage(imageBytes)
									});
								}
							}
						}
					}

					// 最後保存所有變更並提交交易
					await _context.SaveChangesAsync();
					await transaction.CommitAsync();
					return Json(new
					{
						success = true,
						color = "success",
						alertText = "更新成功，請等待審核通過，屆時會再通知您。",
						show = true,
						time = 2000
					});
				}
				catch
				{
					await transaction.RollbackAsync();
					return Json(new
					{
						success = false,
						color = "danger",
						alertText = "更新失敗，請於稍後再次嘗試，謝謝。",
						show = true,
						time = 2000
					});
				}
			}
		}


		// ====================================房間頁面END=====================================
		//會員收藏
		public async Task<IActionResult> FavoriteRoom()
		{
			int? memberID = HttpContext.Session.GetInt32("FavoriteMemberID");
			IQueryable<Room> query = from x in _context.Favor_List
									 where x.member_id == memberID
									 join y in _context.Favorites on x.favor_list_id equals y.favor_list_id
									 join z in _context.Room on y.room_id equals z.room_id
									 select z;

			List<RoomViewModel> result = await query.Select(room => new RoomViewModel
			{
				Room = room,
				deviceList = _context.Device_List
						.Where(x => x.device_list_id == room.device_list_id)
						.Select(x => new DeviceList
						{
							canPet = x.keep_pet,
							canSmoking = x.smoking
						}).ToList(),
				roomImages = (from x in _context.Room
							  where x.room_id == room.room_id
							  join y in _context.Photo on x.album_id equals y.album_id
							  select new RoomImages
							  {
								  room_photo = y.room_photo,
								  photo_type = y.photo_type
							  }).ToList()
			}).ToListAsync();

			// 判斷是否有收藏的房間
			if (result.Count <= 0)
			{
				ViewBag.Message = "目前尚無收藏項目";
				return View(new List<RoomViewModel>());
			}

			// 將查詢結果傳遞到 View 中顯示
			return View(result);
		}
		//送會員編號
		[HttpPost]
		public IActionResult FavoriteMemberID(short memberID)
		{
			HttpContext.Session.SetInt32("FavoriteMemberID", memberID);
			return Ok();
		}
		//ADD收藏
		[HttpPost]
		public async Task<IActionResult> FavoriteRoom(short roomID, short memberID)
		{
			if (roomID == 0)
			{
				return Json(new { state = false, message = "無效的房間ID" });
			}
			//Favor_List要先有對應的會員編號
			//建立Favorites物件
			//找出對應的favor_list_id
			short favorListID = await (from x in _context.Favor_List
									   where x.member_id == memberID
									   select x.favor_list_id
								).FirstOrDefaultAsync();
			Favorites favorite = new Favorites
			{
				room_id = roomID,
				favor_list_id = favorListID
			};
			try
			{
				await _context.Favorites.AddAsync(favorite);
				await _context.SaveChangesAsync();
				return Json(new { state = true, message = "收藏成功" });
			}
			catch (Exception ex)
			{
				return Json(new { state = false, message = "收藏失敗" + ex.InnerException?.Message });
			}
		}
		[HttpPost]
		public async Task<IActionResult> RemoveFavoriteRoom(short roomID, short memberID)
		{
			Favorites? favorite = await (from x in _context.Favor_List
										 where x.member_id == memberID
										 join y in _context.Favorites on x.favor_list_id equals y.favor_list_id
										 where y.room_id == roomID
										 select y).FirstOrDefaultAsync();
			try
			{
				_context.Favorites.Remove(favorite);
				await _context.SaveChangesAsync();
				return Json(new { state = true, message = "成功刪除收藏" });
			}
			catch (Exception ex)
			{
				return Json(new { state = false, message = "刪除失敗" + ex.InnerException?.Message });
			}
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
