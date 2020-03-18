using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace wnacg
{
    static class Utils
    {
        public static string getFolderName(string folderName) {

            folderName = folderName.Replace("\\", "‖");
            folderName = folderName.Replace("/", "‖");
            folderName = folderName.Replace(":", "：");
            folderName = folderName.Replace("*", "※");
            folderName = folderName.Replace("?", "？");
            folderName = folderName.Replace("<", "〈");
            folderName = folderName.Replace(">", "〉");
            folderName = folderName.Replace("\n", " ");
            folderName = folderName.Replace("\r", " ");
            //folderName = Regex.Replace(folderName, @"[/n/r]", " ");

            StringBuilder rBuilder = new StringBuilder(folderName);
            foreach (char rInvalidChar in Path.GetInvalidPathChars())
                rBuilder.Replace(rInvalidChar.ToString(), string.Empty);
            return rBuilder.ToString();
            
        }//method getPath

        public static string parseNumName(int name, int i) {
            string str = name.ToString();
            for (int x = i - str.Length; x > 0; x--)
                str = "0" + str;
            return str;
        }//method parseNumName
        public static string getPhotoExt(string url)
        {
            return getPhotoExt(url, ".jpg");
        }

            public static string getPhotoExt(string url,string defext) {
            //bmp,jpg,png,tif,gif,pcx,tga,exif,fpx,svg,psd,cdr,pcd,dxf,ufo,eps,ai,raw,WMF,webp
            string low = url.ToLowerInvariant();
            if (low.EndsWith(".bmp")) return ".bmp";
            if (low.EndsWith(".png")) return ".png";
            if (low.EndsWith(".tif")) return ".tif";
            if (low.EndsWith(".gif")) return ".gif";
            if (low.EndsWith(".pcx")) return ".pcx";
            if (low.EndsWith(".tga")) return ".tga";
            if (low.EndsWith(".exif")) return ".exif";
            if (low.EndsWith(".fpx")) return ".fpx";
            if (low.EndsWith(".svg")) return ".svg";
            if (low.EndsWith(".psd")) return ".psd";
            if (low.EndsWith(".cdr")) return ".cdr";
            if (low.EndsWith(".pcd")) return ".pcd";
            if (low.EndsWith(".dxf")) return ".dxf";
            if (low.EndsWith(".ufo")) return ".ufo";
            if (low.EndsWith(".eps")) return ".eps";
            if (low.EndsWith(".ai")) return ".ai";
            if (low.EndsWith(".raw")) return ".raw";
            if (low.EndsWith(".wmf")) return ".wmf";
            if (low.EndsWith(".webp")) return ".webp";
            if (low.EndsWith(".jpg")) return ".jpg";
            return defext;
            
        }
    }//class
}
