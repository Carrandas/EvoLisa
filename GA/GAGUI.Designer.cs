namespace GA
{
    partial class frmGA
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
            if (disposing && (components != null))
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
            this.pictureBoxGenerated = new System.Windows.Forms.PictureBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tssFitnesse = new System.Windows.Forms.ToolStripStatusLabel();
            this.tssGeneration = new System.Windows.Forms.ToolStripStatusLabel();
            this.tssPolygonCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.tssTimeInMs = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.priorityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lowestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.belowNormalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.normalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboveNormalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.highestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pictureBoxOriginal = new System.Windows.Forms.PictureBox();
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnStart = new System.Windows.Forms.Button();
            this.pictureBoxDifference = new System.Windows.Forms.PictureBox();
            this.tssZoomLevel = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnSaveImage = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxGenerated)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxOriginal)).BeginInit();
            this.tableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDifference)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxGenerated
            // 
            this.pictureBoxGenerated.Location = new System.Drawing.Point(640, 3);
            this.pictureBoxGenerated.Name = "pictureBoxGenerated";
            this.pictureBoxGenerated.Size = new System.Drawing.Size(386, 456);
            this.pictureBoxGenerated.TabIndex = 0;
            this.pictureBoxGenerated.TabStop = false;
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tssFitnesse,
            this.tssGeneration,
            this.tssPolygonCount,
            this.tssZoomLevel,
            this.tssTimeInMs});
            this.statusStrip1.Location = new System.Drawing.Point(0, 712);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1274, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip";
            // 
            // tssFitnesse
            // 
            this.tssFitnesse.Name = "tssFitnesse";
            this.tssFitnesse.Size = new System.Drawing.Size(52, 17);
            this.tssFitnesse.Text = "Fitnesse:";
            // 
            // tssGeneration
            // 
            this.tssGeneration.Name = "tssGeneration";
            this.tssGeneration.Size = new System.Drawing.Size(68, 17);
            this.tssGeneration.Text = "Generation:";
            // 
            // tssPolygonCount
            // 
            this.tssPolygonCount.Name = "tssPolygonCount";
            this.tssPolygonCount.Size = new System.Drawing.Size(59, 17);
            this.tssPolygonCount.Text = "Polygons:";
            // 
            // tssTimeInMs
            // 
            this.tssTimeInMs.Name = "tssTimeInMs";
            this.tssTimeInMs.Size = new System.Drawing.Size(55, 17);
            this.tssTimeInMs.Text = "Running:";
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.priorityToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1274, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openImageToolStripMenuItem,
            this.btnSaveImage});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openImageToolStripMenuItem
            // 
            this.openImageToolStripMenuItem.Name = "openImageToolStripMenuItem";
            this.openImageToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.openImageToolStripMenuItem.Text = "Open Image";
            this.openImageToolStripMenuItem.Click += new System.EventHandler(this.openImageToolStripMenuItem_Click);
            // 
            // priorityToolStripMenuItem
            // 
            this.priorityToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lowestToolStripMenuItem,
            this.belowNormalToolStripMenuItem,
            this.normalToolStripMenuItem,
            this.aboveNormalToolStripMenuItem,
            this.highestToolStripMenuItem});
            this.priorityToolStripMenuItem.Name = "priorityToolStripMenuItem";
            this.priorityToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
            this.priorityToolStripMenuItem.Text = "Priority";
            // 
            // lowestToolStripMenuItem
            // 
            this.lowestToolStripMenuItem.Name = "lowestToolStripMenuItem";
            this.lowestToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.lowestToolStripMenuItem.Text = "Lowest";
            this.lowestToolStripMenuItem.Click += new System.EventHandler(this.lowestToolStripMenuItem_Click);
            // 
            // belowNormalToolStripMenuItem
            // 
            this.belowNormalToolStripMenuItem.Name = "belowNormalToolStripMenuItem";
            this.belowNormalToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.belowNormalToolStripMenuItem.Text = "Below Normal";
            this.belowNormalToolStripMenuItem.Click += new System.EventHandler(this.belowNormalToolStripMenuItem_Click);
            // 
            // normalToolStripMenuItem
            // 
            this.normalToolStripMenuItem.Checked = true;
            this.normalToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.normalToolStripMenuItem.Name = "normalToolStripMenuItem";
            this.normalToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.normalToolStripMenuItem.Text = "Normal";
            this.normalToolStripMenuItem.Click += new System.EventHandler(this.normalToolStripMenuItem_Click);
            // 
            // aboveNormalToolStripMenuItem
            // 
            this.aboveNormalToolStripMenuItem.Name = "aboveNormalToolStripMenuItem";
            this.aboveNormalToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.aboveNormalToolStripMenuItem.Text = "Above Normal";
            this.aboveNormalToolStripMenuItem.Click += new System.EventHandler(this.aboveNormalToolStripMenuItem_Click);
            // 
            // highestToolStripMenuItem
            // 
            this.highestToolStripMenuItem.Name = "highestToolStripMenuItem";
            this.highestToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.highestToolStripMenuItem.Text = "Highest";
            this.highestToolStripMenuItem.Click += new System.EventHandler(this.highestToolStripMenuItem_Click);
            // 
            // pictureBoxOriginal
            // 
            this.pictureBoxOriginal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxOriginal.Location = new System.Drawing.Point(248, 3);
            this.pictureBoxOriginal.Name = "pictureBoxOriginal";
            this.pictureBoxOriginal.Size = new System.Drawing.Size(386, 456);
            this.pictureBoxOriginal.TabIndex = 3;
            this.pictureBoxOriginal.TabStop = false;
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.AutoScroll = true;
            this.tableLayoutPanel.AutoSize = true;
            this.tableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel.ColumnCount = 2;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel.Controls.Add(this.pictureBoxOriginal, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.pictureBoxGenerated, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.btnStart, 0, 2);
            this.tableLayoutPanel.Controls.Add(this.pictureBoxDifference, 0, 1);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(0, 24);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 3;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(1274, 688);
            this.tableLayoutPanel.TabIndex = 4;
            // 
            // btnStart
            // 
            this.btnStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel.SetColumnSpan(this.btnStart, 2);
            this.btnStart.Location = new System.Drawing.Point(3, 591);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(1268, 23);
            this.btnStart.TabIndex = 4;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // pictureBoxDifference
            // 
            this.pictureBoxDifference.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.tableLayoutPanel.SetColumnSpan(this.pictureBoxDifference, 2);
            this.pictureBoxDifference.Location = new System.Drawing.Point(537, 465);
            this.pictureBoxDifference.Name = "pictureBoxDifference";
            this.pictureBoxDifference.Size = new System.Drawing.Size(200, 50);
            this.pictureBoxDifference.TabIndex = 5;
            this.pictureBoxDifference.TabStop = false;
            // 
            // tssZoomLevel
            // 
            this.tssZoomLevel.Name = "tssZoomLevel";
            this.tssZoomLevel.Size = new System.Drawing.Size(69, 17);
            this.tssZoomLevel.Text = "ZoomLevel:";
            // 
            // btnSaveImage
            // 
            this.btnSaveImage.Name = "btnSaveImage";
            this.btnSaveImage.Size = new System.Drawing.Size(180, 22);
            this.btnSaveImage.Text = "Save Image";
            this.btnSaveImage.Click += new System.EventHandler(this.saveImagesToolStripMenuItem_Click);
            // 
            // frmGA
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(1274, 734);
            this.Controls.Add(this.tableLayoutPanel);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.statusStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmGA";
            this.Text = "GA";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmGA_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxGenerated)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxOriginal)).EndInit();
            this.tableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDifference)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxGenerated;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tssFitnesse;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openImageToolStripMenuItem;
        private System.Windows.Forms.PictureBox pictureBoxOriginal;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.ToolStripStatusLabel tssGeneration;
        private System.Windows.Forms.PictureBox pictureBoxDifference;
        private System.Windows.Forms.ToolStripMenuItem priorityToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem belowNormalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem normalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboveNormalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lowestToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem highestToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel tssPolygonCount;
        private System.Windows.Forms.ToolStripStatusLabel tssTimeInMs;
        private System.Windows.Forms.ToolStripStatusLabel tssZoomLevel;
        private System.Windows.Forms.ToolStripMenuItem btnSaveImage;
    }
}

