using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace wnacg
{
    class Http
    {
        private static string _proxy = null;

        public static void SetProxy(string proxy)
        {
            _proxy = proxy;
        }

        public static HttpWebRequest GetWebRequest(string url)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            if (!string.IsNullOrWhiteSpace(_proxy)) 
                request.Proxy = new WebProxy(_proxy);
            return request;
        }

        public static void HttpDownloadFile(string url, string path, string fileName,int timeOut)
        {
            if (File.Exists(path + fileName))
            {
                return;
            }

            try
            {
                // 设置参数
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                //request.Proxy = new WebProxy(GetProxyServer());
                if (_proxy!=null) request.Proxy = new WebProxy(_proxy);
                if (timeOut != -1) request.Timeout = timeOut;
                //发送请求并获取相应回应数据
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                double dataLengthToRead = response.ContentLength;
                //直到request.GetResponse()程序才开始向目标网页发送Post请求
                Stream responseStream = response.GetResponseStream();

                //创建本地文件写入流
                Stream stream = new FileStream(path + fileName + ".covertemp", FileMode.Create);
                byte[] bArr = new byte[1024 * 512];

                int size = responseStream.Read(bArr, 0, (int)bArr.Length);
                while (size > 0)
                {
                    stream.Write(bArr, 0, size);
                    size = responseStream.Read(bArr, 0, (int)bArr.Length);
                }
                stream.Close();
                responseStream.Close();
                File.Move(path + fileName + ".covertemp", path + fileName);
            }
            catch (Exception ex)
            {
                Console.Out.Write(ex.StackTrace);
            }

        }//method

        public static string GetHtml(string url, Encoding ed)
        {
            string Html = string.Empty;//初始化新的webRequst
            HttpWebRequest Request = GetWebRequest(url);
            Request.ProtocolVersion = HttpVersion.Version11;
            Request.Method = "GET";
            Request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9 ";
            Request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.136 YaBrowser/20.2.4.141 Yowser/2.5 Safari/537.36";
            Request.Referer = url;
            Request.Timeout = 10000;
            HttpWebResponse htmlResponse = (HttpWebResponse)Request.GetResponse();
            //从Internet资源返回数据流
            Stream htmlStream = htmlResponse.GetResponseStream();
            //读取数据流
            StreamReader weatherStreamReader = new StreamReader(htmlStream, ed);
            //读取数据

            Html = weatherStreamReader.ReadToEnd();
            weatherStreamReader.Close();
            htmlStream.Close();
            htmlResponse.Close();
            //针对不同的网站查看html源文件
            return Html;
        }

        public static string GetHtml(string url) {
            return GetHtml(url, 0);
        }

        public static string GetHtml(string url,int deep)
        {
            ++deep;
            try {
                return GetHtml(url, Encoding.UTF8);
            }
            catch (TimeoutException e) {
                if (deep >= 3)
                    throw e;
                else
                    return GetHtml(url, deep);
            }
        }

        public static string GetProxyServer()
        {
            //打开注册表 
            RegistryKey regKey = Registry.CurrentUser;
            string SubKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
            RegistryKey optionKey = regKey.OpenSubKey(SubKeyPath, true);             //更改健值，设置代理， 
            string actualProxy = optionKey.GetValue("ProxyServer").ToString();
            regKey.Close();
            return actualProxy;
        }

        /// <summary>
        /// 下载图片
        /// </summary>
        /// <param name="picUrl">图片Http地址</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="timeOut">Request最大请求时间，如果为-1则无限制</param>
        /// <returns></returns>
        public bool DownloadPicture(string picUrl, string savePath, int timeOut)
        {
            bool value = false;
            WebResponse response = null;
            Stream stream = null;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(picUrl);
                if (timeOut != -1) request.Timeout = timeOut;
                response = request.GetResponse();
                stream = response.GetResponseStream();
                if (!response.ContentType.ToLower().StartsWith("text/"))
                    value = SaveBinaryFile(response, savePath);
            }
            finally
            {
                if (stream != null) stream.Close();
                if (response != null) response.Close();
            }
            return value;
        }
        private static bool SaveBinaryFile(WebResponse response, string savePath)
        {
            bool value = false;
            byte[] buffer = new byte[1024];
            Stream outStream = null;
            Stream inStream = null;
            try
            {
                if (File.Exists(savePath)) File.Delete(savePath);
                outStream = System.IO.File.Create(savePath);
                inStream = response.GetResponseStream();
                int l;
                do
                {
                    l = inStream.Read(buffer, 0, buffer.Length);
                    if (l > 0) outStream.Write(buffer, 0, l);
                } while (l > 0);
                value = true;
            }
            finally
            {
                if (outStream != null) outStream.Close();
                if (inStream != null) inStream.Close();
            }
            return value;
        }

 
    }
}
