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
using ShaderEffects;

namespace ImageProcessor
{
    public class ImageViewForm : Form, IPanelHolder
    {
        private System.ComponentModel.Container components = null;
        DrawingPanel canvas;
        float dpiScaleX = 1;
        float dpiScaleY = 1;
        //double[] effectParameters = new double[] { 0, 0, 1 };
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
        bool userInput = false;
        string deletedImageName = null;
        string deletedImageFile = "deletedImage";
        private Label label2;
        private NumericUpDown angleCtrl;
        private Label label1;
        private NumericUpDown scaleCtrl;
        private Button nextSetButton;
        private Button previousSetButton;
        private ComboBox saveTypeBox;
        private Button sharpenButton;
        private Button restoreButton;
        private Button applyEffectsButton;
        private Button saveAsButton;
        private Button saveButton;
        private GroupBox groupBox5;
        private Button flipVerticalButton;
        private Button rotateLeftButton;
        private Button rotateRightButton;
        private Button flipHorisontalButton;
        private GroupBox imageGroupBox;
        private RadioButton sameTransformButton;
        private ValueControl saturationControl;
        private RangeControl brightnessControl;
        private Button deleteButton;
        private Button nextImageButton;
        private Button previousImageButton;
        private Panel panel;
        private CheckBox resizeBox;
        private ComboBox sensitivityBox;
        private Panel nextImagePanel;
        private TextBox warningBox;
        private Label mouseSensitivityLabel;
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
            this.nextSetButton = new System.Windows.Forms.Button();
            this.previousSetButton = new System.Windows.Forms.Button();
            this.saveTypeBox = new System.Windows.Forms.ComboBox();
            this.sharpenButton = new System.Windows.Forms.Button();
            this.restoreButton = new System.Windows.Forms.Button();
            this.applyEffectsButton = new System.Windows.Forms.Button();
            this.saveAsButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.flipVerticalButton = new System.Windows.Forms.Button();
            this.rotateLeftButton = new System.Windows.Forms.Button();
            this.rotateRightButton = new System.Windows.Forms.Button();
            this.flipHorisontalButton = new System.Windows.Forms.Button();
            this.imageGroupBox = new System.Windows.Forms.GroupBox();
            this.sensitivityBox = new System.Windows.Forms.ComboBox();
            this.mouseSensitivityLabel = new System.Windows.Forms.Label();
            this.sameTransformButton = new System.Windows.Forms.RadioButton();
            this.saturationControl = new CustomControls.ValueControl();
            this.brightnessControl = new CustomControls.RangeControl();
            this.deleteButton = new System.Windows.Forms.Button();
            this.nextImageButton = new System.Windows.Forms.Button();
            this.previousImageButton = new System.Windows.Forms.Button();
            this.resizeBox = new System.Windows.Forms.CheckBox();
            this.panel = new System.Windows.Forms.Panel();
            this.nextImagePanel = new System.Windows.Forms.Panel();
            this.warningBox = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.angleCtrl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scaleCtrl)).BeginInit();
            this.groupBox5.SuspendLayout();
            this.imageGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 738);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 20);
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
            this.angleCtrl.Location = new System.Drawing.Point(94, 735);
            this.angleCtrl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            this.angleCtrl.Size = new System.Drawing.Size(93, 26);
            this.angleCtrl.TabIndex = 82;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 703);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 20);
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
            this.scaleCtrl.Location = new System.Drawing.Point(94, 700);
            this.scaleCtrl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.scaleCtrl.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.scaleCtrl.Name = "scaleCtrl";
            this.scaleCtrl.Size = new System.Drawing.Size(93, 26);
            this.scaleCtrl.TabIndex = 81;
            this.scaleCtrl.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // nextSetButton
            // 
            this.nextSetButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.nextSetButton.Image = ((System.Drawing.Image)(resources.GetObject("nextSetButton.Image")));
            this.nextSetButton.Location = new System.Drawing.Point(154, 18);
            this.nextSetButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nextSetButton.Name = "nextSetButton";
            this.nextSetButton.Size = new System.Drawing.Size(39, 34);
            this.nextSetButton.TabIndex = 78;
            // 
            // previousSetButton
            // 
            this.previousSetButton.Font = new System.Drawing.Font("Webdings", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.previousSetButton.Image = ((System.Drawing.Image)(resources.GetObject("previousSetButton.Image")));
            this.previousSetButton.Location = new System.Drawing.Point(16, 18);
            this.previousSetButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.previousSetButton.Name = "previousSetButton";
            this.previousSetButton.Size = new System.Drawing.Size(39, 34);
            this.previousSetButton.TabIndex = 77;
            // 
            // saveTypeBox
            // 
            this.saveTypeBox.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.saveTypeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.saveTypeBox.FormattingEnabled = true;
            this.saveTypeBox.Location = new System.Drawing.Point(16, 102);
            this.saveTypeBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.saveTypeBox.Name = "saveTypeBox";
            this.saveTypeBox.Size = new System.Drawing.Size(175, 28);
            this.saveTypeBox.TabIndex = 76;
            // 
            // sharpenButton
            // 
            this.sharpenButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Black;
            this.sharpenButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.sharpenButton.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.sharpenButton.Image = ((System.Drawing.Image)(resources.GetObject("sharpenButton.Image")));
            this.sharpenButton.Location = new System.Drawing.Point(112, 218);
            this.sharpenButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.sharpenButton.Name = "sharpenButton";
            this.sharpenButton.Size = new System.Drawing.Size(81, 34);
            this.sharpenButton.TabIndex = 75;
            this.sharpenButton.Text = "Sharpen";
            // 
            // restoreButton
            // 
            this.restoreButton.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.restoreButton.Image = ((System.Drawing.Image)(resources.GetObject("restoreButton.Image")));
            this.restoreButton.Location = new System.Drawing.Point(112, 175);
            this.restoreButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.restoreButton.Name = "restoreButton";
            this.restoreButton.Size = new System.Drawing.Size(81, 34);
            this.restoreButton.TabIndex = 74;
            this.restoreButton.Text = "Restore";
            // 
            // applyEffectsButton
            // 
            this.applyEffectsButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Black;
            this.applyEffectsButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.applyEffectsButton.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.applyEffectsButton.Image = ((System.Drawing.Image)(resources.GetObject("applyEffectsButton.Image")));
            this.applyEffectsButton.Location = new System.Drawing.Point(16, 218);
            this.applyEffectsButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.applyEffectsButton.Name = "applyEffectsButton";
            this.applyEffectsButton.Size = new System.Drawing.Size(81, 34);
            this.applyEffectsButton.TabIndex = 73;
            this.applyEffectsButton.Text = "Effects";
            // 
            // saveAsButton
            // 
            this.saveAsButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Black;
            this.saveAsButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.saveAsButton.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.saveAsButton.Image = ((System.Drawing.Image)(resources.GetObject("saveAsButton.Image")));
            this.saveAsButton.Location = new System.Drawing.Point(112, 58);
            this.saveAsButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.saveAsButton.Name = "saveAsButton";
            this.saveAsButton.Size = new System.Drawing.Size(81, 34);
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
            this.saveButton.Location = new System.Drawing.Point(16, 58);
            this.saveButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(81, 34);
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
            this.groupBox5.Location = new System.Drawing.Point(2, 628);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox5.Size = new System.Drawing.Size(204, 68);
            this.groupBox5.TabIndex = 66;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Flip/Rotate";
            // 
            // flipVerticalButton
            // 
            this.flipVerticalButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.flipVerticalButton.Image = ((System.Drawing.Image)(resources.GetObject("flipVerticalButton.Image")));
            this.flipVerticalButton.Location = new System.Drawing.Point(104, 25);
            this.flipVerticalButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.flipVerticalButton.Name = "flipVerticalButton";
            this.flipVerticalButton.Size = new System.Drawing.Size(33, 34);
            this.flipVerticalButton.TabIndex = 25;
            // 
            // rotateLeftButton
            // 
            this.rotateLeftButton.Font = new System.Drawing.Font("Webdings", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.rotateLeftButton.Image = ((System.Drawing.Image)(resources.GetObject("rotateLeftButton.Image")));
            this.rotateLeftButton.Location = new System.Drawing.Point(16, 23);
            this.rotateLeftButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.rotateLeftButton.Name = "rotateLeftButton";
            this.rotateLeftButton.Size = new System.Drawing.Size(33, 34);
            this.rotateLeftButton.TabIndex = 23;
            // 
            // rotateRightButton
            // 
            this.rotateRightButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.rotateRightButton.Image = ((System.Drawing.Image)(resources.GetObject("rotateRightButton.Image")));
            this.rotateRightButton.Location = new System.Drawing.Point(60, 25);
            this.rotateRightButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.rotateRightButton.Name = "rotateRightButton";
            this.rotateRightButton.Size = new System.Drawing.Size(33, 34);
            this.rotateRightButton.TabIndex = 24;
            // 
            // flipHorisontalButton
            // 
            this.flipHorisontalButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.flipHorisontalButton.Image = ((System.Drawing.Image)(resources.GetObject("flipHorisontalButton.Image")));
            this.flipHorisontalButton.Location = new System.Drawing.Point(147, 23);
            this.flipHorisontalButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.flipHorisontalButton.Name = "flipHorisontalButton";
            this.flipHorisontalButton.Size = new System.Drawing.Size(33, 34);
            this.flipHorisontalButton.TabIndex = 26;
            // 
            // imageGroupBox
            // 
            this.imageGroupBox.Controls.Add(this.sensitivityBox);
            this.imageGroupBox.Controls.Add(this.mouseSensitivityLabel);
            this.imageGroupBox.Controls.Add(this.sameTransformButton);
            this.imageGroupBox.Controls.Add(this.saturationControl);
            this.imageGroupBox.Controls.Add(this.brightnessControl);
            this.imageGroupBox.Location = new System.Drawing.Point(2, 262);
            this.imageGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.imageGroupBox.Name = "imageGroupBox";
            this.imageGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.imageGroupBox.Size = new System.Drawing.Size(204, 357);
            this.imageGroupBox.TabIndex = 71;
            this.imageGroupBox.TabStop = false;
            this.imageGroupBox.Text = "Adjust image";
            // 
            // sensitivityBox
            // 
            this.sensitivityBox.IntegralHeight = false;
            this.sensitivityBox.Location = new System.Drawing.Point(136, 62);
            this.sensitivityBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.sensitivityBox.Name = "sensitivityBox";
            this.sensitivityBox.Size = new System.Drawing.Size(60, 28);
            this.sensitivityBox.TabIndex = 46;
            // 
            // mouseSensitivityLabel
            // 
            this.mouseSensitivityLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.mouseSensitivityLabel.Location = new System.Drawing.Point(2, 62);
            this.mouseSensitivityLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.mouseSensitivityLabel.Name = "mouseSensitivityLabel";
            this.mouseSensitivityLabel.Size = new System.Drawing.Size(134, 28);
            this.mouseSensitivityLabel.TabIndex = 46;
            this.mouseSensitivityLabel.Text = "Mouse sensitivity";
            this.mouseSensitivityLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // sameTransformButton
            // 
            this.sameTransformButton.AutoSize = true;
            this.sameTransformButton.Location = new System.Drawing.Point(15, 26);
            this.sameTransformButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.sameTransformButton.Name = "sameTransformButton";
            this.sameTransformButton.Size = new System.Drawing.Size(130, 24);
            this.sameTransformButton.TabIndex = 31;
            this.sameTransformButton.TabStop = true;
            this.sameTransformButton.Text = "Repeat same";
            this.sameTransformButton.UseVisualStyleBackColor = true;
            this.sameTransformButton.CheckedChanged += new System.EventHandler(this.UseSameColorTransformChanged);
            // 
            // saturationControl
            // 
            this.saturationControl.Colors = null;
            this.saturationControl.Location = new System.Drawing.Point(3, 240);
            this.saturationControl.Name = "saturationControl";
            this.saturationControl.Offset = -0.4F;
            this.saturationControl.Range = 0.8F;
            this.saturationControl.Size = new System.Drawing.Size(198, 105);
            this.saturationControl.TabIndex = 21;
            this.saturationControl.Title = "Saturation";
            // 
            // brightnessControl
            // 
            this.brightnessControl.Location = new System.Drawing.Point(3, 93);
            this.brightnessControl.Name = "brightnessControl";
            this.brightnessControl.Offset = -0.4F;
            this.brightnessControl.Range = 0.8F;
            this.brightnessControl.Size = new System.Drawing.Size(198, 131);
            this.brightnessControl.TabIndex = 29;
            this.brightnessControl.Title = "Brightness";
            // 
            // deleteButton
            // 
            this.deleteButton.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.deleteButton.Image = ((System.Drawing.Image)(resources.GetObject("deleteButton.Image")));
            this.deleteButton.Location = new System.Drawing.Point(16, 177);
            this.deleteButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(81, 34);
            this.deleteButton.TabIndex = 70;
            this.deleteButton.Text = "Delete";
            // 
            // nextImageButton
            // 
            this.nextImageButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.nextImageButton.Image = ((System.Drawing.Image)(resources.GetObject("nextImageButton.Image")));
            this.nextImageButton.Location = new System.Drawing.Point(112, 18);
            this.nextImageButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nextImageButton.Name = "nextImageButton";
            this.nextImageButton.Size = new System.Drawing.Size(39, 34);
            this.nextImageButton.TabIndex = 67;
            // 
            // previousImageButton
            // 
            this.previousImageButton.Font = new System.Drawing.Font("Webdings", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.previousImageButton.Image = ((System.Drawing.Image)(resources.GetObject("previousImageButton.Image")));
            this.previousImageButton.Location = new System.Drawing.Point(58, 18);
            this.previousImageButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.previousImageButton.Name = "previousImageButton";
            this.previousImageButton.Size = new System.Drawing.Size(39, 34);
            this.previousImageButton.TabIndex = 68;
            // 
            // resizeBox
            // 
            this.resizeBox.AutoSize = true;
            this.resizeBox.Location = new System.Drawing.Point(24, 140);
            this.resizeBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.resizeBox.Name = "resizeBox";
            this.resizeBox.Size = new System.Drawing.Size(136, 24);
            this.resizeBox.TabIndex = 65;
            this.resizeBox.Text = "Max size 2000";
            this.resizeBox.UseVisualStyleBackColor = true;
            this.resizeBox.CheckedChanged += new System.EventHandler(this.resizeBox_CheckedChanged);
            // 
            // panel
            // 
            this.panel.Location = new System.Drawing.Point(208, 0);
            this.panel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(1450, 1520);
            this.panel.TabIndex = 84;
            // 
            // nextImagePanel
            // 
            this.nextImagePanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.nextImagePanel.Location = new System.Drawing.Point(5, 775);
            this.nextImagePanel.Margin = new System.Windows.Forms.Padding(0);
            this.nextImagePanel.Name = "nextImagePanel";
            this.nextImagePanel.Size = new System.Drawing.Size(200, 213);
            this.nextImagePanel.TabIndex = 85;
            // 
            // warningBox
            // 
            this.warningBox.Location = new System.Drawing.Point(8, 1004);
            this.warningBox.Margin = new System.Windows.Forms.Padding(1);
            this.warningBox.Multiline = true;
            this.warningBox.Name = "warningBox";
            this.warningBox.Size = new System.Drawing.Size(195, 232);
            this.warningBox.TabIndex = 86;
            // 
            // ImageViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2073, 1903);
            this.Controls.Add(this.warningBox);
            this.Controls.Add(this.nextImagePanel);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.angleCtrl);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.scaleCtrl);
            this.Controls.Add(this.nextSetButton);
            this.Controls.Add(this.previousSetButton);
            this.Controls.Add(this.saveTypeBox);
            this.Controls.Add(this.sharpenButton);
            this.Controls.Add(this.restoreButton);
            this.Controls.Add(this.applyEffectsButton);
            this.Controls.Add(this.saveAsButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.imageGroupBox);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.nextImageButton);
            this.Controls.Add(this.previousImageButton);
            this.Controls.Add(this.resizeBox);
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MinimumSize = new System.Drawing.Size(928, 958);
            this.Name = "ImageViewForm";
            this.Text = "Image Editing Form";
            this.Resize += new System.EventHandler(this.FormResize);
            ((System.ComponentModel.ISupportInitialize)(this.angleCtrl)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.scaleCtrl)).EndInit();
            this.groupBox5.ResumeLayout(false);
            this.imageGroupBox.ResumeLayout(false);
            this.imageGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        #region interface IPanelHolder
        public ToolMode ToolMode { get; private set; } = ToolMode.Crop;
        public void CropRectangleUpdated() { saveButton.Enabled = true; SetWindowTitle(); }
        public void GeometryTransformUpdated() { ShowGeometryTransformParameters(); }
        public void FocusControl() { }
        public double SelectedSensitivity() { return sensitivityBox.SelectedItem != null ? (double)sensitivityBox.SelectedItem : 1; }
        public void ActiveLayerUpdated(int i) { }
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
            nextImageButton.Click += delegate (object sender, EventArgs e) { ShowNewImage(imageFiles?.NavigateTo(1)); };
            previousImageButton.Click += delegate (object sender, EventArgs e) { ShowNewImage(imageFiles?.NavigateTo(-1)); };
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
            sharpenButton.Click += sharpenButton_Click;
            nextImagePanel.Paint += new PaintEventHandler(DrawNextImage);
            sensitivityBox.Items.AddRange(NumEnum.Values(typeof(MouseSensitivity), 0.1));
            sensitivityBox.SelectedIndex = 2;
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
        void SetInitialState(bool XYmirrored = false)
        {
            canvas.XYmirroredFrame = XYmirrored;
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
                nextImagePanel.Invalidate();
                userInput = false;
                //effectParameters = new double[] { 0, 0, 1 };
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
                    ShowWarning(warn);
                }
                backgroundLayer = canvas.BackgroundLayer;
                sameTransformButton.Checked = false;
                if(!colorTransform.IsIdentical)
                    previousColorTransform.CopyFrom(colorTransform);
                colorTransform.Set();
                SetInitialState();
                canvas.UpdateActiveLayer(0);
                SetWindowTitle();
                UpdateColorControls();
                ShowGeometryTransformParameters();
                Show();
                BringToFront();
                sharpenButton.Enabled = true;
                userInput = true;
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
            if(imageFiles == null)
                Close();
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
        void sharpenButton_Click(object sender, EventArgs e)
        {
            var bl = backgroundLayer as BitmapLayer;
            if (bl == null)
                return;
            var vl = new BitmapDerivativeLayer("", bl.Image, new GradientContrastEffect(), 0);
            if (vl == null) 
                return;
            //effectParameters = new double[] { 0.5, 1, 2 };
            //vl.SetEffectParameters(effectParameters);
            vl.SetEffectParameters(0.5, 1, 2);
            canvas.ReplaceVisuallLayer(vl, bl);
            backgroundLayer = canvas.ActiveLayer;
            imageModified = true;
            saveButton.Enabled = true;
            sharpenButton.Enabled = false;   
        }
        void saveButton_Click(object s, EventArgs e)
        {
            try
            {
                if (infoMode)
                    SaveImage(Path.Combine(Path.GetDirectoryName(imageInfo.FSPath), ImageFileName.InfoFileWithExtension(infoType)), -1);
                else
                    SaveImage(imageInfo.FSPath, resizeBox.Checked ? 2000 : 0);
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
            saveAsDialog.Filter = DataAccess.PrivateAccessAllowed ? "regular|*.jpe|Exact|*.exa|MultiLayer|*.drw" : "regular|*.jpg|Exact|*.png|MultiLayer|*.draw"; // safe format relies on this order 
            saveAsDialog.FilterIndex = imageInfo.IsMultiLayer ? 3 : imageInfo.IsExact ? 2 : 1;
            saveAsDialog.RestoreDirectory = true;
            saveAsDialog.InitialDirectory = Path.GetDirectoryName(imageInfo.FSPath);
            saveAsDialog.OverwritePrompt = true;
            if (saveAsDialog.ShowDialog() == DialogResult.OK)
            {
                string saveName = DataAccess.PrivateAccessAllowed ? FileName.MangleFile(saveAsDialog.FileName) : saveAsDialog.FileName;
                SaveImage(saveName, resizeBox.Checked ? 2000 : 0);
            }
        }
        void SaveImage(string path, int maxSize)
        {
            ImageFileName info = new ImageFileName(path);
            BitmapEncoder bitmapEncoder = info.IsExact ? (BitmapEncoder)new PngBitmapEncoder() : new JpegBitmapEncoder();
            string ret = info.IsImage ? canvas.SaveSingleImage(path, maxSize, bitmapEncoder, info.IsEncrypted) : canvas.SaveLayers(path, bitmapEncoder);
            ToolMode = ToolMode.Crop;
            if (ret.Length != 0)
                System.Windows.Forms.MessageBox.Show(ret, "Saving " + path + " failed");
            else
                ShowNewImage(path);
        }
        void DrawNextImage(object sender, PaintEventArgs e)
        {
            if (imageFiles == null || imageFiles.Count < 1)
                return;
            try
            {
                int ind = imageFiles.NewInd(1);
                var file = ind >= 0 ? imageFiles[ind] : null;
                ImageFileInfo ifi = new ImageFileInfo(new FileInfo(file.FSPath));
                Image im = ifi.SynchronizeThumbnail();
                float areaSize = 138 * e.Graphics.DpiX / 96;
                float scale = Math.Min(areaSize / im.Width, areaSize / im.Height);
                float iw = im.Width * scale;
                float ih = im.Height * scale;
                float d = (iw - ih) / 2;
                PointF del = d < 0 ? new PointF(-d, 0) : new PointF(0, d);
                e.Graphics.DrawImage(im, del.X, del.Y, iw, ih);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
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
        void ShowWarning(string warning, bool error = false)
        {
            warningBox.Text = warning;
            warningBox.ForeColor = error ? Color.Red : Color.Black;
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
                //iml.SetEffectParameters(effectParameters);
                saveButton.Enabled = true;
                if (userInput)
                    imageModified = true;
            }
        }
        void UseSameColorTransformChanged(object sender, EventArgs e)
        {
            if (!sameTransformButton.Checked)
                return;
            colorTransform.CopyFrom(previousColorTransform);
            UpdateColorControls();
            UpdateImageColors();
        }
        void UpdateColorControls()
        {
            saturationControl.SetValues(colorTransform.ColorValues);
            brightnessControl.SetValues(colorTransform.BrightnessValues);
        }
        void ShowGeometryTransformParameters()
        {
            if(backgroundLayer != null && backgroundLayer.MatrixControl != null)
            {
                userInput = false;
                scaleCtrl.Value = (decimal)backgroundLayer.MatrixControl.RenderScale;
                angleCtrl.Value = (decimal)backgroundLayer.MatrixControl.Angle;
                userInput = true;
            }
        }
        void GeometryTransformChanged()
        {
            if(userInput)
            {
                backgroundLayer.MatrixControl.RenderScale = (double)scaleCtrl.Value;
                backgroundLayer.MatrixControl.Angle = (double)angleCtrl.Value;
                MatrixControlChanged();
            }
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
                    canvas.CropRectangle?.FlipXY();
                    canvas.UpdateToolDrawing();
                    SetWindowTitle();
                    ShowGeometryTransformParameters();
                }
            }
            backgroundLayer.UpdateRenderTransform();
            saveButton.Enabled = true;
            if (userInput)
                imageModified = true;
        }
        void MatrixControlChanged()
        {
            backgroundLayer.UpdateRenderTransform();
            saveButton.Enabled = true;
            if (userInput)
                imageModified = true;
        }
        #endregion
    }
}
