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
            Gap0 = 0,
            Gap1 = 1,
            Gap2 = 2,
            Gap3 = 3,
            Gap5 = 5,
            Gap8 = 8,
            Gap13 = 13,
            Gap20 = 20,
            Gap30 = 30,
            Gap45 = 45,
            Gap66 = 66,
            Gap99 = 99,
        }
        string[] imageModes = new string[] { ToolMode.Basic.ToString(), ToolMode.Distortion3D.ToString(), ToolMode.Morph.ToString() };
        string[] drawingModes = new string[] { ToolMode.Basic.ToString(), ToolMode.Distortion3D.ToString(), ToolMode.ContourEdit.ToString() };
        private System.ComponentModel.Container components = null;
        private ValueControl saturationControl;
        private RangeControl brightnessControl;
        private RangeControl transperancyControl;
        private ListView layerListView;
        private ColumnHeader typeColumn;
        private ColumnHeader nameColumn;
        private Button saveButton;
        private ComboBox scaleBox;
        private GroupBox modeGroupBox;
        private Label label5;
        private GroupBox layerGroupBox;
        ComboBox sensitivityBox;
        DrawingPanel canvas;
        float dpiScaleX = 1;
        float dpiScaleY = 1;
        int viewingAreaOffset;				// viewing area offset from left of client rectangle
        ImageFileInfo imageInfo;            // image file info
        string savePath = null;
        // image processing members
        public ToolMode ToolMode { get; set; } // mouse mode
        int layerIndex;                     // index of selected layer in layerListView
        bool suspendUpdate = false;         // suspends image update while reseting
        double replaceSpan = 3;             // time span to replace old image in seconds
        bool userInput = false;
        private Label label7;
        private ComboBox edgeGapBox;
        private ValueControl strengthControl;
        private ValueControl levelControl;
        private Label BWbtn;
        private Panel panel;
        private Label mouseSensitivityLabel;
        private TextBox warningBox;
        private ValueControl resolutionControl;
        Button saveSameLocationButton;

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
            this.saveButton = new System.Windows.Forms.Button();
            this.layerListView = new System.Windows.Forms.ListView();
            this.typeColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.layerGroupBox = new System.Windows.Forms.GroupBox();
            this.mouseSensitivityLabel = new System.Windows.Forms.Label();
            this.BWbtn = new System.Windows.Forms.Label();
            this.levelControl = new CustomControls.ValueControl();
            this.resolutionControl = new CustomControls.ValueControl();
            this.strengthControl = new CustomControls.ValueControl();
            this.transperancyControl = new CustomControls.RangeControl();
            this.saturationControl = new CustomControls.ValueControl();
            this.brightnessControl = new CustomControls.RangeControl();
            this.label7 = new System.Windows.Forms.Label();
            this.edgeGapBox = new System.Windows.Forms.ComboBox();
            this.panel = new System.Windows.Forms.Panel();
            this.warningBox = new System.Windows.Forms.TextBox();
            saveSameLocationButton = new System.Windows.Forms.Button();
            sensitivityBox = new System.Windows.Forms.ComboBox();
            this.layerGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // saveSameLocationButton
            // 
            saveSameLocationButton.Location = new System.Drawing.Point(75, 3);
            saveSameLocationButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            saveSameLocationButton.Name = "saveSameLocationButton";
            saveSameLocationButton.Size = new System.Drawing.Size(130, 31);
            saveSameLocationButton.TabIndex = 33;
            saveSameLocationButton.Text = "Save to same";
            saveSameLocationButton.Click += new System.EventHandler(this.saveSameLocation_Click);
            // 
            // label5
            // 
            this.label5.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label5.Location = new System.Drawing.Point(8, 45);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(58, 26);
            this.label5.TabIndex = 38;
            this.label5.Text = "Scale";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // scaleBox
            // 
            this.scaleBox.Location = new System.Drawing.Point(69, 38);
            this.scaleBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.scaleBox.Name = "scaleBox";
            this.scaleBox.Size = new System.Drawing.Size(134, 28);
            this.scaleBox.TabIndex = 37;
            this.scaleBox.SelectedIndexChanged += new System.EventHandler(this.scaleBox_SelectedIndexChanged);
            // 
            // modeGroupBox
            // 
            this.modeGroupBox.Location = new System.Drawing.Point(3, 986);
            this.modeGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.modeGroupBox.Name = "modeGroupBox";
            this.modeGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.modeGroupBox.Size = new System.Drawing.Size(207, 283);
            this.modeGroupBox.TabIndex = 31;
            this.modeGroupBox.TabStop = false;
            this.modeGroupBox.Text = "Mouse mode";
            // 
            // saveButton
            // 
            this.saveButton.BackColor = System.Drawing.SystemColors.Control;
            this.saveButton.Location = new System.Drawing.Point(2, 3);
            this.saveButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(64, 31);
            this.saveButton.TabIndex = 32;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = false;
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
            this.layerListView.HideSelection = false;
            this.layerListView.Location = new System.Drawing.Point(2, 75);
            this.layerListView.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.layerListView.MultiSelect = false;
            this.layerListView.Name = "layerListView";
            this.layerListView.Size = new System.Drawing.Size(202, 247);
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
            this.layerGroupBox.Controls.Add(sensitivityBox);
            this.layerGroupBox.Controls.Add(this.mouseSensitivityLabel);
            this.layerGroupBox.Controls.Add(this.BWbtn);
            this.layerGroupBox.Controls.Add(this.levelControl);
            this.layerGroupBox.Controls.Add(this.resolutionControl);
            this.layerGroupBox.Controls.Add(this.strengthControl);
            this.layerGroupBox.Controls.Add(this.transperancyControl);
            this.layerGroupBox.Controls.Add(this.saturationControl);
            this.layerGroupBox.Controls.Add(this.brightnessControl);
            this.layerGroupBox.Location = new System.Drawing.Point(3, 369);
            this.layerGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.layerGroupBox.Name = "layerGroupBox";
            this.layerGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.layerGroupBox.Size = new System.Drawing.Size(207, 608);
            this.layerGroupBox.TabIndex = 40;
            this.layerGroupBox.TabStop = false;
            this.layerGroupBox.Text = "Adjust layer";
            // 
            // sensitivityBox
            // 
            sensitivityBox.IntegralHeight = false;
            sensitivityBox.Location = new System.Drawing.Point(142, 25);
            sensitivityBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            sensitivityBox.Name = "sensitivityBox";
            sensitivityBox.Size = new System.Drawing.Size(60, 28);
            sensitivityBox.TabIndex = 46;
            // 
            // mouseSensitivityLabel
            // 
            this.mouseSensitivityLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.mouseSensitivityLabel.Location = new System.Drawing.Point(3, 25);
            this.mouseSensitivityLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.mouseSensitivityLabel.Name = "mouseSensitivityLabel";
            this.mouseSensitivityLabel.Size = new System.Drawing.Size(138, 28);
            this.mouseSensitivityLabel.TabIndex = 46;
            this.mouseSensitivityLabel.Text = "Mouse sensitivity";
            this.mouseSensitivityLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // BWbtn
            // 
            this.BWbtn.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.BWbtn.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.BWbtn.ForeColor = System.Drawing.SystemColors.Window;
            this.BWbtn.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.BWbtn.Location = new System.Drawing.Point(154, 218);
            this.BWbtn.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.BWbtn.Name = "BWbtn";
            this.BWbtn.Size = new System.Drawing.Size(48, 23);
            this.BWbtn.TabIndex = 45;
            this.BWbtn.Text = "B/W";
            this.BWbtn.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.BWbtn.Click += new System.EventHandler(this.SetBWTransform);
            // 
            // levelControl
            // 
            this.levelControl.Colors = null;
            this.levelControl.Location = new System.Drawing.Point(4, 541);
            this.levelControl.Name = "levelControl";
            this.levelControl.Offset = -1F;
            this.levelControl.Range = 2F;
            this.levelControl.Size = new System.Drawing.Size(201, 49);
            this.levelControl.TabIndex = 70;
            this.levelControl.Title = "Level";
            this.levelControl.ValueChanged += new System.EventHandler(this.TransformChanged);
            // 
            // resolutionControl
            // 
            this.resolutionControl.Colors = null;
            this.resolutionControl.Location = new System.Drawing.Point(4, 436);
            this.resolutionControl.Name = "resolutionControl";
            this.resolutionControl.Offset = 0F;
            this.resolutionControl.Range = 4F;
            this.resolutionControl.Size = new System.Drawing.Size(201, 49);
            this.resolutionControl.TabIndex = 71;
            this.resolutionControl.Title = "Resolution";
            this.resolutionControl.ValueChanged += new System.EventHandler(this.TransformChanged);
            this.resolutionControl.Click += new System.EventHandler(this.TransformChanged);
            // 
            // strengthControl
            // 
            this.strengthControl.Colors = null;
            this.strengthControl.Location = new System.Drawing.Point(4, 489);
            this.strengthControl.Name = "strengthControl";
            this.strengthControl.Offset = 0F;
            this.strengthControl.Range = 1F;
            this.strengthControl.Size = new System.Drawing.Size(201, 49);
            this.strengthControl.TabIndex = 68;
            this.strengthControl.Title = "Strength";
            this.strengthControl.ValueChanged += new System.EventHandler(this.TransformChanged);
            // 
            // transperancyControl
            // 
            this.transperancyControl.Location = new System.Drawing.Point(4, 329);
            this.transperancyControl.Name = "transperancyControl";
            this.transperancyControl.Offset = 0F;
            this.transperancyControl.Range = 1F;
            this.transperancyControl.Size = new System.Drawing.Size(201, 105);
            this.transperancyControl.TabIndex = 29;
            this.transperancyControl.Title = "Transperancy";
            this.transperancyControl.ValueChanged += new System.EventHandler(this.TransformChanged);
            // 
            // saturationControl
            // 
            this.saturationControl.Colors = null;
            this.saturationControl.Location = new System.Drawing.Point(4, 218);
            this.saturationControl.Name = "saturationControl";
            this.saturationControl.Offset = -1F;
            this.saturationControl.Range = 2F;
            this.saturationControl.Size = new System.Drawing.Size(201, 105);
            this.saturationControl.TabIndex = 21;
            this.saturationControl.Title = "Saturation";
            this.saturationControl.ValueChanged += new System.EventHandler(this.TransformChanged);
            // 
            // brightnessControl
            // 
            this.brightnessControl.Location = new System.Drawing.Point(4, 63);
            this.brightnessControl.Name = "brightnessControl";
            this.brightnessControl.Offset = -1F;
            this.brightnessControl.Range = 2F;
            this.brightnessControl.Size = new System.Drawing.Size(201, 146);
            this.brightnessControl.TabIndex = 29;
            this.brightnessControl.Title = "Brightness";
            this.brightnessControl.ValueChanged += new System.EventHandler(this.TransformChanged);
            // 
            // label7
            // 
            this.label7.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label7.Location = new System.Drawing.Point(6, 337);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(138, 28);
            this.label7.TabIndex = 44;
            this.label7.Text = "Clip edge blur";
            this.label7.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // edgeGapBox
            // 
            this.edgeGapBox.DropDownHeight = 220;
            this.edgeGapBox.IntegralHeight = false;
            this.edgeGapBox.Location = new System.Drawing.Point(146, 332);
            this.edgeGapBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.edgeGapBox.MaxDropDownItems = 10;
            this.edgeGapBox.Name = "edgeGapBox";
            this.edgeGapBox.Size = new System.Drawing.Size(60, 28);
            this.edgeGapBox.TabIndex = 43;
            this.edgeGapBox.SelectedIndexChanged += new System.EventHandler(this.TransformChanged);
            // 
            // panel
            // 
            this.panel.Location = new System.Drawing.Point(214, 3);
            this.panel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(1452, 1535);
            this.panel.TabIndex = 45;
            // 
            // warningBox
            // 
            this.warningBox.Location = new System.Drawing.Point(3, 1277);
            this.warningBox.Multiline = true;
            this.warningBox.Name = "warningBox";
            this.warningBox.Size = new System.Drawing.Size(207, 261);
            this.warningBox.TabIndex = 46;
            // 
            // ImageEditForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2080, 1923);
            this.Controls.Add(this.warningBox);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.edgeGapBox);
            this.Controls.Add(this.layerGroupBox);
            this.Controls.Add(this.layerListView);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.modeGroupBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.scaleBox);
            this.Controls.Add(saveSameLocationButton);
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MinimumSize = new System.Drawing.Size(931, 967);
            this.Name = "ImageEditForm";
            this.Text = "Image Editing Form";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ImageEditForm_FormClosing);
            this.Resize += new System.EventHandler(this.ImageEditForm_Resize);
            this.layerGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        #region interface IPanelHolder
        public void CropRectangleUpdated() { }
        public void GeometryTransformUpdated() { /*ShowGeometryTransformParameters();*/ }
        public void FocusControl() { layerListView.Focus(); }
        public void ActiveLayerUpdated(int i) { UpdateLayerList(i); }
        #endregion
        public ImageEditForm()              
        {
            suspendUpdate = true;
            canvas = new DrawingPanel(this);
            ElementHost host = new ElementHost();
            host.Dock = DockStyle.Fill;
            host.Name = "host";
            host.Child = canvas;
            InitializeComponent();
            panel.Controls.Add(host);
            saturationControl.Colors = new Color[] { Color.Red, Color.Green, Color.Blue };
            saturationControl.ControlPoints = new float[] { 50, 50, 50 };
            brightnessControl.ControlPoints = new float[] { 50, 0, 50, 50, 50, 100 };
            transperancyControl.ControlPoints = new float[] { 0, 0, 0, 100 };
            strengthControl.ControlPoints = new float[] { 0.5f };
            levelControl.ControlPoints = new float[] { 0 };
            resolutionControl.ControlPoints = new float[] { 2 };
            ResetColorControls();
            viewingAreaOffset = panel.Location.X;
            panel.Size = new System.Drawing.Size(ClientSize.Width - viewingAreaOffset, ClientSize.Height);
            layerIndex = -1;       // nothing selected
            KeyDown += CaptureKeyboard;
            scaleBox.Items.AddRange(Enum.GetNames(typeof(ImageScale)));
            edgeGapBox.Items.AddRange(NumEnum.Values(typeof(EdgeGap)));
            sensitivityBox.Items.AddRange(NumEnum.Values(typeof(MouseSensitivity), 0.1));
            scaleBox.SelectedIndex = 0;
            edgeGapBox.SelectedIndex = 0;
            sensitivityBox.SelectedIndex = 2;
            ContextMenu selectMenu = new ContextMenu();
            selectMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                new MenuItem("Move on top", delegate (object s, EventArgs e) { if (canvas.MoveLayerOnTop(layerIndex)) UpdateLayerList(layerIndex); } ),
                new MenuItem("Move back", delegate (object s, EventArgs e) { if (canvas.MoveLayerBack(layerIndex)) UpdateLayerList(layerIndex); } ),
                new MenuItem("Flip", delegate (object s, EventArgs e) { if (canvas.IsActiveLayerVisible) canvas.ActiveLayer.Flip(); }),
                new MenuItem("Edges", delegate(object s, EventArgs e) { AddEffectLayer("Edge", new EdgeEffect()); }),
                new MenuItem("Drawing", delegate(object s, EventArgs e) { UpdateLayerList(canvas.AddStrokeLayer("Drawing")); } ),
                new MenuItem("Sharpness", delegate (object s, EventArgs e) { AddEffectLayer("Sharpness", new GradientContrastEffect()); }),
                new MenuItem("Remove odd", delegate (object s, EventArgs e) { AddNoOddsLayer(); }),
                new MenuItem("To background", delegate (object s, EventArgs e) { CopyToBackground(1); }),
                new MenuItem("To back 1/2", delegate (object s, EventArgs e) { CopyToBackground(2); }),
                //new MenuItem("To back 1/4", delegate (object s, EventArgs e) { CopyToBackground(4); }),
                //new MenuItem("To back 1/8", delegate (object s, EventArgs e) { CopyToBackground(8); }),
                new MenuItem("Delete", new EventHandler(DeleteLayer)) });
            layerListView.ContextMenu = selectMenu;
            layerListView.HideSelection = false;
            Load += ImageEditForm_Load;
        }
        private void ImageEditForm_Load(object sender, EventArgs e)
        {
            Graphics g = this.CreateGraphics();
            if (g != null)
            {
                dpiScaleX = g.DpiX / 96;
                dpiScaleY = g.DpiY / 96;
                g.Dispose();
                RescaleCanvas(false);
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
                CreateControls(drawingModes, new string[] { "Thickness" });
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
        int SelectedEdge() { return edgeGapBox.SelectedItem != null ? (int)(double)edgeGapBox.SelectedItem : 0; }
        public double SelectedSensitivity() { return sensitivityBox.SelectedItem != null ? (double)sensitivityBox.SelectedItem : 1; }
        double DisplayScale
        {
            get { return scaleBox.SelectedItem == null ? 0 : (int)Enum.Parse(typeof(ImageScale), (string)scaleBox.SelectedItem)/10.0; }
        }
        public void ShowNewImage(ImageFileInfo info)
        {
            scaleBox.SelectedItem = ImageScale.Fit.ToString();
            imageInfo = info;
            string warn = canvas.LoadFile(imageInfo, replaceSpan);
            ShowWarning(warn);
            UpdateLayerList(0);
            RescaleCanvas(true);
            Text = imageInfo.RealName + "  " + canvas.FrameSizeString;
            userInput = false;
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
            if (layerIndex == ind)
                return false;
            VisualLayer oldActiveLayer = canvas.ActiveLayer;
            if (!canvas.UpdateActiveLayer(ind))
                return false;
            if (layerIndex < 0 || oldActiveLayer == null || oldActiveLayer.Type != canvas.ActiveLayer.Type)
                CreateToolButtons(canvas.ActiveLayer);
            layerIndex = ind;
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
                lvi.EnsureVisible();
                layerListView.Select();
            }
            SetActiveLayer(newIndex);
            userInput = true;
            return l;
        }
        void AddEffectLayer(string name, ParametricEffect effect)
        {
            BitmapLayer bl = canvas.ActiveLayer as BitmapLayer;
            if (bl == null)
                return;
            BitmapAccess clip = canvas.GetSelected(0);
            int transparencyEdge = SelectedEdge();
            //var vl = new BitmapLayer(name, clip, transparencyEdge);
            var vl = new BitmapDerivativeLayer(name, clip, effect, transparencyEdge);
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
            int transparencyEdge = bl.FromSelection ? SelectedEdge() : 0;
            Vector shift = bl.FromSelection ? (Vector)canvas.SavedPosition : new Vector();
            BitmapAccess ba = clip.ApplyConversion(FilterType.MedianFilter, resolution, level);
            VisualLayer vl = new BitmapLayer(name, ba, transparencyEdge);
            UpdateLayerList(canvas.AddVisualLayer(vl, bl, shift));
        }
        void CopyToBackground(int scale)
        {
            BitmapLayer srcLayer = canvas.ActiveLayer as BitmapLayer;
            BitmapLayer bl = canvas.BackgroundLayer as BitmapLayer;
            if (bl != null && srcLayer != null)
            {
                BitmapAccess src = srcLayer.Image;
                System.Windows.Point c = srcLayer.MatrixControl.Center;
                c = canvas.FromCanvas.Transform(c);
                bl.Image.Overwrite(src, (int)c.X - src.Width / 2, (int)c.Y - src.Height / 2, scale);
                bl.UpdateImage();
                bl.RedrawImage();
                UpdateLayerList(canvas.RemoveActiveLayer());
            }
        }
        void AddClipboardLayer()
        {
            if (!System.Windows.Clipboard.ContainsData(System.Windows.DataFormats.Bitmap))
                return;
            try
            {
                BitmapAccess clip = new BitmapAccess(ClipboardBitmapAccess.GetImage());
                panel.Focus();
                int transparencyEdge = SelectedEdge();
                Debug.WriteLine("AddClipboardLayer: " + clip.ToString());
                Cursor = System.Windows.Forms.Cursors.WaitCursor;
                canvas.Cursor = System.Windows.Input.Cursors.Wait;
                VisualLayer vl = new BitmapDerivativeLayer("Clip" + transparencyEdge, clip, new ViewPointEffect(), transparencyEdge);
                vl?.SetEffectParameters(SelectedSensitivity(), 0, 0);
                UpdateLayerList(canvas.AddVisualLayer(vl, canvas.BackgroundLayer.MatrixControl.RenderScale));
                canvas.Cursor = System.Windows.Input.Cursors.Arrow;
                Cursor = System.Windows.Forms.Cursors.Default;
                SetMode(ToolMode.Basic);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        System.Windows.Media.Color SetMediaColor(Color nc)
        {
            return System.Windows.Media.Color.FromArgb(nc.A, nc.R, nc.G, nc.B);
        }
        void RescaleCanvas(bool initial) { canvas.ResizeImage(initial, DisplayScale, panel.Width / dpiScaleX, panel.Height / dpiScaleY); }
        void saveSameLocation_Click(object s, EventArgs e) { save(savePath == null ? Path.GetDirectoryName(imageInfo.FSPath) : savePath); }
        void saveButton_Click(object s, EventArgs e) { save(Path.GetDirectoryName(imageInfo.FSPath)); }
        void save(string dir)
        {
            if (DisplayScale != 0)
            {   // to ensure that all background image saved
                canvas.ResizeImage(false, 1, panel.Width, panel.Height);
            }
            SaveFileDialog saveAsDialog = new SaveFileDialog();
            saveAsDialog.FileName = imageInfo.RealName;
            saveAsDialog.Filter = DataAccess.PrivateAccessAllowed ? "regular|*.jpe|Exact|*.exa|MultiLayer|*.drw" : "regular|*.jpg|Exact|*.png|MultiLayer|*.draw"; // safe format relies on this order 
            saveAsDialog.FilterIndex = imageInfo.IsMultiLayer ? 3 : imageInfo.IsExact ? 2 : 1;
            saveAsDialog.RestoreDirectory = true;
            saveAsDialog.OverwritePrompt = true;
            saveAsDialog.InitialDirectory = dir;
            if (saveAsDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    savePath = Path.GetDirectoryName(saveAsDialog.FileName);
                    ImageFileInfo info = new ImageFileInfo(new FileInfo(FileName.MangleFile(saveAsDialog.FileName)));
                    BitmapEncoder bitmapEncoder = info.IsExact ? (BitmapEncoder)new PngBitmapEncoder() : new JpegBitmapEncoder();
                    string ret = info.IsImage ? canvas.SaveSingleImage(info.FSPath, 0, bitmapEncoder, info.IsEncrypted) : canvas.SaveLayers(info.FSPath, bitmapEncoder);
                    if (ret.Length == 0)
                        ShowNewImage(info);
                    else
                        System.Windows.Forms.MessageBox.Show(ret, "Saving " + info.FSPath + " failed");
                    SetMode(ToolMode.Basic);
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
                ToolMode = (ToolMode)Enum.Parse(typeof(ToolMode), rb.Name);
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
            //else if (e.Item.Index == layerIndex)
                UpdateLayerList(canvas.ClosestVisibleIndex(layerIndex));
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
        }
        void TransformChanged(object o, EventArgs e)
        {
            if (suspendUpdate)
                return;
            BitmapLayer aLayer = canvas.ActiveLayer as BitmapLayer;
            if (aLayer != null)
            {
                aLayer.ColorTransform.Set(brightnessControl.Values, saturationControl.Values, transperancyControl.Values);
                BitmapDerivativeLayer bdl = aLayer as BitmapDerivativeLayer;
                if (bdl != null && (bdl.DerivativeType == EffectType.ViewPoint || bdl.DerivativeType == EffectType.Morph))
                    bdl.SetEffectParameters(SelectedSensitivity(), bdl.MatrixControl.ViewDistortion.X, bdl.MatrixControl.ViewDistortion.Y);
                else
                    aLayer.SetEffectParameters(SelectedStrength, SelectedLevel, SelectedSize);
            }
        }
        private void SetBWTransform(object sender, EventArgs e)
        {
            BitmapLayer aLayer = canvas.ActiveLayer as BitmapLayer;
            if (aLayer != null)
            {
                aLayer.ColorTransform.CopyFrom(ColorTransform.BWTransform);
                if (aLayer.Type == VisualLayerType.Bitmap || aLayer.Type == VisualLayerType.Derivative)
                    aLayer.SetEffectParameters(SelectedStrength, SelectedLevel, SelectedSize);
                ResetColorControls();
                TransformChanged(null, null);
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
            ToolMode = m;
            Control[] ca = modeGroupBox.Controls.Find(ToolMode.ToString(), false);
            if (ca.Length > 0)
                ((RadioButton)ca[0]).Checked = true;
        }
        void ImageEditForm_FormClosing(object o, FormClosingEventArgs e) { }
        void ImageEditForm_Resize(object s, EventArgs e)
        {
            panel.Size = new System.Drawing.Size(ClientSize.Width - viewingAreaOffset, ClientSize.Height);
            RescaleCanvas(false);
        }
        void DeleteLayer(object s, EventArgs e)
        {
            if (canvas.ActiveLayer == null)
                return;
            UpdateLayerList(canvas.RemoveActiveLayer());
        }
        void scaleBox_SelectedIndexChanged(object s, EventArgs e) { if (userInput) RescaleCanvas(true); }
        void ShowWarning(string warning, bool error=false)
        {
            warningBox.Text = warning;
            warningBox.ForeColor= error ? Color.Red : Color.Black;
        }
        #region not ready
        void inputBox_ValueChanged(object s, EventArgs e)
        {
            //if (activeLayer.IsDrawing)
            //{
            //    activeShape=((DrawingLayer)activeLayer).Drawing.AddShape(GetThickness(), shapeType);
            //    undoButton.Enabled = true;
            //}
        }
        #endregion
    }
    public class NumEnum
    {
        public static object[] Values(Type t, double coef = 1)
        {
            object[] ret = new object[Enum.GetNames(t).Length];
            for (int i = 0; i < ret.Length; i++)
                ret[i] = coef * (int)Enum.GetValues(t).GetValue(i);
            return ret;
        }
    }
    enum MouseSensitivity
    {
        m2 = 2,
        m5 = 5,
        m10 = 10,
        m20 = 20,
    }
}
