using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Drawing;
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
                if (!HttpDownloadImage(qz + c.Cover, comicPath, Utils.parseNumName(0, c.Contents.Count > 9999 ? 5 : 4)))
                {
                    _syncContext.Post(DlTaskSchedule, c.Id + "|封面下载失败");
                    ExeLog.WriteLog("["+c.Title + "]封面下载失败\r\n" + "(" + (qz + c.Cover) + ")\r\n");
                    goto cw;
                }

                Dictionary<string,string> lastDownloadFiles = new Dictionary<string,string>();
                List<string> files = new List<string>(Directory.GetFiles(comicPath));
                foreach(string f in files) 
                {
                    lastDownloadFiles.Add(System.IO.Path.GetFileNameWithoutExtension(f),f);
                }//for

                foreach (int k in c.Contents.Keys)
                {
                    string nk = Utils.parseNumName(k, c.Contents.Count > 9999 ? 5 : 4);
                    if (lastDownloadFiles.ContainsKey(nk)) {
                        string lastFile = lastDownloadFiles[nk];
                        if (new FileInfo(lastFile).Length > 1024 && IsCompletedImage(lastFile))
                            continue;
                    }

                    _syncContext.Post(DlTaskSchedule, c.Id + "|" + nk + "/" + c.Contents.Count);
                    string pid = c.Contents[k];
                    string photoPage = null;
                    try
                    {
                        photoPage = Http.GetHtml(_basePath + String.Format(photoPath, pid));
                    }
                    catch (Exception e) 
                    {
                        _syncContext.Post(DlTaskSchedule, c.Id + "|第" + nk + "页读取失败 e:"+e.Message);
                        ExeLog.WriteLog("[" + c.Title + "]第" + nk + "页读取失败\r\n" + "(" + _basePath + String.Format(photoPath, pid) + ")\r\n");
                        goto cw;
                    }
                    string photoUrl = qz + new Regex(@"<img id=""picarea"" class=""photo"" alt="".*?"" src=""(.*?)"" />").Match(photoPage).Groups[1].Value.Trim();

                    if (!HttpDownloadImage(photoUrl, comicPath, nk))
                    {
                        _syncContext.Post(DlTaskSchedule, c.Id + "|第"+ nk + "页下载失败");
                        ExeLog.WriteLog("[" + c.Title + "]第" + nk + "页下载失败\r\n" + "(" + photoUrl + ")\r\n");
                        goto cw;
                    }
                    FileInfo fileInfo = new FileInfo(comicPath + nk + Utils.getPhotoExt(photoUrl));
                    if (!fileInfo.Exists || fileInfo.Length <= 100) {
                        _syncContext.Post(DlTaskSchedule, c.Id + "|第" + nk + "页下载失败");
                        ExeLog.WriteLog("[" + c.Title + "]第" + nk + "页下载失败\r\n" + "(" + photoUrl + ")\r\n");
                        goto cw;
                    }

                    _syncContext.Post(DlTaskSchedule, c.Id + "|" + nk + "/" + c.Contents.Count);
                    
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



        public bool HttpDownloadImage(string url, string path, string fileName)
        {
            return HttpDownloadImage(url, path, fileName, 0);
        }

        public bool HttpDownloadImage(string url, string path,string fileName,int deep)
        {
            string filePath = path + fileName + Utils.getPhotoExt(url);
            if (deep == 0 && !Directory.Exists(path))
                Directory.CreateDirectory(path);
            
            if (File.Exists(filePath))
                if (new FileInfo(filePath).Length > 1024 && IsCompletedImage(filePath))
                    return true;

            try
            {
                using (WebClient wc = new WebClient())
                {
                    if (!string.IsNullOrWhiteSpace(this._ProxyStr)) {
                        string[] proxys = this._ProxyStr.Split(':');
                        wc.Proxy = new WebProxy(proxys[0], int.Parse(proxys[1]));
                    }
                    wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.136 YaBrowser/20.2.4.141 Yowser/2.5 Safari/537.36");
                    wc.DownloadFile(url, filePath);//保存到本地的文件名和路径，请自行更改
                }
                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists || fileInfo.Length <= 100 || !IsCompletedImage(filePath))
                {
                    return false;
                }
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
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
                    return HttpDownloadImage(url, path, fileName, ++deep);
                }
            }

        }//method

        //检测图片完整性
        private static bool IsCompletedImage(string strFileName)
        {
            try
            {
                FileStream fs = new FileStream(strFileName, FileMode.Open);
                BinaryReader reader = new BinaryReader(fs);
                try
                {
                    byte[] szBuffer = reader.ReadBytes((int)fs.Length);
                    //jpg png图是根据最前面和最后面特殊字节确定. bmp根据文件长度确定
                    //png检查
                    if (szBuffer[0] == 137 && szBuffer[1] == 80 && szBuffer[2] == 78 && szBuffer[3] == 71 && szBuffer[4] == 13
                        && szBuffer[5] == 10 && szBuffer[6] == 26 && szBuffer[7] == 10)
                    {
                        //&& szBuffer[szBuffer.Length - 8] == 73 && szBuffer[szBuffer.Length - 7] == 69 && szBuffer[szBuffer.Length - 6] == 78
                        if (szBuffer[szBuffer.Length - 5] == 68 && szBuffer[szBuffer.Length - 4] == 174 && szBuffer[szBuffer.Length - 3] == 66
                            && szBuffer[szBuffer.Length - 2] == 96 && szBuffer[szBuffer.Length - 1] == 130)
                            return true;
                        //有些情况最后多了些没用的字节
                        for (int i = szBuffer.Length - 1; i > szBuffer.Length / 2; --i)
                        {
                            if (szBuffer[i - 5] == 68 && szBuffer[i - 4] == 174 && szBuffer[i - 3] == 66
                             && szBuffer[i - 2] == 96 && szBuffer[i - 1] == 130)
                                return true;
                        }


                    }
                    else if (szBuffer[0] == 66 && szBuffer[1] == 77)//bmp
                    {
                        //bmp长度
                        //整数转成字符串拼接
                        string str = Convert.ToString(szBuffer[5], 16) + Convert.ToString(szBuffer[4], 16)
                            + Convert.ToString(szBuffer[3], 16) + Convert.ToString(szBuffer[2], 16);
                        int iLength = Convert.ToInt32("0x" + str, 16); //16进制数转成整数
                        if (iLength <= szBuffer.Length) //有些图比实际要长
                            return true;
                    }
                    else if (szBuffer[0] == 71 && szBuffer[1] == 73 && szBuffer[2] == 70 && szBuffer[3] == 56)//gif
                    {
                        //标准gif 检查00 3B
                        if (szBuffer[szBuffer.Length - 2] == 0 && szBuffer[szBuffer.Length - 1] == 59)
                            return true;
                        //检查含00 3B
                        for (int i = szBuffer.Length - 1; i > szBuffer.Length / 2; --i)
                        {
                            if (szBuffer[i] != 0)
                            {
                                if (szBuffer[i] == 59 && szBuffer[i - 1] == 0)
                                    return true;
                            }
                        }
                    }
                    else if (szBuffer[0] == 255 && szBuffer[1] == 216) //jpg
                    {
                        //标准jpeg最后出现ff d9
                        if (szBuffer[szBuffer.Length - 2] == 255 && szBuffer[szBuffer.Length - 1] == 217)
                            return true;
                        else
                        {
                            //有好多jpg最后被人为补了些字符也能打得开, 算作完整jpg, ffd9出现在近末端
                            //jpeg开始几个是特殊字节, 所以最后大于10就行了 从最后字符遍历
                            //有些文件会出现两个ffd9 后半部分ffd9才行
                            for (int i = szBuffer.Length - 2; i > szBuffer.Length / 2; --i)
                            {
                                //检查有没有ffd9连在一起的
                                if (szBuffer[i] == 255 && szBuffer[i + 1] == 217)
                                    return true;
                            }
                        }
                    }
                }
                catch
                {
                }
                finally
                {
                    if (fs != null)
                        fs.Close();
                    if (reader != null)
                        reader.Close();
                }
            }
            catch
            {
                return false;
            }
            return false;
        }
    }//class
}//namespace
