using LibCyStd.Http;
using LibCyStd.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
                        var req = new HttpReq("GET", "https://httpbin.org/cookies/set?name1=val1")
                        {
                            Proxy = Proxy.TryParse("socks5://192.168.2.112:8889")
                        };
                        tasks.Add(HttpClient.RetrRespAsync(req));
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"{e.GetType().Name} ~ {e.Message}");
                }

                await Task.Delay(5000).ConfigureAwait(false);
            }
        }

        private static void Main()
        {
            MainAsync().Wait();
        }
    }
}
