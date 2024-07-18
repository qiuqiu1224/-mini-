namespace PreviewDemo
{
    partial class UserControl1
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.uiLabel = new Sunny.UI.UILabel();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // uiLabel
            // 
            this.uiLabel.BackColor = System.Drawing.Color.Transparent;
            this.uiLabel.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLabel.ForeColor = System.Drawing.Color.White;
            this.uiLabel.Location = new System.Drawing.Point(21, 99);
            this.uiLabel.Name = "uiLabel";
            this.uiLabel.Size = new System.Drawing.Size(110, 45);
            this.uiLabel.Style = Sunny.UI.UIStyle.Custom;
            this.uiLabel.TabIndex = 3;
            this.uiLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // pictureBox
            // 
            this.pictureBox.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox.Location = new System.Drawing.Point(15, 6);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(120, 90);
            this.pictureBox.TabIndex = 2;
            this.pictureBox.TabStop = false;
            // 
            // UserControl1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.uiLabel);
            this.Controls.Add(this.pictureBox);
            this.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(53)))));
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Name = "UserControl1";
            this.RectColor = System.Drawing.Color.Transparent;
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        public Sunny.UI.UILabel uiLabel;
        public System.Windows.Forms.PictureBox pictureBox;
    }
}
