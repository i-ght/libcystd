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
        private const string Key0 = "BXD+XTATh$j+T90hgZl1ulufJf%Df%-!";
        private const string Iv0 = "qZ%/=j2AZ#k5il19";

        private static char a(char arg1, int arg2)
        {
            int v1 = ("tZ*f7>#n)q~Q1z_ID@ewN;bJAgYsk%U8l]W-^3E/R&O.Sco<6u!MxTj ?P0(rHCGd$XiBh{v4+9|K}m2,V:p=F5y[aL".IndexOf(arg1) + arg2) % "tZ*f7>#n)q~Q1z_ID@ewN;bJAgYsk%U8l]W-^3E/R&O.Sco<6u!MxTj ?P0(rHCGd$XiBh{v4+9|K}m2,V:p=F5y[aL".Length;
            if (v1 < 0)
            {
                v1 += "tZ*f7>#n)q~Q1z_ID@ewN;bJAgYsk%U8l]W-^3E/R&O.Sco<6u!MxTj ?P0(rHCGd$XiBh{v4+9|K}m2,V:p=F5y[aL".Length;
            }

            return "tZ*f7>#n)q~Q1z_ID@ewN;bJAgYsk%U8l]W-^3E/R&O.Sco<6u!MxTj ?P0(rHCGd$XiBh{v4+9|K}m2,V:p=F5y[aL"[v1];
        }

        private static String d(String arg3)
        {
            var arg4 = -(906 % 17);
            String v0 = "";
            int v1;
            for (v1 = 0; v1 < arg3.Length; ++v1)
            {
                v0 = v0 + a(arg3[v1], arg4);
            }

            return v0;
        }

        private static ReadOnlyMemory<byte> AesDecrypt(
            in ReadOnlyMemory<byte> key,
            in ReadOnlyMemory<byte> iv,
            in ReadOnlyMemory<byte> input)
        {
            using var aes = new AesCryptoServiceProvider
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
            using var decr = aes.CreateDecryptor(key.AsArraySeg().Array, iv.AsArraySeg().Array);
            var inpArrSeg = input.AsArraySeg();
            return decr.TransformFinalBlock(inpArrSeg.Array, inpArrSeg.Offset, inpArrSeg.Count);
        }

        private static ReadOnlyMemory<byte> DecodeUrlSafeBase64(string s)
        {
            var padding = new[] { '=' };
            var inc = s.Replace('_', '/').Replace('-', '+');
            switch (inc.Length % 4)
            {
                case 2: inc += "=="; break;
                case 3: inc += "="; break;
            }
            return Convert.FromBase64String(inc);
        }

        private static void AESDecrypt()
        {
            var key0 = d(Key0);
            var iv0 = d(Iv0);
            ReadOnlyMemory<byte> base64DecKey0 = Encoding.UTF8.GetBytes(key0);
            ReadOnlyMemory<byte> base64DecIv0 = Encoding.UTF8.GetBytes(iv0);
            var inp0 = "C2wTM6jotOm9U9IUHcdwGnMhFHvumragG53aqZJKRXW7T75W_11WzzDr39X7OJbC";
            ReadOnlyMemory<byte> inp0Base64Dec = DecodeUrlSafeBase64(inp0);
            var dec0 = AesDecrypt(base64DecKey0, base64DecIv0, inp0Base64Dec);
            var dec0Str = Encoding.UTF8.GetString(dec0.AsArraySeg().Array);
            Console.WriteLine(dec0Str);

            var inp1 = "YbZTbjLF7r0nEfYSDSao1wbpcpC-75t6QpkCzRNh5os=";
            ReadOnlyMemory<byte> inp1Base64Dec = DecodeUrlSafeBase64(inp1);
            var dec1 = AesDecrypt(base64DecKey0, base64DecIv0, inp1Base64Dec);
            var dec1Str = Encoding.UTF8.GetString(dec1.AsArraySeg().Array);
            Console.WriteLine(dec1Str);

            var inp2 = "C2wTM6jotOm9U9IUHcdwGnMhFHvumragG53aqZJKRXW7T75W_11WzzDr39X7OJbC";
            ReadOnlyMemory<byte> inp2Base64Dec = DecodeUrlSafeBase64(inp2);
            var dec2 = AesDecrypt(base64DecKey0, base64DecIv0, inp2Base64Dec);
            var dec2Str = Encoding.UTF8.GetString(dec2.AsArraySeg().Array);
            Console.WriteLine(dec2Str);

            var inp3 = "XNw-PrabPfOD2sJtc_Tg1T9T8M-KgTjf5vTnF3z7aOg=";
            ReadOnlyMemory<byte> inp3Base64Dec = DecodeUrlSafeBase64(inp3);
            var dec3 = AesDecrypt(base64DecKey0, base64DecIv0, inp3Base64Dec);
            var dec3Str = Encoding.UTF8.GetString(dec3.AsArraySeg().Array);
            Console.WriteLine(dec3Str);

        }

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
                        var req = new HttpReq("GET", "https://httpbin.org/")
                        {
                            //Cookies = ReadOnlyCollectionModule.OfSeq(new[] { new Cookie("name", "value", "/", ".", DateTimeOffset.MaxValue, false, false) }),
                            Proxy = Proxy.TryParse("socks5://192.168.2.112:8889"),
                            ProtocolVersion = HttpVersion.Http2,
                            //ContentBody = new ReadOnlyMemoryHttpContent(Encoding.UTF8.GetBytes("hello=werld"))
                        };
                        tasks.Add(HttpModule.RetrRespAsync(req));
                    }

                    var responses = await Task.WhenAll(tasks).ConfigureAwait(false);
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
            //046d21f51d8861af
            var bytes = RandomModule.NextBytes(8);
            var h = bytes.ToHex();

            MainAsync().Wait();
        }
    }
}
