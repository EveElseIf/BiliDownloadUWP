using System;

namespace BiliDownload.Exceptions
{

    [Serializable]
    public class ParsingVideoException : Exception
    {
        public ParsingVideoException() { }
        public ParsingVideoException(string message) : base(message) { }
        public ParsingVideoException(string message, System.Exception inner) : base(message, inner) { }
        protected ParsingVideoException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
