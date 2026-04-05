using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using GABase;

namespace GA
{
    public partial class frmGA : Form
    {
        Evolver _evolver;
        Point _dragStart;
        bool _isDragging;
        Rectangle _currentDragRect;

        public frmGA()
        {
            InitializeComponent();
            pictureBoxOriginal.MouseDown += pictureBoxOriginal_MouseDown;
            pictureBoxOriginal.MouseMove += pictureBoxOriginal_MouseMove;
            pictureBoxOriginal.MouseUp += pictureBoxOriginal_MouseUp;
        }

        private void pictureBoxOriginal_MouseDown(object sender, MouseEventArgs e)
        {
            _isDragging = true;
            _dragStart = e.Location;
        }

        private void pictureBoxOriginal_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                int x = Math.Min(_dragStart.X, e.Location.X);
                int y = Math.Min(_dragStart.Y, e.Location.Y);
                int width = Math.Abs(e.Location.X - _dragStart.X);
                int height = Math.Abs(e.Location.Y - _dragStart.Y);
                _currentDragRect = new Rectangle(x, y, width, height);
                pictureBoxOriginal.Invalidate();
            }
        }

        private void pictureBoxOriginal_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isDragging && _currentDragRect.Width > 5 && _currentDragRect.Height > 5)
            {
                Settings.FocusAreas.Add(_currentDragRect);
                Settings.InvalidateFocusWeightMap();
                DrawFocusAreas();
            }
            _isDragging = false;
            _currentDragRect = Rectangle.Empty;
        }

        private void DrawFocusAreas()
        {
            if (pictureBoxOriginal.Image == null) return;
            
            var tempBitmap = new Bitmap(pictureBoxOriginal.Image);
            var g = Graphics.FromImage(tempBitmap);
            foreach (var rect in Settings.FocusAreas)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    g.DrawRectangle(pen, rect);
                }
            }
            if (_currentDragRect.Width > 0)
            {
                using (var pen = new Pen(Color.Yellow, 2))
                {
                    g.DrawRectangle(pen, _currentDragRect);
                }
            }
            g.Dispose();
            
            var oldImage = pictureBoxOriginal.Image;
            pictureBoxOriginal.Image = tempBitmap;
            if (oldImage != null && oldImage != pictureBoxGenerated.Image)
                oldImage.Dispose();
        }

        private void clearFocusAreasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.FocusAreas.Clear();
            Settings.InvalidateFocusWeightMap();
            if (pictureBoxOriginal.Image != null && !string.IsNullOrEmpty(Settings.ImageLocation))
            {
                var oldImage = pictureBoxOriginal.Image;
                pictureBoxOriginal.Image = new Bitmap(Settings.ImageLocation);
                if (oldImage != null && oldImage != pictureBoxGenerated.Image)
                    oldImage.Dispose();
            }
        }

        private void UpdateGui(Image img, long fitnesse, Population pop, int generation, Image differenceImage,long swElapsedMilliseconds, int zoomLevel)
        {
            if(IsDisposed)
                return;

            if (InvokeRequired)
            {
                Invoke(new UpdateGuiDelegate((img1, fitnesse1, pop1, generation1, differenceImage1, zoomLevel1) => 
                  UpdateGui(img1, fitnesse1, pop1, generation1, differenceImage1, swElapsedMilliseconds, zoomLevel1)), img, fitnesse, pop, generation, differenceImage, zoomLevel);
            }
            else
            {
                pictureBoxGenerated.Image = img;
                pictureBoxDifference.Image = differenceImage;
                tssFitnesse.Text = "Fitnesse: " + fitnesse;
                tssGeneration.Text = "Generation: " + generation;
                tssPolygonCount.Text = "Polygons: " + pop.chromosomes.Count;
                tssZoomLevel.Text = "ZoomLevel: " + zoomLevel;
                tssTimeInMs.Text = "Running: " + swElapsedMilliseconds;
            }
        }

        private void openImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog(this) == DialogResult.OK)
            {
                Settings.ImageLocation = fileDialog.FileName;
                //And read file
                Bitmap bitmap = new Bitmap(fileDialog.FileName);

                pictureBoxOriginal.Image = bitmap;
                pictureBoxOriginal.Width = bitmap.Width;
                pictureBoxGenerated.Width = bitmap.Width;
                pictureBoxDifference.Width = bitmap.Width;
                pictureBoxOriginal.Height = bitmap.Height;
                pictureBoxGenerated.Height = bitmap.Height;
                pictureBoxDifference.Height = bitmap.Height;
                
                
                Settings.ScreenHeight = bitmap.Height;
                Settings.ScreenWidth = bitmap.Width;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (_evolver == null || !IsEvolverRunning())
            {
                _evolver = new Evolver((Bitmap)pictureBoxOriginal.Image);
                _evolver.Priority = ThreadPriority.Normal;
                _evolver.PopulationUpdated += UpdateGui;
                _evolver.Start();
                btnStart.Text = "Stop";
            }
            else
            {
                _evolver.Stop();
                btnStart.Text = "Start";
            }
        }

        private bool IsEvolverRunning() => _evolver != null && _evolver.CurrentPopulation != null;

        #region Nested type: UpdateGuiDelegate

        private delegate void UpdateGuiDelegate(Image img, long fitnesse, Population pop, int generation, Image differenceImage, int zoomLevel);

        #endregion

        private void UncheckPriorityMenuItems()
        {
            foreach (ToolStripMenuItem menuItem in priorityToolStripMenuItem.DropDownItems)
            {
                menuItem.Checked = false;
            }
        }
        private void lowestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _evolver.Priority = ThreadPriority.Lowest;
            UncheckPriorityMenuItems();
            lowestToolStripMenuItem.Checked = true;
        }

        private void highestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _evolver.Priority = ThreadPriority.Highest;
            UncheckPriorityMenuItems();
            highestToolStripMenuItem.Checked = true;
        }

        private void aboveNormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _evolver.Priority = ThreadPriority.AboveNormal;
            UncheckPriorityMenuItems();
            aboveNormalToolStripMenuItem.Checked = true;
        }

        private void normalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _evolver.Priority = ThreadPriority.Normal;
            UncheckPriorityMenuItems();
            normalToolStripMenuItem.Checked = true;
        }

        private void belowNormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _evolver.Priority = ThreadPriority.BelowNormal;
            UncheckPriorityMenuItems();
            belowNormalToolStripMenuItem.Checked = true;
        }

        private void frmGA_FormClosing(object sender, FormClosingEventArgs e)
        {
            _evolver?.Stop();
        }

        private void saveImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pop = _evolver.CurrentPopulation.Clone();
            StringBuilder b = new StringBuilder();
            b.AppendLine(
                @"<?xml version=""1.0"" standalone=""no""?>
<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" style=""background-color:black"">");
            //Format example: 	<polygon points=""50,150 50,200 200,200 200,100"" fill=""rgba(120,240,80,100)"" />
            foreach (var c in pop.chromosomes)
            {
                b.Append(@"<polygon points=""");

                foreach(var pos in c.Polygon)
                {
                    b.Append(pos.X + "," + pos.Y + " ");
                }
                var a = (c.PolyColor.A / 255.0);
                var aAsString = a.ToString("N2");//2 decimals
                b.AppendLine(@""" fill=""rgba(" + c.PolyColor.R + "," + c.PolyColor.G + "," + c.PolyColor.B + "," + aAsString + @")"" />");
                //b.AppendLine(@""" fill=""rgb(" + c.PolyColor.R + "," + c.PolyColor.G + "," + c.PolyColor.B + @")"" />");
            }
            b.Append("</svg>");
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "img.svg"), b.ToString());
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new frmSettings())
            {
                settingsForm.nudMaxPolygonCount.Value = GABase.Settings.MaxPolygonCount;
                settingsForm.nudMaxPolygonPointCount.Value = GABase.Settings.MaxPolygonPointCount;
                settingsForm.nudMinPolygonPointCount.Value = GABase.Settings.MinPolygonPointCount;
                settingsForm.cmbPolygonType.SelectedIndex = GABase.Settings.Polygon == GABase.Settings.PolygonType.Lines ? 0 : 1;
                settingsForm.nudFocusWeight.Value = GABase.Settings.FocusWeight;
                
                settingsForm.ShowDialog();
            }
        }
    }
}