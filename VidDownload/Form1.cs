using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace VidDownload
{
    public partial class Form1 : Form
    {
        Form helpForm = new HelpForm();
        private static StringBuilder output = new StringBuilder();

        public Form1()
        {
            InitializeComponent();
            
            string videoPath = @".\MyVideos\";
            string logPath = @".\log\";
            if (!Directory.Exists(videoPath))
            {
                Directory.CreateDirectory(videoPath);
            }
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
        }

        private async void DLBut_ClickAsync(object sender, EventArgs e)
        {
            if (SText.Text == "")
            {
                MessageBox.Show("Скопируйте ссылку!");
            } else
            {
                try
                {
                    var progress = new Progress<string>(s => progressBar1.Value = Convert.ToInt32(s));
                    await Task.Run(() => Download(progress));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public async void Download(IProgress<string> progress)
        {
            DLBut.Invoke(new Action(() => DLBut.Enabled = false));

            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
            string log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"log\" 
                        + dateTime + "_log.txt");
            FileStream fs = new FileStream(log, FileMode.CreateNew);
            StreamWriter w = new StreamWriter(fs, Encoding.Default);

            await Task<string>.Run(() =>
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                if (checkPlaylist.Checked == true)
                {
                    proc.StartInfo.FileName = @".\yt-dlp.exe";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.Arguments = $"yt-dlp -o \"./MyVideos/%(playlist)s/%(playlist_index)s- %(title)s.%(ext)s\" \"{SText.Text}\"";
                }
                else
                {
                    proc.StartInfo.FileName = @".\yt-dlp.exe";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.Arguments = $"yt-dlp -P \"./MyVideos\" {SText.Text}";
                }
                proc.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        if (this.InvokeRequired)
                        {
                            this.Invoke((MethodInvoker)(() =>
                            {
                                logLabel.Text = e.Data;
                                w.WriteLine(e.Data);
                            }
                            ));
                        }
                        else
                        {
                            logLabel.Text = e.Data;
                            w.WriteLine(e.Data);
                        }
                    }
                });
                progress.Report("10");
                proc.Start();
                progress.Report("20");
                proc.BeginOutputReadLine();
                progress.Report("30");
                proc.WaitForExit();
                progress.Report("90");
                proc.Close();
                progress.Report("100");
                w.Close();
                fs.Close();
                DLBut.Invoke(new Action(() => DLBut.Enabled = true));
                logLabel.Invoke(new Action(() => logLabel.Text = ""));
            });
        }

        private void butOpenFolder_Click(object sender, EventArgs e)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"MyVideos\");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            string argument = "/open, \"" + path;
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        private void helpMenu_Click(object sender, EventArgs e)
        {
            Form helpForm = new HelpForm();
            helpForm.ShowDialog();
        }
    }
}
//await Task.Run(() =>
//{
//    if (this.InvokeRequired)
//    {
//        this.Invoke((MethodInvoker)(() =>
//        {
//            App(proc.StandardOutput.ReadLineAsync().ToString());

//        }
//        ));
//    }
//    else
//    {
//        App(proc.StandardOutput.ReadLineAsync().ToString());
//    }
//});