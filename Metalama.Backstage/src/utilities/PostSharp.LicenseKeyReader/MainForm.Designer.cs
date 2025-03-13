namespace PostSharp.LicenseKeyReader
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._licenseKeyTextBox = new System.Windows.Forms.TextBox();
            this._readButton = new System.Windows.Forms.Button();
            this._propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.SuspendLayout();
            // 
            // _licenseKeyTextBox
            // 
            this._licenseKeyTextBox.AcceptsReturn = true;
            this._licenseKeyTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._licenseKeyTextBox.Location = new System.Drawing.Point(12, 12);
            this._licenseKeyTextBox.Multiline = true;
            this._licenseKeyTextBox.Name = "_licenseKeyTextBox";
            this._licenseKeyTextBox.Size = new System.Drawing.Size(1059, 110);
            this._licenseKeyTextBox.TabIndex = 0;
            // 
            // _readButton
            // 
            this._readButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._readButton.Location = new System.Drawing.Point(959, 126);
            this._readButton.Name = "_readButton";
            this._readButton.Size = new System.Drawing.Size(112, 34);
            this._readButton.TabIndex = 1;
            this._readButton.Text = "Read";
            this._readButton.UseVisualStyleBackColor = true;
            this._readButton.Click += new System.EventHandler(this.OnReadButtonClicked);
            // 
            // _propertyGrid
            // 
            this._propertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._propertyGrid.Location = new System.Drawing.Point(12, 128);
            this._propertyGrid.Name = "_propertyGrid";
            this._propertyGrid.Size = new System.Drawing.Size(1059, 766);
            this._propertyGrid.TabIndex = 2;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1083, 906);
            this.Controls.Add(this._readButton);
            this.Controls.Add(this._licenseKeyTextBox);
            this.Controls.Add(this._propertyGrid);
            this.Name = "MainForm";
            this.Text = "PostSharp License Key Reader";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TextBox _licenseKeyTextBox;
        private Button _readButton;
        private PropertyGrid _propertyGrid;
    }
}