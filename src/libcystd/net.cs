using LibCyStd.LibOneOf.Types;
using System;

namespace LibCyStd.Net
{
    public class BasicNetworkCredentials
    {
        public string Username { get; }
        public string Password { get; }

        public override string ToString() => $"{Username}:{Password}";

        public BasicNetworkCredentials(in string username, in string password)
        {
            Username = username;
            Password = password;
        }

        public static Option<BasicNetworkCredentials> TryParse(in string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return None.Value;

            var sp = input.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (sp.Length != 2)
                return None.Value;

            var (username, pw) = (sp[0], sp[1]);
            return new BasicNetworkCredentials(username, pw);
        }
    }

    public class Proxy
    {
        public Uri Uri { get; }
        public Option<BasicNetworkCredentials> Credentials { get; }

        public Proxy(in Uri uri, in Option<BasicNetworkCredentials> credentials)
        {
            Uri = uri;
            Credentials = credentials;
        }

        public Proxy(in Uri uri, in BasicNetworkCredentials credentials) : this(uri, new Option<BasicNetworkCredentials>(credentials))
        {
        }

        public Proxy(in Uri uri) : this(uri, None.Value)
        {
        }

        public override string ToString()
        {
            return Uri.ToString();
        }

        public static Option<Proxy> TryParse(in string input)
        {
            if (!Uri.TryCreate(input, UriKind.Absolute, out var u)
                && !Uri.TryCreate($"http://{input}", UriKind.Absolute, out u))
            {
                return None.Value;
            }

            var cred = BasicNetworkCredentials.TryParse(u.UserInfo);
            return new Proxy(u, cred);
        }
    }
}
