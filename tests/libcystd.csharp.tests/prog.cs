using LibCyStd.Http;
using LibCyStd.Net;
using LibCyStd.Seq;
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
            while (true)
            {
                try
                {
                    var tasks = new List<Task<HttpResp>>();
                    foreach (var _ in Enumerable.Range(0, 1))
                    {
                        var req = new HttpReq("GET", "https://httpbin.org/get")
                        {
                            Cookies = ListUtils.OfSeq(new[] { new Cookie("name", "value", "/", ".", DateTimeOffset.MaxValue, false, false) }),
                            Proxy = Proxy.TryParse("http://localhost:8887"),
                            ProtocolVersion = HttpVersion.Http2
                        };
                        tasks.Add(HttpClient.RetrRespAsync(req));
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    Console.WriteLine("Made 10 requests");
                }
                catch (Exception e) when (e is InvalidOperationException || e is TimeoutException)
                {
                    Console.Error.WriteLine($"{e.GetType().Name} ~ {e.Message}");
                }
                await Task.Delay(2000).ConfigureAwait(false);
            }
        }

        private static void Main()
        {
            MainAsync().Wait();
        }
    }
}
