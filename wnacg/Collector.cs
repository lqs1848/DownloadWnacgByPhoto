using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace wnacg
{
    class Collector
    {
        int startPage = -1;
        int endPage = -1;

        String _basePath = "https://www.wnacg.wtf";//"https://wnacg.net"; //"http://d3.wnacg.download"; //"https://wnacg.net";// "https://www.wnacg.org";
        //9漫画汉化 10短篇汉化
        String collectorPath = "/albums-index-page-{0}-cate-{1}.html";
        String detailPath = "/photos-index-aid-{0}.html";
        String photoPath = "/photos-view-id-{0}.html";
        //String downloadPath = "/download-index-aid-{0}.html";

        int bzType;

        public EventHandler<String> CollectorLog;  
        public EventHandler<String> DownloadList;

        public List<Comic> Comics { get; set; }

        SynchronizationContext _syncContext;
        //HttpClient client;
       
        public Collector(SynchronizationContext formContext,int startPage,int endPage,int bzType,string basePath) {
            this._syncContext = formContext;
            this.startPage = startPage;
            this.endPage = endPage;
            this.bzType = bzType;
            if(basePath!=null && basePath.Trim()!="")
                this._basePath = basePath;
       }


        public void Start() {
            //client = new HttpClient();
            new Thread(Collect).Start();
        }

        public void Collect() {
            string logpath = AppDomain.CurrentDomain.BaseDirectory;
            string dirPath = logpath + "data\\";
               

                Comics = new List<Comic>();
            for (int curPage= startPage; curPage <= endPage; curPage++) { 
                _syncContext.Post(OutLog, "分析页面 page:"+curPage);
                try
                {
                    int bzIndex = 0;
                    string listUrl = _basePath + String.Format(collectorPath, curPage, bzType);
                    //string listResult = client.GetStringAsync(listUrl).Result;
                    string listResult = Http.GetHtml(listUrl);
                    Regex rgx = new Regex(@"<li class=""li gallary_item"">\s*?<div class=""pic_box"">\s*?<a href=""/photos-index-aid-(?<mgid>\d+).html""\s*title=""(?<title>.*?)""><img alt="".*?"" src=""(?<img>.*?)""");
                    foreach (Match mch in rgx.Matches(listResult))
                    {
                        Comic comic = new Comic();
                        bzIndex++;
                        string mgid = mch.Groups["mgid"].Value;
                        string title = mch.Groups["title"].Value;
                        string img = mch.Groups["img"].Value;
                        comic.Title = Utils.getFolderName(title);

                        string fileStr = dirPath + "\\" + comic.Title + ".wnacgdb";
                        if (File.Exists(fileStr)) {
                            _syncContext.Post(OutLog, "已解析.跳过 \r" + title + "");
                            continue;
                        }

                        comic.Id = mgid;
                        comic.Cover = img;
                        string detailPage = Http.GetHtml(_basePath + String.Format(detailPath, mgid));
                        string homePhotoId = new Regex(@"<div class=""pic_box""><a href=""/photos-view-id-(\d*).html"">").Match(detailPage).Groups[1].Value;
                        string photoDetailPage = Http.GetHtml(_basePath + String.Format(photoPath, homePhotoId));

                        MatchCollection mats = new Regex(@"<option\s+value=""(\d+)"".*?>第(\d+)頁</option>").Matches(photoDetailPage);
                        foreach (Match m in mats)
                        {
                            comic.Contents.Add(int.Parse(m.Groups[2].Value), m.Groups[1].Value.Trim());
                        }

                        _syncContext.Post(OutLog, "提取 \r" + title + "");

                        //ExeLog.WriteLog("downloadUrl_zip.txt", dwUrl+"\\"+title+".zip\r\n");
                        //_syncContext.Post(AddDwList, dwUrl + "\\" + title + ".zip\r\n");

                        //ExeLog.WriteLog("downloadUrl_jpg.txt", _basePath + img + "\\" + title + ".jpg\r\n");

                        Comics.Add(comic);
                        Thread.Sleep(100);
                    }//foreach
                    if (bzIndex != 12)
                    {
                        ExeLog.WriteLog("当前页面本子数量缺少:" + bzIndex + "/12\r\n页面:" + listUrl + "内容为:\r\n" + listResult);
                    }

                }
                catch (Exception e)
                {
                    _syncContext.Post(OutLog, "解析 page:"+ curPage + "失败 \r" + e.Message + "");
                }
                    
            }//for
            _syncContext.Post(OutLog, "解析完成");
        }//method 

        private void OutLog(object state)
        {
            //ExeLog.WriteLog("exelog.txt", state.ToString()+"\r\n");
            CollectorLog?.Invoke(this, state.ToString());
        }

        private void AddDwList(object state)
        {
            DownloadList?.Invoke(this, state.ToString());
        }
    }//class
}
