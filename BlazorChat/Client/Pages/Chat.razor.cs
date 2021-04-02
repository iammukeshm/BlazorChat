using BlazorChat.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorChat.Client.Pages
{
    public partial class Chat
    {
        [CascadingParameter] public HubConnection hubConnection { get; set; }
        [Parameter] public string CurrentMessage { get; set; }
        [Parameter] public string CurrentUserId { get; set; }
        [Parameter] public string CurrentUserEmail { get; set; }
        private List<ChatMessage> messages = new List<ChatMessage>();
        private async Task SubmitAsync()
        {
            if (!string.IsNullOrEmpty(CurrentMessage) && !string.IsNullOrEmpty(ContactId))
            {
                //Save Message to DB
                var chatHistory = new ChatMessage()
                {
                    Message = CurrentMessage,
                    ToUserId = ContactId,
                    CreatedDate = DateTime.Now

                };
                await _chatManager.SaveMessageAsync(chatHistory);
                chatHistory.FromUserId = CurrentUserId;
                await hubConnection.SendAsync("SendMessageAsync", chatHistory, CurrentUserEmail);
                CurrentMessage = string.Empty;
            }
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await _jsRuntime.InvokeAsync<string>("ScrollToBottom", "chatContainer");
        }
        protected override async Task OnInitializedAsync()
        {
            if (hubConnection == null)
            {
                hubConnection = new HubConnectionBuilder().WithUrl(_navigationManager.ToAbsoluteUri("/signalRHub")).Build();
            }
            if (hubConnection.State == HubConnectionState.Disconnected)
            {
                await hubConnection.StartAsync();
            }
            hubConnection.On<ChatMessage, string>("ReceiveMessage", async (message, userName) =>
            {
                if ((ContactId == message.ToUserId && CurrentUserId == message.FromUserId) || (ContactId == message.FromUserId && CurrentUserId == message.ToUserId))
                {

                    if ((ContactId == message.ToUserId && CurrentUserId == message.FromUserId))
                    {
                        messages.Add(new ChatMessage { Message = message.Message, CreatedDate = message.CreatedDate, FromUser = new ApplicationUser() { Email = CurrentUserEmail } });
                        await hubConnection.SendAsync("ChatNotificationAsync", $"New Message From {userName}", ContactId, CurrentUserId);
                    }
                    else if ((ContactId == message.FromUserId && CurrentUserId == message.ToUserId))
                    {
                        messages.Add(new ChatMessage { Message = message.Message, CreatedDate = message.CreatedDate, FromUser = new ApplicationUser() { Email = ContactEmail } });
                    }
                    await _jsRuntime.InvokeAsync<string>("ScrollToBottom", "chatContainer");
                    StateHasChanged();
                }
            });
            await GetUsersAsync();
            var state = await _stateProvider.GetAuthenticationStateAsync();
            var user = state.User;
            CurrentUserId = user.Claims.Where(a => a.Type == "sub").Select(a => a.Value).FirstOrDefault();
            CurrentUserEmail = user.Claims.Where(a => a.Type == "name").Select(a => a.Value).FirstOrDefault();
            if (!string.IsNullOrEmpty(ContactId))
            {
                await LoadUserChat(ContactId);
            }
        }
        public List<ApplicationUser> ChatUsers = new List<ApplicationUser>();
        [Parameter] public string ContactEmail { get; set; }
        [Parameter] public string ContactId { get; set; }
        async Task LoadUserChat(string userId)
        {
            var contact = await _chatManager.GetUserDetailsAsync(userId);
            ContactId = contact.Id;
            ContactEmail = contact.Email;
            _navigationManager.NavigateTo($"chat/{ContactId}");
            messages = new List<ChatMessage>();
            messages = await _chatManager.GetConversationAsync(ContactId);
        }
        private async Task GetUsersAsync()
        {
            ChatUsers = await _chatManager.GetUsersAsync();
        }
    }
}
