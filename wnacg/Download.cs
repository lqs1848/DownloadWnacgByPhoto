using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace wnacg
{
    class Download
    {

        String _basePath = "https://www.wnacg.wtf";//"https://wnacg.net"; //"http://d3.wnacg.download"; //"https://wnacg.net";// "https://www.wnacg.org";
        String photoPath = "/photos-view-id-{0}.html";
        string qz = "http:";
        public EventHandler<String> DownloadLog;
        public EventHandler<String> DownloadStart;
        public EventHandler<String> DownloadSpeed;

        SynchronizationContext _syncContext;

        private Queue<Comic> comics;
        //线程数
        private int _CycleNum = 1;

        private string _ProxyStr = null;

        public Download(SynchronizationContext formContext, List<Comic> comiclist, int cycleNum, string basePath,string proxyStr = null)
        {
            this._syncContext = formContext;
            this.comics = new Queue<Comic>(comiclist);
            this._CycleNum = cycleNum;
            if (basePath != null && basePath.Trim() != "")
                this._basePath = basePath;
            if (this._basePath.StartsWith("https"))
                qz = "https:";
            this._ProxyStr = proxyStr;
        }

        private void OutLog(object state)
        {
            DownloadLog?.Invoke(this, state.ToString());
        }

        private void DlTaskStart(object state)
        {
            DownloadStart?.Invoke(this, state.ToString());
        }

        private void DlTaskSchedule(object state)
        {
            DownloadSpeed?.Invoke(this, state.ToString());
        }

        public void Start()
        {
            new Thread(TaskMStart).Start();
        }


        private void TaskMStart()
        {
            Random ran = new Random();
            ThreadPool.SetMinThreads(_CycleNum, _CycleNum);
            ThreadPool.SetMaxThreads(_CycleNum, _CycleNum);
            for (int i = 1; i <= _CycleNum; i++)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(DownloadFun));
                Thread.Sleep(ran.Next(3000)+500);
            }
        }

        private void DownloadFun(object obj)
        {
            Random random = new Random();
            string logpath = AppDomain.CurrentDomain.BaseDirectory;
            string dirPath = logpath + "download\\";
            string downloading = dirPath + "progress\\";
            string downloadok = dirPath + "ok\\";

            if (!Directory.Exists(downloadok))
                Directory.CreateDirectory(downloadok);


            cw: while (comics.Count > 0)
            {
                Comic c = comics.Dequeue();

                string historyPath = dirPath + "history\\";
                if (!Directory.Exists(historyPath))
                {
                    Directory.CreateDirectory(historyPath);
                }
                if (File.Exists(historyPath + c.Title))
                {
                    _syncContext.Post(OutLog, "曾经下载过:" + c.Title + " 跳过\r\n");
                    continue;
                }

                string comicPath = downloading + c.Title + "\\";
                _syncContext.Post(DlTaskStart, c.Id + "|" + c.Title);

                //封面
                if (!HttpDownloadFile(qz + c.Cover, comicPath, Utils.parseNumName(0, 4)))
                {
                    _syncContext.Post(DlTaskSchedule, c.Id + "|封面下载失败");
                    ExeLog.WriteLog("["+c.Title + "]封面下载失败\r\n" + "(" + (qz + c.Cover) + ")\r\n");
                    goto cw;
                }
                int x = 1;
                foreach (int k in c.Contents.Keys)
                {
                    _syncContext.Post(DlTaskSchedule, c.Id + "|" + x + "/" + c.Contents.Count);
                    string pid = c.Contents[k];
                    string photoPage = null;
                    try
                    {
                        photoPage = Http.GetHtml(_basePath + String.Format(photoPath, pid));
                    }
                    catch (Exception e) 
                    {
                        _syncContext.Post(DlTaskSchedule, c.Id + "|第" + x + "页读取失败 e:"+e.Message);
                        ExeLog.WriteLog("[" + c.Title + "]第" + x + "页读取失败\r\n" + "(" + _basePath + String.Format(photoPath, pid) + ")\r\n");
                        goto cw;
                    }
                    string photoUrl = qz + new Regex(@"<img id=""picarea"" class=""photo"" alt="".*?"" src=""(.*?)"" />").Match(photoPage).Groups[1].Value.Trim();

                    if (!HttpDownloadFile(photoUrl, comicPath, Utils.parseNumName(k, 4)))
                    {
                        _syncContext.Post(DlTaskSchedule, c.Id + "|第"+x+"页下载失败");
                        ExeLog.WriteLog("[" + c.Title + "]第" + x + "页下载失败\r\n" + "(" + photoUrl + ")\r\n");
                        goto cw;
                    }
                    FileInfo fileInfo = new FileInfo(comicPath + Utils.parseNumName(k, 4) + Utils.getPhotoExt(photoUrl));
                    if (!fileInfo.Exists || fileInfo.Length <= 100) {
                        _syncContext.Post(DlTaskSchedule, c.Id + "|第" + x + "页下载失败");
                        ExeLog.WriteLog("[" + c.Title + "]第" + x + "页下载失败\r\n" + "(" + photoUrl + ")\r\n");
                        goto cw;
                    }

                    _syncContext.Post(DlTaskSchedule, c.Id + "|" + x + "/" + c.Contents.Count);
                    x++;
                }//for

                _syncContext.Post(DlTaskSchedule, c.Id + "|压缩中...");

                if (ZipHelper.Zip(comicPath, downloadok + c.Title + ".zip"))
                {
                    Directory.Delete(comicPath, true);
                    File.Create(historyPath + c.Title).Close();
                    _syncContext.Post(DlTaskSchedule, c.Id + "|完成");
                }
                else 
                {
                    _syncContext.Post(DlTaskSchedule, c.Id + "|zip压缩失败");
                    ExeLog.WriteLog("[" + c.Title + "]zip压缩失败\r\n");
                }
            }//while comic


            _syncContext.Post(OutLog, "线程退出");
        }//method



        public bool HttpDownloadFile(string url, string path, string fileName)
        {
            return HttpDownloadFile(url, path, fileName, 0);
        }

        public bool HttpDownloadFile(string url, string path,string fileName,int deep)
        {
            string filePath = path + fileName + Utils.getPhotoExt(url);
            if (deep == 0 && !Directory.Exists(path))
                Directory.CreateDirectory(path);
            
            if (File.Exists(filePath))
                if (new FileInfo(filePath).Length > 1024)
                    return true;

            try
            {
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    if (!string.IsNullOrWhiteSpace(this._ProxyStr)) {
                        wc.Proxy = new WebProxy(new Uri(this._ProxyStr));
                    }
                    wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.136 YaBrowser/20.2.4.141 Yowser/2.5 Safari/537.36");
                    wc.DownloadFile(url, filePath);//保存到本地的文件名和路径，请自行更改
                }
                FileInfo fileInfo = new System.IO.FileInfo(filePath);
                if (!fileInfo.Exists || fileInfo.Length <= 100)
                {
                    return false;
                }
                return true;
            }
            catch 
            {
                try
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch 
                {
                    return false;
                }
                
                //重试3次
                if (deep > 3)
                {
                    return false;
                    // _syncContext.Post(DlTaskSchedule, key + "|无法下载");
                }
                else
                {
                    Thread.Sleep(1000);
                    return HttpDownloadFile(url, path, fileName, ++deep);
                }
            }
            /*
                        try
                        {
                            // 设置参数
                            HttpWebRequest request = Http.GetWebRequest(url);
                            //发送请求并获取相应回应数据
                            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                            request.Timeout = 30 * 1000;
                            double dataLengthToRead = response.ContentLength;
                            //直到request.GetResponse()程序才开始向目标网页发送Post请求
                            Stream responseStream = response.GetResponseStream();
                            //创建本地文件写入流
                            Stream stream = new FileStream(path+ fileName + ".temp.wnacg", FileMode.Create);
                            byte[] bArr = new byte[1024 * 512];
                            int size = responseStream.Read(bArr, 0, (int)bArr.Length);

                            while (size > 0)
                            {
                                stream.Write(bArr, 0, size);
                                size = responseStream.Read(bArr, 0, (int)bArr.Length);
                            }
                            stream.Close();
                            responseStream.Close();

                            File.Move(path + fileName + ".temp.wnacg", filePath);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            //Console.Out.Write(ex.StackTrace);

                            //重试3次
                            if (deep > 3)
                            {
                                return false;
                                // _syncContext.Post(DlTaskSchedule, key + "|无法下载");
                            }
                            else
                            {
                                Thread.Sleep(1000);
                                return HttpDownloadFile(url, path, fileName, deep);
                            }
                        }*/

        }//method
    }//class
}//namespace
