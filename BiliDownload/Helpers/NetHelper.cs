using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BiliDownload.Helper
{
    public static class NetHelper
    {
        public static async Task<string> HttpGet(string url, IEnumerable<(string, string)> cookies, params string[] queryStrings)
        {
            var stream = await HttpGetStreamAsync(url, cookies, queryStrings);
            return await new StreamReader(stream).ReadToEndAsync();
        }
        public static async Task<Stream> HttpGetStreamAsync(string url, IEnumerable<(string, string)> cookies, params string[] queryStrings)
        {
            string queryString = "";
            if (queryStrings != null) queryString = '?' + string.Join('&', queryStrings);

            var req = WebRequest.Create(url + queryString) as HttpWebRequest;

            if (cookies != null)
            {
                var container = new CookieContainer();
                foreach (var cookie in cookies)
                {
                    container.Add(new Cookie(cookie.Item1, cookie.Item2, "/", ".bilibili.com"));
                }
                req.CookieContainer = container;
            }

            req.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.135 Safari/537.36 Edg/84.0.522.63");
            req.Headers.Add("referer", "https://www.bilibili.com/");

            var resp = await req.GetResponseAsync();
            var stream = resp.GetResponseStream();

            return stream;
        }
        public static async Task<(string, WebHeaderCollection)> HttpPostAsync(string url, IEnumerable<(string, string)> cookies, string postContent)
        {
            var req = WebRequest.Create(url) as HttpWebRequest;

            if (cookies != null)
            {
                var container = new CookieContainer();
                foreach (var cookie in cookies)
                {
                    container.Add(new Cookie(cookie.Item1, cookie.Item2));
                }
                req.CookieContainer = container;
            }

            req.Method = "POST";
            req.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.135 Safari/537.36 Edg/84.0.522.63");
            req.Headers.Add("referer", "https://www.bilibili.com/");
            req.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

            var bytes = Encoding.UTF8.GetBytes(postContent);
            req.ContentLength = bytes.Length;
            var reqStream = await req.GetRequestStreamAsync();
            await reqStream.WriteAsync(bytes, 0, bytes.Length);
            reqStream.Close();

            var resp = await req.GetResponseAsync();
            var stream = resp.GetResponseStream();
            return (await (new StreamReader(stream)).ReadToEndAsync(), resp.Headers);
        }
        public static async Task<string> HttpPostJsonAsync(string url, IEnumerable<(string, string)> cookies, string json)
        {
            var client = new HttpClient();
            if (cookies != null)
                cookies.ToList().ForEach(c => client.DefaultRequestHeaders.Add(c.Item1, c.Item2));
            var resp = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            return await resp.Content.ReadAsStringAsync();
        }
    }
}
