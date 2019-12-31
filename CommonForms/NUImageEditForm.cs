using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media.Imaging;
using System.IO;
using CustomControls;
using ImageWindows;

namespace ImageProcessor
{
    public class ImageEditForm : EditForm
    {
        enum ImageMode
        {
            None,
            Distortion,
            Morph,
            Selection,
            Color,
            Gradient,
            Smoothing,
            Sharpening,
        }
        enum ImageScale                     // fixed image scales
        {
            Fit = 0,
            Half = 5,
            AsIs = 10,
            Double = 20,
            Quadruple = 40,
        }
        enum FilterSize
        {
            Auto,
            Size1,
            Size2,
            Size3,
        }
        enum EdgeGap
        {
            Auto,
            None,
            Gap2,
            Gap5,
            Gap10,
            Gap20,
        }
        enum SignalLevel
        {
            Auto,
            Dif0,
            Dif2,
            Dif5,
            Dif10,
            Dif20,
            Dif50,
            Dif100,
        }
        private System.ComponentModel.Container components = null;
        private ColorControl saturationControl;
        private GrayscaleControl brightnessControl;
        private GrayscaleControl transperancyControl;
        private Panel panel;
        private ListView layerListView;
        private ColumnHeader typeColumn;
        private ColumnHeader nameColumn;
        private Button saveButton;
        private Button undoButton;
        private ComboBox scaleBox;
        private GroupBox modeGroupBox;
        private Label label5;
        private GroupBox layerGroupBox;
        ArrayList morphPoints=new ArrayList();
        int viewingAreaOffset;				// viewing area offset from left of client rectangle
        string fileName;                    // image file name
        // image processing members
        int oldX;							// old mouse X position 
        int oldY;							// old mouse Y position 
        ImageMode imageMode;                // image layer mode
        bool colorSet = false;              // color pattern selected if true
        ShapeType shapeType;                // active shape type
        EditMode shapeEditMode;             // active shape edit mode
        Shape activeShape;                  // current active shape
        Point scrollPosition;               // previous drawing panel scroll position
        bool mouseDown;                     // true between mouseDown and MouseUp events
        CutRectangle selectRectangle;       // image selection rectangle
        CutPoly selection;                  // selected poly in display pixels
        CutPoly imageSelection;             // selected poly in image pixels
        DistortionInput distortion;
        Renderer renderer;
        Layer activeLayer;
        Operation operation;
        DateTime lastMoveTime;
        ColorTransform colorTransform = new ColorTransform();   // image pixel transformation 
        ByteMatrix processingMask;
        bool suspendUpdate = false;         // suspends image update while reseting
        private RadioButton BWButton;
        bool eventsDisabled = true;
        private Label label6;
        private ComboBox resolutiobBox;
        private Label label7;
        private ComboBox edgeGapBox;
        private Label label8;
        private ComboBox siignalLevelBox;
        ByteMatrix filter = null;
        private Button patternButton;
        private PictureBox patternBox;
        TimeSpan drawingDelay = new TimeSpan(0, 0, 0, 0, 100);
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        void InitializeComponent()
        {
            this.label5 = new System.Windows.Forms.Label();
            this.scaleBox = new System.Windows.Forms.ComboBox();
            this.modeGroupBox = new System.Windows.Forms.GroupBox();
            this.undoButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.panel = new System.Windows.Forms.Panel();
            this.layerListView = new System.Windows.Forms.ListView();
            this.typeColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.layerGroupBox = new System.Windows.Forms.GroupBox();
            this.saturationControl = new CustomControls.ColorControl();
            this.brightnessControl = new CustomControls.GrayscaleControl();
            this.transperancyControl = new CustomControls.GrayscaleControl();
            this.patternBox = new System.Windows.Forms.PictureBox();
            this.patternButton = new System.Windows.Forms.Button();
            this.BWButton = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.resolutiobBox = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.edgeGapBox = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.siignalLevelBox = new System.Windows.Forms.ComboBox();
            this.layerGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.patternBox)).BeginInit();
            this.SuspendLayout();
            // 
            // label5
            // 
            this.label5.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label5.Location = new System.Drawing.Point(5, 29);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(39, 17);
            this.label5.TabIndex = 38;
            this.label5.Text = "Scale";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // scaleBox
            // 
            this.scaleBox.Location = new System.Drawing.Point(46, 25);
            this.scaleBox.Name = "scaleBox";
            this.scaleBox.Size = new System.Drawing.Size(91, 21);
            this.scaleBox.TabIndex = 37;
            this.scaleBox.SelectedIndexChanged += new System.EventHandler(this.scaleBox_SelectedIndexChanged);
            // 
            // modeGroupBox
            // 
            this.modeGroupBox.Location = new System.Drawing.Point(1, 522);
            this.modeGroupBox.Name = "modeGroupBox";
            this.modeGroupBox.Size = new System.Drawing.Size(136, 184);
            this.modeGroupBox.TabIndex = 31;
            this.modeGroupBox.TabStop = false;
            this.modeGroupBox.Text = "Mouse mode";
            // 
            // undoButton
            // 
            this.undoButton.Location = new System.Drawing.Point(74, 2);
            this.undoButton.Name = "undoButton";
            this.undoButton.Size = new System.Drawing.Size(63, 20);
            this.undoButton.TabIndex = 33;
            this.undoButton.Text = "Undo";
            this.undoButton.Click += new System.EventHandler(this.undoButton_Click);
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(1, 2);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(67, 20);
            this.saveButton.TabIndex = 32;
            this.saveButton.Text = "Save";
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // panel
            // 
            this.panel.AutoScroll = true;
            this.panel.AutoScrollMinSize = new System.Drawing.Size(700, 700);
            this.panel.Location = new System.Drawing.Point(143, 0);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(665, 785);
            this.panel.TabIndex = 24;
            this.panel.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_Paint);
            this.panel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel_MouseDown);
            this.panel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panel_MouseMove);
            this.panel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panel_MouseUp);
            // 
            // layerListView
            // 
            this.layerListView.CheckBoxes = true;
            this.layerListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.typeColumn,
            this.nameColumn});
            this.layerListView.FullRowSelect = true;
            this.layerListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.layerListView.Location = new System.Drawing.Point(1, 49);
            this.layerListView.MultiSelect = false;
            this.layerListView.Name = "layerListView";
            this.layerListView.Size = new System.Drawing.Size(136, 92);
            this.layerListView.TabIndex = 39;
            this.layerListView.UseCompatibleStateImageBehavior = false;
            this.layerListView.View = System.Windows.Forms.View.Details;
            this.layerListView.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.layerListView_ItemChecked);
            this.layerListView.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.layerListView_ItemSelectionChanged);
            // 
            // typeColumn
            // 
            this.typeColumn.Text = "";
            this.typeColumn.Width = 21;
            // 
            // nameColumn
            // 
            this.nameColumn.Text = "Name";
            this.nameColumn.Width = 92;
            // 
            // layerGroupBox
            // 
            this.layerGroupBox.Controls.Add(this.transperancyControl);
            this.layerGroupBox.Controls.Add(this.saturationControl);
            this.layerGroupBox.Controls.Add(this.brightnessControl);
            this.layerGroupBox.Controls.Add(this.patternBox);
            this.layerGroupBox.Controls.Add(this.patternButton);
            this.layerGroupBox.Controls.Add(this.BWButton);
            this.layerGroupBox.Location = new System.Drawing.Point(1, 217);
            this.layerGroupBox.Name = "layerGroupBox";
            this.layerGroupBox.Size = new System.Drawing.Size(136, 299);
            this.layerGroupBox.TabIndex = 40;
            this.layerGroupBox.TabStop = false;
            this.layerGroupBox.Text = "Adjust layer";
            // 
            // brightnessControl
            // 
            this.brightnessControl.Location = new System.Drawing.Point(3, 17);
            this.brightnessControl.Name = "brightnessControl";
            this.brightnessControl.Offset = -1F;
            this.brightnessControl.Range = 2F;
            this.brightnessControl.Size = new System.Drawing.Size(120, 85);
            this.brightnessControl.TabIndex = 29;
            this.brightnessControl.Type = CustomControls.MultiIndicatorControl.Adjustment.Brightness;
            this.brightnessControl.ValueLocations = new System.Drawing.Point[] {
        new System.Drawing.Point(47, 0),
        new System.Drawing.Point(47, 38),
        new System.Drawing.Point(47, 100)};
            this.brightnessControl.ValueChanged += new System.EventHandler(this.ColorTransformChanged);
            // 
            // saturationControl
            // 
            this.saturationControl.Location = new System.Drawing.Point(3, 128);
            this.saturationControl.Name = "saturationControl";
            this.saturationControl.Offset = -1F;
            this.saturationControl.Range = 2F;
            this.saturationControl.Size = new System.Drawing.Size(120, 68);
            this.saturationControl.TabIndex = 21;
            this.saturationControl.Type = CustomControls.MultiIndicatorControl.Adjustment.Saturation;
            this.saturationControl.ValueLocations = new System.Drawing.Point[] {
        new System.Drawing.Point(47, -65536),
        new System.Drawing.Point(47, -16744448),
        new System.Drawing.Point(47, -16776961)};
            this.saturationControl.ValueChanged += new System.EventHandler(this.ColorTransformChanged);
            // 
            // transperancyControl
            // 
            this.transperancyControl.Location = new System.Drawing.Point(3, 200);
            this.transperancyControl.Name = "transperancyControl";
            this.transperancyControl.Offset = 0F;
            this.transperancyControl.Range = 1F;
            this.transperancyControl.Size = new System.Drawing.Size(120, 68);
            this.transperancyControl.TabIndex = 29;
            this.transperancyControl.Type = CustomControls.MultiIndicatorControl.Adjustment.Brightness;
            this.transperancyControl.ValueLocations = new System.Drawing.Point[] {
        new System.Drawing.Point(47, 0),
        new System.Drawing.Point(47, 100)};
            this.transperancyControl.ValueChanged += new System.EventHandler(this.ColorTransformChanged);
            // 
            // patternBox
            // 
            this.patternBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.patternBox.Location = new System.Drawing.Point(76, 272);
            this.patternBox.Name = "patternBox";
            this.patternBox.Size = new System.Drawing.Size(53, 20);
            this.patternBox.TabIndex = 66;
            this.patternBox.TabStop = false;
            // 
            // patternButton
            // 
            this.patternButton.Location = new System.Drawing.Point(3, 272);
            this.patternButton.Name = "patternButton";
            this.patternButton.Size = new System.Drawing.Size(67, 20);
            this.patternButton.TabIndex = 47;
            this.patternButton.Text = "Pattern";
            this.patternButton.Click += new System.EventHandler(this.patternButton_Click);
            // 
            // BWButton
            // 
            this.BWButton.AutoSize = true;
            this.BWButton.Location = new System.Drawing.Point(3, 110);
            this.BWButton.Name = "BWButton";
            this.BWButton.Size = new System.Drawing.Size(48, 17);
            this.BWButton.TabIndex = 65;
            this.BWButton.TabStop = true;
            this.BWButton.Text = "B/W";
            this.BWButton.UseVisualStyleBackColor = true;
            this.BWButton.CheckedChanged += new System.EventHandler(this.PresetTransformChanged);
            // 
            // label6
            // 
            this.label6.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label6.Location = new System.Drawing.Point(5, 151);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(73, 17);
            this.label6.TabIndex = 42;
            this.label6.Text = "Resolution";
            this.label6.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // resolutiobBox
            // 
            this.resolutiobBox.Location = new System.Drawing.Point(80, 147);
            this.resolutiobBox.Name = "resolutiobBox";
            this.resolutiobBox.Size = new System.Drawing.Size(57, 21);
            this.resolutiobBox.TabIndex = 41;
            // 
            // label7
            // 
            this.label7.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label7.Location = new System.Drawing.Point(5, 174);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(73, 17);
            this.label7.TabIndex = 44;
            this.label7.Text = "Edge";
            this.label7.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // edgeGapBox
            // 
            this.edgeGapBox.Location = new System.Drawing.Point(80, 170);
            this.edgeGapBox.Name = "edgeGapBox";
            this.edgeGapBox.Size = new System.Drawing.Size(57, 21);
            this.edgeGapBox.TabIndex = 43;
            // 
            // label8
            // 
            this.label8.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label8.Location = new System.Drawing.Point(5, 197);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(73, 17);
            this.label8.TabIndex = 46;
            this.label8.Text = "Level";
            this.label8.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // siignalLevelBox
            // 
            this.siignalLevelBox.Location = new System.Drawing.Point(80, 193);
            this.siignalLevelBox.Name = "siignalLevelBox";
            this.siignalLevelBox.Size = new System.Drawing.Size(57, 21);
            this.siignalLevelBox.TabIndex = 45;
            // 
            // ImageEditForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(810, 797);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.siignalLevelBox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.edgeGapBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.resolutiobBox);
            this.Controls.Add(this.layerGroupBox);
            this.Controls.Add(this.layerListView);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.modeGroupBox);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.scaleBox);
            this.Controls.Add(this.undoButton);
            this.KeyPreview = true;
            this.MinimumSize = new System.Drawing.Size(628, 648);
            this.Name = "ImageEditForm";
            this.Text = "Image Editing Form";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ImageEditForm_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.KeyboardHandler);
            this.Resize += new System.EventHandler(this.ImageEditForm_Resize);
            this.layerGroupBox.ResumeLayout(false);
            this.layerGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.patternBox)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        public ImageEditForm()              
        {
            InitializeComponent();
            viewingAreaOffset = panel.Location.X;
            undoButton.Enabled = false;
            mouseDown = false;
            renderer = new Renderer(panel);
            scaleBox.Items.AddRange(Enum.GetNames(typeof(ImageScale)));
            resolutiobBox.Items.AddRange(Enum.GetNames(typeof(FilterSize)));
            edgeGapBox.Items.AddRange(Enum.GetNames(typeof(EdgeGap)));
            siignalLevelBox.Items.AddRange(Enum.GetNames(typeof(SignalLevel)));
            scaleBox.SelectedIndex = 0;
            resolutiobBox.SelectedIndex = 0;
            edgeGapBox.SelectedIndex = 0;
            siignalLevelBox.SelectedIndex = 0;
            distortion = new DistortionInput();
            selection = new CutPoly();
            imageSelection = new CutPoly();
            selectRectangle = new CutRectangle();
            ContextMenu selectMenu = new ContextMenu();
            MenuItem menuForward = new MenuItem("Move on top", new EventHandler(MoveLayerOnTop));
            MenuItem menuBack = new MenuItem("Move back", new EventHandler(MoveLayerBack));
            MenuItem menuFlip = new MenuItem("Flip", new EventHandler(FlipLayerHorisontal));
            MenuItem menuContour = new MenuItem("Contour", delegate(object s, EventArgs e) { CreateGradientContrast(); });
            MenuItem menuMedian = new MenuItem("Smooth", delegate (object s, EventArgs e) { SmoothImage(); });
            MenuItem menuOdd = new MenuItem("Remove odd", delegate (object s, EventArgs e) { RemoveOdd(); });
            MenuItem menuWavelet = new MenuItem("Sharpen", delegate (object s, EventArgs e) { SharpenImage(); });
            //MenuItem menuPeriodic = new MenuItem("Clear periodic", delegate (object s, EventArgs e) { CreatePeriodic(); });
            MenuItem menuDrawing = new MenuItem("Make drawing", delegate (object s, EventArgs e) { CreateDrawing(); });
            MenuItem menuDelete = new MenuItem("Delete", new EventHandler(DeleteLayer));
            selectMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] { menuForward, menuBack, menuFlip, menuContour, menuWavelet, menuMedian, menuOdd, menuDelete });
            layerListView.ContextMenu = selectMenu;
            ResetColorControls();
        }
        int SelectedLevel(int defaultValue)
        {
            SignalLevel slevel = (SignalLevel)Enum.Parse(typeof(SignalLevel), (string)siignalLevelBox.SelectedItem);
            return slevel == SignalLevel.Auto ? defaultValue :
                   slevel == SignalLevel.Dif0 ? 0 :
                   slevel == SignalLevel.Dif2 ? 2 :
                   slevel == SignalLevel.Dif5 ? 5 :
                   slevel == SignalLevel.Dif10 ? 10 :
                   slevel == SignalLevel.Dif20 ? 20 :
                   slevel == SignalLevel.Dif50 ? 50 : 100;
        }
        int SelectedResolution(int defaultValue)
        {
            FilterSize fsize = (FilterSize)Enum.Parse(typeof(FilterSize), (string)resolutiobBox.SelectedItem);
            return fsize == FilterSize.Auto ? defaultValue :
                   fsize == FilterSize.Size1 ? 1 :
                   fsize == FilterSize.Size2 ? 2 :
                   fsize == FilterSize.Size3 ? 3 : 4;
        }
        int SelectedEdge(int defaultValue)
        {
            EdgeGap gap = (EdgeGap)Enum.Parse(typeof(EdgeGap), (string)edgeGapBox.SelectedItem);
            return gap == EdgeGap.Auto ? defaultValue :
                   gap == EdgeGap.None ? 0 :
                   gap == EdgeGap.Gap2 ? 2 :
                   gap == EdgeGap.Gap5 ? 5 :
                   gap == EdgeGap.Gap10 ? 10 :
                   gap == EdgeGap.Gap20 ? 20 : 1;
        }
        float SelectedScale
        {
            get
            {
                ImageScale scale = (ImageScale)Enum.Parse(typeof(ImageScale), (string)scaleBox.SelectedItem);
                return (int)scale / 10.0f;
            }
        }
        public override void DrawNewImage(BitmapAccess src, string name)
        {
            lastMoveTime = DateTime.Now;
            renderer.NewImage(src, new Geometry(panel.Size, src.Width, src.Height, 2));
            fileName = name;
            Text = fileName + "  " + src.Width + 'x' + src.Height;
            UpdateLayerList(renderer.Layers[0].Name);
            eventsDisabled = true;
            BWButton.Checked = false;
            BringToFront();
            eventsDisabled = false;
        }
        public override void DrawNewImageGradient(BitmapAccess src, string name)
        {
            lastMoveTime = DateTime.Now;
            renderer.NewImage(src, new Geometry(panel.Size, src.Width, src.Height, 2));
            fileName = name;
            Text = fileName + "  " + src.Width + 'x' + src.Height;
            SetActiveLayer(renderer.Layers[0]);
            activeLayer.Enabled = false;
            CreateGradientContrast(-1, -1);
            BringToFront();
            eventsDisabled = false;
        }
        void UpdateLayerList(string selectedLayerName)
        {
            eventsDisabled = true;
            layerListView.Items.Clear();
            foreach (Layer l in renderer.Layers)
            {
                ListViewItem lvi = new ListViewItem(new string[] { "", l.Name });
                lvi.Checked = l.Enabled;
                lvi.Tag = l;
                layerListView.Items.Add(lvi);
                if (l.Name == selectedLayerName)
                {
                    SetActiveLayer(l);
                    lvi.Selected = true;
                }
            }
            SetLayerListColors();
            layerListView.Invalidate();
            eventsDisabled = false;
            operation = Operation.None;
            renderer.Redraw();
        }
        void SetLayerListColors()           
        {
            foreach (ListViewItem lvi in layerListView.Items)
            {
                if (lvi.Selected)
                {
                    lvi.ForeColor = Color.White;
                    lvi.BackColor = Color.DarkBlue;
                }
                else
                {
                    lvi.ForeColor = Color.Black;
                    lvi.BackColor = Color.White;
                }
            }
        }
        void CreateToolButtons(Layer layer) 
        {
            foreach (Control c in modeGroupBox.Controls)
                c.Dispose();
            modeGroupBox.Controls.Clear();
            if(layer.IsImage)
                CreateControls(Enum.GetNames(typeof(ImageMode)), null, null);
            else if(layer.IsDrawing)
                CreateControls(Enum.GetNames(typeof(ShapeType)), Enum.GetNames(typeof(EditMode)), new string[] { "Thickness" });
        }
        void CreateControls(string[] bns, string[] ans, string[] inp)
        {
            int ystep = 20;
            int y = 0;
            int xoffset = 8;
            if (bns != null)
            {
                for (int i = 0; i < bns.Length; i++)
                {
                    RadioButton rb=CreateRadioButton(bns[i], xoffset, y += ystep, 100, ystep - 2);
                    if (i == 0)
                        rb.Checked = true;
                    modeGroupBox.Controls.Add(rb);
                }
            }
            if (ans != null)
            {
                for (int i = 0; i < ans.Length; i++)
                    modeGroupBox.Controls.Add(CreateRadioButton(ans[i], xoffset, y += ystep, 100, ystep - 2));
            }
            if (inp != null)
            {
                for (int i = 0; i < inp.Length; i++)
                    modeGroupBox.Controls.AddRange(CreateInputBox(inp[i], xoffset, y += ystep, 70, 100, ystep - 2));
            }
        }
        Control[] CreateInputBox(string title, int x, int y, int xo, int w, int h)
        {
            Control[] ca = new Control[2];
            ca[0] = new Label();
            ca[0].Name = title + "Label";
            ca[0].Location = new Point(x, y);
            ca[0].Size = new System.Drawing.Size(xo-x-2, h);
            ca[0].Text = title.Replace('_', ' ');
            ca[1] = new NumericUpDown();
            ca[1].Name = title + "Input";
            ca[1].Location = new Point(xo, y);
            ca[1].Size = new System.Drawing.Size(w-xo-2, h);
            ((NumericUpDown)ca[1]).Value = 1;
            ((NumericUpDown)ca[1]).ValueChanged += new System.EventHandler(inputBox_ValueChanged);
            return ca;
        }
        RadioButton CreateRadioButton(string title, int x, int y, int w, int h)
        {
            RadioButton modeButton = new RadioButton();
            modeButton.Name = title;
            modeButton.Location = new Point(x, y);
            modeButton.Size = new System.Drawing.Size(w, h);
            modeButton.Text = title.Replace('_', ' ');
            modeButton.CheckedChanged += new System.EventHandler(modeButton_CheckedChanged);
            return modeButton;
        }
        void panel_Paint(object s, PaintEventArgs e)
        {
            if (scrollPosition != panel.AutoScrollPosition)
            {
                renderer.Layers.Move(panel.AutoScrollPosition.X - scrollPosition.X, panel.AutoScrollPosition.Y - scrollPosition.Y);
                scrollPosition = panel.AutoScrollPosition;
                renderer.Redraw();
            }
            else
                renderer.Draw();
        }
        void ImageEditForm_Resize(object s, EventArgs e)
        {
            panel.Size = new Size(ClientSize.Width - viewingAreaOffset, ClientSize.Height);
            if (scaleBox.SelectedItem != null && ImageScale.Fit == (ImageScale)Enum.Parse(typeof(ImageScale), (string)scaleBox.SelectedItem))
                RescaleLayers(0);
            renderer.Resize(panel);
        }
        void DeleteLayer(object s, EventArgs e)
        {
            if (renderer.Layers.Count <= 1)
                return;
            DialogResult res = MessageBox.Show(this, "Are you sure you want to delete layer '" +activeLayer.Name + "'?", 
                "Delete headers warning", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
            {
                renderer.Layers.Remove(activeLayer.Name);
                UpdateLayerList(renderer.Layers[0].Name);
            }
        }
        void CreateGradientContrast()
        {
            if (!activeLayer.IsImage)
                return;
            BitmapAccess rb = ((ImageLayer)activeLayer).Image;
            int defaultResolution = (rb.Width + rb.Height) / 3000 + 1;
            CreateGradientContrast(SelectedResolution(defaultResolution), SelectedLevel(50));
        }
        void CreateGradientContrast(int resolution, int darkLevel)
        {
            string name = "Contour" + resolution;
            renderer.Layers.Add(new GradientContrastLayer(name, ((ImageLayer)activeLayer).Image, activeLayer.Geometry.Clone(), resolution, darkLevel, colorTransform));
            UpdateLayerList(name);
        }
        void RemoveOdd()
        {
            if (!activeLayer.IsImage)
                return;
            int resolution = SelectedResolution(2);
            string name = "-Odds" + resolution;
            int level = SelectedLevel(5);
            name += "." + level;
            BitmapAccess smoothed = ((ImageLayer)activeLayer).Image.ApplyFilterType(AccessType.MedianFilter, resolution, level);
            renderer.Layers.Add(new ImageLayer(name, smoothed, activeLayer.Geometry.Clone()));
            UpdateLayerList(name);
        }
        void SmoothImage()
        {
            if (!activeLayer.IsImage)
                return;
            int resolution = SelectedResolution(2);
            string name = "Smooth" + resolution;
            BitmapAccess filtered = ((ImageLayer)activeLayer).Image.ApplyFilterType(AccessType.MedianFilter, resolution, 0);
            renderer.Layers.Add(new ImageLayer(name, filtered, activeLayer.Geometry.Clone()));
            UpdateLayerList(name);
        }
        void SharpenImage()
        {
            if (!activeLayer.IsImage)
                return;
            int resolution = SelectedResolution(2);
            string name = "Sharp"+ resolution;
            int level = SelectedLevel(25);
            name += "." + level;
            BitmapAccess filtered = ((ImageLayer)activeLayer).Image.ApplyFilterType(AccessType.WaveletFilter, resolution, level);
            renderer.Layers.Add(new ImageLayer(name, filtered, activeLayer.Geometry.Clone()));
            UpdateLayerList(name);
        }
        void CreateRidge(int resolution)
        {
            if (!activeLayer.IsImage)
                return;
            string name = activeLayer.Name + "R" + resolution;
            renderer.Layers.Add(new RidgeLayer(name, ((ImageLayer)activeLayer).Image, activeLayer.Geometry.Clone(), resolution, colorTransform));
            UpdateLayerList(name);
        }
        int GetThickness()                  // retrieves tool thickness
        {
            try
            {
                NumericUpDown ic = (NumericUpDown)modeGroupBox.Controls["ThicknessInput"];
                return (int)ic.Value;
            }
            catch { return 1; }
        }
        void SetActiveLayer(Layer l)	    // resets image drawing controls to default values
        {
            if (l == null)
                return;
            if (activeLayer != null && activeLayer.Name == l.Name)
                return;
            if (activeLayer==null || activeLayer.Type != l.Type)
                CreateToolButtons(l);
            if (activeLayer != null)
                activeLayer.ColorTransform.CopyFrom(colorTransform);
            activeLayer = l;
            colorTransform.CopyFrom(l.ColorTransform);
            ResetColorControls();
            suspendUpdate = true;
            layerGroupBox.Text=activeLayer.Name;
            distortion.Center = l.Geometry.Center;
            patternBox.BackColor = activeLayer.ColorTransform.Pattern == ColorTransform.ColorNull ? 
                Color.FromKnownColor(KnownColor.Control) : SetFromMediaColor(activeLayer.ColorTransform.Pattern);
            suspendUpdate = false;
        }
        Color SetFromMediaColor(System.Windows.Media.Color nc)
        {
            return Color.FromArgb(nc.A, nc.R, nc.G, nc.B);
        }
        System.Windows.Media.Color SetMediaColor(Color nc)
        {
            return System.Windows.Media.Color.FromArgb(nc.A, nc.R, nc.G, nc.B);
        }
        void inputBox_ValueChanged(object s, EventArgs e)
        {
            if (activeLayer.IsDrawing)
            {
                activeShape=((DrawingLayer)activeLayer).Drawing.AddShape(GetThickness(), shapeType);
                undoButton.Enabled = true;
            }
        }
        void saveButton_Click(object s, EventArgs e)
        {
            SaveFileDialog saveAsDialog = new SaveFileDialog();
            DataType type = ImageFileInfo.FileType(fileName);
            saveAsDialog.FileName = Path.GetFileNameWithoutExtension(fileName);
            saveAsDialog.Filter = "Jpeg|*.jpg|Bitmap|*.png|Gif|*.gif"; // safe format relies on this order
            saveAsDialog.FilterIndex = type == DataType.GIF ? 3 : type == DataType.PNG ? 2 : 1;
            saveAsDialog.RestoreDirectory = true;
            saveAsDialog.InitialDirectory = Path.GetDirectoryName(fileName);
            if (saveAsDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (saveAsDialog.FilterIndex == 1)
                        renderer.Save(saveAsDialog.FileName, selectRectangle.Rect, ImageFormat.Jpeg, 0);
                    else if (saveAsDialog.FilterIndex == 2)
                        renderer.Save(saveAsDialog.FileName, selectRectangle.Rect, ImageFormat.Png, 0);
                    else if (saveAsDialog.FilterIndex == 3)
                        renderer.Save(saveAsDialog.FileName, selectRectangle.Rect, ImageFormat.Gif, 0);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
#endif
                }
            }
        }
        void undoButton_Click(object s, EventArgs e)
        {
            undoButton.Enabled=activeLayer.Undo();
            panel.Invalidate();
        }
        void scaleBox_SelectedIndexChanged(object s, EventArgs e)
        {
            RescaleLayers(SelectedScale);
        }
        void RescaleLayers(float imageScale)
        {
            if (renderer.Layers.Count == 0)
                return;
            Geometry backgroundGeometry = renderer.Layers.Main.Geometry;
            if(imageScale==0)
                imageScale=(new Geometry(panel.Size, backgroundGeometry.Size, 2)).Scale;
            float oldScale = backgroundGeometry.Scale; 
            renderer.Layers.Rescale(imageScale / oldScale, new PointF(panel.Size.Width * 0.5f, panel.Size.Height * 0.5f));
            Point imageShift = SetScrollPosition(backgroundGeometry.Target);
            renderer.Layers.Move(imageShift.X, imageShift.Y);
            renderer.Redraw();
        }
        void InitializeProcessingMask()
        {
            int h = ((ImageLayer)activeLayer).Image.Height;
            int w = ((ImageLayer)activeLayer).Image.Width;
            processingMask = new ByteMatrix(h, w);
        }
        ByteMatrix UpdateProcessingMask(Point from, Point to, int edge)
        {
            System.Drawing.Rectangle target = activeLayer.Geometry.Target;
            if (from.X < 0)
                from.X = 0;
            if (to.X < 0)
                to.X = 0;
            if (from.Y < 0)
                from.Y = 0;
            if (to.Y < 0)
                to.Y = 0;
            if (from.X > target.Width)
                from.X = target.Width;
            if (to.X > target.Width)
                to.X = target.Width;
            if (from.Y > target.Height)
                from.Y = target.Height;
            if (to.Y > target.Height)
                to.Y = target.Height;
            ByteMatrix increment = ByteMatrix.CreateFilterMask(from, to, edge);
            return processingMask.MergeMask(increment);
        }
        Point SetScrollPosition(System.Drawing.Rectangle imageRectangle)
        {
            panel.AutoScrollMinSize = new Size(Math.Max(panel.Size.Width, imageRectangle.Width),
                Math.Max(panel.Size.Height, imageRectangle.Height));
            Point imageShift = new Point();
            scrollPosition.X = 0;
            scrollPosition.Y = 0;
            if (panel.Size.Width < imageRectangle.Width)     // horizontal scrollbar
            {
                if (imageRectangle.X <= 0)
                    scrollPosition.X = imageRectangle.X;
                else
                    imageShift.X = -imageRectangle.X;
            }
            else                                                    // no horizontal scrollbar
                imageShift.X = -imageRectangle.X + (panel.Size.Width - imageRectangle.Width) / 2;
            if (panel.Size.Height < imageRectangle.Height)   // vertical scrollbar
            {
                if (imageRectangle.Y <= 0)
                    scrollPosition.Y = imageRectangle.Y;
                else
                    imageShift.Y = -imageRectangle.Y;
            }
            else                                                     // no vertical scrollbar
                imageShift.Y = -imageRectangle.Y + (panel.Size.Height - imageRectangle.Height) / 2;
            panel.AutoScrollPosition = new Point(-scrollPosition.X, -scrollPosition.Y);
            return imageShift;
        }
        //void ShowTransformParameters()
        //{
        //    try
        //    {
        //        angleCtrl.Value = (decimal)activeLayer.Geometry.Angle;
        //        scaleCtrl.Value = (decimal)activeLayer.Geometry.Scale;
        //        shearCtrl.Value = (decimal)activeLayer.Geometry.Shear;
        //        aspectCtrl.Value = (decimal)activeLayer.Geometry.Aspect;
        //    }
        //    catch { }
        //}
        void DrawOperationString(Operation op, float val, MouseEventArgs e)
        {
            string tip = operation.ToString();
            if (val != 0)
                tip += ' ' + val.ToString("f3");
            renderer.AddString(tip, e.X, e.Y - 13);
        }
        void panel_MouseDown(object s, MouseEventArgs e)
        {
            mouseDown = true;
            oldX = e.X;
            oldY = e.Y;
            lastMoveTime = DateTime.Now;
            if (activeLayer.IsImage)
            {
                if (imageMode == ImageMode.Morph)
                {
                    if (Control.ModifierKeys != Keys.Control)
                        morphPoints.Add(activeLayer.Geometry.ImagePointF(oldX, oldY));
                }
                else if (imageMode == ImageMode.Distortion || imageMode == ImageMode.None)
                {
                    operation = imageMode == ImageMode.Distortion ? distortion.OperationFromPoint(oldX, oldY) : Operation.None;
                    if (operation == Operation.None)
                        operation = OperationFrom.Button((int)e.Button);
                    float val = activeLayer.Geometry.SetFromPoints(operation, e.X, e.Y, oldX, oldY, distortion.Center);
                    DrawOperationString(operation, val, e);
                }
                else if (imageMode == ImageMode.Selection)
                {
                    selection.Poly.Clear();
                    selectRectangle.Rect = new System.Drawing.Rectangle();
                    selection.Poly.Add(new System.Windows.Point(e.X, e.Y));
                }
                //else if (imageMode == ImageMode.Sharpening || imageMode == ImageMode.Smoothing || imageMode == ImageMode.Color)
                //{
                //    InitializeProcessingMask(((ImageLayer)activeLayer).Image.Height, ((ImageLayer)activeLayer).Image.Width);
                //}
            }
            else if (activeLayer.IsDrawing)
            {
                switch (shapeEditMode)
                {
                    case EditMode.Click:
                        if (e.Button == MouseButtons.Right || ((DrawingLayer)activeLayer).Drawing.Count == 0)
                            activeShape = ((DrawingLayer)activeLayer).Drawing.AddShape(GetThickness(), shapeType);
                        ((DrawingLayer)activeLayer).Drawing.AddPoint(activeLayer.Geometry.ImagePoint(oldX, oldY));
                        renderer.Redraw();
                        break;
                    case EditMode.Shape:
                        if (activeShape != null)
                            activeShape.SetActiveControl(e.Location);
                        break;
                }
            }
            BringToFront();
        }
        void panel_MouseMove(object s, MouseEventArgs e)
        {
            if (!mouseDown)
                return;
            else if (DateTime.Now < lastMoveTime + drawingDelay)
                return;
            if (activeLayer.IsImage)
            {
                if (imageMode == ImageMode.Distortion || imageMode == ImageMode.None)
                {
                    float val = 0;
                    if (operation == Operation.Center)
                        distortion.Move(e.X - oldX, e.Y - oldY);
                    else if (operation == Operation.Size)
                        distortion.Resize(e.X, e.Y, oldX, oldY);
                    else if (operation == Operation.Move)
                    {
                        distortion.Move(e.X - oldX, e.Y - oldY);
                        activeLayer.Geometry.Move(e.X - oldX, e.Y - oldY);
                    }
                    else
                    {
                        val = activeLayer.Geometry.SetFromPoints(operation, e.X, e.Y, oldX, oldY, distortion.Center);
                        distortion.Transform = activeLayer.Geometry;
                    }
                    renderer.Redraw();
                    //ShowTransformParameters();
                    DrawOperationString(operation, val, e);
                }
                else if (imageMode == ImageMode.Selection)
                {
                    if (Math.Max(Math.Abs(oldX - e.X), Math.Abs(oldY - e.Y)) > 2)
                    {
                        selection.Poly.Add(new System.Windows.Point(e.X, e.Y));
                        renderer.Draw();
                    }
                }
                else if (imageMode == ImageMode.Color && colorSet)
                {
                    int r = SelectedEdge(5);
                    int level = SelectedLevel(30);
                    Point to = activeLayer.Geometry.ImagePoint(e.X, e.Y);
                    Point from = activeLayer.Geometry.ImagePoint(oldX, oldY);
                    ByteMatrix delta = UpdateProcessingMask(from, to, r);
                    ((ImageLayer)activeLayer).ApplyColor(delta, SetMediaColor(patternBox.BackColor), level);
                    //Console.WriteLine(delta.ToString());
                    renderer.Redraw();
                }
                else if (imageMode == ImageMode.Sharpening && filter!=null)
                {
                    int r = SelectedEdge(5);
                    int level = SelectedLevel(100);
                    Point to = activeLayer.Geometry.ImagePoint(e.X, e.Y);
                    Point from = activeLayer.Geometry.ImagePoint(oldX, oldY);
                    ByteMatrix delta = UpdateProcessingMask(from, to, r);
                    ((ImageLayer)activeLayer).ApplyMaskFilter(delta, filter, level);
                    //Console.WriteLine(delta.ToString());
                    renderer.Redraw();
                }
                oldX = e.X;
                oldY = e.Y;
                lastMoveTime = DateTime.Now;
            }
            else if (activeLayer.IsDrawing)
            {
                if (shapeEditMode == EditMode.Shape && activeShape != null)
                {
                    activeShape.MoveActiveControl(e.Location);
                    renderer.Redraw();
                }
                if (shapeEditMode == EditMode.Find)
                {
                    ((DrawingLayer)activeLayer).Drawing.AddPoint(activeLayer.Geometry.ImagePoint(oldX, oldY));
                    renderer.Redraw();
                }
            }
        }
        void panel_MouseUp(object s, MouseEventArgs e)
        {
            mouseDown = false;
            if (activeLayer.IsImage)
            {
                if (imageMode == ImageMode.Morph)
                {
                    if (Control.ModifierKeys == Keys.Control)
                    {
                        PointF shift = new PointF(oldX-e.X, oldY-e.Y);
                        //ApplyMorph(shift);
                    }
                }
                else if (imageMode == ImageMode.Distortion || imageMode == ImageMode.None)
                {
                    operation = Operation.None;
                }
                else if(imageMode == ImageMode.Color)
                {
                    if (!colorSet)
                    {
                        Layer ori = renderer.Layers[0];
                        if (ori.IsImage)
                        {
                            BitmapAccess im = ((ImageLayer)ori).Image;
                            PointF ip = ori.Geometry.ImagePointF(oldX, oldY);
                            if (ip.X >= 0 && ip.X < im.Width && ip.Y >= 0 && ip.Y < im.Height)
                            {
                                colorTransform.Pattern = im.GetPixel((int)ip.X, (int)ip.Y);
                                patternBox.BackColor = SetFromMediaColor(colorTransform.Pattern);
                                //activeLayer.TransformColor(colorTransform);
                                //renderer.Redraw();
                                colorSet = true;
                            }
                            else
                            {
                                patternBox.BackColor = Color.FromKnownColor(KnownColor.Control);
                                colorTransform.Pattern = ColorTransform.ColorNull;
                                colorSet = false;
                            }
                        }
                    }
                }
                else if (imageMode == ImageMode.Selection)
                {
                    if (Math.Max(Math.Abs(oldX - e.X), Math.Abs(oldY - e.Y)) > 0)
                        selection.Poly.Add(new System.Windows.Point(e.X, e.Y));
                    SetImageSelection();
                }
                renderer.Draw();
            }
            if (activeLayer.IsDrawing && shapeEditMode == EditMode.Shape && activeShape != null)
            {
                activeShape.UnsetActiveControl();
                renderer.Redraw();
            }
        }
        void modeButton_CheckedChanged(object s, EventArgs e)
        {
            RadioButton rb = (RadioButton)s;
            if (rb.Checked && activeLayer!=null)
            {
                if (activeLayer.IsImage)
                {
                    imageMode = (ImageMode)Enum.Parse(typeof(ImageMode), rb.Name);
                    if (imageMode == ImageMode.Gradient)
                    {
                        ApplyLinerColorGradient();
                    }
                    if (imageMode == ImageMode.Selection)
                    {
                        renderer.AddDrawingElement(selection);
                        renderer.AddDrawingElement(selectRectangle);
                        renderer.Draw();
                    }
                    else if (imageMode != ImageMode.Gradient)
                    {
                        renderer.RemoveDrawingElement(selection);
                        renderer.RemoveDrawingElement(selectRectangle);
                        renderer.Draw();
                        selection.Poly.Clear();
                        imageSelection.Poly.Clear();
                        selectRectangle.Rect = new System.Drawing.Rectangle();
                    }
                    if (imageMode == ImageMode.Distortion)
                    {
                        distortion.Transform = activeLayer.Geometry;
                        renderer.AddDrawingElement(distortion);
                        renderer.Draw();
                    }
                    else
                    {
                        renderer.RemoveDrawingElement(distortion);
                        renderer.Draw();
                    }
                    if (imageMode == ImageMode.Smoothing)
                    {
                        int resolution = SelectedResolution(2);
                        filter = ByteMatrix.CreateWaveletFilter(resolution);
                    }
                    else
                    {
                        filter = null;
                    }
                }
                else if (activeLayer.IsDrawing)
                {
                    try
                    {
                        shapeType = (ShapeType)Enum.Parse(typeof(ShapeType), rb.Name);
                        activeShape = ((DrawingLayer)activeLayer).Drawing.AddShape(GetThickness(), shapeType);
                        shapeEditMode = EditMode.Click;
                        undoButton.Enabled = true;
                    }
                    catch
                    {
                        shapeEditMode = (EditMode)Enum.Parse(typeof(EditMode), rb.Name);
                    }
                }
            }
        }
        void SetImageSelection()
        {
            Layer ori = renderer.Layers[0];
            if (selection.Poly.Count < 4 || !ori.IsImage)
                return;
            imageSelection.Poly.Capacity = selection.Poly.Count;
            BitmapAccess im = ((ImageLayer)ori).Image;
            Point ul = ori.Geometry.ImagePoint(selection.Rect.X, selection.Rect.X); ;
            SizeF offset = new SizeF(ul);
            for (int i = 0; i < selection.Poly.Count; i++)
            {
                PointF imagePoint = ori.Geometry.InvertPoint(new PointF((int)selection.Poly[i].X, (int)selection.Poly[i].Y));
                if (imagePoint.X < 0)
                    imagePoint.X = 0;
                if (imagePoint.Y < 0)
                if (imagePoint.X >= im.Width)
                    imagePoint.X = im.Width - 1;
                if (imagePoint.Y >= im.Height)
                    imagePoint.Y = im.Height - 1;
                imagePoint -= offset;
                imageSelection.Poly.Add(new System.Windows.Point(imagePoint.X, imagePoint.Y));
            }
            imageSelection.SetRect(ul);
            Console.WriteLine("image=" + im.Size.ToString() + " rect=" + imageSelection.Rect.ToString());
        }
        void ApplyLinerColorGradient()
        {
            if (selection.Poly.Count < 4 || !renderer.Layers[0].IsImage)
                return;
            //((ImageLayer)activeLayer).ApplyLinerColorGradient();
        }
        void AddImageFromClipboard()
        {
            if (!Clipboard.ContainsData(DataFormats.Bitmap))
                return;
            Bitmap clipboard = (Bitmap)Clipboard.GetData(DataFormats.Bitmap);
            BitmapSource bs;
            IntPtr hBitmap = clipboard.GetHbitmap();
            try { bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); }
            finally { BitmapAccess.DeleteObject(hBitmap); }
            Size bms = new Size(clipboard.Width, clipboard.Height);
            //BitmapSource clipboard = System.Windows.Clipboard.GetImage();
            //Size bms = new Size(clipboard.PixelWidth, clipboard.PixelHeight);
            Geometry lg = new Geometry(panel.Size, bms, 1);
            panel.Focus();
            double dim = Math.Sqrt(bms.Width + bms.Height) / 3;
            int transparencyEdge = Math.Min((int)dim, 6);
            transparencyEdge = SelectedEdge(transparencyEdge);
            string newLayerName = renderer.Layers.Add(new ImageLayer("Clip" + transparencyEdge, new BitmapAccess(bs), lg, transparencyEdge));
            UpdateLayerList(newLayerName);
            SetMode(ImageMode.None);
        }
        void SetClipboardFromSelection()
        {
            if (imageSelection.Poly.Count>4 && renderer.Layers[0].IsImage)
            {
                BitmapAccess src = ((ImageLayer)renderer.Layers[0]).Image;
                BitmapAccess clip = src.SetSelectionBitmap(imageSelection.Rect, imageSelection.Poly.ToArray());
                Bitmap bc = new Bitmap(clip.Width, clip.Height, clip.Stride, PixelFormat.Format32bppPArgb, clip.DataPtr);
                Clipboard.SetData(DataFormats.Bitmap, bc);
            }
        }
        void CreateDrawing()
        {
            string n = renderer.Layers.Add(new DrawingLayer("Drawing", new Shape.Collection(), new Geometry(panel.Size, panel.Size, 2)));
            UpdateLayerList("Drawing");
        }
        void layerListView_ItemChecked(object s, ItemCheckedEventArgs e)
        {
            if (eventsDisabled)
                return;
            //Console.WriteLine("was " + ((Layer)e.Item.Tag).Enabled + " now " + e.Item.Checked);
            ((Layer)e.Item.Tag).Enabled = e.Item.Checked;
            if(e.Item.Checked)
                SetActiveLayer((Layer)e.Item.Tag);
            renderer.Redraw();
        }
        void layerListView_ItemSelectionChanged(object s, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected)
            {
                SetActiveLayer((Layer)e.Item.Tag);
                SetLayerListColors();
            }
        }
        void ColorTransformChanged(object o, EventArgs e)
        {
            if (suspendUpdate)
                return;
            if(!colorTransform.Set(brightnessControl.GetValues(), saturationControl.GetValues(), transperancyControl.GetValues()))
                return;
            BWButton.Checked = false;
            //Console.WriteLine("Color transform " + colorTransform.ToString());
            //Console.WriteLine(brightnessControl.ControlDebugString("Color"));
            //Console.WriteLine(saturationControl.ControlDebugString("Color"));
            activeLayer.TransformColor(colorTransform);
            renderer.Redraw();
        }
        void MoveLayerOnTop(object s, EventArgs e)
        {
            if (renderer.Layers.Count <= 1)
                return;
            renderer.Layers.MoveOnTop(activeLayer.Name);
            UpdateLayerList(activeLayer.Name);
        }
        void MoveLayerBack(object s, EventArgs e)
        {
            if (renderer.Layers.Count <= 1)
                return;
            renderer.Layers.MoveBack(activeLayer.Name);
            UpdateLayerList(activeLayer.Name);
        }
        void FlipLayerHorisontal(object s, EventArgs e)
        {
            activeLayer.FlipHorisontal();
            renderer.Redraw();
        }
        private void PresetTransformChanged(object sender, EventArgs e)
        {
            if (!BWButton.Checked)
                return;
            colorTransform.CopyFrom(ColorTransform.BWTransform);
            ResetColorControls();
            //Console.WriteLine("BW transform " + colorTransform.ToString());
            //Console.WriteLine(brightnessControl.ControlDebugString("BW"));
            //Console.WriteLine(saturationControl.ControlDebugString("BW"));
            activeLayer.TransformColor(colorTransform);
            renderer.Redraw();
        }
        void ResetColorControls()
        {
            saturationControl.SetValues(colorTransform.ColorValues);
            brightnessControl.SetValues(colorTransform.BrightnessValues);
            transperancyControl.SetValues(colorTransform.TransparencyValues);
        }
        private void patternButton_Click(object sender, EventArgs e)
        {
            SetMode(ImageMode.Color);
            colorSet = false;
            InitializeProcessingMask(); 
        }
        void SetMode(ImageMode m)
        {
            imageMode = m;
            Control[] ca = modeGroupBox.Controls.Find(imageMode.ToString(), false);
            if (ca.Length > 0)
                ((RadioButton)ca[0]).Checked = true;
        }
        void ImageEditForm_FormClosing(object o, FormClosingEventArgs e)
        {
            renderer.Dispose();
        }
        void KeyboardHandler(object s, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                AddImageFromClipboard();
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                SetClipboardFromSelection();
            }
        }
    }
}
