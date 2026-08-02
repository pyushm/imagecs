using System.ComponentModel;
using System.Diagnostics;

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
        public DisplayImageList Images { get; private set; } = null; // images to be displayed 
        Button addPrefixButton;
        Button changeNameButton;
        ListBox outputList;
        TextBox outputBox;
        TextBox oldTextBox;
        Panel infoImagePanel;
        TreeView locationTreeView;
        bool privateAccessRequested;
        Navigator navigator;                // object handling directory tree
        DirectoryInfo selectedNode = null;	// currently selected directory
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
        private Button findSoundBtn;
        private Button findFileBtn;
        private Button displayResultsBtn;
        string searchImagePath = "";
        Conversion conversion = Conversion.None;
        Conversion? pendingConversion = null; // queue conversion request here ifimageAdjustmentWorker is busy
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
        private Button reduceButton;
        private TextBox renameResultBox;
        private Label label5;
        private Button compressBtn;
        private ComboBox reduceSizeBox;
        private Button mangleCharButton;
        private Label label1;
        private RadioButton changedBtn;
        private RadioButton viewedBtn;
        Dictionary<string, string[]> matchingImages = new Dictionary<string, string[]>();
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
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            infoImagePanel = new Panel();
            outputList = new ListBox();
            locationTreeView = new TreeView();
            addPrefixButton = new Button();
            oldTextBox = new TextBox();
            changeNameButton = new Button();
            outputBox = new TextBox();
            searchWorker = new BackgroundWorker();
            findNameButton = new Button();
            label2 = new Label();
            patternBox = new TextBox();
            label4 = new Label();
            daysBox = new TextBox();
            runningImage = new PictureBox();
            searchResultBox = new TextBox();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            changedBtn = new RadioButton();
            viewedBtn = new RadioButton();
            label1 = new Label();
            displayResultsBtn = new Button();
            findSoundBtn = new Button();
            findFileBtn = new Button();
            findImagePanel = new Panel();
            tabPage2 = new TabPage();
            label5 = new Label();
            renameResultBox = new TextBox();
            newTextBox = new TextBox();
            directoryNameBox = new TextBox();
            renameDirBtn = new Button();
            tabPage3 = new TabPage();
            mangleCharButton = new Button();
            reduceSizeBox = new ComboBox();
            compressBtn = new Button();
            reduceButton = new Button();
            makePrivateBtn = new Button();
            runningSimilarIcon = new PictureBox();
            runningInfoIcon = new PictureBox();
            findSimilarImagesBtn = new Button();
            imageInfoBtn = new Button();
            ((ISupportInitialize)runningImage).BeginInit();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            tabPage3.SuspendLayout();
            ((ISupportInitialize)runningSimilarIcon).BeginInit();
            ((ISupportInitialize)runningInfoIcon).BeginInit();
            SuspendLayout();
            // 
            // infoImagePanel
            // 
            infoImagePanel.BorderStyle = BorderStyle.FixedSingle;
            infoImagePanel.Location = new Point(390, 463);
            infoImagePanel.Margin = new Padding(3, 6, 3, 6);
            infoImagePanel.Name = "infoImagePanel";
            infoImagePanel.Size = new Size(258, 875);
            infoImagePanel.TabIndex = 7;
            infoImagePanel.DoubleClick += infoImagePanel_DoubleClick;
            // 
            // outputList
            // 
            outputList.Location = new Point(658, 463);
            outputList.Margin = new Padding(3, 6, 3, 6);
            outputList.Name = "outputList";
            outputList.Size = new Size(381, 879);
            outputList.TabIndex = 8;
            outputList.SelectedIndexChanged += DisplayFoundItem;
            outputList.DoubleClick += ActivateFoundItem;
            outputList.MouseMove += OnListBoxMouseMove;
            // 
            // locationTreeView
            // 
            locationTreeView.Location = new Point(13, 63);
            locationTreeView.Margin = new Padding(3, 6, 3, 6);
            locationTreeView.Name = "locationTreeView";
            locationTreeView.ShowNodeToolTips = true;
            locationTreeView.Size = new Size(371, 1279);
            locationTreeView.TabIndex = 10;
            locationTreeView.BeforeExpand += RetrievNodes;
            locationTreeView.AfterSelect += locationTreeView_AfterSelect;
            locationTreeView.Click += locationTreeView_Click;
            // 
            // addPrefixButton
            // 
            addPrefixButton.Location = new Point(403, 88);
            addPrefixButton.Margin = new Padding(3, 6, 3, 6);
            addPrefixButton.Name = "addPrefixButton";
            addPrefixButton.Size = new Size(190, 40);
            addPrefixButton.TabIndex = 15;
            addPrefixButton.Text = "Add prefix";
            // 
            // oldTextBox
            // 
            oldTextBox.Location = new Point(7, 152);
            oldTextBox.Margin = new Padding(3, 6, 3, 6);
            oldTextBox.Name = "oldTextBox";
            oldTextBox.Size = new Size(297, 31);
            oldTextBox.TabIndex = 5;
            // 
            // changeNameButton
            // 
            changeNameButton.Location = new Point(80, 88);
            changeNameButton.Margin = new Padding(3, 6, 3, 6);
            changeNameButton.Name = "changeNameButton";
            changeNameButton.Size = new Size(203, 40);
            changeNameButton.TabIndex = 22;
            changeNameButton.Text = "Change part of name";
            changeNameButton.TextAlign = ContentAlignment.TopCenter;
            // 
            // outputBox
            // 
            outputBox.Location = new Point(13, 15);
            outputBox.Margin = new Padding(3, 6, 3, 6);
            outputBox.Name = "outputBox";
            outputBox.Size = new Size(1039, 31);
            outputBox.TabIndex = 19;
            outputBox.KeyDown += outputBox_KeyDown;
            outputBox.MouseDown += outputBox_MouseDown;
            // 
            // searchWorker
            // 
            searchWorker.DoWork += StartSearchAsync;
            searchWorker.RunWorkerCompleted += SearchCompleted;
            // 
            // findNameButton
            // 
            findNameButton.Location = new Point(67, 12);
            findNameButton.Margin = new Padding(3, 6, 3, 6);
            findNameButton.Name = "findNameButton";
            findNameButton.Size = new Size(88, 40);
            findNameButton.TabIndex = 0;
            findNameButton.Text = "Names";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(17, 68);
            label2.Name = "label2";
            label2.Size = new Size(69, 25);
            label2.TabIndex = 28;
            label2.Text = "pattern";
            // 
            // patternBox
            // 
            patternBox.Location = new Point(92, 64);
            patternBox.Margin = new Padding(3, 6, 3, 6);
            patternBox.Multiline = true;
            patternBox.Name = "patternBox";
            patternBox.Size = new Size(255, 62);
            patternBox.TabIndex = 20;
            patternBox.TextChanged += patternBox_TextChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(218, 138);
            label4.Name = "label4";
            label4.Size = new Size(29, 25);
            label4.TabIndex = 29;
            label4.Text = "@";
            // 
            // daysBox
            // 
            daysBox.Location = new Point(253, 135);
            daysBox.Margin = new Padding(3, 6, 3, 6);
            daysBox.Name = "daysBox";
            daysBox.Size = new Size(49, 31);
            daysBox.TabIndex = 27;
            daysBox.TextChanged += patternBox_TextChanged;
            // 
            // runningImage
            // 
            runningImage.Image = CommonForms.Properties.Resources.wspinner_1_;
            runningImage.Location = new Point(17, 21);
            runningImage.Margin = new Padding(3, 6, 3, 6);
            runningImage.Name = "runningImage";
            runningImage.Size = new Size(27, 31);
            runningImage.TabIndex = 31;
            runningImage.TabStop = false;
            runningImage.Visible = false;
            // 
            // searchResultBox
            // 
            searchResultBox.Location = new Point(10, 225);
            searchResultBox.Margin = new Padding(3, 6, 3, 6);
            searchResultBox.Multiline = true;
            searchResultBox.Name = "searchResultBox";
            searchResultBox.ReadOnly = true;
            searchResultBox.Size = new Size(337, 104);
            searchResultBox.TabIndex = 34;
            // 
            // tabControl1
            // 
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Location = new Point(390, 65);
            tabControl1.Margin = new Padding(3, 6, 3, 6);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(667, 394);
            tabControl1.TabIndex = 25;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(changedBtn);
            tabPage1.Controls.Add(viewedBtn);
            tabPage1.Controls.Add(label1);
            tabPage1.Controls.Add(displayResultsBtn);
            tabPage1.Controls.Add(findSoundBtn);
            tabPage1.Controls.Add(findFileBtn);
            tabPage1.Controls.Add(findImagePanel);
            tabPage1.Controls.Add(searchResultBox);
            tabPage1.Controls.Add(runningImage);
            tabPage1.Controls.Add(findNameButton);
            tabPage1.Controls.Add(daysBox);
            tabPage1.Controls.Add(label4);
            tabPage1.Controls.Add(label2);
            tabPage1.Controls.Add(patternBox);
            tabPage1.Location = new Point(4, 37);
            tabPage1.Margin = new Padding(3, 6, 3, 6);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3, 6, 3, 6);
            tabPage1.Size = new Size(659, 353);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Search";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // changedBtn
            // 
            changedBtn.AutoSize = true;
            changedBtn.Location = new Point(114, 137);
            changedBtn.Name = "changedBtn";
            changedBtn.Size = new Size(105, 29);
            changedBtn.TabIndex = 42;
            changedBtn.Text = "changed";
            changedBtn.UseVisualStyleBackColor = true;
            // 
            // viewedBtn
            // 
            viewedBtn.AutoSize = true;
            viewedBtn.Checked = true;
            viewedBtn.Location = new Point(22, 137);
            viewedBtn.Name = "viewedBtn";
            viewedBtn.Size = new Size(92, 29);
            viewedBtn.TabIndex = 41;
            viewedBtn.TabStop = true;
            viewedBtn.Text = "viewed";
            viewedBtn.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(298, 137);
            label1.Name = "label1";
            label1.Size = new Size(49, 25);
            label1.TabIndex = 40;
            label1.Text = "days";
            // 
            // displayResultsBtn
            // 
            displayResultsBtn.Location = new Point(17, 172);
            displayResultsBtn.Margin = new Padding(3, 6, 3, 6);
            displayResultsBtn.Name = "displayResultsBtn";
            displayResultsBtn.Size = new Size(332, 40);
            displayResultsBtn.TabIndex = 39;
            displayResultsBtn.Text = "Display Found Names";
            // 
            // findSoundBtn
            // 
            findSoundBtn.Location = new Point(267, 12);
            findSoundBtn.Margin = new Padding(3, 6, 3, 6);
            findSoundBtn.Name = "findSoundBtn";
            findSoundBtn.Size = new Size(80, 40);
            findSoundBtn.TabIndex = 37;
            findSoundBtn.Text = "Sound";
            // 
            // findFileBtn
            // 
            findFileBtn.Location = new Point(161, 12);
            findFileBtn.Margin = new Padding(3, 6, 3, 6);
            findFileBtn.Name = "findFileBtn";
            findFileBtn.Size = new Size(100, 40);
            findFileBtn.TabIndex = 36;
            findFileBtn.Text = "Files";
            // 
            // findImagePanel
            // 
            findImagePanel.BorderStyle = BorderStyle.FixedSingle;
            findImagePanel.Location = new Point(360, 6);
            findImagePanel.Margin = new Padding(3, 6, 3, 6);
            findImagePanel.Name = "findImagePanel";
            findImagePanel.Size = new Size(285, 331);
            findImagePanel.TabIndex = 35;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(label5);
            tabPage2.Controls.Add(renameResultBox);
            tabPage2.Controls.Add(newTextBox);
            tabPage2.Controls.Add(directoryNameBox);
            tabPage2.Controls.Add(renameDirBtn);
            tabPage2.Controls.Add(oldTextBox);
            tabPage2.Controls.Add(addPrefixButton);
            tabPage2.Controls.Add(changeNameButton);
            tabPage2.Location = new Point(4, 37);
            tabPage2.Margin = new Padding(3, 6, 3, 6);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3, 6, 3, 6);
            tabPage2.Size = new Size(659, 353);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Rename";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(307, 160);
            label5.Name = "label5";
            label5.Size = new Size(48, 25);
            label5.TabIndex = 42;
            label5.Text = "==>";
            // 
            // renameResultBox
            // 
            renameResultBox.Location = new Point(3, 198);
            renameResultBox.Margin = new Padding(3, 6, 3, 6);
            renameResultBox.Multiline = true;
            renameResultBox.Name = "renameResultBox";
            renameResultBox.ReadOnly = true;
            renameResultBox.Size = new Size(644, 125);
            renameResultBox.TabIndex = 40;
            // 
            // newTextBox
            // 
            newTextBox.Location = new Point(350, 152);
            newTextBox.Margin = new Padding(3, 6, 3, 6);
            newTextBox.Name = "newTextBox";
            newTextBox.Size = new Size(297, 31);
            newTextBox.TabIndex = 37;
            // 
            // directoryNameBox
            // 
            directoryNameBox.Location = new Point(197, 31);
            directoryNameBox.Margin = new Padding(3, 6, 3, 6);
            directoryNameBox.Name = "directoryNameBox";
            directoryNameBox.Size = new Size(451, 31);
            directoryNameBox.TabIndex = 36;
            // 
            // renameDirBtn
            // 
            renameDirBtn.Location = new Point(3, 29);
            renameDirBtn.Margin = new Padding(3, 6, 3, 6);
            renameDirBtn.Name = "renameDirBtn";
            renameDirBtn.Size = new Size(193, 40);
            renameDirBtn.TabIndex = 35;
            renameDirBtn.Text = "New directory name";
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(mangleCharButton);
            tabPage3.Controls.Add(reduceSizeBox);
            tabPage3.Controls.Add(compressBtn);
            tabPage3.Controls.Add(reduceButton);
            tabPage3.Controls.Add(makePrivateBtn);
            tabPage3.Controls.Add(runningSimilarIcon);
            tabPage3.Controls.Add(runningInfoIcon);
            tabPage3.Controls.Add(findSimilarImagesBtn);
            tabPage3.Controls.Add(imageInfoBtn);
            tabPage3.Location = new Point(4, 37);
            tabPage3.Margin = new Padding(3, 6, 3, 6);
            tabPage3.Name = "tabPage3";
            tabPage3.Size = new Size(659, 353);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Processes";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // mangleCharButton
            // 
            mangleCharButton.Location = new Point(57, 217);
            mangleCharButton.Margin = new Padding(3, 6, 3, 6);
            mangleCharButton.Name = "mangleCharButton";
            mangleCharButton.Size = new Size(217, 40);
            mangleCharButton.TabIndex = 46;
            mangleCharButton.Text = "Modify Mangle Symbol";
            // 
            // reduceSizeBox
            // 
            reduceSizeBox.FormattingEnabled = true;
            reduceSizeBox.Location = new Point(153, 266);
            reduceSizeBox.Margin = new Padding(2, 3, 2, 3);
            reduceSizeBox.Name = "reduceSizeBox";
            reduceSizeBox.Size = new Size(121, 33);
            reduceSizeBox.TabIndex = 45;
            // 
            // compressBtn
            // 
            compressBtn.Location = new Point(57, 168);
            compressBtn.Margin = new Padding(3, 6, 3, 6);
            compressBtn.Name = "compressBtn";
            compressBtn.Size = new Size(217, 40);
            compressBtn.TabIndex = 43;
            compressBtn.Text = "Compress PNG images";
            // 
            // reduceButton
            // 
            reduceButton.Location = new Point(57, 262);
            reduceButton.Margin = new Padding(3, 6, 3, 6);
            reduceButton.Name = "reduceButton";
            reduceButton.Size = new Size(96, 38);
            reduceButton.TabIndex = 42;
            reduceButton.Text = "Resize to";
            reduceButton.TextAlign = ContentAlignment.TopCenter;
            // 
            // makePrivateBtn
            // 
            makePrivateBtn.Location = new Point(57, 126);
            makePrivateBtn.Margin = new Padding(3, 6, 3, 6);
            makePrivateBtn.Name = "makePrivateBtn";
            makePrivateBtn.Size = new Size(217, 40);
            makePrivateBtn.TabIndex = 39;
            makePrivateBtn.Text = "Convert to private";
            // 
            // runningSimilarIcon
            // 
            runningSimilarIcon.Image = CommonForms.Properties.Resources.wspinner_1_;
            runningSimilarIcon.Location = new Point(23, 87);
            runningSimilarIcon.Margin = new Padding(3, 6, 3, 6);
            runningSimilarIcon.Name = "runningSimilarIcon";
            runningSimilarIcon.Size = new Size(27, 31);
            runningSimilarIcon.TabIndex = 38;
            runningSimilarIcon.TabStop = false;
            runningSimilarIcon.Visible = false;
            // 
            // runningInfoIcon
            // 
            runningInfoIcon.Image = CommonForms.Properties.Resources.wspinner_1_;
            runningInfoIcon.Location = new Point(23, 35);
            runningInfoIcon.Margin = new Padding(3, 6, 3, 6);
            runningInfoIcon.Name = "runningInfoIcon";
            runningInfoIcon.Size = new Size(27, 31);
            runningInfoIcon.TabIndex = 37;
            runningInfoIcon.TabStop = false;
            runningInfoIcon.Visible = false;
            // 
            // findSimilarImagesBtn
            // 
            findSimilarImagesBtn.Location = new Point(57, 78);
            findSimilarImagesBtn.Margin = new Padding(3, 6, 3, 6);
            findSimilarImagesBtn.Name = "findSimilarImagesBtn";
            findSimilarImagesBtn.Size = new Size(217, 40);
            findSimilarImagesBtn.TabIndex = 36;
            findSimilarImagesBtn.Text = "Find similar images";
            // 
            // imageInfoBtn
            // 
            imageInfoBtn.Location = new Point(57, 31);
            imageInfoBtn.Margin = new Padding(3, 6, 3, 6);
            imageInfoBtn.Name = "imageInfoBtn";
            imageInfoBtn.Size = new Size(217, 40);
            imageInfoBtn.TabIndex = 35;
            imageInfoBtn.Text = "Update ImageInfo";
            // 
            // NavigatorForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1047, 1350);
            Controls.Add(tabControl1);
            Controls.Add(outputBox);
            Controls.Add(locationTreeView);
            Controls.Add(outputList);
            Controls.Add(infoImagePanel);
            Margin = new Padding(3, 6, 3, 6);
            MaximumSize = new Size(1069, 1598);
            MinimumSize = new Size(1069, 881);
            Name = "NavigatorForm";
            Text = "Image Selector";
            FormClosing += NavigatorForm_FormClosing;
            ((ISupportInitialize)runningImage).EndInit();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            tabPage3.ResumeLayout(false);
            ((ISupportInitialize)runningSimilarIcon).EndInit();
            ((ISupportInitialize)runningInfoIcon).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }
        #endregion
        public NavigatorForm(bool accessRequested = false)
        {
            try
            {
                navigator = new Navigator();
                InitializeComponent();
                navigator.onNewImageSelection = NewImageSelected;
                navigator.onNewDirSelection = SetActiveDirAndInfoImages;
                privateAccessRequested = accessRequested;
                if (accessRequested)
                    RequestPassword();
                imageAdjustmentWorker = new BackgroundWorker();
                imageAdjustmentWorker.DoWork += new DoWorkEventHandler(ApplyConversion);
                imageAdjustmentWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ConversionCompleted);
                infoWorker = new BackgroundWorker();
                infoWorker.DoWork += new DoWorkEventHandler(StartInfoUpdate);
                infoWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(InfoUpdateCompleted);
                similarImagesWorker = new BackgroundWorker();
                similarImagesWorker.DoWork += new DoWorkEventHandler(FindSimilarImages);
                similarImagesWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(FindSimilarImagesCompleted);
                outputBox.ForeColor = Color.LightSalmon;
                findNameButton.Click += (object s, EventArgs e) => StartSearch(Navigator.SearchMode.Names);
                findFileBtn.Click += (object s, EventArgs e) => StartSearch(Navigator.SearchMode.File);
                findSoundBtn.Click += (object s, EventArgs e) => StartSearch(Navigator.SearchMode.Sound);
                //findLookBtn.Click += (object s, EventArgs e) => StartSearch(Navigator.SearchMode.Image);
                makePrivateBtn.Click += (object s, EventArgs e) => ConvertToPrivate();
                compressBtn.Click += (object s, EventArgs e) => ConvertTojpg();
                reduceButton.Click += (object s, EventArgs e) => ResizeImages();
                mangleCharButton.Click += (object s, EventArgs e) => ChangeMangleChar();
                reduceButton.Text = "Reduce to";
                changeNameButton.Click += (object s, EventArgs e) => ApplyRenameOperation(RenameType.FileName);
                addPrefixButton.Click += (object s, EventArgs e) => ApplyRenameOperation(RenameType.AddPrefix);
                renameDirBtn.Click += (object s, EventArgs e) => ApplyRenameOperation(RenameType.Directory);
                imageInfoBtn.Click += (object s, EventArgs e) => { runningInfoIcon.Visible = true; infoWorker.RunWorkerAsync(); imageInfoBtn.Enabled = false; };
                findSimilarImagesBtn.Click += (object s, EventArgs e) => { runningSimilarIcon.Visible = true; similarImagesWorker.RunWorkerAsync(); findSimilarImagesBtn.Enabled = false; }; ;
                //outputList.DrawMode = DrawMode.OwnerDrawFixed;
                outputList.ItemHeight = 20;
                locationTreeView.ItemHeight = 22;
                locationTreeView.DoubleClick += (object s, EventArgs e) => { if (selectedNode != null) ShowImageListForm(selectedNode); };
                displayResultsBtn.Click += (object o, EventArgs e) => { onSearchClick?.Invoke(); };
                foreach (int v in Enum.GetValues(typeof(Conversion)))
                    if (v >= (int)Conversion.LimitSize1)
                        reduceSizeBox.Items.Add(v.ToString());
                reduceSizeBox.SelectedIndex = reduceSizeBox.Items.Count - 1;
                //reduceSizeBox.SelectedIndexChanged += (object o, EventArgs e) =>
                //{
                //    var s=reduceSizeBox.SelectedText;
                //    var i=reduceSizeBox.SelectedItem;   
                //};
                TreeNode nodeRoot = locationTreeView.Nodes.Add(Navigator.Root.Name);
                nodeRoot.Tag = Navigator.Root;
                nodeRoot.Nodes.Add("fake");
                itemInfoImages = new DirectoryInfoImages(infoImagePanel);
                findImagePanel.Paint += new PaintEventHandler(DrawSearchImage);
                //findLookBtn.Enabled = false;
                fileManager = new FileManager(navigator);
                fileManager.notifyResults += new NotifyMessage(ShowResults);// temporary suspended: causes cross-thread error
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
                Text = "Image viewer v3.2";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "NavigatorForm");
            }
        }
        ~NavigatorForm() { Dispose(false); }
        void SetActiveDirAndInfoImages(DirectoryInfo di)
        {
            selectedNode = itemInfoImages.ReDrawInfoImages(di);
            outputBox.Text = di == null ? "" : selectedNode != null ? (new ImageDirInfo(selectedNode)).RealPath : di.FullName + " does NOT EXIST";
        }
        void ShowResults(string message) { outputList.Items.Add(message); }
        void ShowFinalResults(List<string> messages)
        {
            foreach (string s in messages)
                outputList.Items.Add(s);
        }
        void ShowStatus(string message) { outputBox.Text = message; }
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
            patternBox.Text = Scramble.UnMangle(Path.GetFileNameWithoutExtension(imagePath));
            userAction = true;
        }
        void DrawSearchImage(object sender, PaintEventArgs e)
        {
            if (searchImagePath.Length == 0)
            {
                //findLookBtn.Enabled = false;
                return;
            }
            try
            {
                ImageFileInfo ifi = new ImageFileInfo(new FileInfo(searchImagePath));
                Image im = ifi.UpdateThumbnail();
                if (im != null)
                {
                    float areaSize = findImagePanel.Size.Width;// * g.DpiX / 96;
                    float scale = Math.Min(findImagePanel.Size.Width / (im.Width + 1f), findImagePanel.Size.Height / (im.Height + 1f));
                    float iw = im.Width * scale;
                    float ih = im.Height * scale;
                    float dx = (findImagePanel.Size.Width - iw) / 2;
                    float dy = (findImagePanel.Size.Height - ih) / 2;
                    e.Graphics.DrawImage(im, dx, dy, iw, ih);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        void StartSearch(Navigator.SearchMode mode)
        {   // user clicks with different nodes
            if (searchWorker.IsBusy)
                return;
            searchRoot = navigator.GetSearchRoot(outputBox.Text);
            if (searchRoot == null || !searchRoot.Exists)
                return;
            Images = null;
            searchMode = mode;
            outputList.Items.Clear();
            if (patternBox.Text.Length == 0 && daysBox.Text.Length == 0 /*&& searchMode != Navigator.SearchMode.Image*/)
                return;
            runningImage.Visible = true;
            searchWorker.RunWorkerAsync();
            SetViewButtonState(SearchState.Stop);
        }
        void FindSimilarImages(object sender, DoWorkEventArgs e)
        {
            DirectoryInfo dii = new DirectoryInfo(outputBox.Text);
            DirectoryInfo[] dirList = null;
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
            Images = new DisplayImageList(searchRoot, navigator.GetMatchedDirNames());
            ImageListForm sif = new ImageListForm(Images, navigator);
            try
            {
                sif.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        void StopSearch() { navigator.StopSearch = true; }
        void OperationButtonsEnabled(bool state)
        {
            mangleCharButton.Enabled = state;
            reduceButton.Enabled = state;
            findSimilarImagesBtn.Enabled = state;
            compressBtn.Enabled = state;
            changeNameButton.Enabled = state;
            makePrivateBtn.Enabled = state;
            addPrefixButton.Enabled = state;
        }
        void ResizeImages()
        {
            if (selectedNode == null || !int.TryParse((string)reduceSizeBox.SelectedItem, out int res))
                return;
            conversion = Conversion.None;
            foreach (var v in Enum.GetValues(typeof(Conversion)))
                if (res == (int)v)
                    conversion = (Conversion)v;
            if (conversion == Conversion.None)
                return;
            // if worker is already running, queue this conversion
            if (imageAdjustmentWorker.IsBusy)
            {
                pendingConversion = conversion;
                outputBox.Text = "Conversion queued";
                return;
            }
            OperationButtonsEnabled(false);
            imageAdjustmentWorker.RunWorkerAsync(); // calls ApplyConversion
        }
        void ConvertToPrivate()
        {
            if (selectedNode == null)
                return;
            conversion = Conversion.Encode;
            OperationButtonsEnabled(false);
            if (imageAdjustmentWorker.IsBusy)
            {
                pendingConversion = conversion;
                outputBox.Text = "Conversion queued";
                return;
            }
            imageAdjustmentWorker.RunWorkerAsync(); // calls ApplyConversion
        }
        void ConvertTojpg()
        {
            if (selectedNode == null)
                return;
            conversion = Conversion.ToJPG;
            OperationButtonsEnabled(false);
            if (imageAdjustmentWorker.IsBusy)
            {
                pendingConversion = conversion;
                outputBox.Text = "Conversion queued";
                return;
            }
            imageAdjustmentWorker.RunWorkerAsync(); // calls ApplyConversion
        }
        void ChangeMangleChar()
        {
            if (selectedNode == null)
                return;
            conversion = Conversion.MangleChar;
            OperationButtonsEnabled(false);
            if (imageAdjustmentWorker.IsBusy)
            {
                pendingConversion = conversion;
                outputBox.Text = "Conversion queued";
                return;
            }
            imageAdjustmentWorker.RunWorkerAsync(); // calls ApplyConversion
        }
        void ApplyConversion(object sender, DoWorkEventArgs e)
        {
            //selectedNode = new DirectoryInfo(@"E:\C\data\OldC\stuff\Work");// AllDevicy");
            processNodeName = selectedNode.Name;
            fileManager.ApplyAdjustmentRecursively(selectedNode, conversion, false);
        }
        void ConversionCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OperationButtonsEnabled(true);
            outputList.Items.Add(conversion.ToString() + " completed: " + processNodeName);
            processNodeName = "";
            conversion = Conversion.None;
            // if a conversion was queued while the worker was busy, start it now
            if (pendingConversion.HasValue)
            {
                conversion = pendingConversion.Value;
                pendingConversion = null;
                OperationButtonsEnabled(false);
                imageAdjustmentWorker.RunWorkerAsync(); // calls ApplyConversion
                return;
            }
        }
        void ApplyRenameOperation(RenameType operation)
        {
            if (selectedNode == null || operation == RenameType.None)
                return;
            if (operation == RenameType.Directory)
            {
                if (Navigator.IsSpecDir(selectedNode))
                {
                    MessageBox.Show("Special directory " + selectedNode.Name + " can't be renamed");
                    return;
                }
                fileManager.NewDirName = directoryNameBox.Text.Trim();
                if (fileManager.NewDirName.Length == 0)
                {
                    MessageBox.Show("New directory name has to be specified", "");
                    return;
                }
            }
            else
            {
                fileManager.TextToReplace = oldTextBox.Text;
                if (operation == RenameType.FileName && fileManager.TextToReplace.Length == 0)
                {
                    MessageBox.Show("Replacement text has to be specified", "");
                    return;
                }
                oldTextBox.Text = "";
                fileManager.TextReplacement = newTextBox.Text;
                if (newTextBox.Text.Trim() != newTextBox.Text)
                    if (MessageBox.Show("Is there a white space in the replacement?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
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
            var ret = fileManager.DirectoryOrFilesRename(selectedNode, operation);
            if (Images != null)
                Images.DeletedFile = ret;
            OperationButtonsEnabled(true);
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
            Cursor = Cursors.WaitCursor;
            TreeNode node = e.Node;
            node.Nodes.Clear();
            var tag = node.Tag as DirectoryInfo;
            if (tag == null)
                return;
            DirectoryInfo[] dia = navigator.GetDirectories(tag);
            string[] fna = new string[dia.Length];
            for (int i = 0; i < dia.Length; i++)
                fna[i] = Scramble.UnMangle(dia[i].Name);
            Array.Sort(fna, dia, new ImageFileInfo.NameComparer());
            for (int i = 0; i < dia.Length; i++)
            {
                TreeNode subNode = node.Nodes.Add(fna[i]);
                subNode.Tag = dia[i];
                subNode.Nodes.Add("fake");
            }
            Cursor = Cursors.Default;
        }
        void locationTreeView_Click(object sender, EventArgs e) { itemInfoImages.ReDrawInfoImages(); } // clear old selection
        void locationTreeView_AfterSelect(object sender, TreeViewEventArgs e) { if (e.Node != null && e.Node.Tag as DirectoryInfo != null) SetActiveDirAndInfoImages((DirectoryInfo)e.Node.Tag); }
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        FileSystemInfo SearchSelectedItem()               // both dir and image in human readable form 
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
                dirPath = Scramble.MangleFile(dirPath);
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
                filePath = Scramble.MangleFile(filePath);
                fi = new FileInfo(filePath);
            }
            return fi.Exists ? fi : null;
        }
        void DisplayFoundItem(object s, EventArgs e)
        {
            FileSystemInfo fsi = SearchSelectedItem();
            if (fsi == null)
                return;
            bool isDir = (fsi.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
            DirectoryInfo di = isDir ? (DirectoryInfo)fsi : ((FileInfo)fsi).Directory;
            SetActiveDirAndInfoImages(di);
        }
        void ActivateFoundItem(object s, EventArgs e)
        {
            FileSystemInfo fsi = SearchSelectedItem();
            if (fsi == null)
                return;
            try
            {
                if ((fsi.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (selectedNode != null)
                        ShowImageListForm(selectedNode);
                }
                else
                {
                    FileInfo fi = (FileInfo)fsi;
                    ImageFileInfo dt = new ImageFileInfo(fi);
                    if (dt.IsImage)
                    {
                        ImageViewForm editForm = new ImageViewForm();
                        invoked.Add(editForm);
                        editForm.ShowNewImage(dt);
                    }
                    else if (dt.IsMovie)
                    {
                        navigator.RunVideoFile(dt);
                    }
                }
            }
            catch { }
        }
        void StartSearchAsync(object sender, DoWorkEventArgs e)
        {
            matchingItems = navigator.GenerateSearchList(searchMode, searchRoot, patternBox.Text, daysBox.Text, viewedBtn.Checked);
        }
        void SearchCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (matchingItems == null)
                return;
            string criteria = "";
            if (patternBox.Text.Length > 0)
            {
                criteria = searchMode == Navigator.SearchMode.Names ? "matching name " :
                    searchMode == Navigator.SearchMode.Sound ? "sound like " :
                    searchMode == Navigator.SearchMode.File ? "matching file " :
                    /*searchMode == Navigator.SearchMode.Image ? "looks like " : */"";
                criteria += patternBox.Text + Environment.NewLine;
            }
            if (daysBox.Text.Length > 0)
                criteria += "updated within " + daysBox.Text + " days";
            bool dirOnly = searchMode != Navigator.SearchMode.File;
            int fileCount = 0;
            var matchedDirs = matchingItems.GetMatchedDirs();
            //Debug.WriteLine("###matchedDirs.Count=" + matchedDirs.Count);
            if (matchedDirs.Count > 0)
            {
                foreach (var matchingDir in matchedDirs)
                {
                    Debug.WriteLineIf(outputList.Items.Contains(matchingDir), "###list contains " + matchingDir.Name);
                    outputList.Items.Add(matchingDir);
                    if (!dirOnly)
                        foreach (var matchingFile in matchingDir.Files)
                        {
                            outputList.Items.Add(matchingFile);
                            fileCount++;
                        }
                }
            }
            searchResultBox.Text = (dirOnly ? matchedDirs.Count + " names" : fileCount + " items") + " in " + searchRoot.FullName +
                Environment.NewLine + criteria;
            SetViewButtonState(SearchState.Display);
            patternBox.Text = daysBox.Text = "";
            runningImage.Visible = false;
        }
        void StartInfoUpdate(object sender, DoWorkEventArgs e)
        {
            try
            {
                //DirectoryInfo di = new DirectoryInfo(outputBox.Text);
                //if (di.Exists)
                //    navigator.CreateImageHashes(di);
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
            if (selectedNode != null)
                ShowImageListForm(selectedNode);
        }
        void patternBox_TextChanged(object sender, EventArgs e)
        {
            if (userAction)
            {
                searchImagePath = "";
                findImagePanel.Invalidate();
            }
            EnableSearchButtons(!string.IsNullOrWhiteSpace(patternBox.Text + daysBox.Text.Length));
        }
        void NavigatorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var form in invoked)
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
        private void RequestPassword()
        {
            privateAccessRequested = true;
            outputBox.Text = passwordText;
            outputBox.ForeColor = Color.Red;
            outputBox.PasswordChar = '\0';
        }
        //private void CompressPNGtoJPG(object sender, EventArgs e)
        //{
        //    DirectoryInfo dii = new DirectoryInfo(outputBox.Text);
        //    if (!dii.Exists)
        //        dii = new DirectoryInfo(FileName.MangleFile(outputBox.Text));
        //    if (!dii.Exists)
        //    {
        //        MessageBox.Show("Directory " + outputBox.Text + " does not exist");
        //        return;
        //    }
        //    if (!Navigator.IsSpecDir(dii) && !Navigator.IsSpecDir(dii.Parent) && !Navigator.IsSpecDir(dii.Parent.Parent))
        //        return;
        //    warnings.Clear();
        //    FileInfo[] fia = dii.GetFiles();
        //    foreach (var fi in fia)
        //    {
        //        ImageFileInfo ifi = new ImageFileInfo(fi);
        //        if (ifi.IsExact)
        //        {
        //            var ba = BitmapAccess.LoadImage(ifi.FSPath, ifi.IsEncrypted);
        //            string filePath = Path.GetFileNameWithoutExtension(ifi.FSPath) + (ifi.IsEncrypted ? ".jpe" : ".jpg");
        //            filePath = Path.Combine(Path.GetDirectoryName(ifi.FSPath), filePath);
        //            ba.SaveToFile(filePath, false, ifi.IsEncrypted);
        //        }
        //    }
        //    if (warnings.Count > 0)
        //        MessageBox.Show(warnings[0]);
        //    Debug.WriteIf(warnings.Count > 0, warnings[0]);
        //    return;
        //}
    }
}
