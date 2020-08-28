using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliDownload.Model.Json
{
    public class SelfInfoJson : BiliJsonBase
    {
        public Data data { get; set; }
        public class Data
        {
            public long mid { get; set; }
            public string name { get; set; }
            public string face { get; set; } //头像url
            public VipStatus vip { get; set; }//大会员状态
        }
        public class VipStatus
        {
            public int type { get; set; }//0,无；1，月会员；2，年会员
            public int status { get; set; }//0，无会员；1，有会员
        }
    }
}
