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
using System.Media;
using ShaderEffects;

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
        ImageFileInfo.FileList imageFiles;// full image file name shown as big image
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
        int direction = 0;
        private Label label2;
        private NumericUpDown angleCtrl;
        private Label label1;
        private NumericUpDown scaleCtrl;
        private Button nextSetButton;
        private Button previousSetButton;
        private ComboBox actionBox;
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
        private Label label3;
        private NumericUpDown delayBox;
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
            this.actionBox = new System.Windows.Forms.ComboBox();
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
            this.label3 = new System.Windows.Forms.Label();
            this.delayBox = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.angleCtrl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scaleCtrl)).BeginInit();
            this.groupBox5.SuspendLayout();
            this.imageGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.delayBox)).BeginInit();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(27, 922);
            this.label2.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 25);
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
            this.angleCtrl.Location = new System.Drawing.Point(125, 919);
            this.angleCtrl.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
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
            this.angleCtrl.Size = new System.Drawing.Size(124, 31);
            this.angleCtrl.TabIndex = 82;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 879);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 25);
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
            this.scaleCtrl.Location = new System.Drawing.Point(125, 875);
            this.scaleCtrl.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.scaleCtrl.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.scaleCtrl.Name = "scaleCtrl";
            this.scaleCtrl.Size = new System.Drawing.Size(124, 31);
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
            this.nextSetButton.Location = new System.Drawing.Point(205, 22);
            this.nextSetButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.nextSetButton.Name = "nextSetButton";
            this.nextSetButton.Size = new System.Drawing.Size(52, 42);
            this.nextSetButton.TabIndex = 78;
            // 
            // previousSetButton
            // 
            this.previousSetButton.Font = new System.Drawing.Font("Webdings", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.previousSetButton.Image = ((System.Drawing.Image)(resources.GetObject("previousSetButton.Image")));
            this.previousSetButton.Location = new System.Drawing.Point(21, 22);
            this.previousSetButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.previousSetButton.Name = "previousSetButton";
            this.previousSetButton.Size = new System.Drawing.Size(52, 42);
            this.previousSetButton.TabIndex = 77;
            // 
            // actionBox
            // 
            this.actionBox.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.actionBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.actionBox.FormattingEnabled = true;
            this.actionBox.Location = new System.Drawing.Point(21, 128);
            this.actionBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.actionBox.Name = "actionBox";
            this.actionBox.Size = new System.Drawing.Size(232, 33);
            this.actionBox.TabIndex = 76;
            // 
            // sharpenButton
            // 
            this.sharpenButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Black;
            this.sharpenButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.sharpenButton.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.sharpenButton.Image = ((System.Drawing.Image)(resources.GetObject("sharpenButton.Image")));
            this.sharpenButton.Location = new System.Drawing.Point(149, 272);
            this.sharpenButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.sharpenButton.Name = "sharpenButton";
            this.sharpenButton.Size = new System.Drawing.Size(108, 42);
            this.sharpenButton.TabIndex = 75;
            this.sharpenButton.Text = "Sharpen";
            // 
            // restoreButton
            // 
            this.restoreButton.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.restoreButton.Image = ((System.Drawing.Image)(resources.GetObject("restoreButton.Image")));
            this.restoreButton.Location = new System.Drawing.Point(149, 219);
            this.restoreButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.restoreButton.Name = "restoreButton";
            this.restoreButton.Size = new System.Drawing.Size(108, 42);
            this.restoreButton.TabIndex = 74;
            this.restoreButton.Text = "Restore";
            // 
            // applyEffectsButton
            // 
            this.applyEffectsButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Black;
            this.applyEffectsButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.applyEffectsButton.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.applyEffectsButton.Image = ((System.Drawing.Image)(resources.GetObject("applyEffectsButton.Image")));
            this.applyEffectsButton.Location = new System.Drawing.Point(21, 272);
            this.applyEffectsButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.applyEffectsButton.Name = "applyEffectsButton";
            this.applyEffectsButton.Size = new System.Drawing.Size(108, 42);
            this.applyEffectsButton.TabIndex = 73;
            this.applyEffectsButton.Text = "Effects";
            // 
            // saveAsButton
            // 
            this.saveAsButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Black;
            this.saveAsButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.saveAsButton.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.saveAsButton.Image = ((System.Drawing.Image)(resources.GetObject("saveAsButton.Image")));
            this.saveAsButton.Location = new System.Drawing.Point(149, 72);
            this.saveAsButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.saveAsButton.Name = "saveAsButton";
            this.saveAsButton.Size = new System.Drawing.Size(108, 42);
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
            this.saveButton.Location = new System.Drawing.Point(21, 72);
            this.saveButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(108, 42);
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
            this.groupBox5.Location = new System.Drawing.Point(3, 785);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.groupBox5.Size = new System.Drawing.Size(272, 85);
            this.groupBox5.TabIndex = 66;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Flip/Rotate";
            // 
            // flipVerticalButton
            // 
            this.flipVerticalButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.flipVerticalButton.Image = ((System.Drawing.Image)(resources.GetObject("flipVerticalButton.Image")));
            this.flipVerticalButton.Location = new System.Drawing.Point(139, 31);
            this.flipVerticalButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.flipVerticalButton.Name = "flipVerticalButton";
            this.flipVerticalButton.Size = new System.Drawing.Size(44, 42);
            this.flipVerticalButton.TabIndex = 25;
            // 
            // rotateLeftButton
            // 
            this.rotateLeftButton.Font = new System.Drawing.Font("Webdings", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.rotateLeftButton.Image = ((System.Drawing.Image)(resources.GetObject("rotateLeftButton.Image")));
            this.rotateLeftButton.Location = new System.Drawing.Point(21, 29);
            this.rotateLeftButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.rotateLeftButton.Name = "rotateLeftButton";
            this.rotateLeftButton.Size = new System.Drawing.Size(44, 42);
            this.rotateLeftButton.TabIndex = 23;
            // 
            // rotateRightButton
            // 
            this.rotateRightButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.rotateRightButton.Image = ((System.Drawing.Image)(resources.GetObject("rotateRightButton.Image")));
            this.rotateRightButton.Location = new System.Drawing.Point(80, 31);
            this.rotateRightButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.rotateRightButton.Name = "rotateRightButton";
            this.rotateRightButton.Size = new System.Drawing.Size(44, 42);
            this.rotateRightButton.TabIndex = 24;
            // 
            // flipHorisontalButton
            // 
            this.flipHorisontalButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.flipHorisontalButton.Image = ((System.Drawing.Image)(resources.GetObject("flipHorisontalButton.Image")));
            this.flipHorisontalButton.Location = new System.Drawing.Point(196, 29);
            this.flipHorisontalButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.flipHorisontalButton.Name = "flipHorisontalButton";
            this.flipHorisontalButton.Size = new System.Drawing.Size(44, 42);
            this.flipHorisontalButton.TabIndex = 26;
            // 
            // imageGroupBox
            // 
            this.imageGroupBox.Controls.Add(this.sensitivityBox);
            this.imageGroupBox.Controls.Add(this.mouseSensitivityLabel);
            this.imageGroupBox.Controls.Add(this.sameTransformButton);
            this.imageGroupBox.Controls.Add(this.saturationControl);
            this.imageGroupBox.Controls.Add(this.brightnessControl);
            this.imageGroupBox.Location = new System.Drawing.Point(3, 328);
            this.imageGroupBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.imageGroupBox.Name = "imageGroupBox";
            this.imageGroupBox.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.imageGroupBox.Size = new System.Drawing.Size(272, 446);
            this.imageGroupBox.TabIndex = 71;
            this.imageGroupBox.TabStop = false;
            this.imageGroupBox.Text = "Adjust image";
            // 
            // sensitivityBox
            // 
            this.sensitivityBox.IntegralHeight = false;
            this.sensitivityBox.Location = new System.Drawing.Point(181, 78);
            this.sensitivityBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.sensitivityBox.Name = "sensitivityBox";
            this.sensitivityBox.Size = new System.Drawing.Size(79, 33);
            this.sensitivityBox.TabIndex = 46;
            // 
            // mouseSensitivityLabel
            // 
            this.mouseSensitivityLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.mouseSensitivityLabel.Location = new System.Drawing.Point(3, 78);
            this.mouseSensitivityLabel.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.mouseSensitivityLabel.Name = "mouseSensitivityLabel";
            this.mouseSensitivityLabel.Size = new System.Drawing.Size(179, 35);
            this.mouseSensitivityLabel.TabIndex = 46;
            this.mouseSensitivityLabel.Text = "Mouse sensitivity";
            this.mouseSensitivityLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // sameTransformButton
            // 
            this.sameTransformButton.AutoSize = true;
            this.sameTransformButton.Location = new System.Drawing.Point(20, 32);
            this.sameTransformButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.sameTransformButton.Name = "sameTransformButton";
            this.sameTransformButton.Size = new System.Drawing.Size(170, 29);
            this.sameTransformButton.TabIndex = 31;
            this.sameTransformButton.TabStop = true;
            this.sameTransformButton.Text = "Repeat same";
            this.sameTransformButton.UseVisualStyleBackColor = true;
            this.sameTransformButton.CheckedChanged += new System.EventHandler(this.UseSameColorTransformChanged);
            // 
            // saturationControl
            // 
            this.saturationControl.Colors = null;
            this.saturationControl.Location = new System.Drawing.Point(4, 300);
            this.saturationControl.Margin = new System.Windows.Forms.Padding(4);
            this.saturationControl.Name = "saturationControl";
            this.saturationControl.Offset = -0.4F;
            this.saturationControl.Range = 0.8F;
            this.saturationControl.Size = new System.Drawing.Size(264, 131);
            this.saturationControl.TabIndex = 21;
            this.saturationControl.Title = "Saturation";
            // 
            // brightnessControl
            // 
            this.brightnessControl.Location = new System.Drawing.Point(4, 116);
            this.brightnessControl.Margin = new System.Windows.Forms.Padding(4);
            this.brightnessControl.Name = "brightnessControl";
            this.brightnessControl.Offset = -0.4F;
            this.brightnessControl.Range = 0.8F;
            this.brightnessControl.Size = new System.Drawing.Size(264, 164);
            this.brightnessControl.TabIndex = 29;
            this.brightnessControl.Title = "Brightness";
            // 
            // deleteButton
            // 
            this.deleteButton.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.deleteButton.Image = ((System.Drawing.Image)(resources.GetObject("deleteButton.Image")));
            this.deleteButton.Location = new System.Drawing.Point(21, 221);
            this.deleteButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(108, 42);
            this.deleteButton.TabIndex = 70;
            this.deleteButton.Text = "Delete";
            // 
            // nextImageButton
            // 
            this.nextImageButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.nextImageButton.Image = ((System.Drawing.Image)(resources.GetObject("nextImageButton.Image")));
            this.nextImageButton.Location = new System.Drawing.Point(149, 22);
            this.nextImageButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.nextImageButton.Name = "nextImageButton";
            this.nextImageButton.Size = new System.Drawing.Size(52, 42);
            this.nextImageButton.TabIndex = 67;
            // 
            // previousImageButton
            // 
            this.previousImageButton.Font = new System.Drawing.Font("Webdings", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.previousImageButton.Image = ((System.Drawing.Image)(resources.GetObject("previousImageButton.Image")));
            this.previousImageButton.Location = new System.Drawing.Point(77, 22);
            this.previousImageButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.previousImageButton.Name = "previousImageButton";
            this.previousImageButton.Size = new System.Drawing.Size(52, 42);
            this.previousImageButton.TabIndex = 68;
            // 
            // resizeBox
            // 
            this.resizeBox.AutoSize = true;
            this.resizeBox.Location = new System.Drawing.Point(32, 175);
            this.resizeBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.resizeBox.Name = "resizeBox";
            this.resizeBox.Size = new System.Drawing.Size(184, 29);
            this.resizeBox.TabIndex = 65;
            this.resizeBox.Text = "Max size 2000";
            this.resizeBox.UseVisualStyleBackColor = true;
            this.resizeBox.CheckedChanged += new System.EventHandler(this.resizeBox_CheckedChanged);
            // 
            // panel
            // 
            this.panel.Location = new System.Drawing.Point(277, 0);
            this.panel.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(1933, 1900);
            this.panel.TabIndex = 84;
            // 
            // nextImagePanel
            // 
            this.nextImagePanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.nextImagePanel.Location = new System.Drawing.Point(7, 969);
            this.nextImagePanel.Margin = new System.Windows.Forms.Padding(0);
            this.nextImagePanel.Name = "nextImagePanel";
            this.nextImagePanel.Size = new System.Drawing.Size(266, 266);
            this.nextImagePanel.TabIndex = 85;
            // 
            // warningBox
            // 
            this.warningBox.Location = new System.Drawing.Point(7, 1285);
            this.warningBox.Margin = new System.Windows.Forms.Padding(1);
            this.warningBox.Multiline = true;
            this.warningBox.Name = "warningBox";
            this.warningBox.Size = new System.Drawing.Size(266, 289);
            this.warningBox.TabIndex = 86;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(27, 1249);
            this.label3.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 25);
            this.label3.TabIndex = 87;
            this.label3.Text = "Delay";
            // 
            // delayBox
            // 
            this.delayBox.DecimalPlaces = 1;
            this.delayBox.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.delayBox.Location = new System.Drawing.Point(125, 1247);
            this.delayBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.delayBox.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.delayBox.Name = "delayBox";
            this.delayBox.Size = new System.Drawing.Size(124, 31);
            this.delayBox.TabIndex = 88;
            // 
            // ImageViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2764, 2379);
            this.Controls.Add(this.delayBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.warningBox);
            this.Controls.Add(this.nextImagePanel);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.angleCtrl);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.scaleCtrl);
            this.Controls.Add(this.nextSetButton);
            this.Controls.Add(this.previousSetButton);
            this.Controls.Add(this.actionBox);
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
            this.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.MinimumSize = new System.Drawing.Size(1229, 1180);
            this.Name = "ImageViewForm";
            this.Text = "Image Editing Form";
            this.Resize += new System.EventHandler(this.FormResize);
            ((System.ComponentModel.ISupportInitialize)(this.angleCtrl)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.scaleCtrl)).EndInit();
            this.groupBox5.ResumeLayout(false);
            this.imageGroupBox.ResumeLayout(false);
            this.imageGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.delayBox)).EndInit();
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
            nextImageButton.Click += delegate (object sender, EventArgs e) { direction = 1; ShowNewImage(imageFiles?.NavigateTo(direction)); };
            previousImageButton.Click += delegate (object sender, EventArgs e) { direction = -1; ShowNewImage(imageFiles?.NavigateTo(direction)); };
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
            actionBox.Items.Add("Crop");
            actionBox.Items.AddRange(Enum.GetNames(typeof(InfoType)));
            actionBox.Items.Add("Selection");
            actionBox.Items.Add("RectSelection");
            actionBox.KeyPress += delegate (object sender, KeyPressEventArgs e) { if (ModifierKeys == Keys.Control) e.Handled = true; };
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
            actionBox.DropDownClosed += new EventHandler(SaveTypeChanged);
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
            {
                SystemSounds.Beep.Play();
                canvas.SetClipboardFromSelection();
            }
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
                saveButton.Enabled = resizeBox.Checked;
                imageModified = false;
                deleteButton.Enabled = true;
                imageInfo = new ImageFileInfo(new FileInfo(fsPath));
                string warn = canvas.LoadFile(imageInfo, (double)delayBox.Value+0.3);
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
                RescaleCanvas(true);
                actionBox.SelectedItem = ToolMode == ToolMode.FreeSelection ? "Selection" : ToolMode == ToolMode.RectSelection ? "RectSelection" : "Crop";
                infoMode = false;
                saveButton.Enabled = resizeBox.Checked;
                imageModified = false;
                deleteButton.Enabled = true;
                angleCtrl.Value = 0;
                scaleCtrl.Value = 1;
                SetCutRectangle();
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
        void DrawNextImage(object sender, PaintEventArgs e)
        {
            if (imageFiles == null || imageFiles.Count < 1)
                return;
            try
            {
                int ind = imageFiles.NewInd(direction);
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
        void SetWindowTitle()
        {
            string dir = imageFiles != null ? imageFiles.RealName : Path.GetDirectoryName(imageInfo.RealPath);
            Text = dir + '/'+ imageInfo.RealName + ' ' + imageInfo.StoreTypeString + ' ' + canvas.FrameSizeString;
        }
        void RescaleCanvas(bool initial) { canvas.ResizeImage(initial, 0, panel.Width / dpiScaleX, panel.Height / dpiScaleY); }
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
            saveAsDialog.Filter = DataAccess.PrivateAccessEnforced ? "regular|*.jpe|Exact|*.exa|MultiLayer|*.drw" : "regular|*.jpg|Exact|*.png|MultiLayer|*.draw"; // safe format relies on this order 
            saveAsDialog.FilterIndex = imageInfo.IsMultiLayer ? 3 : imageInfo.IsExact ? 2 : 1;
            saveAsDialog.RestoreDirectory = true;
            saveAsDialog.InitialDirectory = Path.GetDirectoryName(imageInfo.FSPath);
            saveAsDialog.OverwritePrompt = true;
            if (saveAsDialog.ShowDialog() == DialogResult.OK)
            {
                string saveName = DataAccess.PrivateAccessEnforced ? FileName.MangleFile(saveAsDialog.FileName) : saveAsDialog.FileName;
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
        void SaveTypeChanged(object sender, EventArgs e)
        {
            string str = (string)actionBox.SelectedItem;
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
                SetCutRectangle();
            }
            else
            {
                object o = Enum.Parse(typeof(InfoType), str);
                infoMode = o is InfoType;
                ToolMode = infoMode ? ToolMode.InfoImage : ToolMode.Basic;
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
