﻿namespace VidDownload
{
    partial class MainForm
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.SText = new System.Windows.Forms.TextBox();
            this.DLBut = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.butOpenFolder = new System.Windows.Forms.Button();
            this.checkPlaylist = new System.Windows.Forms.CheckBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.menuAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.logLabel = new System.Windows.Forms.Label();
            this.comboRes = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboCodec = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // SText
            // 
            this.SText.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.SText.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SText.ForeColor = System.Drawing.SystemColors.ControlText;
            this.SText.Location = new System.Drawing.Point(16, 56);
            this.SText.Name = "SText";
            this.SText.Size = new System.Drawing.Size(587, 21);
            this.SText.TabIndex = 0;
            // 
            // DLBut
            // 
            this.DLBut.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.DLBut.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.DLBut.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.DLBut.FlatAppearance.BorderSize = 0;
            this.DLBut.Font = new System.Drawing.Font("Bahnschrift SemiBold", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.DLBut.ForeColor = System.Drawing.Color.Black;
            this.DLBut.Location = new System.Drawing.Point(198, 175);
            this.DLBut.Name = "DLBut";
            this.DLBut.Padding = new System.Windows.Forms.Padding(3);
            this.DLBut.Size = new System.Drawing.Size(214, 43);
            this.DLBut.TabIndex = 1;
            this.DLBut.Text = "Скачать";
            this.DLBut.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.DLBut.UseVisualStyleBackColor = false;
            this.DLBut.Click += new System.EventHandler(this.DLBut_ClickAsync);
            // 
            // progressBar1
            // 
            this.progressBar1.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.progressBar1.ForeColor = System.Drawing.Color.IndianRed;
            this.progressBar1.Location = new System.Drawing.Point(16, 147);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(585, 22);
            this.progressBar1.Step = 1;
            this.progressBar1.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.SystemColors.Control;
            this.label1.Font = new System.Drawing.Font("Bahnschrift SemiBold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.label1.Location = new System.Drawing.Point(12, 33);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(212, 19);
            this.label1.TabIndex = 5;
            this.label1.Text = "Поле для ссылки на видео:";
            // 
            // butOpenFolder
            // 
            this.butOpenFolder.Font = new System.Drawing.Font("Bahnschrift SemiBold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.butOpenFolder.Location = new System.Drawing.Point(418, 182);
            this.butOpenFolder.Name = "butOpenFolder";
            this.butOpenFolder.Size = new System.Drawing.Size(167, 28);
            this.butOpenFolder.TabIndex = 6;
            this.butOpenFolder.Text = "Открыть папку с видео";
            this.butOpenFolder.UseVisualStyleBackColor = true;
            this.butOpenFolder.Click += new System.EventHandler(this.butOpenFolder_Click);
            // 
            // checkPlaylist
            // 
            this.checkPlaylist.AutoSize = true;
            this.checkPlaylist.Font = new System.Drawing.Font("Bahnschrift SemiBold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.checkPlaylist.Location = new System.Drawing.Point(16, 90);
            this.checkPlaylist.Name = "checkPlaylist";
            this.checkPlaylist.Size = new System.Drawing.Size(168, 20);
            this.checkPlaylist.TabIndex = 7;
            this.checkPlaylist.Text = "Скачать сразу плейлист";
            this.checkPlaylist.UseVisualStyleBackColor = true;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuAbout,
            this.menuHelp});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(611, 24);
            this.menuStrip1.TabIndex = 8;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // menuAbout
            // 
            this.menuAbout.Font = new System.Drawing.Font("Bahnschrift SemiBold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.menuAbout.Name = "menuAbout";
            this.menuAbout.Size = new System.Drawing.Size(91, 20);
            this.menuAbout.Text = "О программе";
            this.menuAbout.Click += new System.EventHandler(this.helpMenu_Click);
            // 
            // menuHelp
            // 
            this.menuHelp.Font = new System.Drawing.Font("Bahnschrift SemiBold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.menuHelp.Name = "menuHelp";
            this.menuHelp.Size = new System.Drawing.Size(65, 20);
            this.menuHelp.Text = "Помощь";
            this.menuHelp.Click += new System.EventHandler(this.menuHelp_Click);
            // 
            // logLabel
            // 
            this.logLabel.AutoSize = true;
            this.logLabel.Font = new System.Drawing.Font("Bahnschrift SemiBold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.logLabel.Location = new System.Drawing.Point(13, 128);
            this.logLabel.Name = "logLabel";
            this.logLabel.Size = new System.Drawing.Size(0, 16);
            this.logLabel.TabIndex = 9;
            // 
            // comboRes
            // 
            this.comboRes.Font = new System.Drawing.Font("Bahnschrift SemiBold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.comboRes.FormattingEnabled = true;
            this.comboRes.Items.AddRange(new object[] {
            "144",
            "240",
            "360",
            "480",
            "720",
            "1080",
            "1440",
            "2160"});
            this.comboRes.Location = new System.Drawing.Point(291, 88);
            this.comboRes.Name = "comboRes";
            this.comboRes.Size = new System.Drawing.Size(121, 22);
            this.comboRes.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Bahnschrift SemiBold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.Location = new System.Drawing.Point(202, 91);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(83, 16);
            this.label2.TabIndex = 11;
            this.label2.Text = "Разрешение:";
            // 
            // comboCodec
            // 
            this.comboCodec.FormattingEnabled = true;
            this.comboCodec.Items.AddRange(new object[] {
            "av01",
            "vp9.2",
            "vp9",
            "h265",
            "h264",
            "vp8",
            "h263"});
            this.comboCodec.Location = new System.Drawing.Point(482, 88);
            this.comboCodec.Name = "comboCodec";
            this.comboCodec.Size = new System.Drawing.Size(121, 21);
            this.comboCodec.TabIndex = 12;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Bahnschrift SemiBold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label3.Location = new System.Drawing.Point(429, 91);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(47, 16);
            this.label3.TabIndex = 13;
            this.label3.Text = "Кодек:";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(611, 262);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboCodec);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboRes);
            this.Controls.Add(this.logLabel);
            this.Controls.Add(this.checkPlaylist);
            this.Controls.Add(this.butOpenFolder);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.DLBut);
            this.Controls.Add(this.SText);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Bahnschrift SemiBold", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximumSize = new System.Drawing.Size(627, 301);
            this.MinimumSize = new System.Drawing.Size(627, 301);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "VidDownload";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox SText;
        private System.Windows.Forms.Button DLBut;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button butOpenFolder;
        private System.Windows.Forms.CheckBox checkPlaylist;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuAbout;
        private System.Windows.Forms.Label logLabel;
        private System.Windows.Forms.ComboBox comboRes;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ToolStripMenuItem menuHelp;
        private System.Windows.Forms.ComboBox comboCodec;
        private System.Windows.Forms.Label label3;
    }
}

