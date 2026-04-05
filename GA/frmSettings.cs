using System.Windows.Forms;

namespace GA
{
    public partial class frmSettings : Form
    {
        public NumericUpDown nudMaxPolygonCount;
        public NumericUpDown nudMaxPolygonPointCount;
        public NumericUpDown nudMinPolygonPointCount;
        public ComboBox cmbPolygonType;
        public NumericUpDown nudFocusWeight;

        public frmSettings()
        {
            InitializeComponent();
        }

        private Label lblMaxPolygonCount;
        private Label lblMaxPolygonPointCount;
        private Label lblMinPolygonPointCount;
        private Label lblPolygonType;
        private Label lblFocusWeight;
        private Button btnOK;
        private Button btnCancel;

        private void InitializeComponent()
        {
            this.lblMaxPolygonCount = new Label();
            this.nudMaxPolygonCount = new NumericUpDown();
            this.lblMaxPolygonPointCount = new Label();
            this.nudMaxPolygonPointCount = new NumericUpDown();
            this.lblMinPolygonPointCount = new Label();
            this.nudMinPolygonPointCount = new NumericUpDown();
            this.lblPolygonType = new Label();
            this.cmbPolygonType = new ComboBox();
            this.lblFocusWeight = new Label();
            this.nudFocusWeight = new NumericUpDown();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxPolygonCount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxPolygonPointCount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMinPolygonPointCount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudFocusWeight)).BeginInit();
            this.SuspendLayout();
            // 
            // lblMaxPolygonCount
            // 
            this.lblMaxPolygonCount.AutoSize = true;
            this.lblMaxPolygonCount.Location = new System.Drawing.Point(12, 15);
            this.lblMaxPolygonCount.Name = "lblMaxPolygonCount";
            this.lblMaxPolygonCount.Size = new System.Drawing.Size(115, 13);
            this.lblMaxPolygonCount.Text = "Max Polygon Count:";
            // 
            // nudMaxPolygonCount
            // 
            this.nudMaxPolygonCount.Location = new System.Drawing.Point(150, 12);
            this.nudMaxPolygonCount.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            this.nudMaxPolygonCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudMaxPolygonCount.Name = "nudMaxPolygonCount";
            this.nudMaxPolygonCount.Size = new System.Drawing.Size(120, 20);
            this.nudMaxPolygonCount.Value = new decimal(new int[] { 5000, 0, 0, 0 });
            // 
            // lblMaxPolygonPointCount
            // 
            this.lblMaxPolygonPointCount.AutoSize = true;
            this.lblMaxPolygonPointCount.Location = new System.Drawing.Point(12, 45);
            this.lblMaxPolygonPointCount.Name = "lblMaxPolygonPointCount";
            this.lblMaxPolygonPointCount.Size = new System.Drawing.Size(128, 13);
            this.lblMaxPolygonPointCount.Text = "Max Polygon Points:";
            // 
            // nudMaxPolygonPointCount
            // 
            this.nudMaxPolygonPointCount.Location = new System.Drawing.Point(150, 42);
            this.nudMaxPolygonPointCount.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            this.nudMaxPolygonPointCount.Minimum = new decimal(new int[] { 3, 0, 0, 0 });
            this.nudMaxPolygonPointCount.Name = "nudMaxPolygonPointCount";
            this.nudMaxPolygonPointCount.Size = new System.Drawing.Size(120, 20);
            this.nudMaxPolygonPointCount.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // lblMinPolygonPointCount
            // 
            this.lblMinPolygonPointCount.AutoSize = true;
            this.lblMinPolygonPointCount.Location = new System.Drawing.Point(12, 75);
            this.lblMinPolygonPointCount.Name = "lblMinPolygonPointCount";
            this.lblMinPolygonPointCount.Size = new System.Drawing.Size(126, 13);
            this.lblMinPolygonPointCount.Text = "Min Polygon Points:";
            // 
            // nudMinPolygonPointCount
            // 
            this.nudMinPolygonPointCount.Location = new System.Drawing.Point(150, 72);
            this.nudMinPolygonPointCount.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.nudMinPolygonPointCount.Minimum = new decimal(new int[] { 3, 0, 0, 0 });
            this.nudMinPolygonPointCount.Name = "nudMinPolygonPointCount";
            this.nudMinPolygonPointCount.Size = new System.Drawing.Size(120, 20);
            this.nudMinPolygonPointCount.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // lblPolygonType
            // 
            this.lblPolygonType.AutoSize = true;
            this.lblPolygonType.Location = new System.Drawing.Point(12, 105);
            this.lblPolygonType.Name = "lblPolygonType";
            this.lblPolygonType.Size = new System.Drawing.Size(82, 13);
            this.lblPolygonType.Text = "Polygon Type:";
            // 
            // cmbPolygonType
            // 
            this.cmbPolygonType.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbPolygonType.FormattingEnabled = true;
            this.cmbPolygonType.Items.AddRange(new object[] { "Lines", "Curve" });
            this.cmbPolygonType.Location = new System.Drawing.Point(150, 102);
            this.cmbPolygonType.Name = "cmbPolygonType";
            this.cmbPolygonType.Size = new System.Drawing.Size(120, 21);
            // 
            // lblFocusWeight
            // 
            this.lblFocusWeight.AutoSize = true;
            this.lblFocusWeight.Location = new System.Drawing.Point(12, 135);
            this.lblFocusWeight.Name = "lblFocusWeight";
            this.lblFocusWeight.Size = new System.Drawing.Size(83, 13);
            this.lblFocusWeight.Text = "Focus Weight:";
            // 
            // nudFocusWeight
            // 
            this.nudFocusWeight.Location = new System.Drawing.Point(150, 132);
            this.nudFocusWeight.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.nudFocusWeight.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudFocusWeight.Name = "nudFocusWeight";
            this.nudFocusWeight.Size = new System.Drawing.Size(120, 20);
            this.nudFocusWeight.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(87, 170);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.Text = "OK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(168, 170);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // frmSettings
            // 
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(284, 211);
            this.Controls.Add(this.lblMaxPolygonCount);
            this.Controls.Add(this.nudMaxPolygonCount);
            this.Controls.Add(this.lblMaxPolygonPointCount);
            this.Controls.Add(this.nudMaxPolygonPointCount);
            this.Controls.Add(this.lblMinPolygonPointCount);
            this.Controls.Add(this.nudMinPolygonPointCount);
            this.Controls.Add(this.lblPolygonType);
            this.Controls.Add(this.cmbPolygonType);
            this.Controls.Add(this.lblFocusWeight);
            this.Controls.Add(this.nudFocusWeight);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "frmSettings";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Settings";
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxPolygonCount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxPolygonPointCount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMinPolygonPointCount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudFocusWeight)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void btnOK_Click(object sender, System.EventArgs e)
        {
            GABase.Settings.MaxPolygonCount = (int)nudMaxPolygonCount.Value;
            GABase.Settings.MaxPolygonPointCount = (int)nudMaxPolygonPointCount.Value;
            GABase.Settings.MinPolygonPointCount = (int)nudMinPolygonPointCount.Value;
            GABase.Settings.Polygon = cmbPolygonType.SelectedIndex == 0 
                ? GABase.Settings.PolygonType.Lines 
                : GABase.Settings.PolygonType.Curve;
            GABase.Settings.FocusWeight = (int)nudFocusWeight.Value;
            GABase.Settings.InvalidateFocusWeightMap();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
