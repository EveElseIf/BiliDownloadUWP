using Windows.UI.Xaml.Media.Imaging;

namespace BiliDownload.Models
{
    public class BiliUser
    {
        public string Name { get; set; }
        public long Uid { get; set; }
        public string AvatarUrl { get; set; }
        public BitmapSource AvatarImg { get; set; }
        public VipStatus VipStatus { get; set; }

    }
    public enum VipStatus
    {
        IsNotVip = 0,
        IsVip = 1
    }
}
