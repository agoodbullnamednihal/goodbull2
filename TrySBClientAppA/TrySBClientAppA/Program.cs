using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Relay;

namespace TrySBClientAppA
{
    class Program
    {
        private const string RelayNamespace = @"yourrelay.servicebus.windows.net";
        private const string ConnectionName = @"yourconnection";
        private const string KeyName = @"yourKey";
        private const string Key = @"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

        static void Main(string[] args)
        {
            RunAsync().GetAwaiter().GetResult();
        }

        private static async Task RunAsync()
        {
            Console.WriteLine("Enter lines of text to send to the server with ENTER");

            
            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(KeyName, Key);
            var client = new HybridConnectionClient(new Uri(String.Format("sb://{0}/{1}", RelayNamespace, ConnectionName)), tokenProvider);

            
            var relayConnection = await client.CreateConnectionAsync();
            var reads = Task.Run(async () => {
                
                var reader = new StreamReader(relayConnection);
                var writer = Console.Out;
                do
                {
                    string line = await reader.ReadLineAsync();
                    
                    if (String.IsNullOrEmpty(line))
                        break;
                    
                    await writer.WriteLineAsync(line);
                }
                while (true);
            });

            var writes = Task.Run(async () => {
                var reader = Console.In;
                var writer = new StreamWriter(relayConnection) { AutoFlush = true };
                do
                {
                    string line = await reader.ReadLineAsync();
                    await writer.WriteLineAsync(line);
                    if (String.IsNullOrEmpty(line))
                        break;
                }
                while (true);
            });

            await Task.WhenAll(reads, writes);
            await relayConnection.CloseAsync(CancellationToken.None);
        }
    }
}

