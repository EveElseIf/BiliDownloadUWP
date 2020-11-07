using System;

namespace BiliDownload.Exceptions
{

    [Serializable]
    public class MangaEpisodeNeedBuyException : Exception
    {
        public MangaEpisodeNeedBuyException() { }
        public MangaEpisodeNeedBuyException(string message) : base(message) { }
        public MangaEpisodeNeedBuyException(string message, Exception inner) : base(message, inner) { }
        protected MangaEpisodeNeedBuyException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
