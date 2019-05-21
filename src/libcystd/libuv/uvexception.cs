using System;

namespace LibCyStd.LibUv
{
    public class UvException : InvalidOperationException
    {
        public UvException() : base()
        {
        }

        public UvException(string message) : base(message)
        {
        }

        public UvException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
