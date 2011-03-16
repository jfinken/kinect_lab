using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Diagnostics;

using Earthmine;
using Earthmine.Overlay;

namespace earthmine_kinect
{
    public partial class EmForm : Form
    {
        public string customServiceEndpoint = "http://cloud.earthmine.com/service";
        public string customTileEndpoint = "http://s3.earthmine.com/tile";
        public string apiKey = "efa6u6ccsndmpg46bvgucsvm";
		public string secretKey = "wGQ8jv44XJ";

        private bool _polygonMode = false;
        private List<OverlayVertex> _polyVerts = new List<OverlayVertex>();
        private Bitmap _texture1;
        private Bitmap _texture2;
        private Earthmine.Overlay.Polygon _poly;
        // test
        private bool _toggleTexture = false;

        // Kinect access
        private KinectProvider _kinectProvider;
        private KinectConsumer _kinectConsumer;

        public EmForm()
        {
            Earthmine.Util.Logger.Instance.Log(Earthmine.Util.Logger.INFO, "------------------------------------");
            Earthmine.Util.Logger.Instance.Log(Earthmine.Util.Logger.INFO, "Running earthmine-kinect test app...");
            Earthmine.Util.Logger.Instance.Log(Earthmine.Util.Logger.INFO, "------------------------------------");
            // build the viewer
            _emViewer = new EarthmineViewer();
            _emViewer.SetServiceConfig(apiKey, secretKey, customServiceEndpoint, customTileEndpoint);
            // events and handlers
            _emViewer.OnViewerMouseLeftClick += new EarthmineViewer.EarthmineMouseEventHandler(onMouseLeftClick);
            _emViewer.OnViewerReady += new EarthmineViewer.EarthmineEventHandler(onViewerReady);
            _emViewer.OnViewerKeyUp += new EarthmineViewer.EarthmineEventHandler(onKeyUp);
            _emViewer.OnApiServiceException += new EarthmineViewer.EarthmineEventHandler(onApiServiceException);
            _emViewer.OnOpenGLVersionException += new EarthmineViewer.EarthmineEventHandler(onOpenGLVersionException);
            _emViewer.OnImageLoaderException += new EarthmineViewer.EarthmineEventHandler(onImageLoaderException);

            // poly texture
            _texture1 = new Bitmap(@"C:\Users\josh\projects\kinect_lab\earthmine_kinect\earthmine_kinect\wookies.png");
            _texture2 = new Bitmap(@"C:\Users\josh\projects\kinect_lab\earthmine_kinect\earthmine_kinect\yeah.jpg");
          

            //-----------------------------------------------------------------
            // It's not working out
            // - I think it's a thread space issue
            // - consider adding some sort of texture-provider to the Renderer
            // - or maybe even at this level as a background-worker.
            // - yes.  derive the kinect-provider from background worker and
            // set up the call-backs that then re-set the texture.
            //-----------------------------------------------------------------
            _kinectProvider = new KinectProvider();
            _kinectProvider.WorkerReportsProgress = true;
            _kinectProvider.WorkerSupportsCancellation = true;
            _kinectProvider.DoWork += 
                new DoWorkEventHandler(_kinectProvider.GetData);
            _kinectProvider.ProgressChanged +=
                new ProgressChangedEventHandler(KinectProvider_ProgressChanged);
            //_kinectProvider.OnKinectTextureReady += 
            //    new KinectProvider.EarthmineEventHandler(KinectProvider_OnKinectTextureReady);
            //_kinectConsumer = new KinectConsumer();

            InitializeComponent();
        }

        private void onWindowShown(Object sender, EventArgs e)
        {
            // this will set the initial viewport size 
            _emViewer.Resize(this.ClientRectangle.Width, this.ClientRectangle.Height);
        }
        private void onApiServiceException(EarthmineEventArgs info)
        {
            MessageBox.Show(info.ServiceExceptionMsg, "Earthmine Exception",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
        private void onOpenGLVersionException(EarthmineEventArgs info)
        {
            MessageBox.Show(info.ServiceExceptionMsg, "Earthmine Exception",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            this.Controls.Remove(this._emViewer.GetControl());
        }
        private void onImageLoaderException(EarthmineEventArgs info)
        {
            MessageBox.Show(info.ServiceExceptionMsg, "Earthmine Exception",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
        private void onResize(object sender, EventArgs e)
        {
            _emViewer.Resize(this.ClientRectangle.Width, this.ClientRectangle.Height);
            _emViewer.GetControl().Location = new Point(0, 0);
        }

        private void KinectProvider_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            _poly.Texture = _kinectProvider.KinectBitmap;
        }

        private void KinectProvider_OnKinectTextureReady(EarthmineEventArgs info)
        {
            // first send the texture to our side window
            // show the kinect data as well
            //if (!_kinectConsumer.Visible)
            //    _kinectConsumer.ShowDialog();

            //_kinectConsumer.KinectBitmap = _kinectProvider.KinectBitmap;
            if (_poly != null) 
            {
                // make a (cropped) copy - the data is locked otherwise
                //Bitmap texture = (Bitmap)_kinectProvider.KinectBitmap.Clone();
                //_poly.Texture = _kinectProvider.KinectBitmap;
                //_poly.Texture = _texture2;
            }
        }
		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
            if(_kinectConsumer != null)
                _kinectConsumer.Hide();
            if (_kinectProvider != null)
            {
                //this._kinectProvider.ShouldRun = false;
                //this._kinectProvider.readerThread.Join();

                // Cancel the asynchronous operation.
                _kinectProvider.CancelAsync();
            }
		}
        private void onViewerReady(EarthmineEventArgs args)
        {
            // LA
            //string pano_id = "1000002326738";

            // San Diego
            string pano_id = "1000003067000";
            //string pano_id = "1000003064326";

            // Oregon
            //string pano_id = "1000003729439";

            // no plane data due to tunnel
            //string pano_id = "1000001697457";

            // Santa Clara on houston
            //string pano_id = "1000003043405";

            // aussies
            //string pano_id = "1000002646235";

            // load by pano id
           
            Earthmine.Util.Logger.Instance.Log(Earthmine.Util.Logger.INFO, 
                string.Format("Loading pano id: {0}", pano_id));
            _emViewer.SetSubjectById(pano_id, null);
        }
        private void onMouseLeftClick(EarthmineEventArgs args)
        {
            // set the status bar
            if (args.location != null)
            {
                if (_polygonMode)
                {
                    if(_polyVerts.Count < 2)
                        _polyVerts.Add(new OverlayVertex(args.location, false));

                    // add?
                    if (_polyVerts.Count == 2)
                    {
                        _polygonMode = false;
                        _emViewer.EnableEditMode = false;
                        _poly = new Polygon(_polyVerts);
                        
                        //_poly.Texture = _texture1;
                        //_poly.Texture = _kinectProvider.KinectBitmap;

                        // go
                        _kinectProvider.RunWorkerAsync();

                        _emViewer.AddOverlay(_poly);
                    }
                }
            }
        }
        private void onKeyUp(EarthmineEventArgs args)
        {
            if (args.keyArgs.KeyCode == Keys.P)
            {
                if (!_polygonMode)
                {
                    _polygonMode = true;
                    _emViewer.EnableEditMode = true;
                }
                else
                {
                    _polygonMode = false;
                    _emViewer.EnableEditMode = false;
                }
            }
            else if (args.keyArgs.KeyCode == Keys.C)
            {
                _emViewer.ClearOverlays();
                _polyVerts.Clear();
                // Cancel the kinect texture gen.
                _kinectProvider.CancelAsync();
            }
            else if (args.keyArgs.KeyCode == Keys.D1)
            {
                // just depth
                _kinectProvider.textureMode = 
                    KinectProvider.TextureMode.DEPTH;
            }
            else if (args.keyArgs.KeyCode == Keys.D2)
            {
                // all rgb
                _kinectProvider.textureMode =
                    KinectProvider.TextureMode.RGB;
            }
            else if (args.keyArgs.KeyCode == Keys.D3)
            {
                // just user rgb: scene analysis
                _kinectProvider.textureMode =
                    KinectProvider.TextureMode.RGB_USER_ONLY;
            }
            else if (args.keyArgs.KeyCode == Keys.D4)
            {
                // user rgb over depth
                _kinectProvider.textureMode =
                    KinectProvider.TextureMode.DEPTH_RGB_USER;
            }
            /*
            else if (args.keyArgs.KeyCode == Keys.T)
            {
                if (_toggleTexture)
                {
                    //_poly.Texture = _texture1;
                    _toggleTexture = false;
                }
                else
                {
                    //_poly.Texture = _texture2;
                    _toggleTexture = true;
                }
            }
             */
        }
    }
}
