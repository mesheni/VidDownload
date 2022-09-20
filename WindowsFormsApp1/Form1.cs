using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace VidDownload
{
    public partial class Form1 : Form
    {
        Form helpForm = new HelpForm();
        private static StringBuilder output = new StringBuilder();

        public Form1()
        {
            InitializeComponent();
            
            string filePath = @".\MyVideos\";
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
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
                                LText.Text += Environment.NewLine + e.Data;
                                LText.SelectionStart = LText.TextLength;
                                LText.ScrollToCaret();
                            }
                            ));
                        }
                        else
                        {
                            LText.Text += Environment.NewLine + e.Data;
                            LText.SelectionStart = LText.TextLength;
                            LText.ScrollToCaret();
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
            }); 
        }

        public void App(string i)
        {
            LText.Text += i;
        }

        private void butOpenFolder_Click(object sender, EventArgs e)
        {
            string filePath = @".\MyVideos\";
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            string argument = "/select, \"" + filePath + "\"";
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        private void helpMenu_Click(object sender, EventArgs e)
        {
            if (helpForm.Visible == true)
            {
                helpForm.Focus();
            } else
            {
                helpForm.Show();
            }
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