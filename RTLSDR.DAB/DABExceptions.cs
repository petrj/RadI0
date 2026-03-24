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

        public DABException(SerializationInfo info, StreamingContext context)
        {
            _message = info.GetString("Message");
        }

        /// <summary>
        /// Obsolete, not used anymore. The base class Exception already implements ISerializable and this method is not needed to be implemented in the derived class.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [Obsolete]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Message", _message);
            info.AddValue("Exception", _exception);
        }
    }

    [Serializable]
    public class NoSamplesException : DABException
    {
        public NoSamplesException() : base("No samples received")
        {}
    }
}
