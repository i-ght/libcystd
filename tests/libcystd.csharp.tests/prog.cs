//using LibCyStd.Http;
using LibCyStd.Http;
using LibCyStd.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LibCyStd.CSharp.Tests
{
    internal static class Program
    {
        private static async Task MainAsync()
        {
            var cnt = 0;
            while (cnt++ < 5555)
            {
                try
                {
                    var tasks = new List<Task<HttpResp>>();
                    foreach (var _ in Enumerable.Range(0, 3))
                    {
                        var req = new HttpReq("GET", "https://httpbin.org/get")
                        {
                            //Cookies = ReadOnlyCollectionModule.OfSeq(new[] { new Cookie("name", "value", "/", ".", DateTimeOffset.MaxValue, false, false) }),
                            //Proxy = Proxy.TryParse("socks5://192.168.2.113:8889"),
                            ProxyRequired = false,
                            ProtocolVersion = HttpVersion.Http2,
                            //ContentBody = new ReadOnlyMemoryHttpContent(Encoding.UTF8.GetBytes("hello=werld"))
                        };
                        tasks.Add(HttpModule.RetrRespAsync(req));
                    }

                    var responses = await Task.WhenAll(tasks).ConfigureAwait(false);
                    foreach (var resp in responses)
                        resp.Dispose();
                }
                catch (Exception e) when (e is InvalidOperationException)
                {
                    Console.Error.WriteLine($"{e.GetType().Name} ~ {e.Message}");
                }
                await Task.Delay(5555).ConfigureAwait(false);
            }

            await Task.Delay(2000).ConfigureAwait(false);

            Console.ReadLine();
        }

        private static void Main()
        {
            _ = MainAsync();
            _ = MainAsync();
            _ = MainAsync();
            _ = MainAsync();
            _ = MainAsync();
            Console.In.Read();
        }
    }
}
