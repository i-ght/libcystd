using System;

namespace LibCyStd.LibCurl
{
    public class CurlException : InvalidOperationException
    {
        public CURLcode CurlCode { get; }
        public string CurlErrMessage { get; }

        public CurlException()
        {
            CurlCode = 0;
            CurlErrMessage = "";
        }

        public CurlException(string message, CURLcode code) : base($"{message} ~ {code} ~ {CurlModule.CurlEzStrErr(code)}")
        {
            CurlCode = code;
            CurlErrMessage = CurlModule.CurlEzStrErr(code);
        }

        public CurlException(string message) : base(message)
        {
            CurlCode = 0;
            CurlErrMessage = "";
        }

        public CurlException(string message, Exception innerException) : base(message, innerException)
        {
            CurlCode = 0;
            CurlErrMessage = "";
        }
    }
}
