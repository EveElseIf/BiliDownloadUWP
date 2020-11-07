using System.Collections.Generic;

namespace BiliDownload.Model.Json
{
    public class FavListJson : BiliJsonBase
    {
        public Data data { get; set; }
        public class Data
        {
            public int count { get; set; }
            public List<Fav> list { get; set; }
        }
        public class Fav
        {
            public int id { get; set; }
            public string title { get; set; }
        }
    }
}
