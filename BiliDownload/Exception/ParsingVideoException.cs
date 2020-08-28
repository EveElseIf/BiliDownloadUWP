using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliDownload.Exception
{

    [Serializable]
    public class ParsingVideoException : System.Exception
    {
        public ParsingVideoException() { }
        public ParsingVideoException(string message) : base(message) { }
        public ParsingVideoException(string message, System.Exception inner) : base(message, inner) { }
        protected ParsingVideoException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
