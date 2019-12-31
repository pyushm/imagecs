using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Drawing;

namespace ImageProcessor
{
    public class NavigatorForm : Form
	{
        enum SearchState
        {
            Stop,
            Display
        }
        private Container components = null;
        List<Form> invoked = new List<Form>();
		Button addPrefixButton;
        Button changeNameButton;
        ListBox outputList;
		TextBox outputBox;
        TextBox oldTextBox;
		Panel infoImagePanel;
		TreeView locationTreeView;

		Navigator navigator;				// object handling directory tree
		DirectoryInfo selectedNode=null;	// currently selected directory
        string processNodeName = "";
        FileManager fileManager;            // resize and rename images
        DirectoryInfoImages itemInfoImages;	// shows info images
        string passwordText = "Enter password";
        BackgroundWorker searchWorker;
        BackgroundWorker imageAdjustmentWorker;
        BackgroundWorker infoWorker;
        BackgroundWorker similarImagesWorker;
        ToolTip toolTip1;
        SearchResult matchingItems;
        Navigator.SearchMode searchMode;
        DirectoryInfo searchRoot;
        DirectoryInfo infoImageDir = null;
        private Button findNameButton;
        private Label label2;
        private TextBox patternBox;
        private Label label4;
        private TextBox daysBox;
        private PictureBox runningImage;
        private TextBox searchResultBox;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private Panel findImagePanel;
        private Button findLookBtn;
        private Button findSoundBtn;
        private Button findFileBtn;
        private Button displayResultsBtn;
        string searchImagePath="";
        ImageAdjustmentType adjustmentType = ImageAdjustmentType.None;
        delegate void OnSearchClick();
        OnSearchClick onSearchClick;
        bool userAction = true;
        private TextBox directoryNameBox;
        private Button renameDirBtn;
        private TabPage tabPage3;
        private Button mangleBtn;
        private PictureBox runningSimilarIcon;
        private PictureBox runningInfoIcon;
        private Button findSimilarImagesBtn;
        private Button imageInfoBtn;
        private TextBox newTextBox;
        private Label label5;
        private Label label1;
        private Label label3;
        private TextBox imageSizeBox;
        private Button reduceButton;
        private TextBox renameResultBox;
        private CheckBox pravateAccessBox;
        Dictionary<string, string[]> matchingImages = new Dictionary<string, string[]>();
        protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.infoImagePanel = new System.Windows.Forms.Panel();
            this.outputList = new System.Windows.Forms.ListBox();
            this.locationTreeView = new System.Windows.Forms.TreeView();
            this.addPrefixButton = new System.Windows.Forms.Button();
            this.oldTextBox = new System.Windows.Forms.TextBox();
            this.changeNameButton = new System.Windows.Forms.Button();
            this.outputBox = new System.Windows.Forms.TextBox();
            this.searchWorker = new System.ComponentModel.BackgroundWorker();
            this.findNameButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.patternBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.daysBox = new System.Windows.Forms.TextBox();
            this.runningImage = new System.Windows.Forms.PictureBox();
            this.searchResultBox = new System.Windows.Forms.TextBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.displayResultsBtn = new System.Windows.Forms.Button();
            this.findLookBtn = new System.Windows.Forms.Button();
            this.findSoundBtn = new System.Windows.Forms.Button();
            this.findFileBtn = new System.Windows.Forms.Button();
            this.findImagePanel = new System.Windows.Forms.Panel();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.renameResultBox = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.newTextBox = new System.Windows.Forms.TextBox();
            this.directoryNameBox = new System.Windows.Forms.TextBox();
            this.renameDirBtn = new System.Windows.Forms.Button();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.label3 = new System.Windows.Forms.Label();
            this.imageSizeBox = new System.Windows.Forms.TextBox();
            this.reduceButton = new System.Windows.Forms.Button();
            this.mangleBtn = new System.Windows.Forms.Button();
            this.runningSimilarIcon = new System.Windows.Forms.PictureBox();
            this.runningInfoIcon = new System.Windows.Forms.PictureBox();
            this.findSimilarImagesBtn = new System.Windows.Forms.Button();
            this.imageInfoBtn = new System.Windows.Forms.Button();
            this.pravateAccessBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.runningImage)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.runningSimilarIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.runningInfoIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // infoImagePanel
            // 
            this.infoImagePanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.infoImagePanel.Location = new System.Drawing.Point(242, 241);
            this.infoImagePanel.Name = "infoImagePanel";
            this.infoImagePanel.Size = new System.Drawing.Size(142, 485);
            this.infoImagePanel.TabIndex = 7;
            this.infoImagePanel.DoubleClick += new System.EventHandler(this.infoImagePanel_DoubleClick);
            // 
            // outputList
            // 
            this.outputList.Location = new System.Drawing.Point(394, 241);
            this.outputList.Name = "outputList";
            this.outputList.Size = new System.Drawing.Size(240, 485);
            this.outputList.TabIndex = 8;
            this.outputList.SelectedIndexChanged += new System.EventHandler(this.DisplayFoundItem);
            this.outputList.DoubleClick += new System.EventHandler(this.ActivateFoundItem);
            this.outputList.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnListBoxMouseMove);
            // 
            // locationTreeView
            // 
            this.locationTreeView.Location = new System.Drawing.Point(8, 32);
            this.locationTreeView.Name = "locationTreeView";
            this.locationTreeView.ShowNodeToolTips = true;
            this.locationTreeView.Size = new System.Drawing.Size(224, 694);
            this.locationTreeView.TabIndex = 10;
            this.locationTreeView.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.RetrievNodes);
            this.locationTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.DisplaySelectedNode);
            this.locationTreeView.Click += new System.EventHandler(this.locationTreeView_Click);
            // 
            // addPrefixButton
            // 
            this.addPrefixButton.Location = new System.Drawing.Point(6, 46);
            this.addPrefixButton.Name = "addPrefixButton";
            this.addPrefixButton.Size = new System.Drawing.Size(125, 21);
            this.addPrefixButton.TabIndex = 15;
            this.addPrefixButton.Text = "Add prefix";
            // 
            // oldTextBox
            // 
            this.oldTextBox.Location = new System.Drawing.Point(137, 47);
            this.oldTextBox.Name = "oldTextBox";
            this.oldTextBox.Size = new System.Drawing.Size(182, 20);
            this.oldTextBox.TabIndex = 5;
            // 
            // changeNameButton
            // 
            this.changeNameButton.Location = new System.Drawing.Point(6, 79);
            this.changeNameButton.Name = "changeNameButton";
            this.changeNameButton.Size = new System.Drawing.Size(125, 21);
            this.changeNameButton.TabIndex = 22;
            this.changeNameButton.Text = "Change part of name";
            this.changeNameButton.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // outputBox
            // 
            this.outputBox.Location = new System.Drawing.Point(139, 8);
            this.outputBox.Name = "outputBox";
            this.outputBox.Size = new System.Drawing.Size(495, 20);
            this.outputBox.TabIndex = 19;
            this.outputBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.outputBox_KeyDown);
            this.outputBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.outputBox_MouseDown);
            // 
            // searchWorker
            // 
            this.searchWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.StartSearch);
            this.searchWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.SearchCompleted);
            // 
            // findNameButton
            // 
            this.findNameButton.Location = new System.Drawing.Point(34, 6);
            this.findNameButton.Name = "findNameButton";
            this.findNameButton.Size = new System.Drawing.Size(85, 21);
            this.findNameButton.TabIndex = 0;
            this.findNameButton.Text = "Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 63);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 28;
            this.label2.Text = "Name";
            // 
            // patternBox
            // 
            this.patternBox.Location = new System.Drawing.Point(44, 60);
            this.patternBox.Name = "patternBox";
            this.patternBox.Size = new System.Drawing.Size(166, 20);
            this.patternBox.TabIndex = 20;
            this.patternBox.TextChanged += new System.EventHandler(this.patternBox_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 90);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(48, 13);
            this.label4.TabIndex = 29;
            this.label4.Text = "Days old";
            // 
            // daysBox
            // 
            this.daysBox.Location = new System.Drawing.Point(60, 86);
            this.daysBox.Name = "daysBox";
            this.daysBox.Size = new System.Drawing.Size(36, 20);
            this.daysBox.TabIndex = 27;
            this.daysBox.TextChanged += new System.EventHandler(this.patternBox_TextChanged);
            // 
            // runningImage
            // 
            this.runningImage.Image = global::ImageProcessor.Properties.Resources.wspinner_1_;
            this.runningImage.Location = new System.Drawing.Point(9, 11);
            this.runningImage.Name = "runningImage";
            this.runningImage.Size = new System.Drawing.Size(16, 16);
            this.runningImage.TabIndex = 31;
            this.runningImage.TabStop = false;
            this.runningImage.Visible = false;
            // 
            // searchResultBox
            // 
            this.searchResultBox.Location = new System.Drawing.Point(6, 117);
            this.searchResultBox.Multiline = true;
            this.searchResultBox.Name = "searchResultBox";
            this.searchResultBox.ReadOnly = true;
            this.searchResultBox.Size = new System.Drawing.Size(204, 56);
            this.searchResultBox.TabIndex = 34;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(238, 34);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(396, 205);
            this.tabControl1.TabIndex = 25;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.displayResultsBtn);
            this.tabPage1.Controls.Add(this.findLookBtn);
            this.tabPage1.Controls.Add(this.findSoundBtn);
            this.tabPage1.Controls.Add(this.findFileBtn);
            this.tabPage1.Controls.Add(this.findImagePanel);
            this.tabPage1.Controls.Add(this.searchResultBox);
            this.tabPage1.Controls.Add(this.runningImage);
            this.tabPage1.Controls.Add(this.findNameButton);
            this.tabPage1.Controls.Add(this.daysBox);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.patternBox);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(388, 179);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Search";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // displayResultsBtn
            // 
            this.displayResultsBtn.Location = new System.Drawing.Point(106, 85);
            this.displayResultsBtn.Name = "displayResultsBtn";
            this.displayResultsBtn.Size = new System.Drawing.Size(104, 21);
            this.displayResultsBtn.TabIndex = 39;
            this.displayResultsBtn.Text = "Display Names";
            // 
            // findLookBtn
            // 
            this.findLookBtn.Location = new System.Drawing.Point(125, 33);
            this.findLookBtn.Name = "findLookBtn";
            this.findLookBtn.Size = new System.Drawing.Size(85, 21);
            this.findLookBtn.TabIndex = 38;
            this.findLookBtn.Text = "Looks like";
            // 
            // findSoundBtn
            // 
            this.findSoundBtn.Location = new System.Drawing.Point(34, 33);
            this.findSoundBtn.Name = "findSoundBtn";
            this.findSoundBtn.Size = new System.Drawing.Size(85, 21);
            this.findSoundBtn.TabIndex = 37;
            this.findSoundBtn.Text = "Sound like";
            // 
            // findFileBtn
            // 
            this.findFileBtn.Location = new System.Drawing.Point(125, 6);
            this.findFileBtn.Name = "findFileBtn";
            this.findFileBtn.Size = new System.Drawing.Size(85, 21);
            this.findFileBtn.TabIndex = 36;
            this.findFileBtn.Text = "File";
            // 
            // findImagePanel
            // 
            this.findImagePanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.findImagePanel.Location = new System.Drawing.Point(216, 3);
            this.findImagePanel.Name = "findImagePanel";
            this.findImagePanel.Size = new System.Drawing.Size(173, 173);
            this.findImagePanel.TabIndex = 35;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.renameResultBox);
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.label1);
            this.tabPage2.Controls.Add(this.newTextBox);
            this.tabPage2.Controls.Add(this.directoryNameBox);
            this.tabPage2.Controls.Add(this.renameDirBtn);
            this.tabPage2.Controls.Add(this.oldTextBox);
            this.tabPage2.Controls.Add(this.addPrefixButton);
            this.tabPage2.Controls.Add(this.changeNameButton);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(388, 179);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Rename";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // renameResultBox
            // 
            this.renameResultBox.Location = new System.Drawing.Point(6, 106);
            this.renameResultBox.Multiline = true;
            this.renameResultBox.Name = "renameResultBox";
            this.renameResultBox.ReadOnly = true;
            this.renameResultBox.Size = new System.Drawing.Size(376, 67);
            this.renameResultBox.TabIndex = 40;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(325, 79);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(57, 21);
            this.label5.TabIndex = 39;
            this.label5.Text = "new";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(325, 48);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 21);
            this.label1.TabIndex = 38;
            this.label1.Text = "old";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // newTextBox
            // 
            this.newTextBox.Location = new System.Drawing.Point(137, 79);
            this.newTextBox.Name = "newTextBox";
            this.newTextBox.Size = new System.Drawing.Size(182, 20);
            this.newTextBox.TabIndex = 37;
            // 
            // directoryNameBox
            // 
            this.directoryNameBox.Location = new System.Drawing.Point(137, 16);
            this.directoryNameBox.Name = "directoryNameBox";
            this.directoryNameBox.Size = new System.Drawing.Size(245, 20);
            this.directoryNameBox.TabIndex = 36;
            // 
            // renameDirBtn
            // 
            this.renameDirBtn.Location = new System.Drawing.Point(6, 15);
            this.renameDirBtn.Name = "renameDirBtn";
            this.renameDirBtn.Size = new System.Drawing.Size(125, 21);
            this.renameDirBtn.TabIndex = 35;
            this.renameDirBtn.Text = "Change directory name";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.label3);
            this.tabPage3.Controls.Add(this.imageSizeBox);
            this.tabPage3.Controls.Add(this.reduceButton);
            this.tabPage3.Controls.Add(this.mangleBtn);
            this.tabPage3.Controls.Add(this.runningSimilarIcon);
            this.tabPage3.Controls.Add(this.runningInfoIcon);
            this.tabPage3.Controls.Add(this.findSimilarImagesBtn);
            this.tabPage3.Controls.Add(this.imageInfoBtn);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(388, 179);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Processes";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(262, 107);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(22, 19);
            this.label3.TabIndex = 41;
            this.label3.Text = "pix";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // imageSizeBox
            // 
            this.imageSizeBox.Location = new System.Drawing.Point(196, 107);
            this.imageSizeBox.Name = "imageSizeBox";
            this.imageSizeBox.Size = new System.Drawing.Size(60, 20);
            this.imageSizeBox.TabIndex = 40;
            this.imageSizeBox.Text = "2000";
            this.imageSizeBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // reduceButton
            // 
            this.reduceButton.Location = new System.Drawing.Point(35, 106);
            this.reduceButton.Name = "reduceButton";
            this.reduceButton.Size = new System.Drawing.Size(131, 20);
            this.reduceButton.TabIndex = 42;
            this.reduceButton.Text = "Resize to";
            this.reduceButton.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // mangleBtn
            // 
            this.mangleBtn.Location = new System.Drawing.Point(35, 70);
            this.mangleBtn.Name = "mangleBtn";
            this.mangleBtn.Size = new System.Drawing.Size(131, 21);
            this.mangleBtn.TabIndex = 39;
            this.mangleBtn.Text = "Mangle file names";
            // 
            // runningSimilarIcon
            // 
            this.runningSimilarIcon.Image = global::ImageProcessor.Properties.Resources.wspinner_1_;
            this.runningSimilarIcon.Location = new System.Drawing.Point(13, 45);
            this.runningSimilarIcon.Name = "runningSimilarIcon";
            this.runningSimilarIcon.Size = new System.Drawing.Size(16, 16);
            this.runningSimilarIcon.TabIndex = 38;
            this.runningSimilarIcon.TabStop = false;
            this.runningSimilarIcon.Visible = false;
            // 
            // runningInfoIcon
            // 
            this.runningInfoIcon.Image = global::ImageProcessor.Properties.Resources.wspinner_1_;
            this.runningInfoIcon.Location = new System.Drawing.Point(13, 18);
            this.runningInfoIcon.Name = "runningInfoIcon";
            this.runningInfoIcon.Size = new System.Drawing.Size(16, 16);
            this.runningInfoIcon.TabIndex = 37;
            this.runningInfoIcon.TabStop = false;
            this.runningInfoIcon.Visible = false;
            // 
            // findSimilarImagesBtn
            // 
            this.findSimilarImagesBtn.Location = new System.Drawing.Point(35, 43);
            this.findSimilarImagesBtn.Name = "findSimilarImagesBtn";
            this.findSimilarImagesBtn.Size = new System.Drawing.Size(131, 21);
            this.findSimilarImagesBtn.TabIndex = 36;
            this.findSimilarImagesBtn.Text = "Find similar images";
            // 
            // imageInfoBtn
            // 
            this.imageInfoBtn.Location = new System.Drawing.Point(35, 16);
            this.imageInfoBtn.Name = "imageInfoBtn";
            this.imageInfoBtn.Size = new System.Drawing.Size(131, 21);
            this.imageInfoBtn.TabIndex = 35;
            this.imageInfoBtn.Text = "Update ImageInfo";
            // 
            // pravateAccessBox
            // 
            this.pravateAccessBox.AutoSize = true;
            this.pravateAccessBox.Location = new System.Drawing.Point(8, 10);
            this.pravateAccessBox.Name = "pravateAccessBox";
            this.pravateAccessBox.Size = new System.Drawing.Size(125, 17);
            this.pravateAccessBox.TabIndex = 26;
            this.pravateAccessBox.Text = "Allow Private Access";
            this.pravateAccessBox.UseVisualStyleBackColor = true;
            this.pravateAccessBox.CheckedChanged += new System.EventHandler(this.pravateAccessBox_CheckedChanged);
            // 
            // NavigatorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(639, 734);
            this.Controls.Add(this.pravateAccessBox);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.outputBox);
            this.Controls.Add(this.locationTreeView);
            this.Controls.Add(this.outputList);
            this.Controls.Add(this.infoImagePanel);
            this.MaximumSize = new System.Drawing.Size(655, 875);
            this.MinimumSize = new System.Drawing.Size(655, 705);
            this.Name = "NavigatorForm";
            this.Text = "Image Selector";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.NavigatorForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.runningImage)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.runningSimilarIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.runningInfoIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion
        public NavigatorForm()              
		{
            try
            {
                navigator = new Navigator();
                navigator.onNewImageSelection = NewImageSelected;
                InitializeComponent();
                imageAdjustmentWorker = new BackgroundWorker();
                imageAdjustmentWorker.DoWork += new DoWorkEventHandler(ImageAdjustment);
                imageAdjustmentWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ImageAdjustmentCompleted);
                infoWorker = new BackgroundWorker();
                infoWorker.DoWork += new DoWorkEventHandler(StartInfoUpdate);
                infoWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(InfoUpdateCompleted);
                similarImagesWorker = new BackgroundWorker();
                similarImagesWorker.DoWork += new DoWorkEventHandler(FindSimilarImages);
                similarImagesWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(FindSimilarImagesCompleted);
                outputBox.ForeColor = Color.LightSalmon;
                findNameButton.Click += (object s, EventArgs e) => FindItems(Navigator.SearchMode.Name);
                findFileBtn.Click += (object s, EventArgs e) => FindItems(Navigator.SearchMode.File);
                findSoundBtn.Click += (object s, EventArgs e) => FindItems(Navigator.SearchMode.Sound);
                findLookBtn.Click += (object s, EventArgs e) => FindItems(Navigator.SearchMode.Image);
                reduceButton.Click += (object s, EventArgs e) => ResizeImages();
                changeNameButton.Click += (object s, EventArgs e) => Rename(RenameType.FileName);
                addPrefixButton.Click += (object s, EventArgs e) => Rename(RenameType.AddPrefix);
                renameDirBtn.Click += (object s, EventArgs e) => Rename(RenameType.Directory);
                imageInfoBtn.Click += (object s, EventArgs e) => { runningInfoIcon.Visible = true; infoWorker.RunWorkerAsync(); imageInfoBtn.Enabled = false; };
                mangleBtn.Click += (object s, EventArgs e) => MangleNames();
                findSimilarImagesBtn.Click += (object s, EventArgs e) => { runningSimilarIcon.Visible = true; similarImagesWorker.RunWorkerAsync(); findSimilarImagesBtn.Enabled = false; }; ;
                locationTreeView.DoubleClick += (object s, EventArgs e) => { if (selectedNode != null) ShowImageListForm(selectedNode); };
                displayResultsBtn.Click += (object o, EventArgs e) => { onSearchClick?.Invoke(); };

                TreeNode nodeRoot = locationTreeView.Nodes.Add(navigator.Root.Name);
                nodeRoot.Tag = navigator.Root;
                nodeRoot.Nodes.Add("fake");
                itemInfoImages = new DirectoryInfoImages(locationTreeView, infoImagePanel, navigator.AllDevicy.Name);
                findImagePanel.Paint += new PaintEventHandler(DrawSearchImage);
                findLookBtn.Enabled = false;
                fileManager = new FileManager(navigator);
                fileManager.notifyResults += new NotifyMessage(ShowResults);
                fileManager.notifyFinal += new NotifyMessages(ShowFinalResults);
                fileManager.notifyStatus += new NotifyMessage(ShowStatus);
                toolTip1 = new ToolTip();

                // Set up the delays for the ToolTip.
                toolTip1.AutoPopDelay = 5000;
                toolTip1.InitialDelay = 1000;
                toolTip1.ReshowDelay = 500;
                // Force the ToolTip text to be displayed whether or not the form is active.
                toolTip1.ShowAlways = true;
                EnableSearchButtons(false);
            }
            catch (Exception ex )
            {
                MessageBox.Show(ex.Message, "NavigatorForm");
            }
        }
        ~NavigatorForm()				    { Dispose(true); }
		void ShowResults(string message)	{ outputList.Items.Add(message); }
        void ShowFinalResults(List<string> messages)
        {
            foreach (string s in messages)
                outputList.Items.Add(s);
        }
        void ShowStatus(string message)		{ outputBox.Text=message; }
        void EnableSearchButtons(bool state)
        {
            findNameButton.Enabled = state;
            findFileBtn.Enabled = state;
            findSoundBtn.Enabled = state;
        }
        void SetViewButtonState(SearchState state)
        {
            switch (state)
            {
                case SearchState.Stop: onSearchClick = StopSearch; displayResultsBtn.Text = "Stop Search"; break;
                case SearchState.Display: onSearchClick = DisplayFoundItems; displayResultsBtn.Text = "Display Names"; break;
            }
        }
        void NewImageSelected(string imagePath)
        {
            searchImagePath = imagePath;
            findImagePanel.Invalidate();
            userAction = false;
            patternBox.Text = ImageFileName.UnMangleText(Path.GetFileNameWithoutExtension(imagePath));
            userAction = true;
        }
        void DrawSearchImage(object sender, PaintEventArgs e)
        {
            if (searchImagePath.Length == 0)
            {
                findLookBtn.Enabled = false;
                return;
            }
            try
            {
                ImageFileInfo ifi = new ImageFileInfo(new FileInfo(searchImagePath));
                Image im = ifi.SynchronizeThumbnail();
                float areaSize = 173 * e.Graphics.DpiX / 96;
                float scale = Math.Min(areaSize / im.Width, areaSize / im.Height);
                float iw = im.Width * scale;
                float ih = im.Height * scale; 
                float d = (iw - ih) / 2;
                PointF del = d < 0 ? new PointF(-d, 0) : new PointF(0, d);
                e.Graphics.DrawImage(im, del.X, del.Y, iw, ih);
                findLookBtn.Enabled = true;
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message); }
        }
        void FindItems(Navigator.SearchMode mode)
        {
            searchMode = mode;
            outputList.Items.Clear();
            if (patternBox.Text.Length == 0 && daysBox.Text.Length == 0 && searchMode != Navigator.SearchMode.Image)
                return;
            searchRoot = navigator.GetSearchRoot(outputBox.Text);
            runningImage.Visible = true;
            searchWorker.RunWorkerAsync();
            SetViewButtonState(SearchState.Stop);
        }
        void FindSimilarImages(object sender, DoWorkEventArgs e)
        {
            DirectoryInfo dii = new DirectoryInfo(outputBox.Text);
            DirectoryInfo[] dirList=null;
            if (navigator.IsAllDevicy(dii.Parent))
                dirList = dii.GetDirectories();
            else if (navigator.IsAllDevicy(dii.Parent.Parent))
                dirList = new DirectoryInfo[] { dii };
            foreach (DirectoryInfo dev in dirList)
            {
                FileInfo[] fia = dev.GetFiles();
                foreach (var fi in fia)
                {
                    //string[] matches = navigator.GenerateSearchList(Navigator.SearchMode.Image, navigator.Root, fi.FullName, "");
                    //if (matches.Length>0)
                    //    matchingImages.Add(fi.FullName, matches);
                }
            }
        }
        void FindSimilarImagesCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            outputList.Items.Clear();
            outputList.Items.Add("Find Similar Images Completed: " + processNodeName);
            processNodeName = "";
            runningSimilarIcon.Visible = false;
            findSimilarImagesBtn.Enabled = true;
        }
        void DisplayFoundItems()            
        {
            if (!searchRoot.Exists)
                return;
            ImageListForm sif = new ImageListForm(searchRoot, navigator.GetMatchingDirNames(), navigator);
            try
            {
                sif.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }        
        void StopSearch()                   { navigator.StopSearch = true; }
        void OperationButtonsEnabled(bool state)
        {
            reduceButton.Enabled = state;
            changeNameButton.Enabled = state;
            mangleBtn.Enabled = state;
            addPrefixButton.Enabled = state;
        }
        void ResizeImages()                 
        {
            if (selectedNode == null || !fileManager.SetResizeModifyers(imageSizeBox.Text, 1000))
                return;
            adjustmentType = ImageAdjustmentType.Resize;
            imageSizeBox.Text = "";
            OperationButtonsEnabled(false);
            imageAdjustmentWorker.RunWorkerAsync(); // calls ImageAdjustment
        }
        void MangleNames()                  
        {
            if (selectedNode == null )
                return;
            adjustmentType = ImageAdjustmentType.Mangle;
            OperationButtonsEnabled(false);
            imageAdjustmentWorker.RunWorkerAsync(); // calls ImageAdjustment
        }
        void Rename(RenameType operation)   
        {
            if (selectedNode == null)
                return;
            if (operation == RenameType.Directory)
            {
                fileManager.NewDirName = directoryNameBox.Text.Trim();
                if (fileManager.NewDirName.Length == 0)
                {
                    MessageBox.Show("New directory name has to be specified", "");
                    return;
                }
            }
            else
            {
                fileManager.TextToReplace = oldTextBox.Text.Trim();
                if (operation == RenameType.FileName && fileManager.TextToReplace.Length == 0)
                {
                    MessageBox.Show("Replacement text has to be specified", "");
                    return;
                }
                oldTextBox.Text = "";
                fileManager.TextReplacement = newTextBox.Text.Trim();
                if (operation == RenameType.AddPrefix && fileManager.TextReplacement.Length == 0)
                {
                    MessageBox.Show("Prefix has to be specified", "", MessageBoxButtons.YesNo);
                    return;
                }
                newTextBox.Text = "";
            }
            directoryNameBox.Text = "";
            OperationButtonsEnabled(false);
            fileManager.Rename(selectedNode, operation);
            OperationButtonsEnabled(true);
        }
        void ImageAdjustment(object sender, DoWorkEventArgs e)
        {
            processNodeName = selectedNode.Name;
            fileManager.ApplyAdjustmentRecursively(selectedNode, adjustmentType, false);
        }
        void ImageAdjustmentCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OperationButtonsEnabled(true);
            outputList.Items.Add("Rename Completed: " + processNodeName);
            processNodeName = "";
            adjustmentType = ImageAdjustmentType.None;
        }
        void OnListBoxMouseMove(object sender, MouseEventArgs e)
        {
            Control c = sender as Control;
            if (c == null || c.Name != "outputList")
                return;
            int nIdx = outputList.IndexFromPoint(e.Location);
            if ((nIdx >= 0) && (nIdx < outputList.Items.Count))
                toolTip1.SetToolTip(outputList, outputList.Items[nIdx].ToString());
        }
		void RetrievNodes(object sender, TreeViewCancelEventArgs e)
		{
			Cursor=Cursors.WaitCursor;
			TreeNode node=e.Node;
			node.Nodes.Clear();
			DirectoryInfo[] dia=navigator.GetDirectories(((DirectoryInfo)node.Tag));
            string[] fna = new string[dia.Length];
            for(int i=0; i<dia.Length; i++)
                fna[i] = ImageFileName.UnMangleText(dia[i].Name);
            Array.Sort(fna, dia, new ImageFileInfo.NameComparer());
            for (int i = 0; i < dia.Length; i++)
            {
                TreeNode subNode = node.Nodes.Add(fna[i]);
                subNode.Tag = dia[i];
                subNode.Nodes.Add("fake");
            }
            Cursor = Cursors.Default;
		}
		void DisplaySelectedNode(object sender, TreeViewEventArgs e)
		{
			if(e.Node==null || e.Node.Tag==null)
				return;
			selectedNode=(DirectoryInfo)e.Node.Tag;
            ImageDirInfo idf = new ImageDirInfo(selectedNode);
            outputBox.Text = idf.RealPath;
            if (selectedNode.Exists)
                itemInfoImages.ShowInfoImages(selectedNode);
            else
                outputBox.Text += " does NOT EXIST";
        }
        void ShowImageListForm(DirectoryInfo di)
		{
            if (di == null)
                return;
            if (!di.Exists)
            {
                MessageBox.Show("Directory " + di.FullName + " does not exist");
                return;
            }
            string allName = navigator.AllDevicy.Name;
            ImageListForm sif = new ImageListForm(di, di.FullName == navigator.DirInfo(DirName.NewArticles).FullName, navigator);
            invoked.Add(sif);
            try
            {
				sif.Show();
			}
			catch(Exception ex)
			{
                MessageBox.Show(ex.Message);
            }
		}
        FileSystemInfo selectedItem()               // both dir and image in human readable form 
        {
            if (outputList == null || outputList.SelectedItem == null || searchRoot == null)
                return null;
            SearchResult.MatchingFile mf = outputList.SelectedItem as SearchResult.MatchingFile;
            SearchResult.MatchingDir md = outputList.SelectedItem as SearchResult.MatchingDir;
            bool itemIsDir = md != null;
            if (!itemIsDir)
                md = mf?.MatchingDir;
            if (md == null)
                return null;
            string dirPath = Path.Combine(searchRoot.FullName, md.Name);
            DirectoryInfo di = new DirectoryInfo(dirPath);
            if (!di.Exists)
            {
                dirPath = ImageFileName.FSMangle(dirPath);
                di = new DirectoryInfo(dirPath);
                if (!di.Exists)
                    return null;
            }
            if (itemIsDir)
                return di;
            string filePath = Path.Combine(di.FullName, mf.Name);
            FileSystemInfo fi = new FileInfo(filePath);
            if (!fi.Exists)
            {
                filePath = ImageFileName.FSMangle(filePath);
                fi = new FileInfo(filePath);
            }
            return fi.Exists ? fi : null;
        }
        void DisplayFoundItem(object s, EventArgs e)
        {
            FileSystemInfo fsi = selectedItem();
            if (fsi == null)
                return;
            bool isDir = (fsi.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
            DirectoryInfo di = isDir ? (DirectoryInfo)fsi : ((FileInfo)fsi).Directory;
            infoImageDir = null;
            if (di.Exists)
            {
                itemInfoImages.ShowInfoImages(di);
                infoImageDir = di;
            }
        }
        void ActivateFoundItem(object s, EventArgs e)
		{
            FileSystemInfo fsi = selectedItem();
            if (fsi == null)
                return;
            try
            {
                if ((fsi.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    ShowImageListForm((DirectoryInfo)fsi);
                else
                {
                    FileInfo fi = (FileInfo)fsi;
                    ImageFileInfo dt = new ImageFileInfo(fi);
                    if (dt.IsImage)
                    {
                        ImageViewForm editForm = new ImageViewForm(null);
                        invoked.Add(editForm);
                        editForm.ShowNewImage(dt.FSPath);
                    }
                    else if (dt.IsUnencryptedVideo)
                        Process.Start(navigator.MediaExe, '\"' + dt.FSPath + '\"');
                    else if (dt.IsEncryptedVideo)
                    {
                        try
                        {
                            Cursor = Cursors.WaitCursor;
                            DataAccess.DecryptToFile(navigator.MediaTmpLocation, dt.FSPath);
                            Process.Start(navigator.MediaExe, navigator.MediaTmpLocation);
                        }
                        finally { Cursor = Cursors.Default; }
                    }
                }
            }
            catch { }
        }
        void StartSearch(object sender, DoWorkEventArgs e)
        {
            matchingItems = navigator.GenerateSearchList(searchMode, searchRoot, patternBox.Text, daysBox.Text);
        }
        void SearchCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (matchingItems == null)
                return;
            string res;
            if (patternBox.Text.Length > 0)
            {
                res = searchMode == Navigator.SearchMode.Name ? " matching name " : 
                    searchMode == Navigator.SearchMode.Sound ? " sound like " :
                    searchMode == Navigator.SearchMode.File ? " matching file name " :
                    searchMode == Navigator.SearchMode.Image ? " looks like " : "";
                res += patternBox.Text;
            }
            else
                res = searchMode == Navigator.SearchMode.File ? "Files " : "Directoris ";
            if (daysBox.Text.Length > 0)
                res += Environment.NewLine + "updated within " + daysBox.Text + " days";
            bool dirOnly = searchMode == Navigator.SearchMode.Name || searchMode == Navigator.SearchMode.Sound;
            int fileCount = 0;
            var matchedDirs = matchingItems.GetMatchedDirs();
            if (matchedDirs.Count > 0)
            {
                foreach (var matchingDir in matchedDirs)
                {
                    outputList.Items.Add(matchingDir);
                    if (!dirOnly)
                        foreach (var matchingFile in matchingDir.Files)
                        {
                            outputList.Items.Add(matchingFile);
                            fileCount++;
                        }
                }
                SetViewButtonState(SearchState.Display);
                searchResultBox.Text = (dirOnly ? matchedDirs.Count + " names" : fileCount + " items") + " in " + searchRoot.FullName + 
                    Environment.NewLine + res;
            }
            else
            {
                searchResultBox.Text = "No items in " + searchRoot.FullName + Environment.NewLine + res;
            }
            patternBox.Text = daysBox.Text = "";
            runningImage.Visible = false;
        }
        void StartInfoUpdate(object sender, DoWorkEventArgs e)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(outputBox.Text);
                if (di.Exists)
                    navigator.CreateImageHashes(di);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("encrypted"))
                {
                    PasswordDialog pd = new PasswordDialog();
                    pd.Show();
                }
                else
                    Debug.WriteLine(ex.Message);
            }
        }
        void InfoUpdateCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            outputBox.Text = "Info list created";
            runningInfoIcon.Visible = false;
            imageInfoBtn.Enabled = true;
        }
        void infoImagePanel_DoubleClick(object sender, EventArgs e)
        {
            if (infoImageDir != null)
                ShowImageListForm(infoImageDir);
        }
        void patternBox_TextChanged(object sender, EventArgs e)
        {
            string days = daysBox.Text;
            string search = patternBox.Text = patternBox.Text.Trim();
            if (userAction)
            {
                searchImagePath = "";
                findImagePanel.Invalidate();
            }
            EnableSearchButtons(search.Length + days.Length != 0);
        }
        void NavigatorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach(var form in invoked)
            {
                if (form != null && !form.IsDisposed)
                    form.Close();
            }
        }
        void outputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //DataCipher.ChangePassword(outputBox.Text, outputBox.Text);
                bool ok = DataAccess.AllowPrivateAccess(outputBox.Text);
                outputBox.PasswordChar = '\0';
                if (ok)
                    outputBox.Text = "";
                else
                {
                    outputBox.Text = "Wrong password";
                    outputBox.ForeColor = System.Drawing.Color.Red;
                }
            }
        }
        void outputBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (outputBox.Text == passwordText)
            {
                outputBox.Text = "";
                outputBox.ForeColor = System.Drawing.Color.Black;
                outputBox.PasswordChar = '\u25CF';
            }
        }
        private void locationTreeView_Click(object sender, EventArgs e)
        {
            itemInfoImages.HideInfoImages();
        }

        private void pravateAccessBox_CheckedChanged(object sender, EventArgs e)
        {
            if (pravateAccessBox.Checked)
            {
                outputBox.Text = passwordText;
                outputBox.ForeColor = System.Drawing.Color.Red;
                outputBox.PasswordChar = '\0';
            }
            else
            {
                DataAccess.PrivateAccessAllowed = false;
            }
        }
    }
}
