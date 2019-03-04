using System;
using System.Collections.Generic;
using System.Text;

namespace LibCyStd.LibUv
{
    public class UvException : InvalidOperationException
    {
        public UvException() : base()
        {
        }

        public UvException(in string message) : base(message)
        {
        }

        public UvException(in string message, in Exception innerException) : base(message, innerException)
        {
        }
    }
}
