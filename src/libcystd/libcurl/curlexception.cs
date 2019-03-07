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

        public CurlException(in string message, in CURLcode code) : base($"{message} ~ {code} ~ {CurlModule.CurlEzStrErr(code)}")
        {
            CurlCode = code;
            CurlErrMessage = CurlModule.CurlEzStrErr(code);
        }

        public CurlException(in string message) : base(message)
        {
            CurlCode = 0;
            CurlErrMessage = "";
        }

        public CurlException(in string message, in Exception innerException) : base(message, innerException)
        {
            CurlCode = 0;
            CurlErrMessage = "";
        }
    }
}
