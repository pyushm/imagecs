using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.IO;

using ImageProcessor;

namespace NNTP
{
	public class NewsReaderForm : System.Windows.Forms.Form
	{
		private GroupManager groupManager;
		ComboBox selectGroupCB;
        Label statusLabel;
		private System.ComponentModel.Container components = null;
		ListView headerListView;
		ColumnHeader dateCol;
		ColumnHeader subjectCol;
		ColumnHeader idCol;
		ColumnHeader sizeCol;
		ColumnHeader groupCol;
		Button groupHeaderBtn;
		Button CancelDownloadingBtn;
		Button reconnectButton;
		Button findSubjectBtn;
		TextBox subjectPatternInput;
		string[] shortGroupNames;
		Icon busyIcon;
		Icon idleIcon;
        Color downloadedElswhereColor = Color.PaleGreen;
		Color downloadedColor=Color.Cyan;
		Color unavailableColor=Color.Yellow;
        Color earlierPartMissingColor = Color.LightGray;
		Color availableColor=Color.White;
        Color markedForDownloadingColor=Color.LightCoral;
		int lastID=-1;
        bool ready = false;
		Form nameForm;
		ImageListForm imageListForm;
        Timer updateTimer;
        int updateListFrequency = 1000;      // update frequency of list change, ms
        int updateItemsFrequency = 2000;
        private Button selectGroupButton;
        private Button serverBtn;
        private CheckBox filterBox;
        private TextBox skipBox;    // update frequency of visible items, ms
        int updateItemsTime = 0;            // update happens if updateItemsTime>updateItemsFrequency;
        
		[STAThread]
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
		void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewsReaderForm));
            this.statusLabel = new System.Windows.Forms.Label();
            this.selectGroupCB = new System.Windows.Forms.ComboBox();
            this.groupHeaderBtn = new System.Windows.Forms.Button();
            this.headerListView = new System.Windows.Forms.ListView();
            this.dateCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.sizeCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.subjectCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.idCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.CancelDownloadingBtn = new System.Windows.Forms.Button();
            this.findSubjectBtn = new System.Windows.Forms.Button();
            this.subjectPatternInput = new System.Windows.Forms.TextBox();
            this.reconnectButton = new System.Windows.Forms.Button();
            this.selectGroupButton = new System.Windows.Forms.Button();
            this.serverBtn = new System.Windows.Forms.Button();
            this.filterBox = new System.Windows.Forms.CheckBox();
            this.skipBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // statusLabel
            // 
            this.statusLabel.Location = new System.Drawing.Point(720, -2);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(409, 40);
            this.statusLabel.TabIndex = 1;
            this.statusLabel.Text = "Disconnected";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // selectGroupCB
            // 
            this.selectGroupCB.Location = new System.Drawing.Point(129, 8);
            this.selectGroupCB.MaxDropDownItems = 25;
            this.selectGroupCB.Name = "selectGroupCB";
            this.selectGroupCB.Size = new System.Drawing.Size(80, 21);
            this.selectGroupCB.TabIndex = 5;
            this.selectGroupCB.SelectedIndexChanged += new System.EventHandler(this.ConnectGroup);
            // 
            // groupHeaderBtn
            // 
            this.groupHeaderBtn.Location = new System.Drawing.Point(211, 8);
            this.groupHeaderBtn.Name = "groupHeaderBtn";
            this.groupHeaderBtn.Size = new System.Drawing.Size(85, 22);
            this.groupHeaderBtn.TabIndex = 7;
            this.groupHeaderBtn.Text = "Load Headers";
            this.groupHeaderBtn.Click += new System.EventHandler(this.StartLoadingNewHeaders);
            // 
            // headerListView
            // 
            this.headerListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.dateCol,
            this.sizeCol,
            this.subjectCol,
            this.idCol,
            this.groupCol});
            this.headerListView.FullRowSelect = true;
            this.headerListView.GridLines = true;
            this.headerListView.Location = new System.Drawing.Point(0, 35);
            this.headerListView.Name = "headerListView";
            this.headerListView.Size = new System.Drawing.Size(1128, 605);
            this.headerListView.TabIndex = 8;
            this.headerListView.UseCompatibleStateImageBehavior = false;
            this.headerListView.View = System.Windows.Forms.View.Details;
            this.headerListView.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.headerListView_RetrieveVirtualItem);
            this.headerListView.DoubleClick += new System.EventHandler(this.ActivateSelectedItem);
            // 
            // dateCol
            // 
            this.dateCol.Text = "Date";
            this.dateCol.Width = 119;
            // 
            // sizeCol
            // 
            this.sizeCol.Text = "Size";
            this.sizeCol.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.sizeCol.Width = 50;
            // 
            // subjectCol
            // 
            this.subjectCol.Text = "Subject";
            this.subjectCol.Width = 803;
            // 
            // idCol
            // 
            this.idCol.Text = "Id";
            this.idCol.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.idCol.Width = 66;
            // 
            // groupCol
            // 
            this.groupCol.Text = "Group";
            this.groupCol.Width = 50;
            // 
            // CancelDownloadingBtn
            // 
            this.CancelDownloadingBtn.Location = new System.Drawing.Point(375, 8);
            this.CancelDownloadingBtn.Name = "CancelDownloadingBtn";
            this.CancelDownloadingBtn.Size = new System.Drawing.Size(90, 22);
            this.CancelDownloadingBtn.TabIndex = 16;
            this.CancelDownloadingBtn.Text = "Cancel Loading";
            this.CancelDownloadingBtn.Click += new System.EventHandler(this.CancelDownloading);
            // 
            // findSubjectBtn
            // 
            this.findSubjectBtn.Location = new System.Drawing.Point(577, 8);
            this.findSubjectBtn.Name = "findSubjectBtn";
            this.findSubjectBtn.Size = new System.Drawing.Size(40, 22);
            this.findSubjectBtn.TabIndex = 17;
            this.findSubjectBtn.Text = "Find";
            this.findSubjectBtn.Click += new System.EventHandler(this.GetFilteredHeaders);
            // 
            // subjectPatternInput
            // 
            this.subjectPatternInput.Location = new System.Drawing.Point(616, 8);
            this.subjectPatternInput.MaxLength = 60;
            this.subjectPatternInput.Name = "subjectPatternInput";
            this.subjectPatternInput.Size = new System.Drawing.Size(104, 20);
            this.subjectPatternInput.TabIndex = 18;
            // 
            // reconnectButton
            // 
            this.reconnectButton.Location = new System.Drawing.Point(471, 8);
            this.reconnectButton.Name = "reconnectButton";
            this.reconnectButton.Size = new System.Drawing.Size(100, 22);
            this.reconnectButton.TabIndex = 19;
            this.reconnectButton.Text = "Reconnect Group";
            this.reconnectButton.Click += new System.EventHandler(this.reconnectButton_Click);
            // 
            // selectGroupButton
            // 
            this.selectGroupButton.Location = new System.Drawing.Point(58, 8);
            this.selectGroupButton.Name = "selectGroupButton";
            this.selectGroupButton.Size = new System.Drawing.Size(72, 22);
            this.selectGroupButton.TabIndex = 20;
            this.selectGroupButton.Text = "Edit Groups";
            this.selectGroupButton.Click += new System.EventHandler(this.selectGroupButton_Click);
            // 
            // serverBtn
            // 
            this.serverBtn.Location = new System.Drawing.Point(4, 8);
            this.serverBtn.Name = "serverBtn";
            this.serverBtn.Size = new System.Drawing.Size(50, 22);
            this.serverBtn.TabIndex = 21;
            this.serverBtn.Text = "Server";
            this.serverBtn.UseVisualStyleBackColor = true;
            this.serverBtn.Click += new System.EventHandler(this.serverBtn_Click);
            // 
            // filterBox
            // 
            this.filterBox.AutoSize = true;
            this.filterBox.Checked = true;
            this.filterBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.filterBox.Location = new System.Drawing.Point(328, 11);
            this.filterBox.Name = "filterBox";
            this.filterBox.Size = new System.Drawing.Size(45, 17);
            this.filterBox.TabIndex = 22;
            this.filterBox.Text = "filter";
            this.filterBox.UseVisualStyleBackColor = true;
            // 
            // skipBox
            // 
            this.skipBox.Location = new System.Drawing.Point(299, 8);
            this.skipBox.MaxLength = 60;
            this.skipBox.Name = "skipBox";
            this.skipBox.Size = new System.Drawing.Size(24, 20);
            this.skipBox.TabIndex = 23;
            this.skipBox.Text = "skip";
            // 
            // NewsReaderForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(1128, 641);
            this.Controls.Add(this.skipBox);
            this.Controls.Add(this.filterBox);
            this.Controls.Add(this.serverBtn);
            this.Controls.Add(this.selectGroupButton);
            this.Controls.Add(this.reconnectButton);
            this.Controls.Add(this.subjectPatternInput);
            this.Controls.Add(this.findSubjectBtn);
            this.Controls.Add(this.CancelDownloadingBtn);
            this.Controls.Add(this.headerListView);
            this.Controls.Add(this.groupHeaderBtn);
            this.Controls.Add(this.selectGroupCB);
            this.Controls.Add(this.statusLabel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(1136, 1200);
            this.MinimumSize = new System.Drawing.Size(1136, 200);
            this.Name = "NewsReaderForm";
            this.Text = "News Reader";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.NewsReaderFormClosing);
            this.Resize += new System.EventHandler(this.WindowResized);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

        [STAThread]
        static void Main()					
		{
			Application.Run(new NewsReaderForm());
		}
		public NewsReaderForm()				
		{
			busyIcon=new Icon("NewsReaderBusy.ico");
			idleIcon=Icon;
			InitializeComponent();
			SetReady(true);
			try
			{
				groupManager=new GroupManager(null);
				groupManager.onNeedName+=new GroupManager.OnNeedName(EnterContinuationFileName);
                UpdateGroupCB();
                ContextMenu selectMenu= new System.Windows.Forms.ContextMenu(); 
				MenuItem menuItem1= new MenuItem("Load", new EventHandler(DownloadSelectedItems)); 
				MenuItem menuItem2= new MenuItem("Mark Unread", new EventHandler(MarkSelectedItemsUnread)); 
				MenuItem menuItem3= new MenuItem("Delete Older Headers", new EventHandler(RemoveOldHeaders)); 
				selectMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {menuItem1, menuItem2, menuItem3}); 
				headerListView.ContextMenu = selectMenu;
                headerListView.VirtualMode = true;
                updateTimer = new System.Windows.Forms.Timer();
                updateTimer.Interval = updateListFrequency;
                updateTimer.Tick += new EventHandler(UpdateList);
                updateTimer.Start();
                //ShowLoadedImage("");
			}
			catch(Exception ex)
			{
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                ShowMessage(ex.Message, Common.MessageType.Error);
			}
		}
        void UpdateGroupCB()                
        {
            int ng = groupManager.SubscribedGroups.Length;
            shortGroupNames = new string[ng];
            for (int i = 0; i < ng; i++)
                shortGroupNames[i] = groupManager.SubscribedGroups[i].ShortName;
            selectGroupCB.Items.Clear();
            selectGroupCB.Items.AddRange(shortGroupNames);
        }
        void UpdateList(object s, System.EventArgs e)
        {
            Common.LogEntry[] entry = groupManager.Messages.Retrieve();
            if (entry.Length > 0)
                ShowMessage(entry[entry.Length - 1].Message, entry[entry.Length - 1].Type);
            SetReady(groupManager.Idle);
            if (groupManager.CurrentGroup == null || groupManager.Headers == null || headerListView == null)
                return;
            if (headerListView.VirtualListSize != groupManager.Headers.Count)
                headerListView.VirtualListSize = groupManager.Headers.Count;
            updateItemsTime+=updateListFrequency;
            if (updateItemsTime > updateItemsFrequency)
                UpdateItems();
        }
        void UpdateItems()                  
        {
            updateItemsTime = 0;
            if (headerListView.VirtualListSize == 0)
                return;
            try
            {
                int firstVisible = headerListView.TopItem.Index;
                int lastVisible = firstVisible + headerListView.ClientRectangle.Height / headerListView.GetItemRect(firstVisible).Height + 1;
                for (int i = firstVisible; i < groupManager.Headers.Count && i <= lastVisible; i++)
                {
                    ArticleHeader.Item ahi = groupManager.Headers[i];
                    if (ahi.Updated)
                        headerListView.Invalidate(headerListView.GetItemRect(i));
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }
        void SetReady(bool idling)			
		{
            try
            {
                if (idling && !ready)
                {
                    selectGroupCB.Enabled = true;
                    groupHeaderBtn.Enabled = true;
                    statusLabel.BackColor = Color.LightGreen;
                    Icon = idleIcon;
                    Cursor = Cursors.Default;
                    ready = true;
                }
                else if (!idling && ready)
                {
                    selectGroupCB.Enabled = false;
                    groupHeaderBtn.Enabled = false;
                    Icon = busyIcon;
                    ready = false;
                }
            }
            catch { }
		}
		void ConnectGroup(object s, EventArgs e)
		{
			int ind=selectGroupCB.SelectedIndex;
            if (ind < 0)
                return;
            Group newGroup = groupManager.SubscribedGroups[ind];
            if (ind >= 0 && groupManager.SetCurrentGroup(newGroup))
			{
				Cursor=Cursors.WaitCursor;
				try
				{
					headerListView.Items.Clear();
				}
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
#endif
                }
                groupManager.StartConnectGroup();
                this.Text = shortGroupNames[ind];
				lastID=-1;
				subjectPatternInput.Text="";
			}
		}
        void StartLoadingNewHeaders(object s, EventArgs e)
		{
            int skip = 0;
            try { skip = int.Parse(skipBox.Text); }
            catch { skip = 0; }
            skipBox.Text = "skip";
            groupManager.StartLoadingNewHeaders(filterBox.Checked, skip);
		}
        void ShowMessage(string message, Common.MessageType level)
		{
			statusLabel.Text=message;
			switch(level)
			{
				case Common.MessageType.Info:
					statusLabel.ForeColor=Color.Black;
					statusLabel.BackColor=Color.LightGray;
					break;
				case Common.MessageType.Warning:
					statusLabel.ForeColor=Color.Red;
					statusLabel.BackColor=Color.LightGray;
					break;
				case Common.MessageType.Error:
					statusLabel.ForeColor=Color.Black;
					statusLabel.BackColor=Color.Red;
					break;
			}
		}
		void DownloadSelectedItems(object s, EventArgs e)
		{
            for (int i = 0; i < headerListView.SelectedIndices.Count; i++)
			{
                ListViewItem lvi=headerListView.Items[headerListView.SelectedIndices[i]];
                string itemGroupName = lvi.SubItems[ArticleHeader.Item.NumFields - 1].Text;
                if (itemGroupName != groupManager.CurrentGroup.ShortName)
				{
					ShowMessage(lvi.SubItems[2]+" is not in current group: "+itemGroupName, Common.MessageType.Warning);
					continue;
				}
                ArticleHeader.Item ahi = (ArticleHeader.Item)(lvi.Tag);
				groupManager.AddToDownloadList(ahi);
				lastID=ahi.TrueID;
			}
			groupManager.StartDownloading();
		}
		void ActivateSelectedItem(object s, EventArgs e)
		{
            if (headerListView.SelectedIndices.Count == 0)
				return;
            ListViewItem lvi = headerListView.Items[headerListView.SelectedIndices[0]];
            ArticleHeader.Item ahi = (ArticleHeader.Item)(lvi.Tag);
			if(ahi.Downloaded)
				ShowLoadedImage(ahi.ArticleFileName);
			else
			{
                string itemGroupName = lvi.SubItems[ArticleHeader.Item.NumFields - 1].Text;
                if (itemGroupName != groupManager.CurrentGroup.ShortName)
				{
					ShowMessage(lvi.SubItems[2]+" is not in current group: "+itemGroupName, Common.MessageType.Warning);
					return;
				}
				groupManager.AddToDownloadList(ahi);
				lastID=ahi.TrueID;
				groupManager.StartDownloading();
			}
		}
		void CancelDownloading(object s, EventArgs e)
		{
			groupManager.CancelDownloading();
		}
		void RemoveOldHeaders(object s, EventArgs e)
		{
            if (headerListView.SelectedIndices.Count == 0)
				return;
            ListViewItem lvi = headerListView.Items[headerListView.SelectedIndices[0]];
            ArticleHeader.Item ahi = (ArticleHeader.Item)(lvi.Tag);
            int cutID = ahi.TrueID;
            DialogResult res = MessageBox.Show(this, "Are you sure you want to delete headers older then " +
                cutID + "?", "Delete headers warning", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
			{
				Cursor=Cursors.WaitCursor;
                groupManager.RemoveOldHeaders(cutID);
                Cursor = Cursors.Default;
			}
		}
        void RemoveDeletedHeaders(object s, EventArgs e)// not called
		{
            int nDeleted = headerListView.SelectedIndices.Count;
            if (nDeleted == 0)
                return;
            DialogResult res = MessageBox.Show(this, "Are you sure you want to delete " + nDeleted + " headers?",
                "Delete headers warning", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
            {
                Cursor = Cursors.WaitCursor;
                for (int i = 0; i < headerListView.SelectedIndices.Count; i++)
                    ((ArticleHeader)headerListView.Items[headerListView.SelectedIndices[i]].Tag).ArticleId = 0;
                groupManager.RemoveDeletedHeaders();
                Cursor = Cursors.Default;
            }
        }
		void MarkSelectedItemsUnread(object s, EventArgs e)
		{
            for (int i = 0; i < headerListView.SelectedIndices.Count; i++)
			{
                ListViewItem lvi = headerListView.Items[headerListView.SelectedIndices[i]];
                groupManager.MarkUnread((ArticleHeader.Item)lvi.Tag);
			}
		}
		void GetFilteredHeaders(object s, EventArgs e)
        {
            if (groupManager.CurrentGroup == null)
            {
                MessageBox.Show("No group selected");
                return;
            }
			Cursor=Cursors.WaitCursor;
            groupManager.ResetHeaders(subjectPatternInput.Text, groupManager.CurrentGroup);
			Cursor=Cursors.Default;
		}
		string EnterContinuationFileName(string group, int id)
		{
			nameForm=new Form();
			nameForm.SetBounds(0, 0, 700, 60, BoundsSpecified.Size);
			nameForm.Text=group+": data file name for "+id;
			TextBox textBox=new TextBox();
			textBox.Location = new System.Drawing.Point(4, 4);
			textBox.Name = "textBox";
			textBox.Size = new System.Drawing.Size(585, 20);
			Button button = new Button();
			button.Location = new System.Drawing.Point(595, 4);
            button.Size = new System.Drawing.Size(80, 20);
            button.Text = "Accept";
            button.Click += new EventHandler(CloseNameForm);
			nameForm.FormBorderStyle=FormBorderStyle.FixedSingle;
			nameForm.AcceptButton=button;
			nameForm.Controls.Add(button);
			nameForm.Controls.Add(textBox);
			nameForm.StartPosition=FormStartPosition.CenterParent;
			nameForm.TopMost=true;
            //Console.WriteLine("EnterContinuationFileName shown");
            DialogResult res = nameForm.ShowDialog(this);
            //Console.WriteLine("EnterContinuationFileName result="+res.ToString());
            if (res == DialogResult.OK)
            {
                //Console.WriteLine("Filename='" + textBox.Text+"'");
                return textBox.Text;
            }
            return "";
		}
		void CloseNameForm(object s, EventArgs e)
		{
			nameForm.Close();
		}
		void WindowResized(object s, EventArgs e)
		{
			headerListView.Size = new System.Drawing.Size(ClientSize.Width, ClientSize.Height-38);
		}
		void ShowLoadedImage(string fullFileName)
		{
			try
			{
				if(imageListForm==null || imageListForm.IsDisposed)
                    imageListForm = new ImageListForm(groupManager.Navigator.DirInfo(DirName.NewArticles), groupManager);
				imageListForm.Show();
				if(fullFileName!=null && fullFileName.Length>0)
					imageListForm.ViewImage(fullFileName);
			}
			catch(Exception ex)
			{
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                ShowMessage("Unable to create image view window: " + ex.Message, Common.MessageType.Warning);
			}
		}
		void reconnectButton_Click(object s, EventArgs e)
		{
			Cursor=Cursors.WaitCursor;
			groupManager.Reconnect();
		}
        private void headerListView_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            try
            {
                ArticleHeader.Item header = groupManager.Headers[e.ItemIndex];
                e.Item = new ListViewItem(header.Fields);
                e.Item.Tag = header;
                if (header.DownloadedElsewhere)
                    e.Item.BackColor = downloadedElswhereColor;
                else if (header.DownloadingFailed)
                    e.Item.BackColor = unavailableColor;
                else if (header.Downloaded)
                    e.Item.BackColor = downloadedColor;
                else if (header.MarkedForDownloading)
                    e.Item.BackColor = markedForDownloadingColor;
                else if(header.EarlierPartMissing)
                    e.Item.BackColor = earlierPartMissingColor;
                else if (header.NotLoaded)
                    e.Item.BackColor = availableColor;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                e.Item = new ListViewItem("........................");
            }
        }
        private void NewsReaderFormClosing(object sender, FormClosingEventArgs e)
        {
            if (updateTimer != null)
            {
                updateTimer.Stop();
                updateTimer.Dispose();
            }
            if (groupManager != null)
				groupManager.Cleanup();
			if(imageListForm!=null && !imageListForm.IsDisposed)
				imageListForm.Close();
        }
        private void selectGroupButton_Click(object sender, EventArgs e)
        {
            Cursor=Cursors.WaitCursor;
            GroupListForm glf = new GroupListForm(groupManager);
            Cursor = Cursors.Default;
            glf.ShowDialog();
            UpdateGroupCB();
        }

        private void serverBtn_Click(object sender, EventArgs e)
        {
            ServerBrowserWindow glf = new ServerBrowserWindow();
            glf.ShowDialog();
        }
	}
}