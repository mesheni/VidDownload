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

namespace VidDownload.WPF.ConvertWindow
{
    /// <summary>
    /// Логика взаимодействия для ConvertWindow.xaml
    /// </summary>
    public partial class ConvertWindow : Window
    {
        public ConvertWindow()
        {
            InitializeComponent();
        }

        private async void ButConvert_Click(object sender, RoutedEventArgs e)
        {
            //string outputPath = System.IO.Path.ChangeExtension(System.IO.Path.GetTempFileName(), ".mp4");
            //IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo("Test").ConfigureAwait(false);

            //IStream videoStream = mediaInfo.VideoStreams.FirstOrDefault()
            //    ?.SetCodec(VideoCodec.h264);
            //IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault()
            //    ?.SetCodec(AudioCodec.aac);

            //await FFmpeg.Conversions.New()
            //    .AddStream(audioStream, videoStream)
            //    .SetOutput(outputPath)
            //    .Start().ConfigureAwait(false);

        }

        private void ButChoiseVideo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Video files (*.mp4;*.avi;*.wmv)|*.mp4;*.avi;*.wmv"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LabelFileName.Text = openFileDialog.FileName;
                labelInfoFFmpeg.Content = System.IO.Path.GetFileName(openFileDialog.FileName);
            }
        }
    }
}
