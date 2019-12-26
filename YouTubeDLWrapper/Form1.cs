using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace YouTubeDLWrapper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            addLogBox("Application Start");
            
            if (!File.Exists("youtube-dl.exe"))
            {
                MessageBox.Show("同じディレクトリ内にyoutube-dl.exeを配置してください。",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Application.Exit();
            }
            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }
        }
        void addLogBox(string text)
        {
            string date = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            textBox1.Text = textBox1.Text + "[" + date + "] " + text + Environment.NewLine;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string url = textBox2.Text;
            addLogBox("Add Process Start: " + url);

            string vid = getID(url);
            if (vid == null)
            {
                addLogBox("get vid error [debug: 1]");
                MessageBox.Show("動画IDをURLから取得できませんでした。",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            string title = getTitle(url);
            if (title == null)
            {
                addLogBox("get title error [debug: 1]");
                MessageBox.Show("動画タイトルをURLから取得できませんでした。",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            /*
            string pattern = "[/?=]?([-\\w]{11})";
            Match match = Regex.Match(url, pattern);
            if (!match.Success)
            {
                addLogBox("Pattern mismatch [debug: 1]");
                MessageBox.Show("YouTubeのIDをURLから取得できませんでした。(1)",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (!match.Groups[1].Success)
            {
                addLogBox("Pattern mismatch [debug: 2]");
                MessageBox.Show("YouTubeのIDをURLから取得できませんでした。(2)",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            string vid = match.Groups[1].Value;
            addLogBox("VID: " + vid);
            */
            /*
            string res = null;
            try
            {
                res = new WebClient().DownloadString("http://youtube.com/get_video_info?video_id=" + vid);
            }catch(WebException ex)
            {
                addLogBox("YouTubeAPI Response: " + ex.Message);
                MessageBox.Show("指定されたYouTubeURLの動画情報を取得できませんでした。(1)" + Environment.NewLine +
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            string title = GetArgs(res, "title", '&');
            addLogBox("MovTitle: " + title);
            if (title.Equals(string.Empty))
            {
                MessageBox.Show("指定されたYouTubeURLの動画情報を取得できませんでした。(2)",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            string author = GetArgs(res, "author", '&');
            addLogBox("MovAuthor: " + author);
            */
            foreach (DataGridViewRow data in dataGridView1.Rows.Cast<DataGridViewRow>())
            {
                if(vid.Equals(data.Cells[2].Value.ToString()))
                {
                    MessageBox.Show("指定された動画はすでにキューに登録されています。",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                    return;
                }
            }

            dataGridView1.Rows.Add(title, vid, url, "待機中");
            addLogBox("VID: " + vid + " Added!");
        }

        private string getID(string url)
        {
            Process p = new Process();
            p.StartInfo.FileName = "youtube-dl.exe";
            p.StartInfo.Arguments = "--get-id " + url;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = false;
            p.StartInfo.CreateNoWindow = true;

            p.Start();
            string result = p.StandardOutput.ReadLine();
            p.WaitForExit(60000);
            if (p.ExitCode == 0)
            {
                p.Close();
                return result;
            }
            else
            {
                p.Close();
                return null;
            }
        }

        private string getTitle(string url)
        {
            Process p = new Process();
            p.StartInfo.FileName = "youtube-dl.exe";
            p.StartInfo.Arguments = "-e " + url;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = false;
            p.StartInfo.CreateNoWindow = true;

            p.Start();
            string result = p.StandardOutput.ReadLine();
            p.WaitForExit(60000);
            if (p.ExitCode == 0)
            {
                p.Close();
                return result;
            }
            else
            {
                p.Close();
                return null;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            addLogBox("DL Process Start");

            addLogBox("Files Count: " + dataGridView1.Rows.Count);

            int success = 0;
            int error = 0;

            progressBar1.Maximum = dataGridView1.Rows.Count;
            progressBar1.Value = 0;

            foreach (DataGridViewRow data in dataGridView1.Rows.Cast<DataGridViewRow>())
            {
                string vid = data.Cells[1].Value.ToString();
                string url = data.Cells[2].Value.ToString();
                addLogBox("DL " + vid + " Start... (" + url + ")");
                Process p = Process.Start(
                    "youtube-dl.exe",
                    "-c --ignore-config " + url +
                    " -o output\\%(title)s.%(ext)s");

                p.WaitForExit();
                if (p.ExitCode == 0)
                {
                    addLogBox("Success!");
                    data.Cells[3].Value = "成功";
                    success++;
                }
                else
                {
                    addLogBox("Error... (" + p.ExitCode + ")");
                    data.Cells[3].Value = "エラー (" + p.ExitCode + ")";
                    error++;
                }
                progressBar1.Value++;
                
                addLogBox("DL " + vid + " End...");
            }
            MessageBox.Show("処理終了" + Environment.NewLine + "成功数: " + success + Environment.NewLine + "失敗数: " + error,
                    "Result",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            addLogBox("成功数: " + success);
            addLogBox("失敗数: " + error);
            button2.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            addLogBox("Cleared List");
        }
        private void Button4_Click(object sender, EventArgs e)
        {
            // YouTube DL Updater

            addLogBox("YouTubeDL Update Start...");
            Process p = Process.Start(
                "youtube-dl.exe",
                "-U");

            p.WaitForExit();
            if (p.ExitCode == 0)
            {
                addLogBox("Success!");
            }
            else
            {
                addLogBox("Error... (" + p.ExitCode + ")");
            }
            addLogBox("YouTubeDL Update End.");
        }
    }
}
