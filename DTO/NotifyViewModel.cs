using Zugether.Models;

namespace Zugether.DTO
{
	public class NotifyViewModel
	{
		public Room? Room { get; set; }
		public Member? Member { get; set; }
		public PartialViewModel? PartialViewModel { get; set; }

		//用來儲存抓回的所有資料
		public List<dynamic> GroupedNotifications { get; set; }

		//1112 新增針對邀請者的內容Class
		public List<InviteNotification>? InviteNotification { get; set; } // 通知清單
		public Dictionary<short, Member>? InviterMembers { get; set; } // 邀請人的詳細資料
		public Dictionary<short, Member>? InviteeMembers { get; set; } // 受邀請的詳細資料

		// 11/14新增 用來抓房間相關資料
		public RoomInfo? RoomInfo { get; set; }  // 新增 RoomInfo 屬性

		// 11/19新增的屬性
		public int AgreedCount { get; set; }
		public int? RoommateNum { get; set; }
		public bool ShowConfirmButton { get; set; }
		public bool CanAgreeBtn { get; set; }


		// 新增邀請通知請求資料
		public InviteRequest? InviteRequest { get; set; }  // 將 InviterID 和 RoomID 儲存在這裡

		public InviteNotificationRequest? InviteNotificationRequest { get; set; }

		//// 新增房間狀態(上下架 ||已完成)相關的屬性
		public string? RoomStatusMessage { get; set; }
		public bool? RoomIsEnabled { get; set; }
		public bool? RoomIsCompleted { get; set; }
	}
}

public class RoomInfo
{
	// 11/14 新增 HasPublishedRoom 屬性來表示是否有刊登房間
	public bool RoomExists { get; set; } //抓資料庫是否有房間
	public byte? RoommateNum { get; set; } //用以存放該登入會員的需求室友人數

	public bool IsCompleted { get; set; } // is_completed
	public bool IsEnabled { get; set; } // isEnabled

	public int AgreedCount { get; set; }
}

//以下為用在搜尋房間的詳細頁面
// 新的 InviteRequest 類別(用在發出邀約的按鈕功能)
public class InviteRequest
{
	public int RoomID { get; set; }
	public short InviterID { get; set; }

	public int MemberID { get; set; }
	public int PublisherID { get; set; }
	public string Date { get; set; }
}

// 為避免資料庫代碼洩漏，特別使用處理前端傳來的通知資料的類別。
public class InviteNotificationRequest
{
	public int NotifyId { get; set; }
	public int InviterMemberId { get; set; }
	public int InviteeMemberId { get; set; }
	public string NotifyStatus { get; set; }
	public bool IsFinalized { get; set; }
	public DateTime NotifyDate { get; set; }
}

