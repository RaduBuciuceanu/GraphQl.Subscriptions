﻿using System;
using System.IO;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GraphQl.Subscriptions.Core;
using Newtonsoft.Json;

namespace GraphQL.Subscriptions.WebSockets
{
    internal class Receive : IReceive
    {
        private readonly WebSocket _socket;

        public Receive(WebSocket socket)
        {
            _socket = socket;
        }

        public IObservable<TDto> Execute<TDto>()
        {
            return Observable.Return(ExecuteAsync().GetAwaiter().GetResult())
                .Select(JsonConvert.DeserializeObject<TDto>);
        }

        private async Task<string> ExecuteAsync()
        {
            using (var memoryStream = new MemoryStream())
            {
                await PopulateStream(memoryStream);
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        private async Task PopulateStream(Stream stream)
        {
            WebSocketReceiveResult receiveResult;
            var segment = new ArraySegment<byte>(new byte[1024 * 4]);

            do
            {
                receiveResult = await _socket.ReceiveAsync(segment, CancellationToken.None);

                if (receiveResult.Count == 0)
                {
                    continue;
                }

                await stream.WriteAsync(segment.Array, segment.Offset, receiveResult.Count);
            } while (!receiveResult.EndOfMessage || stream.Length == 0);
        }
    }
}
