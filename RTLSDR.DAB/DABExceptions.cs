using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.DAB
{
    [Serializable]
    public class DABException : Exception, ISerializable
    {
        private Exception? _exception;
        private string? _message;

        public DABException(Exception ex)
        {
            _exception = ex;
            _message = ex.Message;
        }

        public DABException(string message, Exception ex)
        {
            _exception = ex;
            _message = message;
        }

        public DABException(string message)
        {
            _message = message;
        }
    }

    [Serializable]
    public class NoSamplesException : DABException
    {
        public NoSamplesException() : base("No samples received")
        {}
    }
}
