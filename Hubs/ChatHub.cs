using Microsoft.AspNetCore.SignalR;

namespace Zugether.Hubs
{
	public class ChatHub : Hub
	{
		public async Task SendMessage(string user, string message, string searchMemberIdToAvatar, string newBasement)
		{
			try
			{
				Console.WriteLine($"接收到訊息: {user} - {message}");
				await Clients.All.SendAsync("NewMessage", user, message, searchMemberIdToAvatar, newBasement);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"發送消息失敗: {ex.Message}");
			}
		}
		public async Task EditMessage(string inputMessage, string currentBasement, string memberID, string editMessageTime)
		{
			try
			{
				Console.WriteLine($"接收到訊息: {inputMessage} - {currentBasement}");
				await Clients.All.SendAsync("EditMessage", inputMessage, currentBasement, memberID, editMessageTime);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"發送消息失敗: {ex.Message}");
			}
		}
	}
}
