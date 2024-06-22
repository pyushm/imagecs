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
        bool privateAccessRequested;
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
        SaveType adjustmentType = SaveType.None;
        delegate void OnSearchClick();
        OnSearchClick onSearchClick;
        bool userAction = true;
        private TextBox directoryNameBox;
        private Button renameDirBtn;
        private TabPage tabPage3;
        private Button makePrivateBtn;
        private PictureBox runningSimilarIcon;
        private PictureBox runningInfoIcon;
        private Button findSimilarImagesBtn;
        private Button imageInfoBtn;
        private TextBox newTextBox;
        private Label label3;
        private TextBox imageSizeBox;
        private Button reduceButton;
        private TextBox renameResultBox;
        private Label label5;
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
            this.label5 = new System.Windows.Forms.Label();
            this.renameResultBox = new System.Windows.Forms.TextBox();
            this.newTextBox = new System.Windows.Forms.TextBox();
            this.directoryNameBox = new System.Windows.Forms.TextBox();
            this.renameDirBtn = new System.Windows.Forms.Button();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.label3 = new System.Windows.Forms.Label();
            this.imageSizeBox = new System.Windows.Forms.TextBox();
            this.reduceButton = new System.Windows.Forms.Button();
            this.makePrivateBtn = new System.Windows.Forms.Button();
            this.runningSimilarIcon = new System.Windows.Forms.PictureBox();
            this.runningInfoIcon = new System.Windows.Forms.PictureBox();
            this.findSimilarImagesBtn = new System.Windows.Forms.Button();
            this.imageInfoBtn = new System.Windows.Forms.Button();
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
            this.infoImagePanel.Location = new System.Drawing.Point(484, 464);
            this.infoImagePanel.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.infoImagePanel.Name = "infoImagePanel";
            this.infoImagePanel.Size = new System.Drawing.Size(282, 931);
            this.infoImagePanel.TabIndex = 7;
            this.infoImagePanel.DoubleClick += new System.EventHandler(this.infoImagePanel_DoubleClick);
            // 
            // outputList
            // 
            this.outputList.ItemHeight = 25;
            this.outputList.Location = new System.Drawing.Point(788, 464);
            this.outputList.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.outputList.Name = "outputList";
            this.outputList.Size = new System.Drawing.Size(476, 929);
            this.outputList.TabIndex = 8;
            this.outputList.SelectedIndexChanged += new System.EventHandler(this.DisplayFoundItem);
            this.outputList.DoubleClick += new System.EventHandler(this.ActivateFoundItem);
            this.outputList.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnListBoxMouseMove);
            // 
            // locationTreeView
            // 
            this.locationTreeView.Location = new System.Drawing.Point(16, 63);
            this.locationTreeView.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.locationTreeView.Name = "locationTreeView";
            this.locationTreeView.ShowNodeToolTips = true;
            this.locationTreeView.Size = new System.Drawing.Size(444, 1332);
            this.locationTreeView.TabIndex = 10;
            this.locationTreeView.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.RetrievNodes);
            this.locationTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.DisplaySelectedNode);
            this.locationTreeView.Click += new System.EventHandler(this.locationTreeView_Click);
            // 
            // addPrefixButton
            // 
            this.addPrefixButton.Location = new System.Drawing.Point(483, 89);
            this.addPrefixButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.addPrefixButton.Name = "addPrefixButton";
            this.addPrefixButton.Size = new System.Drawing.Size(228, 40);
            this.addPrefixButton.TabIndex = 15;
            this.addPrefixButton.Text = "Add prefix";
            // 
            // oldTextBox
            // 
            this.oldTextBox.Location = new System.Drawing.Point(7, 152);
            this.oldTextBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.oldTextBox.Name = "oldTextBox";
            this.oldTextBox.Size = new System.Drawing.Size(355, 31);
            this.oldTextBox.TabIndex = 5;
            // 
            // changeNameButton
            // 
            this.changeNameButton.Location = new System.Drawing.Point(95, 89);
            this.changeNameButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.changeNameButton.Name = "changeNameButton";
            this.changeNameButton.Size = new System.Drawing.Size(244, 40);
            this.changeNameButton.TabIndex = 22;
            this.changeNameButton.Text = "Change part of name";
            this.changeNameButton.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // outputBox
            // 
            this.outputBox.Location = new System.Drawing.Point(16, 15);
            this.outputBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.outputBox.Name = "outputBox";
            this.outputBox.Size = new System.Drawing.Size(1246, 31);
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
            this.findNameButton.Location = new System.Drawing.Point(68, 11);
            this.findNameButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.findNameButton.Name = "findNameButton";
            this.findNameButton.Size = new System.Drawing.Size(171, 40);
            this.findNameButton.TabIndex = 0;
            this.findNameButton.Text = "Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 121);
            this.label2.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(68, 25);
            this.label2.TabIndex = 28;
            this.label2.Text = "Name";
            // 
            // patternBox
            // 
            this.patternBox.Location = new System.Drawing.Point(88, 115);
            this.patternBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.patternBox.Name = "patternBox";
            this.patternBox.Size = new System.Drawing.Size(328, 31);
            this.patternBox.TabIndex = 20;
            this.patternBox.TextChanged += new System.EventHandler(this.patternBox_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 172);
            this.label4.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(96, 25);
            this.label4.TabIndex = 29;
            this.label4.Text = "Days old";
            // 
            // daysBox
            // 
            this.daysBox.Location = new System.Drawing.Point(120, 165);
            this.daysBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.daysBox.Name = "daysBox";
            this.daysBox.Size = new System.Drawing.Size(68, 31);
            this.daysBox.TabIndex = 27;
            this.daysBox.TextChanged += new System.EventHandler(this.patternBox_TextChanged);
            // 
            // runningImage
            // 
            this.runningImage.Image = global::ImageProcessor.Properties.Resources.wspinner_1_;
            this.runningImage.Location = new System.Drawing.Point(19, 21);
            this.runningImage.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.runningImage.Name = "runningImage";
            this.runningImage.Size = new System.Drawing.Size(32, 31);
            this.runningImage.TabIndex = 31;
            this.runningImage.TabStop = false;
            this.runningImage.Visible = false;
            // 
            // searchResultBox
            // 
            this.searchResultBox.Location = new System.Drawing.Point(12, 225);
            this.searchResultBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.searchResultBox.Multiline = true;
            this.searchResultBox.Name = "searchResultBox";
            this.searchResultBox.ReadOnly = true;
            this.searchResultBox.Size = new System.Drawing.Size(404, 104);
            this.searchResultBox.TabIndex = 34;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(476, 65);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(792, 394);
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
            this.tabPage1.Location = new System.Drawing.Point(8, 39);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.tabPage1.Size = new System.Drawing.Size(776, 347);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Search";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // displayResultsBtn
            // 
            this.displayResultsBtn.Location = new System.Drawing.Point(212, 164);
            this.displayResultsBtn.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.displayResultsBtn.Name = "displayResultsBtn";
            this.displayResultsBtn.Size = new System.Drawing.Size(208, 40);
            this.displayResultsBtn.TabIndex = 39;
            this.displayResultsBtn.Text = "Display Names";
            // 
            // findLookBtn
            // 
            this.findLookBtn.Location = new System.Drawing.Point(251, 64);
            this.findLookBtn.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.findLookBtn.Name = "findLookBtn";
            this.findLookBtn.Size = new System.Drawing.Size(171, 40);
            this.findLookBtn.TabIndex = 38;
            this.findLookBtn.Text = "Looks like";
            // 
            // findSoundBtn
            // 
            this.findSoundBtn.Location = new System.Drawing.Point(68, 64);
            this.findSoundBtn.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.findSoundBtn.Name = "findSoundBtn";
            this.findSoundBtn.Size = new System.Drawing.Size(171, 40);
            this.findSoundBtn.TabIndex = 37;
            this.findSoundBtn.Text = "Sound like";
            // 
            // findFileBtn
            // 
            this.findFileBtn.Location = new System.Drawing.Point(251, 11);
            this.findFileBtn.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.findFileBtn.Name = "findFileBtn";
            this.findFileBtn.Size = new System.Drawing.Size(171, 40);
            this.findFileBtn.TabIndex = 36;
            this.findFileBtn.Text = "File";
            // 
            // findImagePanel
            // 
            this.findImagePanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.findImagePanel.Location = new System.Drawing.Point(432, 6);
            this.findImagePanel.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.findImagePanel.Name = "findImagePanel";
            this.findImagePanel.Size = new System.Drawing.Size(343, 331);
            this.findImagePanel.TabIndex = 35;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.renameResultBox);
            this.tabPage2.Controls.Add(this.newTextBox);
            this.tabPage2.Controls.Add(this.directoryNameBox);
            this.tabPage2.Controls.Add(this.renameDirBtn);
            this.tabPage2.Controls.Add(this.oldTextBox);
            this.tabPage2.Controls.Add(this.addPrefixButton);
            this.tabPage2.Controls.Add(this.changeNameButton);
            this.tabPage2.Location = new System.Drawing.Point(8, 39);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.tabPage2.Size = new System.Drawing.Size(776, 347);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Rename";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(368, 159);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(48, 25);
            this.label5.TabIndex = 42;
            this.label5.Text = "==>";
            // 
            // renameResultBox
            // 
            this.renameResultBox.Location = new System.Drawing.Point(5, 198);
            this.renameResultBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.renameResultBox.Multiline = true;
            this.renameResultBox.Name = "renameResultBox";
            this.renameResultBox.ReadOnly = true;
            this.renameResultBox.Size = new System.Drawing.Size(771, 125);
            this.renameResultBox.TabIndex = 40;
            // 
            // newTextBox
            // 
            this.newTextBox.Location = new System.Drawing.Point(420, 152);
            this.newTextBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.newTextBox.Name = "newTextBox";
            this.newTextBox.Size = new System.Drawing.Size(355, 31);
            this.newTextBox.TabIndex = 37;
            // 
            // directoryNameBox
            // 
            this.directoryNameBox.Location = new System.Drawing.Point(235, 31);
            this.directoryNameBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.directoryNameBox.Name = "directoryNameBox";
            this.directoryNameBox.Size = new System.Drawing.Size(540, 31);
            this.directoryNameBox.TabIndex = 36;
            // 
            // renameDirBtn
            // 
            this.renameDirBtn.Location = new System.Drawing.Point(3, 29);
            this.renameDirBtn.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.renameDirBtn.Name = "renameDirBtn";
            this.renameDirBtn.Size = new System.Drawing.Size(232, 40);
            this.renameDirBtn.TabIndex = 35;
            this.renameDirBtn.Text = "New directory name";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.label3);
            this.tabPage3.Controls.Add(this.imageSizeBox);
            this.tabPage3.Controls.Add(this.reduceButton);
            this.tabPage3.Controls.Add(this.makePrivateBtn);
            this.tabPage3.Controls.Add(this.runningSimilarIcon);
            this.tabPage3.Controls.Add(this.runningInfoIcon);
            this.tabPage3.Controls.Add(this.findSimilarImagesBtn);
            this.tabPage3.Controls.Add(this.imageInfoBtn);
            this.tabPage3.Location = new System.Drawing.Point(8, 39);
            this.tabPage3.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(776, 347);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Processes";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(524, 206);
            this.label3.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(44, 36);
            this.label3.TabIndex = 41;
            this.label3.Text = "pix";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // imageSizeBox
            // 
            this.imageSizeBox.Location = new System.Drawing.Point(392, 206);
            this.imageSizeBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.imageSizeBox.Name = "imageSizeBox";
            this.imageSizeBox.Size = new System.Drawing.Size(116, 31);
            this.imageSizeBox.TabIndex = 40;
            this.imageSizeBox.Text = "2000";
            this.imageSizeBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // reduceButton
            // 
            this.reduceButton.Location = new System.Drawing.Point(69, 204);
            this.reduceButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.reduceButton.Name = "reduceButton";
            this.reduceButton.Size = new System.Drawing.Size(261, 39);
            this.reduceButton.TabIndex = 42;
            this.reduceButton.Text = "Resize to";
            this.reduceButton.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // makePrivateBtn
            // 
            this.makePrivateBtn.Location = new System.Drawing.Point(69, 135);
            this.makePrivateBtn.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.makePrivateBtn.Name = "makePrivateBtn";
            this.makePrivateBtn.Size = new System.Drawing.Size(261, 40);
            this.makePrivateBtn.TabIndex = 39;
            this.makePrivateBtn.Text = "Convert to private";
            // 
            // runningSimilarIcon
            // 
            this.runningSimilarIcon.Image = global::ImageProcessor.Properties.Resources.wspinner_1_;
            this.runningSimilarIcon.Location = new System.Drawing.Point(27, 86);
            this.runningSimilarIcon.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.runningSimilarIcon.Name = "runningSimilarIcon";
            this.runningSimilarIcon.Size = new System.Drawing.Size(32, 31);
            this.runningSimilarIcon.TabIndex = 38;
            this.runningSimilarIcon.TabStop = false;
            this.runningSimilarIcon.Visible = false;
            // 
            // runningInfoIcon
            // 
            this.runningInfoIcon.Image = global::ImageProcessor.Properties.Resources.wspinner_1_;
            this.runningInfoIcon.Location = new System.Drawing.Point(27, 35);
            this.runningInfoIcon.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.runningInfoIcon.Name = "runningInfoIcon";
            this.runningInfoIcon.Size = new System.Drawing.Size(32, 31);
            this.runningInfoIcon.TabIndex = 37;
            this.runningInfoIcon.TabStop = false;
            this.runningInfoIcon.Visible = false;
            // 
            // findSimilarImagesBtn
            // 
            this.findSimilarImagesBtn.Location = new System.Drawing.Point(69, 82);
            this.findSimilarImagesBtn.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.findSimilarImagesBtn.Name = "findSimilarImagesBtn";
            this.findSimilarImagesBtn.Size = new System.Drawing.Size(261, 40);
            this.findSimilarImagesBtn.TabIndex = 36;
            this.findSimilarImagesBtn.Text = "Find similar images";
            // 
            // imageInfoBtn
            // 
            this.imageInfoBtn.Location = new System.Drawing.Point(69, 31);
            this.imageInfoBtn.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.imageInfoBtn.Name = "imageInfoBtn";
            this.imageInfoBtn.Size = new System.Drawing.Size(261, 40);
            this.imageInfoBtn.TabIndex = 35;
            this.imageInfoBtn.Text = "Update ImageInfo";
            // 
            // NavigatorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1261, 1411);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.outputBox);
            this.Controls.Add(this.locationTreeView);
            this.Controls.Add(this.outputList);
            this.Controls.Add(this.infoImagePanel);
            this.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.MaximumSize = new System.Drawing.Size(1287, 1627);
            this.MinimumSize = new System.Drawing.Size(1287, 1300);
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
        public NavigatorForm(bool accessRequested = false)              
		{
            try
            {
                navigator = new Navigator();
                navigator.onNewImageSelection = NewImageSelected;
                InitializeComponent();
                privateAccessRequested = accessRequested;
                if (accessRequested)
                    RequestPassword();
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
                makePrivateBtn.Click += (object s, EventArgs e) => ConvertToPrivate();
                findSimilarImagesBtn.Click += (object s, EventArgs e) => { runningSimilarIcon.Visible = true; similarImagesWorker.RunWorkerAsync(); findSimilarImagesBtn.Enabled = false; }; ;
                locationTreeView.DoubleClick += (object s, EventArgs e) => { if (selectedNode != null) ShowImageListForm(selectedNode); };
                displayResultsBtn.Click += (object o, EventArgs e) => { onSearchClick?.Invoke(); };

                TreeNode nodeRoot = locationTreeView.Nodes.Add(Navigator.Root.Name);
                nodeRoot.Tag = Navigator.Root;
                nodeRoot.Nodes.Add("fake");
                itemInfoImages = new DirectoryInfoImages(locationTreeView, infoImagePanel, Enum.GetName(typeof(SpecName), SpecName.AllDevicy));
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
            patternBox.Text = FileName.UnMangle(Path.GetFileNameWithoutExtension(imagePath));
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
            if (Navigator.IsSpecDir(dii.Parent, SpecName.AllDevicy))
                dirList = dii.GetDirectories();
            else if (Navigator.IsSpecDir(dii.Parent.Parent, SpecName.AllDevicy))
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
            ImageListForm sif = new ImageListForm(searchRoot, navigator.GetMatchedDirNames(), navigator);
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
            makePrivateBtn.Enabled = state;
            addPrefixButton.Enabled = state;
        }
        void ResizeImages()                 
        {
            if (selectedNode == null || !fileManager.SetResizeModifyers(imageSizeBox.Text, 1000))
                return;
            adjustmentType = SaveType.LimitSize;
            imageSizeBox.Text = "";
            OperationButtonsEnabled(false);
            imageAdjustmentWorker.RunWorkerAsync(); // calls ImageAdjustment
        }
        void ConvertToPrivate()                  
        {
            if (selectedNode == null )
                return;
            adjustmentType = SaveType.Private;
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
            adjustmentType = SaveType.None;
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
                fna[i] = FileName.UnMangle(dia[i].Name);
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
            //string allName = Navigator.AllDevicy.Name;
            ImageListForm sif = new ImageListForm(di, navigator);
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
                dirPath = FileName.MangleFile(dirPath);
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
                filePath = FileName.MangleFile(filePath);
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
                    else if (dt.Is(DataType.MOV))
                        Process.Start(navigator.MediaExe, '\"' + dt.FSPath + '\"');
                    else if (dt.Is(DataType.EncMOV))
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
                searchResultBox.Text = (dirOnly ? matchedDirs.Count + " names" : fileCount + " items") + " in " + searchRoot.FullName + 
                    Environment.NewLine + res;
            }
            else
            {
                searchResultBox.Text = "No items in " + searchRoot.FullName + Environment.NewLine + res;
            }
            SetViewButtonState(SearchState.Display);
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
                //if (ex.Message.Contains("encrypted"))
                //{
                //    PasswordDialog pd = new PasswordDialog();
                //    pd.Show();
                //}
                //else
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
            if (e.KeyCode == Keys.Enter && privateAccessRequested)
            {
                bool ok = DataAccess.AllowPrivateAccess(outputBox.Text);
                outputBox.PasswordChar = '\0';
                if (ok)
                {
                    outputBox.Text = "";
                    privateAccessRequested = false;
                }
                else
                {
                    outputBox.Text = "Wrong password";
                    outputBox.ForeColor = System.Drawing.Color.Red;
                }
            }
        }
        void outputBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (privateAccessRequested && outputBox.Text == passwordText)
            {
                outputBox.PasswordChar = '\u25CF';
                outputBox.Text = "";
                outputBox.ForeColor = System.Drawing.Color.Black;
            }
        }
        private void locationTreeView_Click(object sender, EventArgs e)
        {
            itemInfoImages.HideInfoImages();
        }
        private void RequestPassword()
        {
            privateAccessRequested = true;
            outputBox.Text = passwordText;
            outputBox.ForeColor = Color.Red;
            outputBox.PasswordChar = '\0';
        }
    }
}
