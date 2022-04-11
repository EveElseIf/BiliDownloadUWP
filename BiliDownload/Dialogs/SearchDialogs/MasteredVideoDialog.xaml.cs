using BiliDownload.Dialogs;
using BiliDownload.Exceptions;
using BiliDownload.Helper;
using BiliDownload.Helpers;
using BiliDownload.Models;
using BiliDownload.Others;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace BiliDownload.SearchDialogs
{
    public sealed partial class MasteredVideoDialog : ContentDialog
    {
        MasteredVideoDialogViewModel vm;
        bool needToClose = false;
        private MasteredVideoDialog(MasteredVideoDialogViewModel vm, XamlRoot xamlRoot)
        {
            this.vm = vm;
            this.DataContext = vm;
            this.InitializeComboBoxData(vm);
            this.InitializeComponent();
            this.Closing += MasteredVideoDialog_Closing;
            XamlRoot = xamlRoot;
        }

        private void MasteredVideoDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (!needToClose) args.Cancel = true;
        }

        private void InitializeComboBoxData(MasteredVideoDialogViewModel vm)
        {
            var list = new List<MasteredVideoDialogViewModel.ComboBoxData>()
            {
                new MasteredVideoDialogViewModel.ComboBoxData("360P 流畅",16 ),
                new MasteredVideoDialogViewModel.ComboBoxData("480P 清晰",32 ),
                new MasteredVideoDialogViewModel.ComboBoxData("720P 高清(需要登录)",64 ),
                new MasteredVideoDialogViewModel.ComboBoxData("720P60 高清(大会员)" ,74),
                new MasteredVideoDialogViewModel.ComboBoxData("1080P 高清 (需要登录)" ,80),
                new MasteredVideoDialogViewModel.ComboBoxData("1080P+ 高清(大会员)" ,112),
                new MasteredVideoDialogViewModel.ComboBoxData("1080P60 高清(大会员)" ,116),
                //new MasteredVideoDialogViewModel.ComboBoxData("4K 超清(大会员)" ,120)
            };
            vm.ComboBoxDataList = list;
        }
        public static async Task<MasteredVideoDialog> CreateAsync(BiliVideoMaster master, XamlRoot xamlRoot)
        {
            var model = new MasteredVideoDialogViewModel();

            model.VideoTitle = master.Title;

            //var stream = await NetHelper.HttpGetStreamAsync(master.Picture, null, null);
            //var file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("videocovercache", CreationCollisionOption.GenerateUniqueName);
            //var fileStream = await file.OpenStreamForWriteAsync();
            //await stream.CopyToAsync(fileStream);
            //stream.Close();
            //fileStream.Close();
            //var img = new BitmapImage();
            //await img.SetSourceAsync((await file.OpenStreamForReadAsync()).AsRandomAccessStream());
            var img = new BitmapImage(new Uri(master.Picture));
            model.VideoCover = img;

            var list = master.VideoList.Select(v => new VideoInfo()
            {
                Info = new BiliVideoInfo()
                {
                    Bv = v.Bv,
                    Cid = v.Cid,
                    Name = v.Name,
                    CoverUrl = master.Picture
                },
                ToDownload = false,
                BackGroundColorBrush = master.VideoList.IndexOf(v) % 2 == 0
                ? new SolidColorBrush(Colors.WhiteSmoke) : new SolidColorBrush(Colors.White)
            }).ToList();
            model.VideoList = new ObservableCollection<VideoInfo>(list);

            var dialog = new MasteredVideoDialog(model, xamlRoot);
            return dialog;
        }
        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            //多个视频下载
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
                await DownloadHelper.CreateDownloadsAsync(list, quality, sESSDATA, XamlRoot);
            }
            catch (ParsingVideoException ex)
            {
                var dialog = new ErrorDialog(ex.Message, XamlRoot);
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    MainPage.NavView.SelectedItem = MainPage.NavViewItems[2];
                    MainPage.ContentFrame.Navigate(typeof(UserPage));
                }
            }
            catch (NullReferenceException)
            {
                var dialog = new ContentDialog()
                {
                    Title = "错误",
                    Content = new TextBlock()
                    {
                        Text = "该视频不存在dash格式的视频文件",
                        FontFamily = new FontFamily("Microsoft Yahei UI"),
                        FontSize = 20
                    },
                    PrimaryButtonText = "知道了"
                };
                await dialog.ShowAsync();
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.needToClose = true;
        }

        private async void Button_Click(object sender, RoutedEventArgs e) //单个视频下载
        {
            var sESSDATA = ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] as string;
            var model = (sender as Button)?.DataContext as VideoInfo;
            this.needToClose = true;
            this.Hide();

            try
            {
                var info = await BiliVideoHelper.GetSingleVideoInfoAsync(model.Info.Bv, model.Info.Cid, 64, sESSDATA);
                var dialog = await SingleVideoDialog.CreateAsync(info, XamlRoot);
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Secondary) { this.needToClose = false; await this.ShowAsync(); }
            }
            catch (ParsingVideoException ex)
            {
                var dialog = new ErrorDialog(ex.Message, XamlRoot);
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    MainPage.NavView.SelectedItem = MainPage.NavViewItems[2];
                    MainPage.ContentFrame.Navigate(typeof(UserPage));
                }
            }
            catch (NullReferenceException)
            {
                var dialog = new ContentDialog()
                {
                    Title = "错误",
                    Content = new TextBlock()
                    {
                        Text = "该视频不存在dash格式的视频文件",
                        FontFamily = new FontFamily("Microsoft Yahei UI"),
                        FontSize = 20
                    },
                    PrimaryButtonText = "知道了"
                };
                await dialog.ShowAsync();
            }
        }

        private void selectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)(sender as CheckBox).IsChecked)
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
        private async void DanmakuDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var model = btn?.DataContext as VideoInfo;
            btn.Content = new ProgressRing()
            {
                IsActive = true
            };

            if (string.IsNullOrWhiteSpace(Settings.DownloadPath))
            {
                this.needToClose = true;
                this.Hide();
                var dialog = new ErrorDialog("未设置下载储存文件夹，请前往设置以更改", XamlRoot)
                {
                    PrimaryButtonText = "前往设置"
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Secondary) return;
                else
                {
                    MainPage.ContentFrame.Navigate(typeof(SettingPage));
                    MainPage.NavView.SelectedItem = MainPage.NavView.SettingsItem;
                    return;
                }
            }

            try
            {
                await BiliDanmakuHelper.DownloadDanmakuAsync(this.vm.VideoTitle, model.Info.Name, model.Info.Cid);
                btn.Content = "下载完成";
                btn.IsEnabled = false;
            }
            catch (NullReferenceException)
            {
                this.needToClose = true;
                this.Hide();
                var dialog = new ContentDialog()
                {
                    Title = "错误",
                    Content = new TextBlock()
                    {
                        Text = "未知错误",
                        FontFamily = new FontFamily("Microsoft Yahei UI"),
                        FontSize = 20
                    },
                    PrimaryButtonText = "知道了"
                };
                await dialog.ShowAsync();
            }
            catch (DirectoryNotFoundException ex)
            {
                this.needToClose = true;
                this.Hide();
                var dialog = new ExceptionDialog(ex.Message, XamlRoot);
                _ = await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                this.needToClose = true;
                this.Hide();
                var dialog = new ContentDialog()
                {
                    Title = "错误",
                    Content = new TextBlock()
                    {
                        Text = ex.Message,
                        FontFamily = new FontFamily("Microsoft Yahei UI"),
                        FontSize = 20
                    },
                    PrimaryButtonText = "知道了"
                };
                await dialog.ShowAsync();
            }
        }
    }
    public class MasteredVideoDialogViewModel
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
    public class VideoInfo : INotifyPropertyChanged
    {
        public BiliVideoInfo Info { get; set; }
        private bool toDownload;

        public bool ToDownload
        {
            get { return toDownload; }
            set { toDownload = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ToDownload")); }
        }

        public SolidColorBrush BackGroundColorBrush { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
