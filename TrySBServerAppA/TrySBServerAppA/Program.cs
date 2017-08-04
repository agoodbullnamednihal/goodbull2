using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Relay;

namespace TrySBServerAppA
{
    class Program
    {
        private const string RelayNamespace = @"tryrelayb.servicebus.windows.net";
        private const string ConnectionName = @"tryhybridconnb";
        private const string KeyName = @"RootManageSharedAccessKey";
        private const string Key = @"gNMx5NjtEFK8iJMxieTA6GIM9dMUpQD9D11vofUIils=";
        static void Main(string[] args)
        {
            RunAsync().GetAwaiter().GetResult();
        }

        private static async void ProcessMessagesOnConnection(HybridConnectionStream relayConnection, CancellationTokenSource cts)
        {
            Console.WriteLine("New Session");
            
            var reader = new StreamReader(relayConnection);
            var writer = new StreamWriter(relayConnection) { AutoFlush = true};

            while(!cts.IsCancellationRequested)
            {
                try
                {
                    var line = reader.ReadLine();

                    if(string.IsNullOrEmpty(line))
                    {
                        await relayConnection.ShutdownAsync(cts.Token);
                        break;
                    }

                    Console.WriteLine(line);
                    await writer.WriteLineAsync($"Echo: {line}");
                }
                catch (IOException ex)
                {
                    Console.WriteLine(ex.Message);
                    break;
                }
            }

            Console.WriteLine("End session");
            await relayConnection.CloseAsync(cts.Token);
        }

        private static async Task RunAsync()
        {
            var cts = new CancellationTokenSource();

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(KeyName, Key);
            var listener = new HybridConnectionListener(new Uri(string.Format("sb://{0}/{1}", RelayNamespace, ConnectionName)), tokenProvider);

            listener.Connecting += (o, e) => { Console.WriteLine("Connecting ..."); };
            listener.Offline += (o, e) => { Console.WriteLine("Offine"); };
            listener.Online += (o, e) => { Console.WriteLine("Onine"); };

            await listener.OpenAsync(cts.Token);
            Console.WriteLine("Server Listening ...");

            cts.Token.Register(() => listener.CloseAsync(CancellationToken.None));

            new Task(() => Console.In.ReadLineAsync().ContinueWith((s) => { cts.Cancel(); })).Start();

            while(true)
            {
                var relayConnection = await listener.AcceptConnectionAsync();
                if (relayConnection == null)
                    break;

                ProcessMessagesOnConnection(relayConnection, cts);
            }

            await listener.CloseAsync(cts.Token);
        }
    }
}
