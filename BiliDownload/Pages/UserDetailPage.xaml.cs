using BiliDownload.Helper;
using BiliDownload.Model;
using BiliDownload.SearchDialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliDownload.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UserDetailPage : Page
    {
        public static string SESSDATA { get => ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] as string; }
        public static long Uid { get => (long)ApplicationData.Current.LocalSettings.Values["biliUserUid"]; }
        private bool favListViewLoaded = false;
        private bool bangumiListViewLoaded = false;

        public UserDetailPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            this.InitializeUserAsync();
        }
        private async Task InitializeUserAsync()
        {
            this.initializingProgressRing.IsActive = true;
            var user = await BiliUserHelper.GetCurrentUserInfoAsync();
            this.avatarImage.Source = user.AvatarImg;
            this.userNameTextBlock.Text = user.Name;
            this.initializingProgressRing.IsActive = false;
        }
        private void logoutBtn_Click(object sender, RoutedEventArgs e)//登出
        {
            ApplicationData.Current.LocalSettings.Values["isLogined"] = false;
            ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] = string.Empty;
            ApplicationData.Current.LocalSettings.Values["biliUserUid"] = 0;
            this.Frame.Navigate(typeof(UserPage));
        }
        private void StartLoadingAnimation()
        {
            this.progressRingStackPanel.Visibility = Visibility.Visible;
            this.commonProgressRing.IsActive = true;
        }
        private void StopLoadingAnimation()
        {
            this.progressRingStackPanel.Visibility = Visibility.Collapsed;
            this.commonProgressRing.IsActive = false;

        }

        #region 下面是收藏夹用的

        private async void favListView_Loaded(object sender, RoutedEventArgs e)
        {
            if (favListViewLoaded) return;
            favListViewLoaded = true;
            this.StartLoadingAnimation();
            var favInfoList = await BiliFavHelper.GetUserFavListInfoAsync(Uid, SESSDATA);
            var favList = new List<FavViewModel>();
            foreach (var info in favInfoList)
            {
                var fav = await BiliFavHelper.GetBiliFavAsync(info.Id, 1, SESSDATA);
                if (fav.MediaCount > 0)
                {
                    favList.Add(new FavViewModel()
                    {
                        Id = fav.Id,
                        Title = fav.Title,
                        VideoCount = fav.MediaCount,
                        VideoList = new ObservableCollection<FavVideoViewModel>()
                    });
                }
            }
            var collection = new ObservableCollection<FavViewModel>(favList);
            this.favListView.ItemsSource = collection;
            this.StopLoadingAnimation();
        }

        private async void favVideoGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if ((e.ClickedItem as FavVideoViewModel).Bv == "加载更多")
            {
                var viewModel = this.favListView.SelectedItem as FavViewModel;
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
                catch (NullReferenceException)
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
                catch
                {

                }
            }
        }
        private async void favListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.StartLoadingAnimation();
            await favListView_LoadItem(this.favListView.SelectedItem as FavViewModel);
            this.favVideoGridView.ItemsSource = (this.favListView.SelectedItem as FavViewModel).VideoList;
            this.StopLoadingAnimation();
        }
        private async Task favListView_LoadItem(FavViewModel viewModel)
        {
            if (viewModel.VideoList.Count > 0) return;
            var fav = await BiliFavHelper.GetBiliFavAsync(viewModel.Id, 1, UserPage.SESSDATA);

            if (fav == null)
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

            list.ForEach(v => viewModel.VideoList.Add(v));

            viewModel.VideoList.Add(new FavVideoViewModel()
            {
                Bv = "加载更多",
                Title = "加载更多",
                CoverImg = new BitmapImage(new Uri("ms-appx:///Assets/LoadMore.png"))
            });
        }
        #endregion
        #region 下面是追番用的
        private async void bangumiListGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var bangumiInfo = e.ClickedItem as BangumiViewModel;
            if (bangumiInfo.SessonId == 0)//加载更多
            {
                var list = this.bangumiListGridView.ItemsSource as ObservableCollection<BangumiViewModel>;
                var count = list.Count - 1;
                if ((count < 15) || (count % 15 != 0))
                {
                    var dialog = new ErrorDialog("已经没有更多了")
                    {
                        PrimaryButtonText = ""
                    };
                    await dialog.ShowAsync();
                    return;
                }

                var newList = await BiliBangumiListHelper.GetBangumiListAsync((count / 15) + 1, UserPage.Uid, UserPage.SESSDATA);

                if (newList == null)
                {
                    var dialog = new ErrorDialog("已经没有更多了")
                    {
                        PrimaryButtonText = ""
                    };
                    await dialog.ShowAsync();
                    return;
                }

                var toAddList = new List<BangumiViewModel>();
                list.Remove(list.Last());
                foreach (var bangumi in newList) // 添加新视频
                {
                    var model = new BangumiViewModel()
                    {
                        Title = bangumi.Title,
                        SessonId = bangumi.SeasonId
                    };
                    try
                    {
                        var file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("VideoCoverCache", CreationCollisionOption.GenerateUniqueName);
                        var imgStream = await NetHelper.HttpGetStreamAsync(bangumi.Cover, null, null);
                        var fileStream = await file.OpenStreamForWriteAsync();
                        await imgStream.CopyToAsync(fileStream);
                        imgStream.Close();
                        fileStream.Close();
                        var imgSource = new BitmapImage();
                        await imgSource.SetSourceAsync((await file.OpenStreamForReadAsync()).AsRandomAccessStream());

                        model.CoverImg = imgSource;
                    }
                    catch (System.Exception ex)//封面下载不了的异常
                    {
                        model.CoverImg = new BitmapImage(new Uri("ms-appx:///Assets/LockScreenLogo.scale-200.png"));
                    }
                    toAddList.Add(model);
                }
                toAddList.ForEach(b => list.Add(b));//把新番剧添加到list里
                list.Add(new BangumiViewModel()//添加加载更多按钮
                {
                    Title = "加载更多",
                    SessonId = 0,
                    CoverImg = new BitmapImage(new Uri("ms-appx:///Assets/LoadMore.png"))
                });
            }
            else
            {
                var bangumi = await BiliVideoHelper.GetBangumiInfoAsync(bangumiInfo.SessonId, 1, SESSDATA);
                var dialog = await BangumiDialog.CreateAsync(bangumi);
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Secondary) return;
            }
        }

        private async void bangumiListGridView_Loaded(object sender, RoutedEventArgs e)//加载番剧
        {
            if (bangumiListViewLoaded) return;
            bangumiListViewLoaded = true;
            this.StartLoadingAnimation();
            var list = new List<BangumiViewModel>();
            var infoList = await BiliBangumiListHelper.GetBangumiListAsync(1, Uid, SESSDATA);
            if (infoList.Count < 1)
            {
                list.Add(new BangumiViewModel()
                {
                    Title = "追番为空",
                    SessonId = 0
                });
            }
            else
            {
                foreach (var bangumi in infoList)
                {
                    var model = new BangumiViewModel()
                    {
                        Title = bangumi.Title,
                        SessonId = bangumi.SeasonId
                    };
                    try
                    {
                        var file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("VideoCoverCache", CreationCollisionOption.GenerateUniqueName);
                        var imgStream = await NetHelper.HttpGetStreamAsync(bangumi.Cover, null, null);
                        var fileStream = await file.OpenStreamForWriteAsync();
                        await imgStream.CopyToAsync(fileStream);
                        imgStream.Close();
                        fileStream.Close();
                        var imgSource = new BitmapImage();
                        await imgSource.SetSourceAsync((await file.OpenStreamForReadAsync()).AsRandomAccessStream());

                        model.CoverImg = imgSource;
                    }
                    catch (System.Exception ex)//封面下载不了的异常
                    {
                        model.CoverImg = new BitmapImage(new Uri("ms-appx:///Assets/LockScreenLogo.scale-200.png"));
                    }
                    list.Add(model);
                }
            }
            list.Add(new BangumiViewModel()
            {
                Title = "加载更多",
                SessonId = 0,
                CoverImg = new BitmapImage(new Uri("ms-appx:///Assets/LoadMore.png"))
            });
            var colletion = new ObservableCollection<BangumiViewModel>(list);
            this.bangumiListGridView.ItemsSource = colletion;
            this.StopLoadingAnimation();
        }
        #endregion
    }
    public class FavViewModel
    {
        public static async Task<FavViewModel> CreateAsync(BiliFav fav)
        {
            var model = new FavViewModel()
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

            if (fav == null)
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
        public int VideoCount { get; set; }
        public ObservableCollection<FavVideoViewModel> VideoList { get; set; }
    }

    public class FavVideoViewModel
    {
        public static async Task<FavVideoViewModel> CreateAsync(string title, string bv, string coverUrl)
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
            catch (System.Exception ex)//封面下载不了的异常
            {
                model.CoverImg = new BitmapImage(new Uri("ms-appx:///Assets/LockScreenLogo.scale-200.png"));
            }
            return model;
        }
        public string Bv { get; set; }
        public string Title { get; set; }
        public ImageSource CoverImg { get; set; }
    }
    public class BangumiViewModel
    {
        public int SessonId { get; set; }
        public string Title { get; set; }
        public ImageSource CoverImg { get; set; }
    }

}
