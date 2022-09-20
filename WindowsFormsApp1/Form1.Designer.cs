namespace VidDownload
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.SText = new System.Windows.Forms.TextBox();
            this.DLBut = new System.Windows.Forms.Button();
            this.LText = new System.Windows.Forms.TextBox();
            this.LabelProg = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.butOpenFolder = new System.Windows.Forms.Button();
            this.checkPlaylist = new System.Windows.Forms.CheckBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.helpMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // SText
            // 
            this.SText.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.SText.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SText.ForeColor = System.Drawing.SystemColors.ControlText;
            this.SText.Location = new System.Drawing.Point(16, 47);
            this.SText.Name = "SText";
            this.SText.Size = new System.Drawing.Size(587, 20);
            this.SText.TabIndex = 0;
            // 
            // DLBut
            // 
            this.DLBut.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.DLBut.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.DLBut.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.DLBut.Font = new System.Drawing.Font("Bahnschrift SemiBold", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.DLBut.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.DLBut.Location = new System.Drawing.Point(198, 124);
            this.DLBut.Name = "DLBut";
            this.DLBut.Padding = new System.Windows.Forms.Padding(3);
            this.DLBut.Size = new System.Drawing.Size(214, 43);
            this.DLBut.TabIndex = 1;
            this.DLBut.Text = "Скачать";
            this.DLBut.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.DLBut.UseVisualStyleBackColor = false;
            this.DLBut.Click += new System.EventHandler(this.DLBut_ClickAsync);
            // 
            // LText
            // 
            this.LText.BackColor = System.Drawing.SystemColors.HighlightText;
            this.LText.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.LText.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.LText.ForeColor = System.Drawing.SystemColors.ControlText;
            this.LText.Location = new System.Drawing.Point(16, 173);
            this.LText.Multiline = true;
            this.LText.Name = "LText";
            this.LText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.LText.Size = new System.Drawing.Size(585, 286);
            this.LText.TabIndex = 2;
            // 
            // LabelProg
            // 
            this.LabelProg.AutoSize = true;
            this.LabelProg.Location = new System.Drawing.Point(16, 103);
            this.LabelProg.Name = "LabelProg";
            this.LabelProg.Size = new System.Drawing.Size(0, 13);
            this.LabelProg.TabIndex = 3;
            // 
            // progressBar1
            // 
            this.progressBar1.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.progressBar1.ForeColor = System.Drawing.Color.IndianRed;
            this.progressBar1.Location = new System.Drawing.Point(16, 96);
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
            this.label1.Location = new System.Drawing.Point(12, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(212, 19);
            this.label1.TabIndex = 5;
            this.label1.Text = "Поле для ссылки на видео:";
            // 
            // butOpenFolder
            // 
            this.butOpenFolder.Font = new System.Drawing.Font("Bahnschrift SemiBold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.butOpenFolder.Location = new System.Drawing.Point(436, 124);
            this.butOpenFolder.Name = "butOpenFolder";
            this.butOpenFolder.Size = new System.Drawing.Size(167, 43);
            this.butOpenFolder.TabIndex = 6;
            this.butOpenFolder.Text = "Открыть папку с видео";
            this.butOpenFolder.UseVisualStyleBackColor = true;
            this.butOpenFolder.Click += new System.EventHandler(this.butOpenFolder_Click);
            // 
            // checkPlaylist
            // 
            this.checkPlaylist.AutoSize = true;
            this.checkPlaylist.Font = new System.Drawing.Font("Bahnschrift SemiBold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.checkPlaylist.Location = new System.Drawing.Point(16, 70);
            this.checkPlaylist.Name = "checkPlaylist";
            this.checkPlaylist.Size = new System.Drawing.Size(168, 20);
            this.checkPlaylist.TabIndex = 7;
            this.checkPlaylist.Text = "Скачать сразу плейлист";
            this.checkPlaylist.UseVisualStyleBackColor = true;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.helpMenu});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(611, 24);
            this.menuStrip1.TabIndex = 8;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // helpMenu
            // 
            this.helpMenu.Name = "helpMenu";
            this.helpMenu.Size = new System.Drawing.Size(68, 20);
            this.helpMenu.Text = "Помощь";
            this.helpMenu.Click += new System.EventHandler(this.helpMenu_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(611, 468);
            this.Controls.Add(this.checkPlaylist);
            this.Controls.Add(this.butOpenFolder);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.LabelProg);
            this.Controls.Add(this.LText);
            this.Controls.Add(this.DLBut);
            this.Controls.Add(this.SText);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "VidDownload";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox SText;
        private System.Windows.Forms.Button DLBut;
        private System.Windows.Forms.TextBox LText;
        private System.Windows.Forms.Label LabelProg;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button butOpenFolder;
        private System.Windows.Forms.CheckBox checkPlaylist;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem helpMenu;
    }
}

