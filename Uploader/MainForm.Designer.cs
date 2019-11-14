namespace Uploader
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if ( disposing && (components != null) )
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.Label teensysLabel;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this._teensys = new System.Windows.Forms.ListBox();
            this._uploadButton = new System.Windows.Forms.Button();
            this._fileButton = new System.Windows.Forms.Button();
            this._progress = new System.Windows.Forms.ProgressBar();
            this._openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this._rebootButton = new System.Windows.Forms.Button();
            this._status = new System.Windows.Forms.Label();
            teensysLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // teensysLabel
            // 
            teensysLabel.AutoSize = true;
            teensysLabel.Location = new System.Drawing.Point(13, 13);
            teensysLabel.Name = "teensysLabel";
            teensysLabel.Size = new System.Drawing.Size(167, 13);
            teensysLabel.TabIndex = 0;
            teensysLabel.Text = "Select Teensy to Receive Upload";
            // 
            // _teensys
            // 
            this._teensys.BackColor = System.Drawing.Color.White;
            this._teensys.CausesValidation = false;
            this._teensys.ForeColor = System.Drawing.Color.Black;
            this._teensys.Location = new System.Drawing.Point(16, 30);
            this._teensys.Name = "_teensys";
            this._teensys.Size = new System.Drawing.Size(522, 95);
            this._teensys.Sorted = true;
            this._teensys.TabIndex = 3;
            this._teensys.SelectedIndexChanged += new System.EventHandler(this.SetUiState);
            // 
            // _uploadButton
            // 
            this._uploadButton.CausesValidation = false;
            this._uploadButton.Location = new System.Drawing.Point(254, 131);
            this._uploadButton.Name = "_uploadButton";
            this._uploadButton.Size = new System.Drawing.Size(139, 23);
            this._uploadButton.TabIndex = 1;
            this._uploadButton.Text = "Upload to Teensy";
            this._uploadButton.UseVisualStyleBackColor = true;
            this._uploadButton.Click += new System.EventHandler(this.Upload);
            // 
            // _fileButton
            // 
            this._fileButton.CausesValidation = false;
            this._fileButton.Location = new System.Drawing.Point(16, 131);
            this._fileButton.Name = "_fileButton";
            this._fileButton.Size = new System.Drawing.Size(232, 23);
            this._fileButton.TabIndex = 0;
            this._fileButton.Text = "Choose File to Upload to Teensy...";
            this._fileButton.UseVisualStyleBackColor = true;
            this._fileButton.Click += new System.EventHandler(this.ChooseHexFile);
            // 
            // _progress
            // 
            this._progress.Location = new System.Drawing.Point(16, 172);
            this._progress.Name = "_progress";
            this._progress.Size = new System.Drawing.Size(522, 23);
            this._progress.Step = 1;
            this._progress.TabIndex = 4;
            // 
            // _openFileDialog
            // 
            this._openFileDialog.DefaultExt = "hex";
            this._openFileDialog.Filter = "HEX Files|*.hex|All Files|*.*";
            this._openFileDialog.Title = "Select Firmware File";
            // 
            // _rebootButton
            // 
            this._rebootButton.CausesValidation = false;
            this._rebootButton.Location = new System.Drawing.Point(399, 131);
            this._rebootButton.Name = "_rebootButton";
            this._rebootButton.Size = new System.Drawing.Size(139, 23);
            this._rebootButton.TabIndex = 2;
            this._rebootButton.Text = "Reboot Teensy";
            this._rebootButton.UseVisualStyleBackColor = true;
            this._rebootButton.Click += new System.EventHandler(this.Reboot);
            // 
            // _status
            // 
            this._status.BackColor = System.Drawing.Color.Black;
            this._status.CausesValidation = false;
            this._status.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._status.ForeColor = System.Drawing.Color.White;
            this._status.Location = new System.Drawing.Point(16, 202);
            this._status.Name = "_status";
            this._status.Size = new System.Drawing.Size(522, 23);
            this._status.TabIndex = 5;
            this._status.Text = "Nothing Uploaded";
            this._status.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(550, 237);
            this.Controls.Add(this._status);
            this.Controls.Add(this._rebootButton);
            this.Controls.Add(this._progress);
            this.Controls.Add(this._fileButton);
            this.Controls.Add(this._uploadButton);
            this.Controls.Add(this._teensys);
            this.Controls.Add(teensysLabel);
            this.ForeColor = System.Drawing.Color.Black;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Teensy Upload Utility";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox _teensys;
        private System.Windows.Forms.Button _uploadButton;
        private System.Windows.Forms.Button _fileButton;
        private System.Windows.Forms.ProgressBar _progress;
        private System.Windows.Forms.OpenFileDialog _openFileDialog;
        private System.Windows.Forms.Button _rebootButton;
        private System.Windows.Forms.Label _status;
    }
}

