using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace BiliDownload
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public bool Initialized { get => (bool)ApplicationData.Current.LocalSettings.Values["Initialized"]; }
        private readonly List<(string, Type)> pages = new List<(string, Type)>
        {
            ("DownloadPage",typeof(DownloadPage)),
            ("SearchPage",typeof(SearchPage)),
            ("UserPage",typeof(UserPage))
        };
        public static NavigationView NavView { private set; get; }
        public static List<NavigationViewItem> NavViewItems { private set; get; }
        public static Frame ContentFrame { private set; get; }
        public MainPage()
        {
            this.InitializeComponent();
            this.contentFrame.Navigate(typeof(DownloadPage));
            if (NavView == null) NavView = this.navView;
            if (NavViewItems == null) NavViewItems = new List<NavigationViewItem>()
            {
                this.DownloadItem,//0
                this.SearchItem,//1
                this.UserItem//2
            };
            if (ContentFrame == null) ContentFrame = this.contentFrame;
            if (ApplicationData.Current.LocalSettings.Values["HEVC"] == null)
                ApplicationData.Current.LocalSettings.Values["HEVC"] = false;
            ApplicationData.Current.LocalSettings.Values["lock"] = false;//创建锁
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (ApplicationData.Current.LocalSettings.Values["Initialized"] == null)
                ApplicationData.Current.LocalSettings.Values["Initialized"] = false;
            if (Initialized == false)
            {
                var dialog = new ContentDialog()
                {
                    Title = "初次使用，请阅读说明",
                    Content = new TextBlock()
                    {
                        Text = "欢迎使用，此程序处于早期测试版本，您可能会遇到各种各样的BUG。由于您是初次启动本程序，请完成设置。",
                        FontFamily = new FontFamily("Microsoft Yahei UI"),
                        FontSize = 20,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(10)
                    },
                    PrimaryButtonText = "前往设置",
                    SecondaryButtonText = "关闭"
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    this.contentFrame.Navigate(typeof(SettingPage));
                    this.navView.SelectedItem = navView.SettingsItem;
                }
                ApplicationData.Current.LocalSettings.Values["Initialized"] = true;
            }

                this.navView.IsPaneOpen = true;
            await Task.Delay(1500);
            this.navView.IsPaneOpen = false;
        }
        private void navView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                contentFrame.Navigate(typeof(SettingPage));
            }
            else
            {
                var page = pages.Where(p => p.Item1 == args.InvokedItemContainer.Tag.ToString()).Single().Item2;

                if (contentFrame.CurrentSourcePageType == page) return;
                else contentFrame.Navigate(page);
            }
        }
    }
}