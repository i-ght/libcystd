//using LibCyStd.Http;
using LibCyStd.Http;
using LibCyStd.Net;
using LibCyStd.Seq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibCyStd.CSharp.Tests
{
    internal static class Program
    {
        private static async Task MainAsync()
        {
            var cnt = 0;
            while (cnt++ < 5)
            {
                try
                {
                    var tasks = new List<Task<HttpResp>>();
                    foreach (var _ in Enumerable.Range(0, 5))
                    {
                        var req = new HttpReq("GET", "https://httpbin.org/get")
                        {
                            Cookies = ListModule.OfSeq(new[] { new Cookie("name", "value", "/", ".", DateTimeOffset.MaxValue, false, false) }),
                            Proxy = Proxy.TryParse("socks5://192.168.2.112:8889"),
                            ProtocolVersion = HttpVersion.Http2,
                            ContentBody = new ReadOnlyMemoryHttpContent(Encoding.UTF8.GetBytes("hello=werld"))
                        };
                        var s = req.ToString();
                        tasks.Add(HttpModule.RetrRespAsync(req));
                    }

                    var responses = await Task.WhenAll(tasks).ConfigureAwait(false);
                    Console.WriteLine("Made 10 requests");
                }
                catch (Exception e) when (e is InvalidOperationException || e is TimeoutException)
                {
                    Console.Error.WriteLine($"{e.GetType().Name} ~ {e.Message}");
                }
                await Task.Delay(2000).ConfigureAwait(false);
            }

            await Task.Delay(5000).ConfigureAwait(false);
            HttpModule.Agent.Dispose();
        }

        private static void Main()
        {
            MainAsync().Wait();
        }
    }
}
