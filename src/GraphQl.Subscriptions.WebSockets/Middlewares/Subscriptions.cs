﻿using System;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Subscriptions.WebSockets.Middlewares
{
    internal class Subscriptions
    {
        private readonly RequestDelegate _next;
        private readonly string _path;
        private readonly string _protocol;

        public Subscriptions(RequestDelegate next, string path, string protocol)
        {
            _next = next;
            _path = path;
            _protocol = protocol;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path == _path && context.WebSockets.IsWebSocketRequest)
            {
                WebSocket socket = await context.WebSockets.AcceptWebSocketAsync(_protocol).ConfigureAwait(false);
                StartCommunication(socket, context.RequestServices);
            }
            else
            {
                await _next(context);
            }
        }

        private static void StartCommunication(WebSocket socket, IServiceProvider provider)
        {
            using (socket)
            {
                Communicate communication = BuildCommunication(socket, provider);
                communication.Execute().Wait();
            }
        }

        private static Communicate BuildCommunication(WebSocket socket, IServiceProvider provider)
        {
            var schema = provider.GetService<Schema>();
            var shouldReceive = new ShouldReceive(socket);
            var receive = new Receive(socket);
            var send = new Send(socket);
            return new Communicate(schema, shouldReceive, receive, send);
        }
    }
}
