using System;
using System.IO;
using System.Drawing;
using System.Windows.Input;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Diagnostics;
using CustomControls;
using System.Windows.Media.Imaging;

namespace ImageProcessor
{
    public class ImageViewForm : Form, IPanelHolder
    {
        private System.ComponentModel.Container components = null;
        DrawingPanel canvas;
        float dpiScaleX = 1;
        float dpiScaleY = 1;
        ImageEditForm editForm = null;
        int viewingAreaOffset;				// viewing area offset from left of client rectangle
        ImageListForm parent;				// parent image list form
        ImageFileInfo.Collection imageFiles;// full image file name shown as big image
        ImageFileInfo imageInfo;            // curently displayed image file info 
        bool infoMode;				        // indicates if info or original has to be extracted
        InfoType infoType;                  // tipe of info image when infoMode==true
        bool imageModified = false;
        ColorTransform colorTransform = new ColorTransform();
        ColorTransform previousColorTransform = new ColorTransform(); // previous non-identical transform
        VisualLayer backgroundLayer;
        bool userInput = true;
        string deletedImageName = null;
        string deletedImageFile = "deletedImage";
        private Label label2;
        private NumericUpDown angleCtrl;
        private Label label1;
        private NumericUpDown scaleCtrl;
        private CheckBox qualityBox;
        private Button nextSetButton;
        private Button previousSetButton;
        private ComboBox saveTypeBox;
        private Button gradientButton;
        private Button restoreButton;
        private Button applyEffectsButton;
        private Button saveAsButton;
        private Button saveButton;
        private GroupBox groupBox5;
        private Button flipVerticalButton;
        private Button rotateLeftButton;
        private Button rotateRightButton;
        private Button flipHorisontalButton;
        private GroupBox groupBox2;
        private RadioButton sameTransformButton;
        private ValueControl saturationControl;
        private RangeControl brightnessControl;
        private Button deleteButton;
        private Button nextImageButton;
        private Button previousImageButton;
        private Panel panel;
        private CheckBox resizeBox;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImageViewForm));
            this.label2 = new System.Windows.Forms.Label();
            this.angleCtrl = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.scaleCtrl = new System.Windows.Forms.NumericUpDown();
            this.qualityBox = new System.Windows.Forms.CheckBox();
            this.nextSetButton = new System.Windows.Forms.Button();
            this.previousSetButton = new System.Windows.Forms.Button();
            this.saveTypeBox = new System.Windows.Forms.ComboBox();
            this.gradientButton = new System.Windows.Forms.Button();
            this.restoreButton = new System.Windows.Forms.Button();
            this.applyEffectsButton = new System.Windows.Forms.Button();
            this.saveAsButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.flipVerticalButton = new System.Windows.Forms.Button();
            this.rotateLeftButton = new System.Windows.Forms.Button();
            this.rotateRightButton = new System.Windows.Forms.Button();
            this.flipHorisontalButton = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.sameTransformButton = new System.Windows.Forms.RadioButton();
            this.saturationControl = new CustomControls.ValueControl();
            this.brightnessControl = new CustomControls.RangeControl();
            this.deleteButton = new System.Windows.Forms.Button();
            this.nextImageButton = new System.Windows.Forms.Button();
            this.previousImageButton = new System.Windows.Forms.Button();
            this.resizeBox = new System.Windows.Forms.CheckBox();
            this.panel = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.angleCtrl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scaleCtrl)).BeginInit();
            this.groupBox5.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 450);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 13);
            this.label2.TabIndex = 83;
            this.label2.Text = "Angle";
            // 
            // angleCtrl
            // 
            this.angleCtrl.DecimalPlaces = 1;
            this.angleCtrl.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.angleCtrl.Location = new System.Drawing.Point(63, 448);
            this.angleCtrl.Maximum = new decimal(new int[] {
            999,
            0,
            0,
            0});
            this.angleCtrl.Minimum = new decimal(new int[] {
            999,
            0,
            0,
            -2147483648});
            this.angleCtrl.Name = "angleCtrl";
            this.angleCtrl.Size = new System.Drawing.Size(62, 20);
            this.angleCtrl.TabIndex = 82;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 427);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 80;
            this.label1.Text = "Scale";
            // 
            // scaleCtrl
            // 
            this.scaleCtrl.DecimalPlaces = 3;
            this.scaleCtrl.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.scaleCtrl.Location = new System.Drawing.Point(63, 425);
            this.scaleCtrl.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.scaleCtrl.Name = "scaleCtrl";
            this.scaleCtrl.Size = new System.Drawing.Size(62, 20);
            this.scaleCtrl.TabIndex = 81;
            this.scaleCtrl.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // qualityBox
            // 
            this.qualityBox.AutoSize = true;
            this.qualityBox.Location = new System.Drawing.Point(75, 92);
            this.qualityBox.Name = "qualityBox";
            this.qualityBox.Size = new System.Drawing.Size(58, 17);
            this.qualityBox.TabIndex = 79;
            this.qualityBox.Text = "Quality";
            this.qualityBox.UseVisualStyleBackColor = true;
            // 
            // nextSetButton
            // 
            this.nextSetButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.nextSetButton.Image = ((System.Drawing.Image)(resources.GetObject("nextSetButton.Image")));
            this.nextSetButton.Location = new System.Drawing.Point(103, 12);
            this.nextSetButton.Name = "nextSetButton";
            this.nextSetButton.Size = new System.Drawing.Size(26, 22);
            this.nextSetButton.TabIndex = 78;
            // 
            // previousSetButton
            // 
            this.previousSetButton.Font = new System.Drawing.Font("Webdings", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.previousSetButton.Image = ((System.Drawing.Image)(resources.GetObject("previousSetButton.Image")));
            this.previousSetButton.Location = new System.Drawing.Point(11, 12);
            this.previousSetButton.Name = "previousSetButton";
            this.previousSetButton.Size = new System.Drawing.Size(26, 22);
            this.previousSetButton.TabIndex = 77;
            // 
            // saveTypeBox
            // 
            this.saveTypeBox.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.saveTypeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.saveTypeBox.FormattingEnabled = true;
            this.saveTypeBox.Location = new System.Drawing.Point(11, 66);
            this.saveTypeBox.Name = "saveTypeBox";
            this.saveTypeBox.Size = new System.Drawing.Size(118, 21);
            this.saveTypeBox.TabIndex = 76;
            // 
            // gradientButton
            // 
            this.gradientButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Black;
            this.gradientButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.gradientButton.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.gradientButton.Image = ((System.Drawing.Image)(resources.GetObject("gradientButton.Image")));
            this.gradientButton.Location = new System.Drawing.Point(75, 142);
            this.gradientButton.Name = "gradientButton";
            this.gradientButton.Size = new System.Drawing.Size(54, 22);
            this.gradientButton.TabIndex = 75;
            this.gradientButton.Text = "Contour";
            // 
            // restoreButton
            // 
            this.restoreButton.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.restoreButton.Image = ((System.Drawing.Image)(resources.GetObject("restoreButton.Image")));
            this.restoreButton.Location = new System.Drawing.Point(75, 114);
            this.restoreButton.Name = "restoreButton";
            this.restoreButton.Size = new System.Drawing.Size(54, 22);
            this.restoreButton.TabIndex = 74;
            this.restoreButton.Text = "Restore";
            // 
            // applyEffectsButton
            // 
            this.applyEffectsButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Black;
            this.applyEffectsButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.applyEffectsButton.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.applyEffectsButton.Image = ((System.Drawing.Image)(resources.GetObject("applyEffectsButton.Image")));
            this.applyEffectsButton.Location = new System.Drawing.Point(11, 142);
            this.applyEffectsButton.Name = "applyEffectsButton";
            this.applyEffectsButton.Size = new System.Drawing.Size(54, 22);
            this.applyEffectsButton.TabIndex = 73;
            this.applyEffectsButton.Text = "Effects";
            // 
            // saveAsButton
            // 
            this.saveAsButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Black;
            this.saveAsButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.saveAsButton.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.saveAsButton.Image = ((System.Drawing.Image)(resources.GetObject("saveAsButton.Image")));
            this.saveAsButton.Location = new System.Drawing.Point(75, 38);
            this.saveAsButton.Name = "saveAsButton";
            this.saveAsButton.Size = new System.Drawing.Size(54, 22);
            this.saveAsButton.TabIndex = 72;
            this.saveAsButton.Text = "Save as";
            this.saveAsButton.Click += new System.EventHandler(this.saveAsButton_Click);
            // 
            // saveButton
            // 
            this.saveButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Black;
            this.saveButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.saveButton.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.saveButton.Image = ((System.Drawing.Image)(resources.GetObject("saveButton.Image")));
            this.saveButton.Location = new System.Drawing.Point(11, 38);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(54, 22);
            this.saveButton.TabIndex = 69;
            this.saveButton.Text = "Save";
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.flipVerticalButton);
            this.groupBox5.Controls.Add(this.rotateLeftButton);
            this.groupBox5.Controls.Add(this.rotateRightButton);
            this.groupBox5.Controls.Add(this.flipHorisontalButton);
            this.groupBox5.Location = new System.Drawing.Point(5, 378);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(128, 44);
            this.groupBox5.TabIndex = 66;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Flip/Rotate";
            // 
            // flipVerticalButton
            // 
            this.flipVerticalButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.flipVerticalButton.Image = ((System.Drawing.Image)(resources.GetObject("flipVerticalButton.Image")));
            this.flipVerticalButton.Location = new System.Drawing.Point(69, 16);
            this.flipVerticalButton.Name = "flipVerticalButton";
            this.flipVerticalButton.Size = new System.Drawing.Size(22, 22);
            this.flipVerticalButton.TabIndex = 25;
            // 
            // rotateLeftButton
            // 
            this.rotateLeftButton.Font = new System.Drawing.Font("Webdings", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.rotateLeftButton.Image = ((System.Drawing.Image)(resources.GetObject("rotateLeftButton.Image")));
            this.rotateLeftButton.Location = new System.Drawing.Point(11, 15);
            this.rotateLeftButton.Name = "rotateLeftButton";
            this.rotateLeftButton.Size = new System.Drawing.Size(22, 22);
            this.rotateLeftButton.TabIndex = 23;
            // 
            // rotateRightButton
            // 
            this.rotateRightButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.rotateRightButton.Image = ((System.Drawing.Image)(resources.GetObject("rotateRightButton.Image")));
            this.rotateRightButton.Location = new System.Drawing.Point(40, 16);
            this.rotateRightButton.Name = "rotateRightButton";
            this.rotateRightButton.Size = new System.Drawing.Size(22, 22);
            this.rotateRightButton.TabIndex = 24;
            // 
            // flipHorisontalButton
            // 
            this.flipHorisontalButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.flipHorisontalButton.Image = ((System.Drawing.Image)(resources.GetObject("flipHorisontalButton.Image")));
            this.flipHorisontalButton.Location = new System.Drawing.Point(98, 15);
            this.flipHorisontalButton.Name = "flipHorisontalButton";
            this.flipHorisontalButton.Size = new System.Drawing.Size(22, 22);
            this.flipHorisontalButton.TabIndex = 26;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.sameTransformButton);
            this.groupBox2.Controls.Add(this.saturationControl);
            this.groupBox2.Controls.Add(this.brightnessControl);
            this.groupBox2.Location = new System.Drawing.Point(6, 170);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(127, 202);
            this.groupBox2.TabIndex = 71;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Adjust image";
            // 
            // sameTransformButton
            // 
            this.sameTransformButton.AutoSize = true;
            this.sameTransformButton.Location = new System.Drawing.Point(10, 19);
            this.sameTransformButton.Name = "sameTransformButton";
            this.sameTransformButton.Size = new System.Drawing.Size(88, 17);
            this.sameTransformButton.TabIndex = 31;
            this.sameTransformButton.TabStop = true;
            this.sameTransformButton.Text = "Repeat same";
            this.sameTransformButton.UseVisualStyleBackColor = true;
            this.sameTransformButton.CheckedChanged += new System.EventHandler(this.UsePresetTransformChanged);
            // 
            // saturationControl
            // 
            this.saturationControl.Colors = null;
            this.saturationControl.Location = new System.Drawing.Point(3, 128);
            this.saturationControl.Name = "saturationControl";
            this.saturationControl.Offset = -0.4F;
            this.saturationControl.Range = 0.8F;
            this.saturationControl.Size = new System.Drawing.Size(120, 68);
            this.saturationControl.TabIndex = 21;
            this.saturationControl.Title = "Saturation";
            // 
            // brightnessControl
            // 
            this.brightnessControl.Location = new System.Drawing.Point(3, 37);
            this.brightnessControl.Name = "brightnessControl";
            this.brightnessControl.Offset = -0.4F;
            this.brightnessControl.Range = 0.8F;
            this.brightnessControl.Size = new System.Drawing.Size(120, 85);
            this.brightnessControl.TabIndex = 29;
            this.brightnessControl.Title = "Brightness";
            // 
            // deleteButton
            // 
            this.deleteButton.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.deleteButton.Image = ((System.Drawing.Image)(resources.GetObject("deleteButton.Image")));
            this.deleteButton.Location = new System.Drawing.Point(11, 115);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(54, 22);
            this.deleteButton.TabIndex = 70;
            this.deleteButton.Text = "Delete";
            // 
            // nextImageButton
            // 
            this.nextImageButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.nextImageButton.Image = ((System.Drawing.Image)(resources.GetObject("nextImageButton.Image")));
            this.nextImageButton.Location = new System.Drawing.Point(75, 12);
            this.nextImageButton.Name = "nextImageButton";
            this.nextImageButton.Size = new System.Drawing.Size(26, 22);
            this.nextImageButton.TabIndex = 67;
            // 
            // previousImageButton
            // 
            this.previousImageButton.Font = new System.Drawing.Font("Webdings", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.previousImageButton.Image = ((System.Drawing.Image)(resources.GetObject("previousImageButton.Image")));
            this.previousImageButton.Location = new System.Drawing.Point(39, 12);
            this.previousImageButton.Name = "previousImageButton";
            this.previousImageButton.Size = new System.Drawing.Size(26, 22);
            this.previousImageButton.TabIndex = 68;
            // 
            // resizeBox
            // 
            this.resizeBox.AutoSize = true;
            this.resizeBox.Location = new System.Drawing.Point(11, 92);
            this.resizeBox.Name = "resizeBox";
            this.resizeBox.Size = new System.Drawing.Size(50, 17);
            this.resizeBox.TabIndex = 65;
            this.resizeBox.Text = "2000";
            this.resizeBox.UseVisualStyleBackColor = true;
            this.resizeBox.CheckedChanged += new System.EventHandler(this.resizeBox_CheckedChanged);
            // 
            // panel
            // 
            this.panel.Location = new System.Drawing.Point(139, 0);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(967, 988);
            this.panel.TabIndex = 84;
            // 
            // ImageViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1382, 1237);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.angleCtrl);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.scaleCtrl);
            this.Controls.Add(this.qualityBox);
            this.Controls.Add(this.nextSetButton);
            this.Controls.Add(this.previousSetButton);
            this.Controls.Add(this.saveTypeBox);
            this.Controls.Add(this.gradientButton);
            this.Controls.Add(this.restoreButton);
            this.Controls.Add(this.applyEffectsButton);
            this.Controls.Add(this.saveAsButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.nextImageButton);
            this.Controls.Add(this.previousImageButton);
            this.Controls.Add(this.resizeBox);
            this.KeyPreview = true;
            this.MinimumSize = new System.Drawing.Size(628, 648);
            this.Name = "ImageViewForm";
            this.Text = "Image Editing Form";
            this.Resize += new System.EventHandler(this.FormResize);
            ((System.ComponentModel.ISupportInitialize)(this.angleCtrl)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.scaleCtrl)).EndInit();
            this.groupBox5.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        #region interface IPanelHolder
        public ToolMode ToolMode { get; private set; } = ToolMode.Crop;
        public void CropRectangleUpdated() { saveButton.Enabled = true; SetWindowTitle(); }
        public void GeometryTransformUpdated() { ShowGeometryTransformParameters(); }
        public void FocusControl() { }
        public void SetViewPosition(double x, double y) { }
        #endregion
        public ImageViewForm(ImageListForm parentListForm)    
        {
            canvas = new DrawingPanel(this);
            InitializeComponent();
            ElementHost host = new ElementHost();
            host.Dock = DockStyle.Fill;
            host.Name = "host";
            host.Child = canvas;
            panel.Controls.Add(host);
            saturationControl.Colors = new Color[] { Color.Red, Color.Green, Color.Blue };
            saturationControl.ControlPoints = new float[] { 50, 50, 50 };
            brightnessControl.ControlPoints = new float[] { 50, 0, 50, 50, 50, 100 };
            viewingAreaOffset = panel.Location.X;
            panel.Size = new System.Drawing.Size(ClientSize.Width - viewingAreaOffset, ClientSize.Height);
            nextImageButton.Click += delegate (object sender, EventArgs e) { ShowNewImage(imageFiles?.NavigateTo(true)); };
            previousImageButton.Click += delegate (object sender, EventArgs e) { ShowNewImage(imageFiles?.NavigateTo(false)); };
            nextSetButton.Click += delegate (object sender, EventArgs e) { ShowNewImage(imageFiles?.NavigateToGroup(true)); };
            previousSetButton.Click += delegate (object sender, EventArgs e) { ShowNewImage(imageFiles?.NavigateToGroup(false)); };
            scaleCtrl.ValueChanged += delegate (object sender, EventArgs e) { GeometryTransformChanged(); };
            angleCtrl.ValueChanged += delegate (object sender, EventArgs e) { GeometryTransformChanged(); };
            saturationControl.ValueChanged += delegate (object sender, EventArgs e) { ColorTransformChanged(); };
            brightnessControl.ValueChanged += delegate (object sender, EventArgs e) { ColorTransformChanged(); };
            deleteButton.Click += deleteButton_Click;
            restoreButton.Click += restoreButton_Click;
            rotateLeftButton.Click += flipRotateButton_Click;
            rotateRightButton.Click += flipRotateButton_Click;
            flipVerticalButton.Click += flipRotateButton_Click;
            flipHorisontalButton.Click += flipRotateButton_Click;
            applyEffectsButton.Click += effectsButton_Click;
            gradientButton.Click += gradientButton_Click;
            //KeyDown += CaptureCtrlC;
            KeyUp += CaptureCtrlC;
            saveTypeBox.Items.Add("Crop");
            saveTypeBox.Items.AddRange(Enum.GetNames(typeof(InfoType)));
            saveTypeBox.Items.Add("Selection");
            saveTypeBox.Items.Add("RectSelection");
            saveTypeBox.KeyPress += delegate (object sender, KeyPressEventArgs e) { if (ModifierKeys == Keys.Control) e.Handled = true; };
            parent = parentListForm;
            if (parent != null)             // launched from parent list form
                imageFiles = parent.ImageCollection;
            else
            {                               // for single image display
                imageFiles = null;
                nextImageButton.Enabled = false;
                previousImageButton.Enabled = false;
                nextSetButton.Enabled = false;
                previousSetButton.Enabled = false;
            }
            saveTypeBox.DropDownClosed += new EventHandler(SaveTypeChanged);
            Load += ImageViewForm_Load;
        }
        private void ImageViewForm_Load(object sender, EventArgs e)
        {

            Graphics g = this.CreateGraphics();
            if(g != null)
            {
                dpiScaleX = g.DpiX / 96;
                dpiScaleY = g.DpiY / 96;
                g.Dispose();
                RescaleCanvas(false);
            }
        }
        public void CaptureCtrlC(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            //Debug.WriteLine("View Modifier=" + Keyboard.Modifiers.ToString() + " key=" + e.KeyCode.ToString() + " " + Keyboard.IsKeyDown(Key.LeftCtrl));
            if (e.KeyCode == Keys.C && Keyboard.IsKeyDown(Key.LeftCtrl))
                canvas.SetClipboardFromSelection();
        }
        void SetInitialState(bool rotate90=false)
        {
            canvas.XYmirroredFrame = rotate90;
            RescaleCanvas(true);
            //if (saveTypeBox.SelectedIndex != 0)
            //    saveTypeBox.SelectedIndex = 0;
            saveTypeBox.SelectedItem = SaveTypeString();
            infoMode = false;
            saveButton.Enabled = resizeBox.Checked;
            imageModified = false;
            deleteButton.Enabled = true;
            angleCtrl.Value = 0;
            scaleCtrl.Value = 1;
            SetCutRectangle();
        }
        void SetCutRectangle()
        {
            if (infoMode == true)
                canvas.InitializeToolDrawing(ImageFileInfo.PixelSize(infoType));
            else
                canvas.InitializeToolDrawing();
        }
        public void ShowNewImage(string fsPath)
        {
            if (fsPath == null || fsPath.Length == 0)
                return;
            try
            {
                saveButton.Enabled = resizeBox.Checked;
                imageModified = false;
                deleteButton.Enabled = true;
                imageInfo = new ImageFileInfo(new FileInfo(fsPath));
                string warn = canvas.LoadFile(imageInfo, 0.5);
                if (warn.Length > 0)
                {
                    if (imageInfo.IsEncrypted && warn == DataAccess.NullCipher)
                    {
                        PasswordDialog pd = new PasswordDialog();
                        pd.Show();
                    }
                    else
                        System.Windows.MessageBox.Show(warn, "loading " + imageInfo.FSPath + " failed");
                }
                backgroundLayer = canvas.BackgroundLayer;
                sameTransformButton.Checked = false;
                if(!colorTransform.IsIdentical)
                    previousColorTransform.CopyFrom(colorTransform);
                colorTransform.Set();
                SetInitialState();
                canvas.SetActiveLayer(0);
                SetWindowTitle();
                ResetColorControls();
                ShowGeometryTransformParameters();
                Show();
                BringToFront();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ShowNewImage: Unable to diplay image " + imageInfo.FSPath + ": " + ex.Message);
            }
        }
        void SetWindowTitle()
        {
            string dir = imageFiles != null ? imageFiles.RealName : Path.GetDirectoryName(imageInfo.RealPath);
            Text = dir + '/'+ imageInfo.RealName + ' ' + imageInfo.StoreTypeString + ' ' + canvas.FrameSizeString;
        }
        void RescaleCanvas(bool initial) { canvas.Resize(initial, 0, panel.Width / dpiScaleX, panel.Height / dpiScaleY); }
        void FormResize(object s, EventArgs e)
        {
            panel.Size = new System.Drawing.Size(ClientSize.Width - viewingAreaOffset, ClientSize.Height);
            RescaleCanvas(false);
            ShowGeometryTransformParameters();
        }
        #region Operations
        void deleteButton_Click(object s, EventArgs e)
        {
            File.SetAttributes(imageInfo.FSPath, FileAttributes.Normal);
            if (imageModified)
            {
                MessageBoxResult res = System.Windows.MessageBox.Show("Are you sure you want to delete current image?",
                    "Delete image warning", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                    return;
            }
            FileInfo fi = new FileInfo(imageInfo.FSPath);
            if (fi.Exists)
            {
                try
                {
                    deletedImageName = imageInfo.FSPath;
                    File.Delete(deletedImageFile);
                    if (parent == null)
                        fi.MoveTo(deletedImageFile);
                    else
                    {
                        fi.CopyTo(deletedImageFile);
                        parent.DeleteActiveImage();
                    }
                    File.SetAttributes(deletedImageFile, FileAttributes.Normal);
                }
                catch(Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message, "Failed deleting " + deletedImageName);
                }
            }
        }
        void restoreButton_Click(object sender, EventArgs e)
        {
            FileInfo fi = new FileInfo(deletedImageFile);
            if (!fi.Exists || deletedImageName == null)
                return;
            fi.MoveTo(deletedImageName);
            ShowNewImage(deletedImageName);
        }
        void effectsButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (editForm == null || editForm.IsDisposed)
                    editForm = new ImageEditForm();
                editForm.ShowNewImage(imageInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }
        void gradientButton_Click(object sender, EventArgs e)
        {
            if (editForm == null || editForm.IsDisposed)
                editForm = new ImageEditForm();
            editForm.DrawNewImageContour(imageInfo);
            editForm.Show();
        }
        void saveButton_Click(object s, EventArgs e)
        {
            try
            {
                if (infoMode)
                    SaveImage(Path.Combine(Path.GetDirectoryName(imageInfo.FSPath), ImageFileName.InfoFileWithExtension(infoType)), -1, 100);
                else
                    SaveImage(imageInfo.FSPath, resizeBox.Checked ? 2000 : 0, (qualityBox.Checked ? 87 : 75));
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "Failed saving " + imageInfo.FSPath);
            }
        }
        void saveAsButton_Click(object s, EventArgs e)
        {
            SaveFileDialog saveAsDialog = new SaveFileDialog();
            saveAsDialog.FileName = imageInfo.RealName;
            if (DataAccess.PrivateAccessAllowed)
            {
                saveAsDialog.Filter = "regular|*.jpe|Exact|*.exa|MultiLayer|*.drw"; // safe format relies on this order 
                saveAsDialog.FilterIndex = imageInfo.IsMultiLayer ? 3 : imageInfo.IsExact ? 2 : 1;
            }
            else
            {
                saveAsDialog.Filter = "regular|*.jpg|Exact|*.png"; // safe format relies on this order 
                saveAsDialog.FilterIndex = imageInfo.IsExact ? 2 : 1;
            }
            saveAsDialog.RestoreDirectory = true;
            saveAsDialog.InitialDirectory = Path.GetDirectoryName(imageInfo.FSPath);
            if (saveAsDialog.ShowDialog() == DialogResult.OK)
            {
                string saveName = DataAccess.PrivateAccessAllowed ? ImageFileName.FSMangle(saveAsDialog.FileName) : saveAsDialog.FileName;
                SaveImage(saveName, resizeBox.Checked ? 2000 : 0, (qualityBox.Checked ? 87 : 75));
            }
        }
        void SaveImage(string path, int maxSize, int qualityLevel)
        {
            ImageFileName info = new ImageFileName(path);
            BitmapEncoder bitmapEncoder = info.IsExact || qualityLevel > 90 ? (BitmapEncoder)new PngBitmapEncoder() : new JpegBitmapEncoder();
            if (bitmapEncoder as JpegBitmapEncoder != null)
                ((JpegBitmapEncoder)bitmapEncoder).QualityLevel = qualityLevel;
            string ret = info.IsImage ? canvas.SaveSingleImage(path, maxSize, bitmapEncoder, info.IsEncrypted) : canvas.SaveLayers(path, bitmapEncoder);
            ToolMode = ToolMode.Crop;
            if (ret.Length != 0 )
                System.Windows.Forms.MessageBox.Show(ret, "Saving "+path+" failed");
            else
                ShowNewImage(path);
        }
        string SaveTypeString()
        {
            switch(ToolMode)
            {
                case ToolMode.FreeSelection:    return "Selection";
                case ToolMode.RectSelection:    return "RectSelection";
                default:                        return "Crop";
            }
        }
        void SaveTypeChanged(object sender, EventArgs e)
        {
            string str = (string)saveTypeBox.SelectedItem;
            if (str.StartsWith("Selection"))
            {
                ToolMode = ToolMode.FreeSelection;
                saveButton.Enabled = false;
                canvas.InitializeToolDrawing();
            }
            else if (str.StartsWith("RectSelection"))
            {
                ToolMode = ToolMode.RectSelection;
                saveButton.Enabled = false;
                canvas.InitializeToolDrawing();
            }
            else if (str.StartsWith("Crop"))
            {
                ToolMode = ToolMode.Crop;
                SetInitialState();
            }
            else
            {
                object o = Enum.Parse(typeof(InfoType), str);
                infoMode = o is InfoType;
                ToolMode = infoMode ? ToolMode.InfoImage : ToolMode.None;
                if (infoMode)
                {
                    infoType = (InfoType)o;
                    saveButton.Enabled = true;
                    deleteButton.Enabled = false;
                    SetCutRectangle();
                }
            }
        }
        #endregion
        #region Image transforms
        void ColorTransformChanged()
        {
            if (!userInput)
                return;
            if (colorTransform.Set(brightnessControl.Values, saturationControl.Values))
                UpdateImageColors();
        }
        void UpdateImageColors()
        {
            BitmapLayer iml = backgroundLayer as BitmapLayer;
            if (iml != null)
            {
                iml.ColorTransform.CopyFrom(colorTransform);
                iml.SetEffectParameters(0, 0, 1);
                saveButton.Enabled = true;
                imageModified = true;
            }
        }
        void UsePresetTransformChanged(object sender, EventArgs e)
        {
            if (!sameTransformButton.Checked)
                return;
            colorTransform.CopyFrom(previousColorTransform);
            ResetColorControls();
            UpdateImageColors();
        }
        void ResetColorControls()
        {
            userInput = false;
            saturationControl.SetValues(colorTransform.ColorValues);
            brightnessControl.SetValues(colorTransform.BrightnessValues);
            userInput = true;
        }
        void ShowGeometryTransformParameters()
        {
            try
            {
                //scaleCtrl.Value = (decimal)backgroundLayer.MatrixControl.RenderScale;
                angleCtrl.Value = (decimal)backgroundLayer.MatrixControl.Angle;
            }
            catch { }
        }
        void GeometryTransformChanged()
        {
            //backgroundLayer.MatrixControl.RenderScale = (double)scaleCtrl.Value;
            backgroundLayer.MatrixControl.Angle = (double)angleCtrl.Value;
            MatrixControlChanged();
        }
        void resizeBox_CheckedChanged(object sender, EventArgs e)
        {
            if (resizeBox.Checked)
                saveButton.Enabled = true;
        }
        void flipRotateButton_Click(object sender, EventArgs e)
        {
            if (backgroundLayer != null)
            {
                Control s = (Control)sender;
                if (s.Name == "flipVerticalButton")
                    backgroundLayer.MatrixControl.FlipY();
                else if (s.Name == "flipHorisontalButton")
                    backgroundLayer.MatrixControl.FlipX();
                else if (s.Name == "rotateRightButton")
                    backgroundLayer.MatrixControl.RotateRight();
                else if (s.Name == "rotateLeftButton")
                    backgroundLayer.MatrixControl.RotateLeft();
                if (s.Name.StartsWith("rotate"))
                {
                    SetInitialState(true);
                    SetWindowTitle();
                }
            }
            backgroundLayer.UpdateRenderTransform();
            saveButton.Enabled = true;
            imageModified = true;
        }
        void MatrixControlChanged()
        {
            backgroundLayer.UpdateRenderTransform();
            saveButton.Enabled = true;
            imageModified = true;
        }
        #endregion
    }
}
