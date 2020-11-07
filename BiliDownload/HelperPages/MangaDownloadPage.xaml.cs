using BiliDownload.Helper;
using BiliDownload.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BiliDownload.HelperPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MangaDownloadPage : Page
    {
        private BiliMangaMaster master;
        public MangaDownloadPage()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (!(e.Parameter is BiliMangaMaster)) return;
            this.master = e.Parameter as BiliMangaMaster;
            this.mangaListView.ItemsSource = new ObservableCollection<MangaViewModel>(master.EpList.OrderBy(m => m.Order).Select(m => new MangaViewModel()
            {
                Epid = m.Epid,
                Title = m.Title,
                CoverUrl = m.CoverUrl,
                ToDownload = false
            }));
            _ = SetCoverImage(this.master.VerticalCoverUrl);
        }
        private async Task SetCoverImage(string url)
        {
            var stream = await NetHelper.HttpGetStreamAsync(url, null, null);
            var file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("mangacovercache", CreationCollisionOption.GenerateUniqueName);
            var fileStream = await file.OpenStreamForWriteAsync();
            await stream.CopyToAsync(fileStream);
            stream.Close();
            fileStream.Close();
            var img = new BitmapImage();
            await img.SetSourceAsync((await file.OpenStreamForReadAsync()).AsRandomAccessStream());
            this.mangaCoverImage.Source = img;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void downloadSelectedBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            await SearchPage.Current.CloseMangaWindow(this.master);
        }
        private void checkAllBox_Click(object sender, RoutedEventArgs e)
        {
            foreach (var m in (this.mangaListView.ItemsSource as ObservableCollection<MangaViewModel>))
            {
                m.ToDownload = (bool)this.checkAllBox.IsChecked;
            }
        }
    }
    public class MangaViewModel : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public int Epid { get; set; }
        public string CoverUrl { get; set; }
        private bool toDownload;

        public bool ToDownload
        {
            get { return toDownload; }
            set { toDownload = value;this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ToDownload")); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
    }
}
