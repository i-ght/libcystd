//using LibCyStd.Http;
using LibCyStd.Http;
using LibCyStd.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LibCyStd.CSharp.Tests
{
    internal static class Program
    {
        private static async Task MainAsync()
        {
            var cnt = 0;
            while (cnt++ < 1)
            {
                try
                {
                    var tasks = new List<Task<HttpResp>>();
                    foreach (var _ in Enumerable.Range(0, 1))
                    {
                        var req = new HttpReq("GET", "https://aol.com/")
                        {
                            //Cookies = ListModule.OfSeq(new[] { new Cookie("name", "value", "/", ".", DateTimeOffset.MaxValue, false, false) }),
                            Proxy = Proxy.TryParse("socks5://192.168.2.112:8889"),
                            ProtocolVersion = HttpVersion.Http2,
                            KeepAlive = true,
                            Timeout = TimeSpan.FromMilliseconds(1.0)
                            //ContentBody = new ReadOnlyMemoryHttpContent(Encoding.UTF8.GetBytes("hello=werld"))
                        };
                        tasks.Add(HttpModule.RetrRespAsync(req));
                    }

                    var responses = await Task.WhenAll(tasks).ConfigureAwait(false);
                    //sConsole.WriteLine("Made 10 requests");
                }
                catch (Exception e) when (e is InvalidOperationException)
                {
                    Console.Error.WriteLine($"{e.GetType().Name} ~ {e.Message}");
                }
                await Task.Delay(5555).ConfigureAwait(false);
            }

            await Task.Delay(2000).ConfigureAwait(false);
#if DEBUG
            HttpModule.Agent.Dispose();
#endif
            Console.ReadLine();
        }

        private static void Main()
        {
            MainAsync().Wait();
        }
    }
}
