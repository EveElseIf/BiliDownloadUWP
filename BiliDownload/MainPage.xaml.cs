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
        private readonly List<(string, Type)> pages = new List<(string, Type)>
        {
            ("DownloadPage",typeof(DownloadPage)),
            ("SearchPage",typeof(SearchPage)),
            ("UserPage",typeof(UserPage))
        };
        public static NavigationView NavView { private set; get; }
        public static List<NavigationViewItem> NavViewItems { private set; get; }
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
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
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