using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace wnacg
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.comboBox1.Text = this.comboBox1.Items[0].ToString();
            this.comboBox2.Text = this.comboBox2.Items[0].ToString();
            //this.textBox4.Text = Http.GetProxyServer();
        }

        private int radioType = -1;

        private List<Comic> Comics = null;

        private void button2_Click(object sender, EventArgs e)
        {
            if (radioType == -1)
            {
                MessageBox.Show("请选择要解析的本子类型");
                return;
            }
            if(textBox1.Text == null || textBox1.Text == "")
            {
                MessageBox.Show("请输入要从第几页开始解析");
                return;
            }
            if (textBox2.Text == null || textBox2.Text == "")
            {
                MessageBox.Show("请输入要到第几页结束解析");
                return;
            }

            button2.Enabled = false;
            Collector cl = new Collector(SynchronizationContext.Current, int.Parse(textBox1.Text), int.Parse(textBox2.Text), radioType, comboBox2.Text);
            cl.CollectorLog += (o, text) => 
            {
                if (text == "解析完成") {
                    Comics = cl.Comics;
                }
               this.textCollectorLog.AppendText(text + "\r\n");
            };
            cl.DownloadList += (o, text) =>
            {
                //this.dlList.AppendText(text);
            };
            cl.Start();
        }

        private void download_Click(object sender, EventArgs e)
        {
            try
            {
                if (Comics == null )
                {
                    MessageBox.Show("请先解析页面");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("请先解析页面");
                return;
            }
            

            tabControl1.SelectTab(1);
            comboBox1.Enabled = false;
            download.Enabled = false;
            Download dw = new Download(SynchronizationContext.Current, Comics, int.Parse(comboBox1.Text), comboBox2.Text);
            dw.DownloadLog += (o, text) => {
                this.textCollectorLog.AppendText(text + "\r\n");
            };
            dw.DownloadStart += (o, test) =>
            {
                //int c = this.dlPanel.Controls.Count;
                string[] strs = ((string)test).Split('|');
                Panel p = new System.Windows.Forms.Panel();
                p.SuspendLayout();
                p.Size = new System.Drawing.Size(420, 43);
                //p.Location = new System.Drawing.Point(6, 49 * c + 6);
                p.Location = new System.Drawing.Point(6, 55);
                p.Margin = new System.Windows.Forms.Padding(6,6,6,6);
                p.BackColor = System.Drawing.Color.Gray;
                p.Name = strs[0];

                Label name = new System.Windows.Forms.Label();
                name.Name = "bzName";
                name.AutoSize = true;
                name.Location = new System.Drawing.Point(3, 5);
                name.Size = new System.Drawing.Size(41, 12);
                name.TabIndex = 1;
                name.Text = strs[1];

                Label speed = new System.Windows.Forms.Label();
                speed.Name = "bzSpeed";
                speed.AutoSize = true;
                speed.Location = new System.Drawing.Point(3, 25);
                speed.Size = new System.Drawing.Size(41, 12);
                speed.TabIndex = 1;
                speed.Text = "任务创建中...";

                p.Controls.Add(name);
                p.Controls.Add(speed);

                this.flowDlPanel.Controls.Add(p);     
            };
            dw.DownloadSpeed += (o, test) =>
            {
                string[] strs = ((string)test).Split('|');
                Panel p = (Panel)this.flowDlPanel.Controls.Find(strs[0],false)[0];
                Label speed = (Label)p.Controls.Find("bzSpeed", false)[0];
                speed.Text = strs[1];

                if (strs[1] == "完成")
                {
                    this.flowDlPanel.Controls.Remove(p);
                    this.flowOkPanel.Controls.Add(p);
                }
                else if (((string)test).IndexOf("失败") != -1) 
                {
                    this.flowDlPanel.Controls.Remove(p);
                    this.flowErrorPanel.Controls.Add(p);
                }
            };
            dw.Start();
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            //单行本
            this.radioType = 9;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            //短篇
            this.radioType = 10;
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            this.radioType = 1;
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            Http.SetProxy(textBox4.Text);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (Comics == null)
                {
                    MessageBox.Show("请先解析页面");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("请先解析页面");
                return;
            }

            string logpath = AppDomain.CurrentDomain.BaseDirectory;
            string dirPath = logpath + "data\\";
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
            foreach(Comic c in Comics)
            {
                string fileStr = dirPath + "\\" + c.Title + ".wnacgdb";
                if (!File.Exists(fileStr))
                {
                    FileStream fs = null;
                    StreamWriter sw = null;
                    try
                    {
                        fs = new FileStream(fileStr, FileMode.Create, FileAccess.Write);//创建写入文件
                        sw = new StreamWriter(fs);
                        sw.WriteLine(c.Id);
                        sw.WriteLine(c.Cover);
                        foreach (int k in c.Contents.Keys) {
                            sw.WriteLine(k + "|" + c.Contents[k]);
                        }
                    } catch {

                    }
                    finally {
                        if (sw != null) sw.Close();
                        if(fs != null) fs.Close();
                    }
                }
            }
            
        }//method 

        private void button3_Click(object sender, EventArgs e)
        {
            if (Comics != null) {
                MessageBox.Show("请勿重复导入");
                return;
            }
            Comics = new List<Comic>();
            string logpath = AppDomain.CurrentDomain.BaseDirectory;
            string dirPath = logpath + "data\\";
            string historyPath = logpath + "download\\history\\";
            if (!Directory.Exists(dirPath)) return;

            List<string> files = new List<string>(Directory.GetFiles(dirPath));
            files.ForEach(filePath =>
            {
                string title = System.IO.Path.GetFileNameWithoutExtension(filePath);
                if (File.Exists(historyPath + title)) return ;
                int x = 0;
                Comic c = new Comic();
                c.Title = title;
                StreamReader sr = null;
                try
                {
                    sr = new StreamReader(filePath, Encoding.UTF8);
                    String line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (x == 0)
                            c.Id = line;
                        else if (x == 1)
                            c.Cover = line;
                        else
                        {
                            string[] cc = line.Split('|');
                            c.Contents.Add(int.Parse(cc[0]), cc[1]);
                        }
                        x++;
                    }//while
                    Comics.Add(c);
                }
                catch{ 

                }
                finally {
                    if (sr != null) sr.Close();
                }
            });
            Comics = RandomSortList(Comics);
            MessageBox.Show("导入数量为:"+Comics.Count);
        }

        public List<T> RandomSortList<T>(List<T> ListT)
        {
            Random random = new Random();
            List<T> newList = new List<T>();
            foreach (T item in ListT)
            {
                newList.Insert(random.Next(newList.Count + 1), item);
            }
            return newList;
        }

        private void pageKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsNumber(e.KeyChar)) {
                e.Handled = true;
                this.textCollectorLog.AppendText("页码只能输入数字\r\n");
            }   
        }
    }//class
}
