using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.DAB
{
    [Serializable]
    /// <summary>
    /// The DAB exception.
    /// </summary>
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

        [Obsolete( "This constructor is not needed to be implemented in the derived class since the base class Exception already implements ISerializable. This constructor can be removed in future versions.")]
        protected DABException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _message = info.GetString("Message");
        }

        /// <summary>
        /// Obsolete, not used anymore. The base class Exception already implements ISerializable and this method is not needed to be implemented in the derived class.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [Obsolete("This method is not needed to be implemented in the derived class since the base class Exception already implements ISerializable. This method can be removed in future versions.")]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Message", _message);
            info.AddValue("Exception", _exception);
        }
    }

    [Serializable]
    /// <summary>
    /// The no samples exception.
    /// </summary>
    public class NoSamplesException : DABException
    {
        public NoSamplesException() : base("No samples received")
        {}

        [Obsolete(" This constructor is not needed to be implemented in the derived class since the base class Exception already implements ISerializable. This constructor can be removed in future versions.")]
        protected NoSamplesException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {}
    }
}
