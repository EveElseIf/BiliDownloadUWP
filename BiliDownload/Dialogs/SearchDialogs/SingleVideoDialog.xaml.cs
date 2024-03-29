﻿using BiliDownload.Dialogs;
using BiliDownload.Exceptions;
using BiliDownload.Helper;
using BiliDownload.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace BiliDownload.SearchDialogs
{
    public sealed partial class SingleVideoDialog : ContentDialog
    {
        public int QualitySelectionProperty
        {
            get { return (int)GetValue(QualitySelectionPropertyProperty); }
            set { SetValue(QualitySelectionPropertyProperty, value); }
        }
        public static readonly DependencyProperty QualitySelectionPropertyProperty =
            DependencyProperty.Register("QualitySelectionProperty", typeof(int), typeof(SingleVideoDialog), new PropertyMetadata(0));

        SingleVideoDialogViewModel vm;
        bool needToClose = false;
        private SingleVideoDialog(SingleVideoDialogViewModel vm, XamlRoot xamlRoot)
        {
            this.vm = vm;
            this.DataContext = vm;
            this.InitializeComponent();
            this.Closing += SingleVideoDialog_Closing;
            XamlRoot = xamlRoot;
        }

        private void SingleVideoDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (!needToClose) args.Cancel = true;
        }

        public static async Task<SingleVideoDialog> CreateAsync(BiliVideoInfo videoInfo, XamlRoot xamlRoot)
        {
            var vm = await InitializeVideoModel(videoInfo);
            var model = new SingleVideoDialog(vm, xamlRoot);
            return model;
        }
        private static async Task<SingleVideoDialogViewModel> InitializeVideoModel(BiliVideoInfo videoInfo)
        {
            var model = new SingleVideoDialogViewModel();
            model.VideoName = videoInfo.Name;
            model.Bv = videoInfo.Bv;
            model.Cid = videoInfo.Cid;

            //var stream = await NetHelper.HttpGetStreamAsync(videoInfo.CoverUrl, null, null);
            //var file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("videocovercache", CreationCollisionOption.GenerateUniqueName);
            //var fileStream = await file.OpenStreamForWriteAsync();
            //await stream.CopyToAsync(fileStream);
            //stream.Close();
            //fileStream.Close();
            //var img = new BitmapImage();
            //await img.SetSourceAsync((await file.OpenStreamForReadAsync()).AsRandomAccessStream());
            var img = new BitmapImage(new Uri(videoInfo.CoverUrl));
            model.VideoCover = img;

            model.VideoQualityList = new List<SingleVideoDialogViewModel.VideoQuality>();
            foreach (var item in videoInfo.QualityCodeList)
            {
                var key = item;
                var value = QualityDictionary[item];
                model.VideoQualityList.Add(new SingleVideoDialogViewModel.VideoQuality(key, value));
            }

            return model;
        }
        private static readonly Dictionary<int, string> QualityDictionary = new Dictionary<int, string>()
        {
            {16,"360P 流畅" },
            {32,"480P 清晰" },
            {64,"720P 高清(需要登录)" },
            {74,"720P60 高清(大会员)" },
            {80,"1080P 高清 (需要登录)" },
            {112,"1080P+ 高清(大会员)" },
            {116,"1080P60 高清(大会员)" },
            {120,"4K 超清(大会员)" }
        };
        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (qualitySelectListBox.SelectedItem == null)
            {
                PrimaryButtonText = "请选择清晰度";
                await Task.Delay(2000);
                PrimaryButtonText = "下载";
                return;
            }
            this.needToClose = true;
            this.Hide();
            try
            {
                await DownloadHelper.CreateDownloadAsync
                    (vm.Bv, vm.Cid, this.QualitySelectionProperty, ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] as string, XamlRoot);
            }
            catch (ParsingVideoException ex)
            {
                var dialog = new ErrorDialog(ex.Message, XamlRoot);
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    MainPage.ContentFrame.Navigate(typeof(UserPage));
                    MainPage.NavView.SelectedItem = MainPage.NavViewItems[2];
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                var dialog = new ExceptionDialog(ex.Message, XamlRoot);
                _ = await dialog.ShowAsync();
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.needToClose = true;
        }

        private void qualitySelectListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.QualitySelectionProperty =
                ((sender as ListBox).SelectedItem as SingleVideoDialogViewModel.VideoQuality).QualityCode;
        }
    }
    public class SingleVideoDialogViewModel
    {
        public string Bv { get; set; }
        public long Cid { get; set; }
        public string VideoName { get; set; }
        public ImageSource VideoCover { get; set; }
        public List<VideoQuality> VideoQualityList { get; set; }
        public class VideoQuality
        {
            public VideoQuality(int code, string name)
            {
                this.QualityCode = code;
                this.QualityName = name;
            }
            public int QualityCode { get; set; }
            public string QualityName { get; set; }
        }
    }
}
