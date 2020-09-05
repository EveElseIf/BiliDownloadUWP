using BiliDownload.Exception;
using BiliDownload.Helper;
using BiliDownload.Model;
using BiliDownload.Model.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace BiliDownload.SearchDialogs
{
    public sealed partial class BangumiDialog : ContentDialog
    {
        BangumiDialogViewModel vm;
        bool needToClose = false;
        private BangumiDialog(BangumiDialogViewModel vm)
        {
            this.vm = vm;
            this.DataContext = vm;
            InitializeComboBoxData(vm);
            this.InitializeComponent();
            this.Closing += (s, e) =>
            {
                if (!needToClose) e.Cancel = true;
            };
        }
        private void InitializeComboBoxData(BangumiDialogViewModel vm)
        {
            var list = new List<BangumiDialogViewModel.ComboBoxData>()
            {
                new BangumiDialogViewModel.ComboBoxData("360P 流畅",16 ),
                new BangumiDialogViewModel.ComboBoxData("480P 清晰",32 ),
                new BangumiDialogViewModel.ComboBoxData("720P 高清(需要登录)",64 ),
                new BangumiDialogViewModel.ComboBoxData("720P60 高清(大会员)" ,74),
                new BangumiDialogViewModel.ComboBoxData("1080P 高清 (需要登录)" ,80),
                new BangumiDialogViewModel.ComboBoxData("1080P+ 高清(大会员)" ,112),
                new BangumiDialogViewModel.ComboBoxData("1080P60 高清(大会员)" ,116),
                //new BangumiDialogViewModel.ComboBoxData("4K 超清(大会员)" ,120)
            };
            vm.ComboBoxDataList = list;
        }
        public static async Task<BangumiDialog> CreateAsync(BiliBangumi bangumi)
        {
            var model = new BangumiDialogViewModel();
            model.VideoTitle = bangumi.Title;

            var stream = await NetHelper.HttpGetStreamAsync(bangumi.Cover, null, null);
            var file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("videocovercache", CreationCollisionOption.GenerateUniqueName);
            var fileStream = await file.OpenStreamForWriteAsync();
            await stream.CopyToAsync(fileStream);
            stream.Close();
            fileStream.Close();
            var img = new BitmapImage();
            await img.SetSourceAsync((await file.OpenStreamForReadAsync()).AsRandomAccessStream());
            model.VideoCover = img;

            var list = bangumi.EpisodeList.Select(v => new VideoInfo()
            {
                Info = new BiliVideoInfo()
                {
                    Bv = v.Bv,
                    Cid = v.Cid,
                    Name = v.Name,
                    CoverUrl = v.Cover
                },
                ToDownload = false,
                BackGroundColorBrush = bangumi.EpisodeList.IndexOf(v) % 2 == 0
                ? new SolidColorBrush(Colors.WhiteSmoke) : new SolidColorBrush(Colors.White)
            }).ToList();
            var i = 1;
            foreach (var video in list)//添加序号
            {
                video.Info.Name = i + "." + video.Info.Name;
                i++;
            }
            model.VideoList = new ObservableCollection<VideoInfo>(list);

            return new BangumiDialog(model);
        }
        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var sESSDATA = ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] as string;

            var list = new List<BiliVideoInfo>();
            foreach (var item in vm.VideoList)
            {
                if (item.ToDownload == false) continue;
                list.Add(item.Info);
            }
            if (list.Count < 1)
            {
                PrimaryButtonText = "请选择至少一个视频";
                await Task.Delay(2000);
                if (PrimaryButtonText == "请选择至少一个视频")
                    PrimaryButtonText = "下载所选项";
                return;
            }
            if (this.qualityComboBox.SelectedValue == null)
            {
                PrimaryButtonText = "请选择清晰度";
                await Task.Delay(2000);
                if (PrimaryButtonText == "请选择清晰度")
                    PrimaryButtonText = "下载所选项";
                return;
            }
            this.needToClose = true;
            var quality = (int)this.qualityComboBox.SelectedValue;

            try
            {
                await DownloadHelper.CreateDownloadsAsync(list, quality, sESSDATA);
            }
            catch (ParsingVideoException ex)
            {
                var dialog = new ErrorDialog(ex.Message);
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    MainPage.NavView.SelectedItem = MainPage.NavViewItems[2];
                    MainPage.ContentFrame.Navigate(typeof(UserPage));
                }
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.needToClose = true;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var sESSDATA = ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] as string;
            var model = (sender as Button)?.DataContext as VideoInfo;
            this.needToClose = true;
            this.Hide();

            try
            {
                var info = await BiliVideoHelper.GetSingleVideoInfoAsync(model.Info.Bv, model.Info.Cid, 64, sESSDATA);
                var dialog = await SingleVideoDialog.CreateAsync(info);
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Secondary)  { this.needToClose = false; await this.ShowAsync(); }
            }
            catch (ParsingVideoException ex)
            {
                var dialog = new ErrorDialog(ex.Message);
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    MainPage.NavView.SelectedItem = MainPage.NavViewItems[2];
                    MainPage.ContentFrame.Navigate(typeof(UserPage));
                }
            }
            catch(System.Exception ex)
            {
                var dialog = new ErrorDialog(ex.Message);
                dialog.PrimaryButtonText = "";
                await dialog.ShowAsync();
            }
        }

        private void selectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)this.selectAllCheckBox.IsChecked)
            {
                foreach (var download in vm.VideoList)
                {
                    download.ToDownload = true;
                }
            }
            else
            {
                foreach (var download in vm.VideoList)
                {
                    download.ToDownload = false;
                }
            }
        }
    }
    public class BangumiDialogViewModel
    {
        public string VideoTitle { get; set; }
        public ImageSource VideoCover { get; set; }
        public ObservableCollection<VideoInfo> VideoList { get; set; }
        public List<ComboBoxData> ComboBoxDataList { get; set; }
        public class ComboBoxData
        {
            public ComboBoxData(string text, int value)
            {
                this.Text = text;
                this.Value = value;
            }
            public string Text { get; set; }
            public int Value { get; set; }
        }
    }
}
