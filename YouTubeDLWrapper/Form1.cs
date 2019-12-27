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
using YouTubeDLWrapper.Properties;

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
                MessageBox.Show(Resources.NOTFOUND_YOUTUBEDL,
                    Resources.ERROR,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Application.Exit();
                return;
            }
            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }
        }

        delegate void Delegate(string text);
        void addLogBox(string text)
        {
            string date = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            textBox1.Text = textBox1.Text + "[" + date + "] " + text + Environment.NewLine;

            textBox1.SelectionStart = textBox1.Text.Length - Environment.NewLine.Length;
            textBox1.ScrollToCaret();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string url = textBox2.Text;

            await Task.Run(() => Add(url));
        }

        private async void parseCPButton_Click(object sender, EventArgs e)
        {
            parseCPButton.Enabled = false;
            addLogBox(Resources.ParseClipBoard);
            if (!Clipboard.ContainsText())
            {
                MessageBox.Show(Resources.INFO,
                    Resources.ERROR,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                parseCPButton.Enabled = true;
                return;
            }
            string urls_str = Clipboard.GetText();
            if (!urls_str.Contains('\n'))
            {
                if (await Task.Run(() => Add(urls_str.Trim(), false)))
                {
                    MessageBox.Show(string.Format(Resources.URL_PARSED_AND_ADDED, 1),
                        Resources.INFO,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(string.Format(Resources.URL_PARSED_AND_ADDERR, 1),
                        Resources.INFO,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                parseCPButton.Enabled = true;
                return;
            }
            string[] urls = urls_str.Split('\n');
            int success = 0;
            int failed = 0;
            foreach (string url in urls)
            {
                if(url.Trim().Length == 0)
                {
                    continue;
                }
                if(await Task.Run(() => Add(url, false)))
                {
                    success++;
                }
                else
                {
                    failed++;
                }
            }

            MessageBox.Show(string.Format(Resources.URL_PARSED, 1) + Environment.NewLine + Resources.SUCCESS + ": " + success + Environment.NewLine + Resources.FAILED + ": " + failed,
                Resources.INFO,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
                parseCPButton.Enabled = true;
        }

        private bool Add(string url)
        {
            return Add(url, true);
        }
        private bool Add(string url, bool popup)
        {
            Invoke(new Delegate(addLogBox), Resources.START_ADD_PROCESS + ": " + url);

            string vid = getID(url);
            if (vid == null)
            {
                Invoke(new Delegate(addLogBox), Resources.COULD_NOT_GET_VIDEOID + " (" + url + ")");
                if (popup)
                {
                    MessageBox.Show(Resources.COULD_NOT_GET_VIDEOID,
                        Resources.ERROR,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                return false;
            }
            Invoke(new Delegate(addLogBox), "VID: " + vid);

            string title = getTitle(url);
            if (title == null)
            {
                Invoke(new Delegate(addLogBox), Resources.COULD_NOT_GET_VIDEOTITLE + " (" + vid + ")");
                if (popup)
                {
                    MessageBox.Show(Resources.COULD_NOT_GET_VIDEOTITLE,
                    Resources.ERROR,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                }
                return false;
            }
            Invoke(new Delegate(addLogBox), "Title: " + title);

            foreach (DataGridViewRow data in dataGridView1.Rows.Cast<DataGridViewRow>())
            {
                if (vid.Equals(data.Cells[2].Value.ToString()))
                {
                    Invoke(new Delegate(addLogBox), Resources.ALREADY_ADDED_QUEUE + " (" + vid + ")");
                    if (popup)
                    {
                        MessageBox.Show(Resources.ALREADY_ADDED_QUEUE,
                        Resources.ERROR,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    }
                    return false;
                }
            }

            Invoke(new Delegate(addLogBox), Resources.FINISHED_ADD_PROCESS + ": " + url);
            Invoke(new DelegateRowsAdd(RowsAdd), title, vid, url, Resources.WAITING);
            return true;
        }

        delegate void DelegateRowsAdd(string title, string vid, string url, string status);
        void RowsAdd(string title, string vid, string url, string status)
        {
            dataGridView1.Rows.Add(title, vid, url, status);
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
            addLogBox(Resources.START_DL_PROCESS);

            addLogBox(string.Format(Resources.NUMBER_OF_FILES, dataGridView1.Rows.Count));

            int success = 0;
            int error = 0;

            progressBar1.Maximum = dataGridView1.Rows.Count;
            progressBar1.Value = 0;

            foreach (DataGridViewRow data in dataGridView1.Rows.Cast<DataGridViewRow>())
            {
                string vid = data.Cells[1].Value.ToString();
                string url = data.Cells[2].Value.ToString();
                addLogBox(string.Format(Resources.START_DL, vid) + " (" + url + ")");
                Process p = Process.Start(
                    "youtube-dl.exe",
                    "-c --ignore-config " + url +
                    " -o output\\%(title)s.%(ext)s");

                p.WaitForExit();
                if (p.ExitCode == 0)
                {
                    addLogBox(Resources.SUCCESS);
                    data.Cells[3].Value = Resources.SUCCESS;
                    success++;
                }
                else
                {
                    addLogBox(Resources.ERROR + " (" + p.ExitCode + ")");
                    data.Cells[3].Value = Resources.SUCCESS + " (" + p.ExitCode + ")";
                    error++;
                }
                progressBar1.Value++;

                addLogBox(string.Format(Resources.COMPLETED_DL, vid));
            }
            MessageBox.Show(Resources.DL_PROCESS_FINISHED + Environment.NewLine + Resources.SUCCESS + ": " + success + Environment.NewLine + Resources.FAILED + ": " + error,
                    Resources.INFO,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            addLogBox(Resources.SUCCESS + ": " + success);
            addLogBox(Resources.FAILED + ": " + error);
            button2.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            addLogBox(Resources.QUEUE_CLEARED);
        }
        private void Button4_Click(object sender, EventArgs e)
        {
            // YouTube DL Updater

            addLogBox(Resources.START_UPDATE_YOUTUBEDL);
            Process p = Process.Start(
                "youtube-dl.exe",
                "-U");

            p.WaitForExit();
            if (p.ExitCode == 0)
            {
                addLogBox(Resources.SUCCESSFUL_UPDATE_YOUTUBEDL);
            }
            else
            {
                addLogBox(Resources.FAILED_UPDATE_YOUTUBEDL + " (" + p.ExitCode + ")");
            }
        }
    }
}
