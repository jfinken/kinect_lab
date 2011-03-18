using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using xn;
using System.Threading;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Windows.Forms;

using System.ComponentModel;

using Earthmine;

namespace earthmine_kinect
{
    class KinectProvider: BackgroundWorker
    {
        public enum TextureMode 
        {
            DEPTH,
            RGB,
            RGB_USER_ONLY,
            DEPTH_RGB_USER
        }

  		//private readonly string SAMPLE_XML_FILE = @"../../../Data/SamplesConfig.xml";
		private readonly string SAMPLE_XML_FILE = @"../../SamplesConfig.xml";

		private Context context;
		private DepthGenerator depth;
		private ImageGenerator image;
        private SceneAnalyzer scene;
        // effectively same as scene-analyzer
        private UserGenerator userGenerator;
		public Thread readerThread;
		public Bitmap _kinectBitmap;
		public Bitmap bitmap;
		private int[] histogram;
        private bool shouldPrintID = true;
        private Color[] anticolors = { Color.Green, Color.Orange, Color.Red, Color.Purple, Color.Blue, Color.Yellow, Color.Black };
        private Color[] colors = { Color.Red, Color.Blue, Color.ForestGreen, Color.Yellow, Color.Orange, Color.Purple, Color.White };
        private int ncolors = 6;
        private int _frame = 0;
        private Object padLock = new Object();

        public TextureMode textureMode = TextureMode.DEPTH;
        //---------------------------------------------------------------------
        // Kinect provider as background-worker 
        //---------------------------------------------------------------------
        public KinectProvider() 
        {
            try
            {
                this.context = new Context(SAMPLE_XML_FILE);
            }
            catch (Exception exp)
            {
                MessageBox.Show("Exception: " + exp.Message, "earthmine Kinect", 
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

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

			//this.bitmap = new Bitmap((int)mapMode.nXRes, (int)mapMode.nYRes, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			this.bitmap = new Bitmap((int)mapMode.nXRes, (int)mapMode.nYRes, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			//this.KinectBitmap = new Bitmap((int)mapMode.nXRes, (int)mapMode.nYRes, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			this.KinectBitmap = new Bitmap((int)mapMode.nXRes, (int)mapMode.nYRes, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            //this.ShouldRun = true;
			//this.readerThread = new Thread(ReaderThread);
			//this.readerThread.Start();
        }

		//private unsafe void ReaderThread()
        public unsafe void GetData(object sender, DoWorkEventArgs e)
		{
			DepthMetaData depthMD = new DepthMetaData();
            ImageMetaData imageMD = new ImageMetaData();
            //SceneMetaData sceneMD = new SceneMetaData();

			//while (this.ShouldRun)
			while (!this.CancellationPending)
			{
                //if (this.CancellationPending)
                    //this.ShouldRun = false;

				try
				{
					//this.context.WaitOneUpdateAll(this.depth);
                    this.context.WaitAndUpdateAll();
				}
				catch (Exception)
				{
				}

                // if no device connected
                if (this.depth == null ||
                    this.image == null)
                {
                    return;
                }
				this.depth.GetMetaData(depthMD);
                this.image.GetMetaData(imageMD);
                // NOTE: interesting to note if this is the same as GetUserPixels
                //MapData<ushort> sceneMap = this.scene.GetLabelMap();

				CalcHist(depthMD);

                lock (this)
                {
                    Rectangle rect = new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);
                    //BitmapData data = this.bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    BitmapData data = this.bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    RGB24Pixel* pImage = (RGB24Pixel*)this.image.GetImageMapPtr().ToPointer(); 
                    ushort* pDepth = (ushort*)this.depth.GetDepthMapPtr().ToPointer();
                    ushort* pLabels = (ushort*)this.userGenerator.GetUserPixels(0).SceneMapPtr.ToPointer();
                    
                    // set pixels
                    for (int y = 0; y < depthMD.YRes; ++y)
                    {
                        byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;

                        //for (int x = 0; x < depthMD.XRes; ++x, ++pDepth, ++pLabels, ++pImage, pDest += 3)
                        for (int x = 0; x < depthMD.XRes; ++x, ++pDepth, ++pLabels, ++pImage, pDest += 4)
                        {
                            //pDest[0] = pDest[1] = pDest[2] = 0;
                            pDest[0] = pDest[1] = pDest[2] = pDest[3] = 0;

                            ushort label = *pLabels;

                            //-------------------------------------------------
                            // Grab the pixel according to mode 
                            //-------------------------------------------------
                            if (textureMode == TextureMode.DEPTH)
                            {
                                byte pixel = (byte)this.histogram[*pDepth];
                                pDest[0] = 0;
                                pDest[1] = pixel;
                                pDest[2] = pixel;
                                pDest[3] = 255;   // alpha
                            }
                            else if (textureMode == TextureMode.DEPTH_RGB_USER)
                            {
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
                                    pDest[3] = 255;   // alpha
                                }
                                else
                                {
                                    byte pixel = (byte)this.histogram[*pDepth];
                                    pDest[0] = 0;
                                    pDest[1] = pixel;
                                    pDest[2] = pixel;
                                    pDest[3] = 255;   // alpha
                                }
                            }
                            else if (textureMode == TextureMode.RGB)
                            {
                                pDest[0] = pImage->nBlue;
                                pDest[1] = pImage->nGreen;
                                pDest[2] = pImage->nRed; 
                                pDest[3] = 255;   // alpha

                            }
                            else if (textureMode == TextureMode.RGB_USER_ONLY)
                            {
                                if (*pLabels != 0)
                                {
                                    /*
                                    Color labelColor = Color.White;
                                    if (label != 0)
                                    {
                                        labelColor = colors[label % ncolors];
                                    }
                                     */

                                    // grab the rgb pixel for the user
                                    // translate from RGB to BGR (windows format)
                                    pDest[0] = pImage->nBlue;
                                    pDest[1] = pImage->nGreen;
                                    pDest[2] = pImage->nRed;
                                    pDest[3] = 255;   // alpha
                                }
                                else
                                {
                                    // set all other pixels to be transparent
                                    pDest[0] = 0;
                                    pDest[1] = 0;
                                    pDest[2] = 0;
                                    pDest[3] = 0;   // alpha
                                }
                            }
                            /*
                            if (*pLabels != 0)
                            {
                                Color labelColor = Color.White;
                                if (label != 0)
                                {
                                    labelColor = colors[label % ncolors];
                                }

                                //byte pixel = (byte)this.histogram[*pDepth];
                                //pDest[0] = (byte)(pixel * (labelColor.B / 256.0));
                                //pDest[1] = (byte)(pixel * (labelColor.G / 256.0));
                                //pDest[2] = (byte)(pixel * (labelColor.R / 256.0));
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
                             */
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

                // locked copy
                this.KinectBitmap.Dispose();
                this.KinectBitmap = (Bitmap)this.bitmap.Clone();

                // makes all black pixels transparent
                //if(textureMode == TextureMode.RGB_USER_ONLY)
                //    this.KinectBitmap.MakeTransparent( Color.Black );

                // will signal up-stream to come get the bitmap
                this.ReportProgress(1);

                /*
                if (_frame == 30)
                {
                    //Debug.WriteLine("\t\tNow: " + DateTime.Now.ToString());
                    // will signal to come get the bitmap
                    this.ReportProgress(_frame);
                    //if (OnKinectTextureReady != null)
                    //    OnKinectTextureReady(new EarthmineEventArgs(null, null, null));

                    _frame = 0;
                }
                else
                    _frame++;
                 */
			}
		}// reader-thread
        public Bitmap KinectBitmap
        {
            get
            {
                lock (padLock)
                {
                    return _kinectBitmap;
                }
            }
            set
            {
                lock (padLock)
                {
                    _kinectBitmap = value;
                }
            }
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
		}// calc-hist
    }
}
