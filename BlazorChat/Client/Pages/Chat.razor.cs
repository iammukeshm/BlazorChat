using BlazorChat.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Logging;
using SignalR.Strong;
using System.Collections.Immutable;
using BlazorChat.Shared.Extensions;
using BlazorChat.Client.Managers;

namespace BlazorChat.Client.Pages
{
    public partial class Chat:IAsyncDisposable,IChatClientHub
    {
        [Inject] public ILogger<Chat> Logger { get; set; }
        [Inject] private IAccessTokenProvider TokenProvider { get; set; }
        [Inject] private ILoggerProvider LoggerProvider { get; set; }
        [Inject] private ChatClient ChatClient { get; set; }
        public HubConnection HubConnection { get; set; }
        [Parameter] public string CurrentMessage { get; set; }
        [Parameter] public string CurrentUserId { get; set; }
        [Parameter] public string CurrentUserEmail { get; set; }
        private ImmutableList<ChatMessage> messages = ImmutableList<ChatMessage>.Empty;
        private IChatServerHub _chatServerHub;
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
                chatHistory = await ChatClient.SaveMessageAsync(chatHistory);
                await _chatServerHub.SendMessageAsync(chatHistory, ContactId);
                CurrentMessage = string.Empty;
                messages = messages.Add(chatHistory);
            }
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await _jsRuntime.InvokeAsync<string>("ScrollToBottom", "chatContainer");
        }
        protected override async Task OnInitializedAsync()
        {
            var state = await _stateProvider.GetAuthenticationStateAsync();
            var user = state.User;
            CurrentUserId = user.Claims.Where(a => a.Type == "sub").Select(a => a.Value).FirstOrDefault();
            CurrentUserEmail = user.Claims.Where(a => a.Type == "name").Select(a => a.Value).FirstOrDefault();

            if (HubConnection == null)
            {
                Logger.LogWarning("hub created");
                HubConnection = new HubConnectionBuilder()
                    .WithUrl(_navigationManager.ToAbsoluteUri("/signalRHub"),
                    opt =>
                    {
                        opt.Transports = HttpTransportType.WebSockets;
                        opt.AccessTokenProvider = async () =>
                        {
                            var token = await TokenProvider.RequestAccessToken();
                            token.TryGetToken(out var accessToken);
                            Logger.LogWarning($"Token: {accessToken.Value}");
                             return accessToken.Value;
                        };
                    })
                    .ConfigureLogging( logging =>
                    {
                        logging.AddProvider(LoggerProvider);
                        logging.SetMinimumLevel(LogLevel.Debug);
                    })
                    .Build();
                await HubConnection.StartAsync();
                _chatServerHub = HubConnection.AsGeneratedHub<IChatServerHub>();
                HubConnection.RegisterSpoke<IChatClientHub>(this);
            }
            if (HubConnection.State == HubConnectionState.Disconnected)
            {
                await HubConnection.StartAsync();
            }
            await GetUsersAsync();
            if (!string.IsNullOrEmpty(ContactId))
            {
                await LoadUserChat(ContactId);
            }
        }
        public ImmutableList<ApplicationUser> ChatUsers = ImmutableList<ApplicationUser>.Empty;
        [Parameter] public string ContactEmail { get; set; }
        [Parameter] public string ContactId { get; set; }

        private async Task LoadUserChat(string userId)
        {
            var contact = await ChatClient.GetUserDetailsAsync(userId);
            ContactId = contact.Id;
            ContactEmail = contact.Email;
            _navigationManager.NavigateTo($"chat/{ContactId}");
             messages = await ChatClient
                              .GetConversationAsync(ContactId)
                              .ToImmutable();

            ChatUsers = ChatUsers.Update(
                x => x.Id == userId,
                x => x.UnreadCount = 0);

            await UpdateConversationStatus(userId, ChatStatus.Seen).ConfigureAwait(false);
        }
        private async Task UpdateConversationStatus(string userId,ChatStatus chatStatus)
        {
            await ChatClient
                .UpdateConversationStatusAsync(userId, ChatStatus.Seen)
                .ConfigureAwait(false);
            await _chatServerHub.UpdateConversationStatusAsync(userId, CurrentUserId, ChatStatus.Seen)
                .ConfigureAwait(false);
        }
        private async Task GetUsersAsync()
        {
            ChatUsers = await ChatClient
                              .GetUsersAsync()
                              .ToImmutable();
        }

        public async ValueTask DisposeAsync()
        {
            Logger.LogWarning("dispose");
            _snackBar?.Dispose();
            await HubConnection.StopAsync();
            await HubConnection.DisposeAsync();
        }

        public async Task ReceiveMessage(ChatMessage message)
        {
            if ((ContactId == message.ToUserId && CurrentUserId == message.FromUserId) || (ContactId == message.FromUserId && CurrentUserId == message.ToUserId))
            {

                if ((ContactId == message.ToUserId && CurrentUserId == message.FromUserId))
                {
                  messages=  messages.Add(message);
                    await _chatServerHub.ChatNotificationAsync($"New Message From {ChatUsers.FirstOrDefault(x=> x.Id == message.ToUserId)?.Email}", ContactId, CurrentUserId);
                    _ = UpdateMessageStatus(message.Id, ChatStatus.Delivered).ConfigureAwait(false);
                }
                else if ((ContactId == message.FromUserId && CurrentUserId == message.ToUserId))
                {
                    messages= messages.Add(message);
                    _ = UpdateMessageStatus(message.Id, ChatStatus.Seen).ConfigureAwait(false);
                }
                await _jsRuntime.InvokeAsync<string>("ScrollToBottom", "chatContainer");
 
            }
            else
            {
                ChatUsers = ChatUsers.Update(
                    x => x.Id == message.FromUserId,
                    x => ++x.UnreadCount);
                _ = UpdateMessageStatus(message.Id, ChatStatus.Delivered,message.FromUserId).ConfigureAwait(false);
            }
            StateHasChanged();
        }
        private Task UpdateMessageStatus(long id, ChatStatus status, string fromUserId = null)
        {
            _ = ChatClient
                        .UpdateMsgStatusAsync(id,(int)status)
                        .ConfigureAwait(false);
            _ = _chatServerHub
                        .UpdateMessageStatusAsync(id,fromUserId??ContactId, status)
                        .ConfigureAwait(false);
            return Task.CompletedTask;
        }
        public async Task ReceiveChatNotification(string message, string senderUserId)
        {
            await _jsRuntime.InvokeAsync<string>("PlayAudio", "notification");
            _snackBar.Add(message, Severity.Info, config =>
            {
                config.VisibleStateDuration = 10000;
                config.HideTransitionDuration = 500;
                config.ShowTransitionDuration = 500;
                config.Action = "Chat?";
                config.ActionColor = Color.Info;
                config.Onclick = _ =>
                {
                    _navigationManager.NavigateTo($"chat/{senderUserId}");
                    return Task.CompletedTask;
                };
            });
        }

        public Task ReceiveConversationStatus(string userId, ChatStatus status)
        {
            if (ContactId == userId)
            {
                messages.ForEach(x=> x.Status = status);
                StateHasChanged();
            }

            return Task.CompletedTask;
        }

        public Task ReceiveMessageStatus(long msgId, ChatStatus status)
        {
            if (!ContactId.IsNullEmpty())
            {
                messages = messages.Update(
                     x => x.Id == msgId,
                     x => x.Status = status);
                StateHasChanged();
            }

            return Task.CompletedTask;
        }
    }
}
