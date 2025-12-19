using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using GABase;
using GAgeneratedImagese.Tools;

namespace GA
{
    public partial class frmGA : Form
    {
        public frmGA()
        {
            InitializeComponent();

            worker = new Thread(worker_DoWork);
            worker.Priority = ThreadPriority.Normal;
        }

        private void worker_DoWork()
        {
            worker_DoWork(this, new DoWorkEventArgs(null));
        }

        Population popA;
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            long lastUpdate = sw.ElapsedMilliseconds;

	        var resizeFactor = 4;
            var resizedBitmap = new Bitmap(pictureBoxOriginal.Image,
			 new Size(pictureBoxOriginal.Image.Width / resizeFactor, pictureBoxOriginal.Image.Height / resizeFactor));
			Settings.ScreenWidth = resizedBitmap.Width;
			Settings.ScreenHeight = resizedBitmap.Height;


			FastBitmap originalPictureBitmap = new FastBitmap((Bitmap)((resizedBitmap).Clone()));
			//FastBitmap originalPictureBitmap = new FastBitmap((Bitmap)((pictureBoxOriginal.Image).Clone()));

	        popA = new Population(Settings.MaxPolygonCount);
	        popA.GenerateRandomChromosomes();
	        Chromosome chromosome = new Chromosome(Settings.MaxPolygonPointCount);
	        popA.chromosomes.Add(chromosome);
	        chromosome.GenerateRandomChromosome();

			Population popB;

			Selector selector = new Selector(originalPictureBitmap);
            Mutator mutator = new Mutator();
	        long previousFitnesse = long.MaxValue;
			int generation = 1;
            while (true)
            {
                double improvedPercentage = 0.0;

                popB = popA.Clone();

                int mutation = RandomGenerator.GetRandomInt(100);
                if (mutation < 20)
                {
                    mutator.Recolor(popB);
                }
                else if (mutation < 89)
                {
                    mutator.ChangePoint(popB);
                }
                else if (mutation < 92)
                {
                    mutator.AddPolygonPoint(popB);
                    //improvedPercentage = 5;
                }
                else if (mutation < 95)
                {
                    mutator.RemovePolygonPoint(popB);
                }
                else if (mutation < 98)
                {
                    mutator.SwitchChromosomes(popB);
                }
                else if (mutation < 99)
                {
                    mutator.AddChromosome(popB, originalPictureBitmap);
                    improvedPercentage = 1;
                }
                else if (mutation < 100)
                {
                    improvedPercentage = -1;
                    mutator.RemoveChromosome(popB);
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (popB.IsDirty)
                {
                    popB.IsDirty = false;
                    popA = selector.SelectPopulation(popA, popB, out var newPartialFitnesse, improvedPercentage);

					if (sw.ElapsedMilliseconds > lastUpdate + 2500)
					{
	                    var currentFitnesse = selector.CalculateFitness(popA);
                        //var currentFitnesse = newPartialFitnesse;

                        UpdateGui(new Bitmap(popA.GetPicture(), pictureBoxOriginal.Width, pictureBoxOriginal.Height), currentFitnesse, popA, generation, DifferencePicture.GetDifferencePicture(popA, originalPictureBitmap), sw.ElapsedMilliseconds, resizeFactor);
                        lastUpdate = sw.ElapsedMilliseconds;
						if (previousFitnesse > 0 &&
                            (previousFitnesse - currentFitnesse) * 1.0 / previousFitnesse < 0.0001 && resizeFactor > 1)
						{
							resizeFactor /= 2;
							if (resizeFactor > 1)
							{
								resizedBitmap = new Bitmap(pictureBoxOriginal.Image,
									new Size(pictureBoxOriginal.Image.Width / resizeFactor,
										pictureBoxOriginal.Image.Height / resizeFactor));
							}
							else
							{
								resizedBitmap = new Bitmap(pictureBoxOriginal.Image);
							}
							originalPictureBitmap = new FastBitmap((Bitmap)((resizedBitmap).Clone()));

							Settings.ScreenWidth = resizedBitmap.Width;
							Settings.ScreenHeight = resizedBitmap.Height;
							selector = new Selector(new FastBitmap((Bitmap)((resizedBitmap).Clone())));
							foreach (var c in popA.chromosomes)
							{
								for (var index = 0; index < c.Polygon.Count; index++)
								{
									var p = c.Polygon[index];
									c.Polygon[index] = new Point(Math.Min(p.X * 2, Settings.ScreenWidth), Math.Min(p.Y * 2, Settings.ScreenHeight));
								}
							}
							DifferencePicture.GetDifferencePicture(popA, originalPictureBitmap);
                            currentFitnesse = 0;//Reset to 0 after resize, can't compare fitnesses of <> resizeFactors.
                        }

						previousFitnesse = currentFitnesse;
					}
                }

                generation++;
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

        private Thread worker;
        private void btnStart_Click(object sender, EventArgs e)
        {
            if(!worker.IsAlive)
            {
                worker.Start();
                //BackgroundWorker worker = new BackgroundWorker();
                //worker.DoWork += worker_DoWork;
                //worker.RunWorkerAsync();
                btnStart.Text = "Stop";
            }
            else
            {
                worker.Abort();
                btnStart.Text = "Start";
            }
        }

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
            worker.Priority = ThreadPriority.Lowest;
            UncheckPriorityMenuItems();
            lowestToolStripMenuItem.Checked = true;
        }

        private void highestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            worker.Priority = ThreadPriority.Highest;
            UncheckPriorityMenuItems();
            highestToolStripMenuItem.Checked = true;
        }

        private void aboveNormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            worker.Priority = ThreadPriority.AboveNormal;
            UncheckPriorityMenuItems();
            aboveNormalToolStripMenuItem.Checked = true;
        }

        private void normalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            worker.Priority = ThreadPriority.Normal;
            UncheckPriorityMenuItems();
            normalToolStripMenuItem.Checked = true;
        }

        private void belowNormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            worker.Priority = ThreadPriority.BelowNormal;
            UncheckPriorityMenuItems();
            belowNormalToolStripMenuItem.Checked = true;
        }

        private void frmGA_FormClosing(object sender, FormClosingEventArgs e)
        {
            worker.Abort();
        }

        private void saveImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pop = popA.Clone();
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
    }
}