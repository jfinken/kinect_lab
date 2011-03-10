using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using xn;
using System.Threading;
using System.Drawing.Imaging;

namespace SimpleViewer.net
{
	public partial class MainWindow : Form
	{
		private readonly string SAMPLE_XML_FILE = @"../../../Data/SamplesConfig.xml";

		private Context context;
		private DepthGenerator depth;
		private ImageGenerator image;
        private SceneAnalyzer scene;
        // effectively same as scene-analyzer
        private UserGenerator userGenerator;
		private Thread readerThread;
		private bool shouldRun;
		private Bitmap bitmap;
		private int[] histogram;
        private bool shouldPrintID = true;
        private Color[] anticolors = { Color.Green, Color.Orange, Color.Red, Color.Purple, Color.Blue, Color.Yellow, Color.Black };
        private Color[] colors = { Color.Red, Color.Blue, Color.ForestGreen, Color.Yellow, Color.Orange, Color.Purple, Color.White };
        private int ncolors = 6;

		public MainWindow()
		{
			InitializeComponent();

			this.context = new Context(SAMPLE_XML_FILE);
			this.depth = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
            this.image = context.FindExistingNode(NodeType.Image) as ImageGenerator;
            //this.scene = context.FindExistingNode(NodeType.Scene) as SceneAnalyzer;

            // align depth with rgb
            this.depth.GetAlternativeViewPointCap().SetViewPoint(image);

			if (this.depth == null ||
                this.image == null)
			{
				throw new Exception("Viewer must have depth and image nodes!");
			}

            this.userGenerator = new UserGenerator(this.context);
            this.userGenerator.StartGenerating();

			this.histogram = new int[this.depth.GetDeviceMaxDepth()];

			MapOutputMode mapMode = this.depth.GetMapOutputMode();

			this.bitmap = new Bitmap((int)mapMode.nXRes, (int)mapMode.nYRes, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			this.shouldRun = true;
			this.readerThread = new Thread(ReaderThread);
			this.readerThread.Start();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			lock (this)
			{
				e.Graphics.DrawImage(this.bitmap,
					this.panelView.Location.X,
					this.panelView.Location.Y,
					this.panelView.Size.Width,
					this.panelView.Size.Height);
			}
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			//Don't allow the background to paint
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			this.shouldRun = false;
			this.readerThread.Join();
			base.OnClosing(e);
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == 27)
			{
				Close();
			}
			base.OnKeyPress(e);
		}

		private unsafe void CalcHist(DepthMetaData depthMD)
		{
			// reset
			for (int i = 0; i < this.histogram.Length; ++i)
				this.histogram[i] = 0;

			ushort* pDepth = (ushort*)depthMD.DepthMapPtr.ToPointer();

            // acclumulative histogram
			int points = 0;
			for (int y = 0; y < depthMD.YRes; ++y)
			{
				for (int x = 0; x < depthMD.XRes; ++x, ++pDepth)
				{
					ushort depthVal = *pDepth;
					if (depthVal != 0)
					{
						this.histogram[depthVal]++;
						points++;
					}
				}
			}

			for (int i = 1; i < this.histogram.Length; i++)
			{
				this.histogram[i] += this.histogram[i-1];
			}

			if (points > 0)
			{
				for (int i = 1; i < this.histogram.Length; i++)
				{
					this.histogram[i] = (int)(256 * (1.0f - (this.histogram[i] / (float)points)));
				}
			}
		}

		private unsafe void ReaderThread()
		{
			DepthMetaData depthMD = new DepthMetaData();
            ImageMetaData imageMD = new ImageMetaData();
            //SceneMetaData sceneMD = new SceneMetaData();

			while (this.shouldRun)
			{
				try
				{
					//this.context.WaitOneUpdateAll(this.depth);
                    this.context.WaitAndUpdateAll();
				}
				catch (Exception)
				{
				}

				this.depth.GetMetaData(depthMD);
                this.image.GetMetaData(imageMD);
                // NOTE: interesting to note if this is the same as GetUserPixels
                //MapData<ushort> sceneMap = this.scene.GetLabelMap();

				CalcHist(depthMD);

                lock (this)
                {
                    Rectangle rect = new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);
                    BitmapData data = this.bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                    RGB24Pixel* pImage = (RGB24Pixel*)this.image.GetImageMapPtr().ToPointer(); 
                    ushort* pDepth = (ushort*)this.depth.GetDepthMapPtr().ToPointer();
                    ushort* pLabels = (ushort*)this.userGenerator.GetUserPixels(0).SceneMapPtr.ToPointer();
                    
                    // set pixels
                    for (int y = 0; y < depthMD.YRes; ++y)
                    {
                        byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;

                        for (int x = 0; x < depthMD.XRes; ++x, ++pDepth, ++pLabels, ++pImage, pDest += 3)
                        {
                            pDest[0] = pDest[1] = pDest[2] = 0;

                            ushort label = *pLabels;

                            if (*pLabels != 0)
                            {
                                Color labelColor = Color.White;
                                if (label != 0)
                                {
                                    labelColor = colors[label % ncolors];
                                }

                                /*
                                byte pixel = (byte)this.histogram[*pDepth];
                                pDest[0] = (byte)(pixel * (labelColor.B / 256.0));
                                pDest[1] = (byte)(pixel * (labelColor.G / 256.0));
                                pDest[2] = (byte)(pixel * (labelColor.R / 256.0));
                                 */
                                // grab the rgb pixel for the user
                                // translate from RGB to BGR (windows format)
                                pDest[0] = pImage->nBlue;
                                pDest[1] = pImage->nGreen;
                                pDest[2] = pImage->nRed; 
                            }
                            else
                            {
                                byte pixel = (byte)this.histogram[*pDepth];
                                pDest[0] = 0;
                                pDest[1] = pixel;
                                pDest[2] = pixel;
                            }
                        }
                    }

                    this.bitmap.UnlockBits(data);

                    Graphics g = Graphics.FromImage(this.bitmap);
                    uint[] users = this.userGenerator.GetUsers();
                    foreach (uint user in users)
                    {
                        if (this.shouldPrintID)
                        {
                            Point3D com = this.userGenerator.GetCoM(user);
                            com = this.depth.ConvertRealWorldToProjective(com);

                            string label = "";

                            g.DrawString(label, new Font("Arial", 6), new SolidBrush(anticolors[user % ncolors]), com.X, com.Y);

                        }
                    }
                }

				this.Invalidate();
			}
		}

	}
}
