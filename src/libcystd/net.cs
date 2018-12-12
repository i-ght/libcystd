using Optional;
using System;

namespace LibCyStd.Net
{
    public class BasicNetworkCredentials
    {
        public string Username { get; }
        public string Password { get; }

        public BasicNetworkCredentials(in string username, in string password)
        {
            Username = username;
            Password = password;
        }

        public override string ToString() => $"{Username}:{Password}";

        public static Option<BasicNetworkCredentials> TryParse(in string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Option.None<BasicNetworkCredentials>();

            var sp = input.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (sp.Length != 2)
                return Option.None<BasicNetworkCredentials>();

            var (username, pw) = (sp[0], sp[1]);
            return new BasicNetworkCredentials(username, pw).Some();
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

        public Proxy(in Uri uri, in BasicNetworkCredentials credentials) : this(uri, credentials.Some())
        {
        }

        public Proxy(in Uri uri) : this(uri, Option.None<BasicNetworkCredentials>())
        {
        }

        public static Option<Proxy> TryParse(in string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Option.None<Proxy>();

            if (!Uri.TryCreate(input, UriKind.Absolute, out var u)
                && !Uri.TryCreate($"http://{input}", UriKind.Absolute, out u))
            {
                return Option.None<Proxy>();
            }

            var cred = BasicNetworkCredentials.TryParse(u.UserInfo);
            return new Proxy(u, cred).Some();
        }
    }
}
