using BiliDownload.Helper;
using BiliDownload.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using XC.RSAUtil;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliDownload.HelperPage
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class PwdLoginPage : Page
    {
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public string Key { get; set; }
        public string Challenge { get; set; }
        public string Validate { get; set; }
        public string Seccode { get; set; }
        public PwdLoginPage()
        {
            this.InitializeComponent();
            this.pwdPasswordBox.KeyDown += (s, e) => //回车登录
            {
                if (e.Key == Windows.System.VirtualKey.Enter) loginBtn_Click(loginBtn, new RoutedEventArgs());
            };
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        /*
        private async void LoginWebView_ScriptNotify(object sender, NotifyEventArgs e)//验证成功时触发
        {
            this.Validate = e.Value.Substring(0, e.Value.LastIndexOf('&'));
            this.Seccode = e.Value.Substring(e.Value.LastIndexOf('&') + 1);
            await this.Dispatcher.RunAsync
                (CoreDispatcherPriority.High, async () =>
                 {
                     this.loginWebView.Visibility = Visibility.Collapsed;
                     this.progressRing.Visibility = Visibility.Collapsed;
                     this.UserName = userNameTextBox.Text;
                     this.PasswordHash = await GeneratePwdHash(this.pwdPasswordBox.Password);

                     var postContent = $"captchaType=6&username={UrlEncode(UserName)}&keep=true&key={UrlEncode(this.Key)}&challenge={UrlEncode(Challenge)}&validate={UrlEncode(Validate)}&seccode={UrlEncode(Seccode)}&password={UrlEncode(PasswordHash)}";

                     var result = await NetHelper.HttpPostAsync
                         ("http://passport.bilibili.com/web/login/v2", null, postContent);
                     var json = JsonConvert.DeserializeObject<LoginJson>(result.Item1);
                     if (json.code != 0) { this.errorTextBlock.Text = json.code.ToString(); this.loginBtn.IsEnabled = true; }
                     else if (json.code == 0)
                     {
                         var sESSDATA = Regex.Match(result.Item2.ToString(), "(?<=SESSDATA=)[%|a-z|A-Z|0-9|*]*")?.Value;
                         var uid = Regex.Match(result.Item2.ToString(), "(?<=DedeUserID=)[0-9]*")?.Value;
                         ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] = sESSDATA;
                         ApplicationData.Current.LocalSettings.Values["biliUserUid"] = long.Parse(uid);
                         ApplicationData.Current.LocalSettings.Values["isLogined"] = true;
                         await UserLoginPage.Current.PwdLoginOk();
                     }
                 });
        }
        */

        private async Task<(string, string, string)> GetCaptchaCodeAsync()
        {
            var json = JsonConvert.DeserializeObject<CaptchaJson>(await NetHelper.HttpGet("http://passport.bilibili.com/web/captcha/combine?plat=6", null, null));
            return (json.data.result.gt, json.data.result.challenge, json.data.result.key);
        }

        private async void loginBtn_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (string.IsNullOrWhiteSpace(userNameTextBox.Text)) return;
            if (string.IsNullOrWhiteSpace(pwdPasswordBox.Password)) return;

            var captchaCodes = await GetCaptchaCodeAsync();
            var gt = captchaCodes.Item1;
            var challenge = captchaCodes.Item2;
            var key = captchaCodes.Item3;

            this.Challenge = challenge;
            this.Key = key;

            this.loginWebView.ScriptNotify += LoginWebView_ScriptNotify;
            this.loginWebView.NavigationCompleted +=
                async (s, e) => await this.Dispatcher.RunAsync
                (CoreDispatcherPriority.Normal, () => this.loginWebView.Visibility = Visibility.Visible);

            await WebView.ClearTemporaryWebDataAsync();
            this.progressRing.Visibility = Visibility.Visible;
            (sender as Button).IsEnabled = false;
            this.loginWebView.Navigate(new Uri($"https://eveelseif.gitee.io/geetest-validator?gt={gt}&challenge={challenge}"));
            */
        }

        private async void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            await UserLoginPage.Current.PwdLoginCancel();
        }
        private async Task<string> GeneratePwdHash(string pwd)
        {
            var json = JsonConvert.DeserializeObject<HashJson>(await NetHelper.HttpGet("http://passport.bilibili.com/login?act=getkey", null, null));
            var before = json.hash + pwd;
            var rsa = new RsaPkcs1Util(Encoding.UTF8, json.key);
            var after = rsa.Encrypt(before, RSAEncryptionPadding.Pkcs1);
            return after;
        }
        private string UrlEncode(string content)
        {
            return content.Replace("%", "%25").Replace("+", "%2B").Replace("/", "%2F").Replace(":", "%3A")
                .Replace("|", "%7C").Replace("?", "%3F").Replace("#", "%23").Replace("&", "%26").Replace("=", "%3D");
        }
    }
    public class CaptchaJson
    {
        public int code { get; set; }
        public Data data { get; set; }
        public class Data
        {
            public Result result { get; set; }
            public int type { get; set; }

        }
        public class Result
        {
            public int success { get; set; }
            public string gt { get; set; }
            public string challenge { get; set; }
            public string key { get; set; }
        }
    }
    public class HashJson
    {
        public string hash { get; set; }
        public string key { get; set; }//RSA公钥
    }
    public class LoginJson
    {
        public int code { get; set; }
        public string message { get; set; }
    }
}
