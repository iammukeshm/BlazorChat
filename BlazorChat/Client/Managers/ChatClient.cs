using ChatClientApi;
using System.Net.Http;
using System.Text.Json;

namespace BlazorChat.Client.Managers
{
    public class ChatClient : ChatClientGenerated
    {
        public ChatClient(HttpClient httpClient) : base(httpClient)
        {
            // https://github.com/dotnet/aspnetcore/issues/21736
            base.ReadResponseAsString = true;
            base.JsonSerializerSettings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        }
    }


}