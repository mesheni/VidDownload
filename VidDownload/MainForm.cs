using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VidDownload.Download;
using VidDownload.OtherForms;

namespace VidDownload
{
    public partial class MainForm : Form
    {
        private int res = 1080;
        private static List<string> codecList = new List<string>();
        private string codec = "av01";
        private ParseLog parseLog = new ParseLog();

        Form aboutForm = new AboutForm();

        public MainForm()
        {
            InitializeComponent();
            InitApp();
        }

        private async void DLBut_ClickAsync(object sender, EventArgs e)
        {
            if (SText.Text == "" || comboRes.Text == "")
            {
                MessageBox.Show("Пустое поле ссылки или поле разрешения!", "Ошибка!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (int.TryParse(comboRes.Text, out res))
                {
                    if (comboCodec.Text == "" || !(codecList.Exists((i) => i == comboCodec.Text.ToString())))
                    {
                        var progress = new Progress<string>(s => progressBar1.Value = Convert.ToInt32(s));
                        await Task.Run(() => Download(progress));
                    }
                    else
                    {
                        codec = comboCodec.Text;
                        var progress = new Progress<string>(s => progressBar1.Value = Convert.ToInt32(s));
                        await Task.Run(() => Download(progress));
                    }

                }
                else
                {
                    MessageBox.Show("Некорректное значение в поле \"Расширение\"", "Ошибка!",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public async void Download(IProgress<string> progress)
        {
            DLBut.Invoke(new Action(() => DLBut.Enabled = false));

            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
            string log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"log\" + dateTime + "_log.txt");

            FileStream fs = new FileStream(log, FileMode.CreateNew);
            StreamWriter w = new StreamWriter(fs, Encoding.Default);

            await Task<string>.Run(() =>
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process();

                proc.StartInfo.FileName = @".\yt-dlp.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.CreateNoWindow = true;

                if (checkPlaylist.Checked == true)
                {
                    proc.StartInfo.Arguments = $"yt-dlp -S \"+codec:{codec},res:{res},fps\" -o \"./MyVideos/%(playlist)s/%(playlist_index)s- %(title)s.%(ext)s\" \"{SText.Text}\"";
                }
                else
                {
                    proc.StartInfo.Arguments = $"yt-dlp -S \"+codec:{codec},res:{res},fps\" -P \"./MyVideos\" {SText.Text}";
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
                                progress.Report(parseLog.Parse(e.Data));
                            }
                            ));
                        }
                        else
                        {
                            logLabel.Text = e.Data;
                            w.WriteLine(e.Data);
                            progress.Report(parseLog.Parse(e.Data));
                        }
                    }
                });

                proc.Start();
                proc.BeginOutputReadLine();
                proc.WaitForExit();
                proc.Close();

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
            Form aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void menuHelp_Click(object sender, EventArgs e)
        {
            Form helpForm = new HelpForm();
            helpForm.ShowDialog();
        }

        private void InitApp()
        {
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

            foreach (var i in comboCodec.Items)
            {
                codecList.Add(i.ToString());
            }
        }
    }
}