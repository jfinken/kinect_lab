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
        private Bitmap _texture;

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
            _texture = new Bitmap(@"C:\Users\jfinken\Documents\Projects\kinect_lab\earthmine_kinect\earthmine_kinect\wookies.png");

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
            _emViewer.Resize(this.ClientRectangle.Width, this.ClientRectangle.Height/2);
            _emViewer.GetControl().Location = new Point(0, 0);
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
                        Earthmine.Overlay.Polygon poly = new Polygon(_polyVerts);
                        poly.Texture = _texture;
                        _emViewer.AddOverlay(poly);
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
            }
        }
    }
}
