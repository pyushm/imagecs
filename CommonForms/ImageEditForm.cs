using System;
using System.IO;
using System.Drawing;
using System.Windows.Input;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Diagnostics;
using CustomControls;
using ShaderEffects;
using System.Windows.Media.Imaging;

namespace ImageProcessor
{
    public class ImageEditForm : Form, IPanelHolder
    {
        enum ImageScale                     // fixed image scales
        {
            Fit = 0,
            Half = 5,
            AsIs = 10,
            Double = 20,
            Quadruple = 40,
        }
        enum EdgeGap
        {
            Auto = -1,
            Gap0 = 0,
            Gap2 = 2,
            Gap5 = 5,
            Gap10 = 10,
            Gap15 = 15,
            Gap20 = 20,
            Gap30 = 30,
            Gap45 = 45,
            Gap70 = 70,
        }
        string[] imageModes = new string[] { ToolMode.None.ToString(), ToolMode.Distortion.ToString(), ToolMode.RectSelection.ToString(), ToolMode.FreeSelection.ToString(), ToolMode.Crop.ToString() };
        string[] drawingeModes = new string[] { ToolMode.None.ToString(), ToolMode.Distortion.ToString(), ToolMode.StrokeEdit.ToString() };
        private System.ComponentModel.Container components = null;
        private ValueControl saturationControl;
        private RangeControl brightnessControl;
        private RangeControl transperancyControl;
        private Panel panel = new System.Windows.Forms.Panel();
        private ListView layerListView;
        private ColumnHeader typeColumn;
        private ColumnHeader nameColumn;
        private Button saveButton;
        private Button saveSameLocationButton;
        private ComboBox scaleBox;
        private GroupBox modeGroupBox;
        private Label label5;
        private GroupBox layerGroupBox;
        DrawingPanel canvas;
        float dpiScaleX = 1;
        float dpiScaleY = 1;
        int viewingAreaOffset;				// viewing area offset from left of client rectangle
        ImageFileInfo imageInfo;            // image file info
        // image processing members
        ToolMode toolMode;                  // mouse mode
        int selectedIndex;                  // index of selected layer in layerListView
        bool suspendUpdate = false;         // suspends image update while reseting
        double replaceSpan = 3;             // time span to replace old image in seconds
        private RadioButton BWButton;
        bool userInput = false;
        private Label label7;
        private ComboBox edgeGapBox;
        private ValueControl strengthControl;
        private ValueControl levelControl;
        private ValueControl resolutionControl;

        double SelectedStrength { get { return strengthControl.Values[0]; } }   // [0:1]
        double SelectedLevel { get { return levelControl.Values[0]; } }         // [-1:1]
        double SelectedSize { get { return resolutionControl.Values[0]; } }     // [0:4]
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
            this.saveSameLocationButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.layerListView = new System.Windows.Forms.ListView();
            this.typeColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.layerGroupBox = new System.Windows.Forms.GroupBox();
            this.levelControl = new CustomControls.ValueControl();
            this.resolutionControl = new CustomControls.ValueControl();
            this.strengthControl = new CustomControls.ValueControl();
            this.BWButton = new System.Windows.Forms.RadioButton();
            this.transperancyControl = new CustomControls.RangeControl();
            this.saturationControl = new CustomControls.ValueControl();
            this.brightnessControl = new CustomControls.RangeControl();
            this.label7 = new System.Windows.Forms.Label();
            this.edgeGapBox = new System.Windows.Forms.ComboBox();
            this.layerGroupBox.SuspendLayout();
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
            this.modeGroupBox.Location = new System.Drawing.Point(1, 641);
            this.modeGroupBox.Name = "modeGroupBox";
            this.modeGroupBox.Size = new System.Drawing.Size(136, 184);
            this.modeGroupBox.TabIndex = 31;
            this.modeGroupBox.TabStop = false;
            this.modeGroupBox.Text = "Mouse mode";
            // 
            // saveSameLocationButton
            // 
            this.saveSameLocationButton.Location = new System.Drawing.Point(50, 2);
            this.saveSameLocationButton.Name = "saveSameLocationButton";
            this.saveSameLocationButton.Size = new System.Drawing.Size(87, 20);
            this.saveSameLocationButton.TabIndex = 33;
            this.saveSameLocationButton.Text = "Save to same";
            this.saveSameLocationButton.Click += new System.EventHandler(this.saveSameLocation_Click);
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(1, 2);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(43, 20);
            this.saveButton.TabIndex = 32;
            this.saveButton.Text = "Save";
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // layerListView
            // 
            this.layerListView.CheckBoxes = true;
            this.layerListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.typeColumn,
            this.nameColumn});
            this.layerListView.FullRowSelect = true;
            this.layerListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.layerListView.LabelEdit = true;
            this.layerListView.Location = new System.Drawing.Point(1, 49);
            this.layerListView.MultiSelect = false;
            this.layerListView.Name = "layerListView";
            this.layerListView.Size = new System.Drawing.Size(136, 162);
            this.layerListView.TabIndex = 39;
            this.layerListView.UseCompatibleStateImageBehavior = false;
            this.layerListView.View = System.Windows.Forms.View.Details;
            this.layerListView.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.layerListView_AfterLabelEdit);
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
            this.layerGroupBox.Controls.Add(this.levelControl);
            this.layerGroupBox.Controls.Add(this.resolutionControl);
            this.layerGroupBox.Controls.Add(this.strengthControl);
            this.layerGroupBox.Controls.Add(this.BWButton);
            this.layerGroupBox.Controls.Add(this.transperancyControl);
            this.layerGroupBox.Controls.Add(this.saturationControl);
            this.layerGroupBox.Controls.Add(this.brightnessControl);
            this.layerGroupBox.Location = new System.Drawing.Point(1, 240);
            this.layerGroupBox.Name = "layerGroupBox";
            this.layerGroupBox.Size = new System.Drawing.Size(136, 395);
            this.layerGroupBox.TabIndex = 40;
            this.layerGroupBox.TabStop = false;
            this.layerGroupBox.Text = "Adjust layer";
            // 
            // levelControl
            // 
            this.levelControl.Location = new System.Drawing.Point(3, 322);
            this.levelControl.Name = "levelControl";
            this.levelControl.Offset = -1F;
            this.levelControl.Range = 2F;
            this.levelControl.Size = new System.Drawing.Size(120, 32);
            this.levelControl.TabIndex = 70;
            this.levelControl.Title = "Level";
            this.levelControl.ValueLocations = new System.Drawing.Point[] {
        new System.Drawing.Point(43, 0)};
            this.levelControl.ValueChanged += new System.EventHandler(this.TransformChanged);
            // 
            // resolutionControl
            // 
            this.resolutionControl.Location = new System.Drawing.Point(3, 250);
            this.resolutionControl.Name = "resolutionControl";
            this.resolutionControl.Offset = 0F;
            this.resolutionControl.Range = 4F;
            this.resolutionControl.Size = new System.Drawing.Size(120, 32);
            this.resolutionControl.TabIndex = 69;
            this.resolutionControl.Title = "Resolution";
            this.resolutionControl.ValueLocations = new System.Drawing.Point[] {
        new System.Drawing.Point(43, 0)};
            this.resolutionControl.ValueChanged += new System.EventHandler(this.TransformChanged);
            // 
            // strengthControl
            // 
            this.strengthControl.Location = new System.Drawing.Point(3, 286);
            this.strengthControl.Name = "strengthControl";
            this.strengthControl.Offset = 0F;
            this.strengthControl.Range = 1F;
            this.strengthControl.Size = new System.Drawing.Size(120, 32);
            this.strengthControl.TabIndex = 68;
            this.strengthControl.Title = "Strength";
            this.strengthControl.ValueLocations = new System.Drawing.Point[] {
        new System.Drawing.Point(43, 0)};
            this.strengthControl.ValueChanged += new System.EventHandler(this.TransformChanged);
            // 
            // BWButton
            // 
            this.BWButton.AutoSize = true;
            this.BWButton.Location = new System.Drawing.Point(75, 106);
            this.BWButton.Name = "BWButton";
            this.BWButton.Size = new System.Drawing.Size(48, 17);
            this.BWButton.TabIndex = 65;
            this.BWButton.TabStop = true;
            this.BWButton.Text = "B/W";
            this.BWButton.UseVisualStyleBackColor = true;
            this.BWButton.CheckedChanged += new System.EventHandler(this.SetBWTransform);
            // 
            // transperancyControl
            // 
            this.transperancyControl.Location = new System.Drawing.Point(3, 180);
            this.transperancyControl.Name = "transperancyControl";
            this.transperancyControl.Offset = 0F;
            this.transperancyControl.Range = 1F;
            this.transperancyControl.Size = new System.Drawing.Size(120, 68);
            this.transperancyControl.TabIndex = 29;
            this.transperancyControl.Title = "Transperancy";
            this.transperancyControl.ValueLocations = new System.Drawing.Point[] {
        new System.Drawing.Point(43, 0),
        new System.Drawing.Point(43, 100)};
            this.transperancyControl.ValueChanged += new System.EventHandler(this.TransformChanged);
            // 
            // saturationControl
            // 
            this.saturationControl.Location = new System.Drawing.Point(3, 108);
            this.saturationControl.Name = "saturationControl";
            this.saturationControl.Offset = -1F;
            this.saturationControl.Range = 2F;
            this.saturationControl.Size = new System.Drawing.Size(120, 68);
            this.saturationControl.TabIndex = 21;
            this.saturationControl.Title = "Saturation";
            this.saturationControl.ValueLocations = new System.Drawing.Point[] {
        new System.Drawing.Point(43, 0),
        new System.Drawing.Point(43, 45),
        new System.Drawing.Point(43, 100)};
            this.saturationControl.ValueChanged += new System.EventHandler(this.TransformChanged);
            // 
            // brightnessControl
            // 
            this.brightnessControl.Location = new System.Drawing.Point(3, 17);
            this.brightnessControl.Name = "brightnessControl";
            this.brightnessControl.Offset = -1F;
            this.brightnessControl.Range = 2F;
            this.brightnessControl.Size = new System.Drawing.Size(120, 85);
            this.brightnessControl.TabIndex = 29;
            this.brightnessControl.Title = "Brightness";
            this.brightnessControl.ValueLocations = new System.Drawing.Point[] {
        new System.Drawing.Point(43, 0),
        new System.Drawing.Point(43, 45),
        new System.Drawing.Point(43, 100)};
            this.brightnessControl.ValueChanged += new System.EventHandler(this.TransformChanged);
            // 
            // label7
            // 
            this.label7.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label7.Location = new System.Drawing.Point(5, 220);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(73, 17);
            this.label7.TabIndex = 44;
            this.label7.Text = "Edge";
            this.label7.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // edgeGapBox
            // 
            this.edgeGapBox.Location = new System.Drawing.Point(80, 216);
            this.edgeGapBox.Name = "edgeGapBox";
            this.edgeGapBox.Size = new System.Drawing.Size(57, 21);
            this.edgeGapBox.TabIndex = 43;
            this.edgeGapBox.SelectedIndexChanged += new System.EventHandler(this.TransformChanged);
            // 
            // ImageEditForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1110, 1000);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.edgeGapBox);
            this.Controls.Add(this.layerGroupBox);
            this.Controls.Add(this.layerListView);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.modeGroupBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.scaleBox);
            this.Controls.Add(this.saveSameLocationButton);
            this.KeyPreview = true;
            this.MinimumSize = new System.Drawing.Size(628, 648);
            this.Name = "ImageEditForm";
            this.Text = "Image Editing Form";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ImageEditForm_FormClosing);
            this.Resize += new System.EventHandler(this.ImageEditForm_Resize);
            this.layerGroupBox.ResumeLayout(false);
            this.layerGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        #region interface ILayerTool
        public ToolMode ToolMode { get { return toolMode; } }
        public void CropRectangleUpdated() { }
        public void GeometryTransformUpdated() { /*ShowGeometryTransformParameters();*/ }
        public void FocusControl() { scaleBox.Focus(); }
        public void SetViewPosition(double x, double y) { canvas.ActiveLayer.SetEffectParameters(SelectedStrength-2, x, y); }
        #endregion
        public ImageEditForm()              
        {
            suspendUpdate = true;
            canvas = new DrawingPanel(this);
            ElementHost host = new ElementHost();
            host.Dock = DockStyle.Fill;
            host.Name = "host";
            host.Child = canvas;
            panel.Controls.Add(host);
            InitializeComponent();
            ResetColorControls();
            viewingAreaOffset = panel.Location.X;
            panel.Size = new System.Drawing.Size(ClientSize.Width - viewingAreaOffset, ClientSize.Height);
            selectedIndex = -1;       // nothing selected
            KeyDown += CaptureKeyboard;
            saveSameLocationButton.Enabled = false;
            scaleBox.Items.AddRange(Enum.GetNames(typeof(ImageScale)));
            edgeGapBox.Items.AddRange(Enum.GetNames(typeof(EdgeGap)));
            scaleBox.SelectedIndex = 0;
            edgeGapBox.SelectedIndex = 0;
            ContextMenu selectMenu = new ContextMenu();
            selectMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                new MenuItem("Move on top", delegate (object s, EventArgs e) { if (canvas.MoveLayerOnTop(selectedIndex)) UpdateLayerList(selectedIndex); } ),
                new MenuItem("Move back", delegate (object s, EventArgs e) { if (canvas.MoveLayerBack(selectedIndex)) UpdateLayerList(selectedIndex); } ),
                new MenuItem("Flip", delegate (object s, EventArgs e) { if (canvas.IsActiveLayerVisible) canvas.ActiveLayer.Flip(); }),
                new MenuItem("Edges", delegate(object s, EventArgs e) { AddEffectLayer("Edge", new EdgeEffect()); }),
                new MenuItem("Drawing", delegate(object s, EventArgs e) { UpdateLayerList(canvas.AddStrokeLayer("Drawing")); } ),
                new MenuItem("Sharpness", delegate (object s, EventArgs e) { AddEffectLayer("Sharpness", new GradientContrastEffect()); }),
                new MenuItem("Remove odd", delegate (object s, EventArgs e) { AddNoOddsLayer(); }),
                new MenuItem("To background", delegate (object s, EventArgs e) { CopyToBackground(1); }),
                new MenuItem("To back 1/2", delegate (object s, EventArgs e) { CopyToBackground(2); }),
                new MenuItem("Delete", new EventHandler(DeleteLayer)) });
            layerListView.ContextMenu = selectMenu;
            suspendUpdate = false;
            Load += ImageViewForm_Load;
        }
        private void ImageViewForm_Load(object sender, EventArgs e)
        {

            Graphics g = this.CreateGraphics();
            if (g != null)
            {
                dpiScaleX = g.DpiX / 96;
                dpiScaleY = g.DpiY / 96;
                g.Dispose();
                RescaleCanvas(true);
            }
        }
        void CreateToolButtons(VisualLayer vl)
        {
            foreach (Control c in modeGroupBox.Controls)
                c.Dispose();
            modeGroupBox.Controls.Clear();
            if (vl.IsImage)
                CreateControls(imageModes, null);
            else
                CreateControls(drawingeModes, new string[] { "Thickness" });
        }
        RadioButton CreateControls(string[] bns, string[] inp)
        {
            RadioButton none = null;
            int ystep = 20;
            int y = 0;
            int xoffset = 8;
            int cc = 0;
            if (bns != null)
            {
                for (int i = 0; i < bns.Length; i++)
                {
                    RadioButton rb = CreateRadioButton(bns[i], xoffset, y += ystep, 100, ystep - 2);
                    if (i == 0)
                        rb.Checked = true;
                    modeGroupBox.Controls.Add(rb);
                }
                cc += bns.Length;
            }
            if (inp != null)
            {
                for (int i = 0; i < inp.Length; i++)
                    modeGroupBox.Controls.AddRange(CreateInputBox(inp[i], xoffset, y += ystep, 70, 100, ystep - 2));
                cc += inp.Length;
            }
            modeGroupBox.Height = ystep * cc + 24;
            return none;
        }
        Control[] CreateInputBox(string title, int x, int y, int xo, int w, int h)
        {
            Control[] ca = new Control[2];
            ca[0] = new Label();
            ca[0].Name = title + "Label";
            ca[0].Location = new System.Drawing.Point(x, y);
            ca[0].Size = new System.Drawing.Size(xo - x - 2, h);
            ca[0].Text = title.Replace('_', ' ');
            ca[1] = new NumericUpDown();
            ca[1].Name = title + "Input";
            ca[1].Location = new System.Drawing.Point(xo, y);
            ca[1].Size = new System.Drawing.Size(w - xo - 2, h);
            ((NumericUpDown)ca[1]).Value = 1;
            ((NumericUpDown)ca[1]).ValueChanged += new System.EventHandler(inputBox_ValueChanged);
            return ca;
        }
        RadioButton CreateRadioButton(string title, int x, int y, int w, int h)
        {
            RadioButton modeButton = new RadioButton();
            modeButton.Name = title;
            modeButton.Location = new System.Drawing.Point(x, y);
            modeButton.Size = new System.Drawing.Size(w, h);
            modeButton.Text = title.Replace('_', ' ');
            modeButton.CheckedChanged += new System.EventHandler(toolButton_CheckedChanged);
            return modeButton;
        }
        public void CaptureKeyboard(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            //Debug.WriteLine("Edit Modifier=" + Keyboard.Modifiers.ToString() + " key=" + e.KeyCode.ToString() + " " + Keyboard.IsKeyDown(Key.LeftCtrl));
            if (e.KeyCode == Keys.V && Keyboard.IsKeyDown(Key.LeftCtrl))
                AddClipboardLayer();
            else if (e.KeyCode == Keys.C && Keyboard.IsKeyDown(Key.LeftCtrl))
                canvas.SetClipboardFromSelection();
            else if (e.KeyCode == Keys.Delete)
                canvas.DeleteSelection();
        }
        int SelectedEdge(int defaultValue)
        {
            int gap = edgeGapBox.SelectedItem != null ? (int)Enum.Parse(typeof(EdgeGap), (string)edgeGapBox.SelectedItem) : defaultValue;
            return gap < 0 ? defaultValue : gap;
        }
        double DisplayScale
        {
            get { return scaleBox.SelectedItem == null ? 0 : (int)Enum.Parse(typeof(ImageScale), (string)scaleBox.SelectedItem)/10.0; }
        }
        public void ShowNewImage(ImageFileInfo info)
        {
            scaleBox.SelectedItem = ImageScale.Fit.ToString();
            imageInfo = info;
            string warn = canvas.LoadFile(imageInfo, replaceSpan);
            if (warn.Length > 0)
                System.Windows.MessageBox.Show(warn, "loading " + imageInfo.FSPath + " failed"); UpdateLayerList(0);
            RescaleCanvas(true);
            Text = imageInfo.RealName + "  " + canvas.FrameSizeString;
            userInput = false;
            BWButton.Checked = false;
            Show();
            BringToFront();
            userInput = true;
        }
        public void DrawNewImageContour(ImageFileInfo info)
        {
            ShowNewImage(info);
            AddEffectLayer("Edge", new EdgeEffect());
        }
        bool SetActiveLayer(int ind)	    // resets image drawing controls to default values
        {
            if (selectedIndex == ind)
                return false;
            selectedIndex = ind;
            SetLayerListColors();
            VisualLayer oldActiveLayer = canvas.ActiveLayer;
            if (!canvas.SetActiveLayer(ind))
                return false;
            if (oldActiveLayer == null || oldActiveLayer.Type != canvas.ActiveLayer.Type)
                CreateToolButtons(canvas.ActiveLayer);
            ResetColorControls();
            suspendUpdate = true;
            layerGroupBox.Text = canvas.ActiveLayer.Name;
            //patternBox.BackColor = canvas.ActiveLayer.ColorTransform.Pattern == ColorTransform.ColorNull ?
            //    Color.FromKnownColor(KnownColor.Control) : SetFromMediaColor(canvas.ActiveLayer.ColorTransform.Pattern);
            suspendUpdate = false;
            return true;
        }
        VisualLayer UpdateLayerList(int index)
        {
            userInput = false;
            layerListView.Items.Clear();
            int newIndex = -1;
            VisualLayer l = null;
            for ( int i=0; i<canvas.LayerCount; i++)
            {
                l = canvas.GetLayer(i);
                if (l==null || l.Deleted)
                    continue;
                ListViewItem lvi = new ListViewItem(new string[] { "", l.Name });
                lvi.Selected = index == i;
                if(index == i)
                    newIndex = i;
                lvi.Checked = l.Visibility == Visibility.Visible;
                lvi.Tag = l;
                layerListView.Items.Add(lvi);
            }
            SetActiveLayer(newIndex);
            userInput = true;
            return l;
        }
        void SetLayerListItemColor(ListViewItem lvi)
        {
            //Debug.WriteLine(lvi.Index.ToString()+" checked=" + lvi.Checked+" active="+ (lvi.Index == activeIndex));
            if (lvi.Index == selectedIndex)
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
        void SetLayerListColors()
        {
            foreach (ListViewItem lvi in layerListView.Items)
                SetLayerListItemColor(lvi);
        }
        void AddEffectLayer(string name, ParametricEffect effect)
        {
            BitmapLayer bl = canvas.ActiveLayer as BitmapLayer;
            if (bl == null)
                return;
            BitmapAccess clip = canvas.GetSelected(0);
            int transparencyEdge = imageInfo.IsExact ? 0 : Math.Min((int)Math.Sqrt(clip.Width + clip.Height) / 3, 6);
            transparencyEdge = SelectedEdge(transparencyEdge);
            BitmapDerivativeLayer vl = new BitmapDerivativeLayer(name, clip, effect, transparencyEdge);
            vl?.SetEffectParameters(SelectedStrength, SelectedLevel, SelectedSize);
            UpdateLayerList(canvas.AddVisualLayer(vl, bl));
            //Debug.WriteLine(vl.Image.ToColorsString());
        }
        void AddNoOddsLayer()
        {
            BitmapLayer bl = canvas.ActiveLayer as BitmapLayer;
            if (bl == null)
                return;
            int resolution = 2; // 2 is cleaner then 1
            string name = "-Odds" + resolution;
            int level = 5;
            name += "." + level;
            BitmapAccess clip = canvas.GetSelected(resolution);
            bool selection = clip.Origin == BitmapOrigin.Selection;
            int transparencyEdge = selection ? SelectedEdge(Math.Min((int)Math.Sqrt(clip.Width + clip.Height) / 3, 6)) : 0;
            Vector shift = selection ? (Vector)canvas.SavedPosition : new Vector();
            BitmapAccess ba = clip.ApplyConversion(ConversionType.MedianFilter, resolution, level);
            VisualLayer vl = new BitmapLayer(name, ba, transparencyEdge);
            UpdateLayerList(canvas.AddVisualLayer(vl, bl, shift));
        }
        void CopyToBackground(int scale)
        {
            BitmapLayer srcLayer = canvas.ActiveLayer as BitmapLayer;
            BitmapLayer bl = canvas.BackgroundLayer as BitmapLayer;
            if (bl != null && srcLayer != null)
            {
                System.Windows.Point c = srcLayer.MatrixControl.Center;
                c = canvas.FromCanvas.Transform(c);
                BitmapAccess src = srcLayer.Image;
                bl.SetImage(bl.Image.ToPArgbImage(), 0);
                bl.Image.Overwrite(src, (int)c.X - src.Width / 2, (int)c.Y - src.Height / 2, scale);
                UpdateLayerList(canvas.RemoveActiveLayer());
            }
        }
        void AddClipboardLayer()
        {
            if (!System.Windows.Clipboard.ContainsData(System.Windows.DataFormats.Bitmap))
                return;
            try
            {
                BitmapAccess clip = new BitmapAccess(ClipboardBitmapAccess.GetImage(), BitmapOrigin.Clipboard);
                panel.Focus();
                int transparencyEdge = imageInfo.IsExact ? 0 : Math.Min((int)Math.Sqrt(clip.Width + clip.Height) / 3, 6);
                transparencyEdge = SelectedEdge(transparencyEdge);
                //Debug.WriteLine("AddClipboardLayer");
                //Debug.WriteLine(clip.ToString());
                VisualLayer vl = new BitmapLayer("Clip" + transparencyEdge, clip, transparencyEdge);
                //VisualLayer vl = new Bitmap3DLayer("Clip" + transparencyEdge, clip, transparencyEdge);
                UpdateLayerList(canvas.AddVisualLayer(vl, canvas.BackgroundLayer.MatrixControl.RenderScale));

                //canvas.TEST(vl);
                //
                //UIElement uie = vl.GetVisual(0) as UIElement;
                //canvas.Children.Add(uie);

                //these 3 lines work
                //var vp3d = Bitmap3DLayer.CreateProjection(clip.Source, 0, 0.2);
                //vp3d.Width = vp3d.Height = 300;
                //canvas.Children.Add(vp3d);
                SetMode(ToolMode.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
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
        Color SetFromMediaColor(System.Windows.Media.Color nc)
        {
            return Color.FromArgb(nc.A, nc.R, nc.G, nc.B);
        }
        System.Windows.Media.Color SetMediaColor(Color nc)
        {
            return System.Windows.Media.Color.FromArgb(nc.A, nc.R, nc.G, nc.B);
        }
        void RescaleCanvas(bool initial) { canvas.Resize(initial, DisplayScale, panel.Width / dpiScaleX, panel.Height / dpiScaleY); }
        void saveSameLocation_Click(object s, EventArgs e)
        {
        }
        void saveButton_Click(object s, EventArgs e)
        {
            if (DisplayScale != 0)
            {   // to ensure that all background image saved
                canvas.Resize(false, 1, panel.Width, panel.Height);
            }
            SaveFileDialog saveAsDialog = new SaveFileDialog();
            saveAsDialog.FileName = imageInfo.RealName;
            saveAsDialog.Filter = "regular|*.jpe|Exact|*.exa|MultiLayer|*.drw"; // safe format relies on this order 
            saveAsDialog.FilterIndex = imageInfo.IsMultiLayer ? 3 : imageInfo.IsExact ? 2 : 1;
            saveAsDialog.RestoreDirectory = true;
            saveAsDialog.InitialDirectory = Path.GetDirectoryName(imageInfo.FSPath);
            if (saveAsDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ImageFileInfo info = new ImageFileInfo(new FileInfo(ImageFileName.FSMangle(saveAsDialog.FileName)));
                    BitmapEncoder bitmapEncoder = info.IsExact ? (BitmapEncoder)new PngBitmapEncoder() : new JpegBitmapEncoder();
                    if (bitmapEncoder as JpegBitmapEncoder != null)
                        ((JpegBitmapEncoder)bitmapEncoder).QualityLevel = 87;
                    string ret = info.IsImage ? canvas.SaveSingleImage(info.FSPath, 0, bitmapEncoder, info.IsEncrypted) : canvas.SaveLayers(info.FSPath, bitmapEncoder);
                    if (ret.Length == 0)
                        ShowNewImage(info);
                    else
                        System.Windows.Forms.MessageBox.Show(ret, "Saving " + info.FSPath + " failed");
                    SetMode(ToolMode.None);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
            }
        }
        void toolButton_CheckedChanged(object s, EventArgs e)
        {
            RadioButton rb = s as RadioButton;
            if (rb == null)
                return;
            if (rb.Checked && canvas.ActiveLayer != null)
            {
                toolMode = (ToolMode)Enum.Parse(typeof(ToolMode), rb.Name);
                canvas.InitializeToolDrawing();
                //else if (activeLayer.IsDrawing)
                //{
                //    try
                //    {
                //        shapeType = (ShapeType)Enum.Parse(typeof(ShapeType), rb.Name);
                //        activeShape = ((DrawingLayer)activeLayer).Drawing.AddShape(GetThickness(), shapeType);
                //        shapeEditMode = EditMode.Click;
                //        undoButton.Enabled = true;
                //    }
                //    catch
                //    {
                //        shapeEditMode = (EditMode)Enum.Parse(typeof(EditMode), rb.Name);
                //    }
                //}
            }
        }
        void layerListView_ItemChecked(object s, ItemCheckedEventArgs e)
        {
            if (!userInput)
                return;
            ((VisualLayer)e.Item.Tag).Visibility = e.Item.Checked ? Visibility.Visible : Visibility.Hidden;
            if (e.Item.Checked)
                SetActiveLayer(e.Item.Index);
            else if (e.Item.Index == selectedIndex)
                UpdateLayerList(canvas.ClosestVisibleIndex(selectedIndex));
        }
        void layerListView_ItemSelectionChanged(object s, ListViewItemSelectionChangedEventArgs e)
        {
            if (!userInput)
                return;
            if (e.Item.Selected)
            {
                //Debug.WriteLine("Selected=" + e.ItemIndex+ " isChecked: " + e.Item.Checked+ " oldActive="+ activeIndex);
                userInput = false;
                SetActiveLayer(e.ItemIndex);
                userInput = true;
            }
            SetLayerListColors();
        }
        void TransformChanged(object o, EventArgs e)
        {
            if (suspendUpdate)
                return;
            BitmapLayer aLayer = canvas.ActiveLayer as BitmapLayer;
            if (aLayer != null)
            {
                aLayer.ColorTransform.Set(brightnessControl.Values, saturationControl.Values, transperancyControl.Values);
                BWButton.Checked = false;
                aLayer.SetEffectParameters(SelectedStrength, SelectedLevel, SelectedSize);
            }
        }
        private void SetBWTransform(object sender, EventArgs e)
        {
            if (!BWButton.Checked)
                return;
            BitmapLayer aLayer = canvas.ActiveLayer as BitmapLayer;
            if (aLayer != null)
            {
                aLayer.ColorTransform.CopyFrom(ColorTransform.BWTransform);
                if (aLayer.Type == VisualLayerType.Bitmap)
                    aLayer.SetEffectParameters(SelectedStrength, SelectedLevel, SelectedSize);
                ResetColorControls();
            }
        }
        void ResetColorControls()
        {
            if (canvas.ActiveLayer == null)
                return;
            saturationControl.SetValues(canvas.ActiveLayer.ColorTransform.ColorValues);
            brightnessControl.SetValues(canvas.ActiveLayer.ColorTransform.BrightnessValues);
            transperancyControl.SetValues(canvas.ActiveLayer.ColorTransform.TransparencyValues);
        }
        void SetMode(ToolMode m)
        {
            toolMode = m;
            Control[] ca = modeGroupBox.Controls.Find(toolMode.ToString(), false);
            if (ca.Length > 0)
                ((RadioButton)ca[0]).Checked = true;
        }
        void ImageEditForm_FormClosing(object o, FormClosingEventArgs e) { }
        private void layerListView_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (e.Label != null)
            {
                canvas.RenameActiveLayer(e.Label);
                UpdateLayerList(selectedIndex);
            }
        }
        void ImageEditForm_Resize(object s, EventArgs e)
        {
            panel.Size = new System.Drawing.Size(ClientSize.Width - viewingAreaOffset, ClientSize.Height);
            RescaleCanvas(false);
        }
        void DeleteLayer(object s, EventArgs e)
        {
            if (canvas.ActiveLayer == null)
                return;
            MessageBoxResult res = System.Windows.MessageBox.Show("Are you sure you want to delete layer '" + canvas.ActiveLayer.Name + "'?",
                "Delete headers warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes)
                UpdateLayerList(canvas.RemoveActiveLayer());
        }
        void scaleBox_SelectedIndexChanged(object s, EventArgs e) { if (userInput) RescaleCanvas(true); }
        #region not ready
        void inputBox_ValueChanged(object s, EventArgs e)
        {
            //if (activeLayer.IsDrawing)
            //{
            //    activeShape=((DrawingLayer)activeLayer).Drawing.AddShape(GetThickness(), shapeType);
            //    undoButton.Enabled = true;
            //}
        }
        void undoButton_Click(object s, EventArgs e)
        {
            //undoButton.Enabled=activeLayer.Undo();
            //panel.Invalidate();
        }
        #endregion
    }
}
