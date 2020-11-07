using BiliDownload.Exceptions;
using BiliDownload.Helper;
using BiliDownload.Helpers;
using BiliDownload.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        public string SESSDATA { get => ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] as string; }
        private BiliMangaMaster master;
        private ObservableCollection<MangaDownloadViewModel> downloadList = new ObservableCollection<MangaDownloadViewModel>();
        public MangaDownloadPage()
        {
            this.InitializeComponent();
            this.mangaDownloadListView.ItemsSource = downloadList;
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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as MangaViewModel;
            var download = new MangaDownloadViewModel()
            {
                Epid = vm.Epid,
                Mcid = this.master.Mcid,
                Status = "准备中",
                Title = vm.Title
            };
            try
            {
                download.UrlList = await MangaDownloadHelper.GetPicUrlsAsync(this.master.Mcid, vm.Epid, SESSDATA);
            }
            catch (MangaEpisodeNeedBuyException)
            {
                download.UrlList = null;
            }
            catch
            {

            }
            downloadList.Add(download);
            this.pivot.SelectedIndex = 1;
            await download.StartDownloadAsync();
        }

        private async void downloadSelectedBtn_Click(object sender, RoutedEventArgs e)
        {
            var taskList = new List<Task>();
            foreach (var item in this.mangaListView.ItemsSource as ObservableCollection<MangaViewModel>)
            {
                if (!item.ToDownload) continue;
                var download = new MangaDownloadViewModel()
                {
                    Epid = item.Epid,
                    Mcid = this.master.Mcid,
                    Status = "准备中",
                    Title = item.Title
                };
                try
                {
                    download.UrlList = await MangaDownloadHelper.GetPicUrlsAsync(this.master.Mcid, item.Epid, SESSDATA);
                }
                catch (MangaEpisodeNeedBuyException)
                {
                    download.UrlList = null;
                }
                catch
                {

                }
                downloadList.Add(download);
                taskList.Add(download.StartDownloadAsync());
            }
            this.pivot.SelectedIndex = 1;
            await Task.WhenAll(taskList);
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
            set { toDownload = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ToDownload")); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
    }
    public class MangaDownloadViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string Title { get; set; }
        public int Epid { get; set; }
        public int Mcid { get; set; }
        public List<string> UrlList { get; set; }
        private string status;

        public string Status
        {
            get { return status; }
            set { status = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Status")); }
        }
        public async Task StartDownloadAsync()
        {
            if (UrlList == null)
            {
                this.Status = $"需要购买";
                return;
            }
            var title = Title.Replace("\\", "").Replace("/", "").Replace(":", "").Replace("*", "")
            .Replace("?", "").Replace("\"", "").Replace("<", "").Replace(">", "").Replace("|", "");
            var parentfolder = await StorageFolder.GetFolderFromPathAsync(ApplicationData.Current.LocalSettings.Values["downloadPath"] as string);
            var folder = await parentfolder.CreateFolderAsync($"{title}(mc{Mcid}.ep{Epid})", CreationCollisionOption.ReplaceExisting);
            var client = new HttpClient();
            var i = 0;
            foreach (var item in UrlList)
            {
                this.Status = $"开始下载第{i}张图片";
                var fileStream = await (await folder.CreateFileAsync
                                    ($"{i}.jpg", CreationCollisionOption.ReplaceExisting)).OpenStreamForWriteAsync();
                await (await client.GetStreamAsync(item)).CopyToAsync(fileStream);
                fileStream.Dispose();
                this.Status = $"第{i}张图片下载完成";
                i++;
            }
            this.Status = "全部下载完成";
        }

    }
}
