using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Toolbelt.DynamicBinderExtension;

namespace BlazorChat.Server.Extensions
{
    public static class SignlrExtensions
    {
        public static IEnumerable<KeyValuePair<string, HubConnectionContext>> GetAllConnectedClients(this IHubCallerClients hub)
        {
            ConcurrentDictionary<string, HubConnectionContext> connections =
                hub.All.ToDynamic()
                   ._lifetimeManager
                   ._connections
                   ._connections;

            return connections.AsEnumerable();
        }
        public static IEnumerable<KeyValuePair<string,HubConnectionContext>> GetAllConnectedClients<T>(this IHubCallerClients<T> hub)
        {
            ConcurrentDictionary<string, HubConnectionContext> connections = 
                hub.All.ToDynamic()
                   ._proxy
                   ._lifetimeManager
                   ._connections
                   ._connections;
            return connections.AsEnumerable();
        }
    }
}
