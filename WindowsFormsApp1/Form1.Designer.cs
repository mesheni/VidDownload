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
            this.SText = new System.Windows.Forms.TextBox();
            this.DLBut = new System.Windows.Forms.Button();
            this.LText = new System.Windows.Forms.TextBox();
            this.LabelProg = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // SText
            // 
            this.SText.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.SText.ForeColor = System.Drawing.SystemColors.ControlText;
            this.SText.Location = new System.Drawing.Point(12, 32);
            this.SText.Name = "SText";
            this.SText.Size = new System.Drawing.Size(546, 20);
            this.SText.TabIndex = 0;
            // 
            // DLBut
            // 
            this.DLBut.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.DLBut.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.DLBut.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.DLBut.Location = new System.Drawing.Point(12, 58);
            this.DLBut.Name = "DLBut";
            this.DLBut.Size = new System.Drawing.Size(139, 43);
            this.DLBut.TabIndex = 1;
            this.DLBut.Text = "Скачать";
            this.DLBut.UseVisualStyleBackColor = false;
            this.DLBut.Click += new System.EventHandler(this.DLBut_ClickAsync);
            // 
            // LText
            // 
            this.LText.BackColor = System.Drawing.SystemColors.HighlightText;
            this.LText.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.LText.ForeColor = System.Drawing.SystemColors.ControlText;
            this.LText.Location = new System.Drawing.Point(12, 107);
            this.LText.Multiline = true;
            this.LText.Name = "LText";
            this.LText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.LText.Size = new System.Drawing.Size(546, 360);
            this.LText.TabIndex = 2;
            // 
            // LabelProg
            // 
            this.LabelProg.AutoSize = true;
            this.LabelProg.Location = new System.Drawing.Point(12, 88);
            this.LabelProg.Name = "LabelProg";
            this.LabelProg.Size = new System.Drawing.Size(0, 13);
            this.LabelProg.TabIndex = 3;
            // 
            // progressBar1
            // 
            this.progressBar1.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.progressBar1.ForeColor = System.Drawing.Color.IndianRed;
            this.progressBar1.Location = new System.Drawing.Point(157, 58);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(401, 43);
            this.progressBar1.Step = 1;
            this.progressBar1.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.SystemColors.Control;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.label1.Location = new System.Drawing.Point(21, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(218, 20);
            this.label1.TabIndex = 5;
            this.label1.Text = "Поле для ссылки на видео:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(570, 476);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.LabelProg);
            this.Controls.Add(this.LText);
            this.Controls.Add(this.DLBut);
            this.Controls.Add(this.SText);
            this.Name = "Form1";
            this.Text = "Form1";
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
    }
}

