using System;

namespace LibCyStd.LibCurl
{
    public class CurlException : InvalidOperationException
    {
        public CurlException(in string format, params object[] args) : base(string.Format(format, args))
        {
        }

        public CurlException(in CURLcode code) : base(code.ToString())
        {
        }

        public CurlException(in CURLMcode code) : base(code.ToString())
        {
        }

        public CurlException()
        {
        }

        public CurlException(in string message) : base(message)
        {
        }

        public CurlException(in string message, in Exception innerException) : base(message, innerException)
        {
        }
    }
}
