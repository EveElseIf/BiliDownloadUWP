using BiliDownload.Helper;
using BiliDownload.LoginDialogs;
using BiliDownload.Model;
using BiliDownload.SearchDialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliDownload
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UserPage : Page
    {
        public static string SESSDATA { get => ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] as string; }
        public static long Uid { get => (long)ApplicationData.Current.LocalSettings.Values["biliUserUid"]; }
        bool loginStatus { set; get; }
        bool favLoaded { set; get; } = false;
        public UserPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ResetAll();

            NavigationCacheMode = NavigationCacheMode.Enabled;
            if (ApplicationData.Current.LocalSettings.Values["isLogined"] == null)
                ApplicationData.Current.LocalSettings.Values["isLogined"] = false;
            if ((bool)ApplicationData.Current.LocalSettings.Values["isLogined"]) loginStatus = true;
            else loginStatus = false;

            if (!loginStatus) this.realContentGrid.Visibility = Visibility.Collapsed;
            if (loginStatus)//如果登录了，就移除登录界面
            {
                //contentGrid.Children.Remove(this.FindName("loginGrid") as UIElement);
                this.realContentGrid.Visibility = Visibility.Visible;
                this.loginGrid.Visibility = Visibility.Collapsed;
                initializingProgressRing.Visibility = Visibility.Visible;
                initializingProgressRing.IsActive = true;

                await InitializeUserAsync();//初始化用户信息

                logoutBtn.Visibility = Visibility.Visible;
                avatarAndNameGrid.Visibility = Visibility.Visible;
                initializingProgressRing.Visibility = Visibility.Collapsed;
                initializingProgressRing.IsActive = false;
            }
        }
        private void ResetAll()
        {
            this.avatarAndNameGrid.Visibility = Visibility.Collapsed;
            this.loginGrid.Visibility = Visibility.Visible;
            this.realContentGrid.Visibility = Visibility.Collapsed;
            this.progressRingStackPanel.Visibility = Visibility.Collapsed;
        }
        private async Task InitializeUserAsync()
        {
            var user = await BiliUserHelper.GetCurrentUserInfoAsync();
            this.avatarImage.Source = user.AvatarImg;
            this.userNameTextBlock.Text = user.Name;
        }
        private void ShowProgressRing()
        {
            this.progressRingStackPanel.Visibility = Visibility.Visible;
            this.commonProgressRing.Visibility = Visibility.Visible;
            this.commonProgressRing.IsActive = true;
        }
        private void HideProgressRing()
        {
            this.progressRingStackPanel.Visibility = Visibility.Collapsed;
            this.commonProgressRing.Visibility = Visibility.Collapsed;
            this.commonProgressRing.IsActive = false;
        }

        #region 下面都是登录用的
        private async void loginBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new QRcodeLoginDialog();
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                this.Frame.Navigate(typeof(UserPage));
            }
        }

        private async void pwdLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog()
            {
                Title = "提示",
                Content = "人机验证好难搞，这东西暂时不考虑了",
                PrimaryButtonText = "知道了。。"
            };
            await dialog.ShowAsync();
        }

        private void logoutBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!loginStatus) return;
            ApplicationData.Current.LocalSettings.Values["isLogined"] = false;
            ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] = string.Empty;
            ApplicationData.Current.LocalSettings.Values["biliUserUid"] = 0;
            this.loginStatus = false;
            this.Frame.Navigate(typeof(UserPage));
        }

        private async void sESSDATALoginBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SESSDATALoginDialog();
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary) this.Frame.Navigate(typeof(UserPage));
        }
        #endregion

        private async void favGridViewList_Loaded(object sender, RoutedEventArgs e)
        {
            if (favLoaded) return;
            ShowProgressRing();

            var favInfoList = await BiliFavHelper.GetUserFavListInfoAsync(Uid, SESSDATA);
            var favList = new List<BiliFav>();
            foreach (var info in favInfoList)
            {
                var fav = await BiliFavHelper.GetBiliFavAsync(info.Id, 1, SESSDATA);
                if (fav == null)
                {
                    favList.Add(new BiliFav()
                    {
                        VideoList = new List<BiliVideoMaster>()
                        {
                            new BiliVideoMaster()
                            {
                                Title="该收藏夹为空",
                                Bv="加载更多"
                            }
                        }
                    });
                }
                else
                    favList.Add(new BiliFav()
                    {
                        Id = info.Id,
                        Title = info.Title,
                        VideoList = fav.VideoList
                    });
            }

            var collection = new ObservableCollection<FavViewModel>();
            foreach (var fav in favList)
            {
                collection.Add(await FavViewModel.CreateAsync(fav));
            }

            this.favGridViewList.ItemsSource = collection;

            HideProgressRing();
            favLoaded = true;
        }

        private async void favVideoGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if ((e.ClickedItem as FavVideoViewModel).Bv == "加载更多")
            {
                var viewModel = (sender as GridView).DataContext as FavViewModel;
                await viewModel.GetMoreVideoAsync();
            }
            else if (!Regex.IsMatch((e.ClickedItem as FavVideoViewModel).Bv, "[B|b][V|v][0-9]*")) return;
            else
            {
                try
                {
                    var infoVideo = e.ClickedItem as FavVideoViewModel;
                    var video = await BiliVideoHelper.GetVideoMasterInfoAsync(infoVideo.Bv, SESSDATA);
                    var dialog = await MasteredVideoDialog.CreateAsync(video);//创建下载对话框
                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Secondary) return;
                }
                catch(NullReferenceException ex)
                {
                    var dialog = new ContentDialog()
                    {
                        Title = "错误",
                        Content = new TextBlock()
                        {
                            Text = "该视频已失效",
                            FontFamily = new FontFamily("Microsoft Yahei UI"),
                            FontSize = 20
                        },
                        PrimaryButtonText = "知道了"
                    };
                    await dialog.ShowAsync();
                }
            }
        }
    }
    public class FavViewModel
    {
        public static async Task<FavViewModel> CreateAsync(BiliFav fav)
        {
            var model = new FavViewModel
            {
                Title = fav.Title,
                Id = fav.Id
            };

            var list = new ObservableCollection<FavVideoViewModel>();

            foreach (var video in fav.VideoList)
            {
                list.Add(await FavVideoViewModel.CreateAsync(video.Title, video.Bv, video.Picture));
            }

            list.Add(new FavVideoViewModel()
            {
                Bv = "加载更多",
                Title = "加载更多",
                CoverImg = new BitmapImage(new Uri("ms-appx:///Assets/LoadMore.png"))
            });

            model.VideoList = list;

            return model;
        }
        public async Task GetMoreVideoAsync()
        {
            var count = this.VideoList.Count - 1;
            if ((count < 20) || (count % 20 != 0))
            {
                var dialog = new ErrorDialog("已经没有更多了")
                {
                    PrimaryButtonText = ""
                };
                await dialog.ShowAsync();
                return;
            }

            var fav = await BiliFavHelper.GetBiliFavAsync(this.Id, (count / 20) + 1, UserPage.SESSDATA);

            if(fav == null)
            {
                var dialog = new ErrorDialog("已经没有更多了")
                {
                    PrimaryButtonText = ""
                };
                await dialog.ShowAsync();
                return;
            }

            var list = new List<FavVideoViewModel>();
            foreach (var video in fav.VideoList)
            {
                list.Add(await FavVideoViewModel.CreateAsync(video.Title, video.Bv, video.Picture));
            }

            this.VideoList.Remove(this.VideoList.TakeLast(1).Single());
            list.ForEach(v => this.VideoList.Add(v));

            this.VideoList.Add(new FavVideoViewModel()
            {
                Bv = "加载更多",
                Title = "加载更多",
                CoverImg = new BitmapImage(new Uri("ms-appx:///Assets/LoadMore.png"))
            });
        }
        public int Id { get; set; }
        public string Title { get; set; }
        public ObservableCollection<FavVideoViewModel> VideoList { get; set; }
    }
    public class FavVideoViewModel
    {
        public static async Task<FavVideoViewModel> CreateAsync(string title,string bv,string coverUrl)
        {
            var model = new FavVideoViewModel();
            model.Title = title;
            model.Bv = bv;

            try
            {
                var file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("VideoCoverCache", CreationCollisionOption.GenerateUniqueName);
                var imgStream = await NetHelper.HttpGetStreamAsync(coverUrl, null, null);
                var fileStream = await file.OpenStreamForWriteAsync();
                await imgStream.CopyToAsync(fileStream);
                imgStream.Close();
                fileStream.Close();
                var imgSource = new BitmapImage();
                await imgSource.SetSourceAsync((await file.OpenStreamForReadAsync()).AsRandomAccessStream());

                model.CoverImg = imgSource;
            }
            catch(System.Exception ex)//封面下载不了的异常
            {
                model.CoverImg = new BitmapImage(new Uri("ms-appx:///Assets/LockScreenLogo.scale-200.png"));
            }
            return model;
        }
        public string Bv { get; set; }
        public string Title { get; set; }
        public ImageSource CoverImg { get; set; }
    }
}
