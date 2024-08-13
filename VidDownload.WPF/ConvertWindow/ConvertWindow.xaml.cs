using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xabe.FFmpeg;
using Xabe;
using Microsoft.Win32;
using System.Diagnostics;
using HandyControl.Controls;

namespace VidDownload.WPF.ConvertWindow
{
    /// <summary>
    /// Логика взаимодействия для ConvertWindow.xaml
    /// </summary>
    public partial class ConvertWindow : System.Windows.Window
    {
        private string fileName = String.Empty;

        public ConvertWindow()
        {
            InitializeComponent();
        }

        private async void ButConvert_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(async () => 
            {
                //TODO: Сделать аппаратное ускорение конвертации (по возможности)
                string outputPath = System.IO.Path.ChangeExtension(System.IO.Path.GetTempFileName(), ".mp4");

                var conversion = await FFmpeg.Conversions.FromSnippet.ToMp4(fileName, "test.mp4").ConfigureAwait(false);
                var percent = 0;

                IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileName).ConfigureAwait(false);

                IStream videoStream = mediaInfo.VideoStreams.FirstOrDefault()
                    ?.SetCodec(VideoCodec.hevc);
                IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault()
                    ?.SetCodec(AudioCodec.aac);

                conversion.OnProgress += (sender, args) =>
                {

                    //Debug.WriteLine($"{args.Data}{Environment.NewLine}");

                    //Dispatcher.Invoke(() => 
                    //{
                        percent = (int)(Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds, 2) * 100);

                    //});
                    Debug.WriteLine($"[{args.Duration} / {args.TotalLength}] {percent}%");

                    //var percent = (int)(Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds, 2) * 100);

                    Dispatcher.Invoke(() => labelInfoFFmpeg.Content = $"[{args.Duration} / {args.TotalLength}] {percent}%");
                    Dispatcher.Invoke(() => ProgressBarFFmpeg.Value = percent);
                };
                await conversion.Start().ConfigureAwait(false);

                await FFmpeg.Conversions.New()
                    .AddStream(audioStream, videoStream)
                    .SetOutput(outputPath)
                    .Start().ConfigureAwait(false);
                
            }).ConfigureAwait(false);

            //Dispatcher.Invoke(() => labelInfoFFmpeg.Content = String.Empty);
            //Dispatcher.Invoke(() => ProgressBarFFmpeg.Value = 0);
            
        }

        private void ButChoiseVideo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Video files (*.mp4;*.avi;*.wmv;*.mkv)|*.mp4;*.avi;*.wmv;*.mkv"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LabelFileName.Text = openFileDialog.FileName;
                fileName = openFileDialog.FileName;
                //fileName = System.IO.Path.GetFileName(openFileDialog.FileName);
            }
        }
    }
}
