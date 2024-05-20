using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace ImageProcessor
{
    public class ImageListForm : Form
	{
        public enum InfoSize
        {
            Small = 50, // % of full size
            Large = 100,
        }
        float dpiScaleY = 1;
        ListView imageListView;
		Button sortNameButton;
		Button moveAllButton;
        Button findButton;
		TextBox findPatternBox;
		ComboBox infoModeBox;
		private System.ComponentModel.Container components = null;
        IAssociatedPath associatedPath;
        ImageDirInfo sourceDir;	                // direcory to build sourceCollection from
        bool subdirListMode;                    // if true shows info images of subdirectories
        string[] extList;
        bool tempStore;                         // underlying directory is new article temp store if true
        public ImageFileInfo.Collection ImageCollection { get; private set; } = null; // currently displayed images
        ImageList thumbnails;                   // currently displayed thumbnailes
        ImageViewForm viewForm;                 // form displaying active image of ImageCollection
        Timer listUpdateTimer;
        int updateListFrequency = 300;          // update frequency of list change, ms
        bool redrawRequest = true;
        private Button previousSetButton;
        private Button nextSetButton;       
        private ComboBox sizeBox;
        protected override void Dispose(bool disposing)
		{
            ImageCollection?.Clear();
            if (disposing)
			{
				if(components != null)
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImageListForm));
            this.imageListView = new System.Windows.Forms.ListView();
            this.sortNameButton = new System.Windows.Forms.Button();
            this.findButton = new System.Windows.Forms.Button();
            this.findPatternBox = new System.Windows.Forms.TextBox();
            this.moveAllButton = new System.Windows.Forms.Button();
            this.infoModeBox = new System.Windows.Forms.ComboBox();
            this.previousSetButton = new System.Windows.Forms.Button();
            this.nextSetButton = new System.Windows.Forms.Button();
            this.sizeBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // imageListView
            // 
            this.imageListView.BackColor = System.Drawing.SystemColors.Control;
            this.imageListView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.imageListView.GridLines = true;
            this.imageListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.imageListView.LabelEdit = true;
            this.imageListView.Location = new System.Drawing.Point(0, 31);
            this.imageListView.Name = "imageListView";
            this.imageListView.OwnerDraw = true;
            this.imageListView.Size = new System.Drawing.Size(844, 248);
            this.imageListView.TabIndex = 0;
            this.imageListView.UseCompatibleStateImageBehavior = false;
            this.imageListView.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.imageListView_AfterLabelEdit);
            this.imageListView.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.imageListView_DrawItem);
            this.imageListView.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.imageListView_RetrieveVirtualItem);
            this.imageListView.Click += new System.EventHandler(this.imageListView_Click);
            this.imageListView.DoubleClick += new System.EventHandler(this.ActivateSelectedItem);
            // 
            // sortNameButton
            // 
            this.sortNameButton.Location = new System.Drawing.Point(69, 2);
            this.sortNameButton.Name = "sortNameButton";
            this.sortNameButton.Size = new System.Drawing.Size(42, 24);
            this.sortNameButton.TabIndex = 2;
            this.sortNameButton.Text = "Sort";
            this.sortNameButton.Click += new System.EventHandler(this.SortByName);
            // 
            // findButton
            // 
            this.findButton.Location = new System.Drawing.Point(117, 2);
            this.findButton.Name = "findButton";
            this.findButton.Size = new System.Drawing.Size(42, 24);
            this.findButton.TabIndex = 5;
            this.findButton.Text = "Find";
            this.findButton.Click += new System.EventHandler(this.FindFiles);
            // 
            // findPatternBox
            // 
            this.findPatternBox.Location = new System.Drawing.Point(158, 4);
            this.findPatternBox.Name = "findPatternBox";
            this.findPatternBox.Size = new System.Drawing.Size(112, 20);
            this.findPatternBox.TabIndex = 6;
            // 
            // moveAllButton
            // 
            this.moveAllButton.Location = new System.Drawing.Point(299, 2);
            this.moveAllButton.Name = "moveAllButton";
            this.moveAllButton.Size = new System.Drawing.Size(88, 24);
            this.moveAllButton.TabIndex = 9;
            this.moveAllButton.Text = "Move All To...";
            this.moveAllButton.Click += new System.EventHandler(this.MoveAll);
            // 
            // infoModeBox
            // 
            this.infoModeBox.Location = new System.Drawing.Point(471, 4);
            this.infoModeBox.Name = "infoModeBox";
            this.infoModeBox.Size = new System.Drawing.Size(72, 21);
            this.infoModeBox.TabIndex = 10;
            // 
            // previousSetButton
            // 
            this.previousSetButton.Font = new System.Drawing.Font("Webdings", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.previousSetButton.Image = ((System.Drawing.Image)(resources.GetObject("previousSetButton.Image")));
            this.previousSetButton.Location = new System.Drawing.Point(0, 2);
            this.previousSetButton.Name = "previousSetButton";
            this.previousSetButton.Size = new System.Drawing.Size(26, 22);
            this.previousSetButton.TabIndex = 40;
            this.previousSetButton.Click += new System.EventHandler(this.previousSetButton_Click);
            // 
            // nextSetButton
            // 
            this.nextSetButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.nextSetButton.Image = ((System.Drawing.Image)(resources.GetObject("nextSetButton.Image")));
            this.nextSetButton.Location = new System.Drawing.Point(30, 2);
            this.nextSetButton.Name = "nextSetButton";
            this.nextSetButton.Size = new System.Drawing.Size(26, 22);
            this.nextSetButton.TabIndex = 41;
            this.nextSetButton.Click += new System.EventHandler(this.nextSetButton_Click);
            // 
            // sizeBox
            // 
            this.sizeBox.Location = new System.Drawing.Point(393, 4);
            this.sizeBox.Name = "sizeBox";
            this.sizeBox.Size = new System.Drawing.Size(72, 21);
            this.sizeBox.TabIndex = 42;
            // 
            // ImageListForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(959, 281);
            this.Controls.Add(this.sizeBox);
            this.Controls.Add(this.nextSetButton);
            this.Controls.Add(this.previousSetButton);
            this.Controls.Add(this.infoModeBox);
            this.Controls.Add(this.moveAllButton);
            this.Controls.Add(this.findPatternBox);
            this.Controls.Add(this.findButton);
            this.Controls.Add(this.sortNameButton);
            this.Controls.Add(this.imageListView);
            this.Name = "ImageListForm";
            this.Text = "Image List Form";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ImageListForm_FormClosing);
            this.Resize += new System.EventHandler(this.FormResized);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion
        bool IsItemVisible(int i)               
        {
            redrawRequest = false;
            bool ret = imageListView.GetItemRect(i).IntersectsWith(imageListView.ClientRectangle);
            redrawRequest = true;
            return ret;
        }
        public ImageListForm(DirectoryInfo di, string[] list, IAssociatedPath paths) { Initialize(di, list, false, paths); }
        public ImageListForm(DirectoryInfo di, bool tempStore_, IAssociatedPath paths) { Initialize(di, null, tempStore_, paths);  }
        public ImageListForm(DirectoryInfo di, IAssociatedPath paths) { Initialize(di, null, true, paths); }
        void Initialize(DirectoryInfo di, string[] list, bool tempStore_, IAssociatedPath paths)
		{
            InitializeComponent();
            tempStore = tempStore_;
            sourceDir = new ImageDirInfo(di);
            extList = list;
            associatedPath = paths;
            if (tempStore)
            {
                nextSetButton.Visible=false;
                previousSetButton.Visible = false;
            }
            imageListView.VirtualMode = true;
            Text = sourceDir.RealPath;
            infoModeBox.Items.AddRange(Enum.GetNames(typeof(InfoType)));
            infoModeBox.SelectedIndex = 1;  // calls ModeChanged
            sizeBox.Items.AddRange(Enum.GetNames(typeof(InfoSize)));
            sizeBox.SelectedIndex = 1;  // calls ModeChanged
            thumbnails = new ImageList();
            thumbnails.ColorDepth = ColorDepth.Depth16Bit;
            imageListView.LargeImageList = thumbnails;
            infoModeBox.SelectedIndexChanged += delegate (object s, System.EventArgs e) { ShowImages(); };
            sizeBox.SelectedIndexChanged += delegate (object s, System.EventArgs e) { ShowImages(); };
            FormResized(null, null);
			ContextMenu selectMenu= new ContextMenu(); 
            selectMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                new MenuItem("Open with Paint", new EventHandler(OpenPaint)),
                new MenuItem("Move to ...", new EventHandler(MoveSelected)),
                new MenuItem("Copy to ...", new EventHandler(CopySelected)),
                new MenuItem("Delete", new EventHandler(DeleteSelected)) }); 
			imageListView.ContextMenu = selectMenu;
            listUpdateTimer = new Timer();
            listUpdateTimer.Interval = updateListFrequency;
            listUpdateTimer.Tick += new EventHandler(UpdateList);
            listUpdateTimer.Start();
            try { subdirListMode = !tempStore_ && (list != null || sourceDir.DirInfo.GetDirectories().Length > 0); }
            catch { }
            infoModeBox.Visible = subdirListMode;
            ShowImages();
            Load += ImageViewForm_Load;
        }
        private void ImageViewForm_Load(object sender, EventArgs e)
        {
            Graphics g = CreateGraphics();
            if (g != null)
            {
                dpiScaleY = g.DpiY / 96;
                Height -= (int)((ImageFileInfo.ThumbnailSize().Height + 13) * (dpiScaleY - 1));
                g.Dispose();
            }
        }
        void ImageListForm_FormClosing(object s, FormClosingEventArgs e)
        {
            ImageCollection?.Clear();
            if (viewForm != null && !viewForm.IsDisposed)
                viewForm.Close();
            if (listUpdateTimer != null)
            {
                listUpdateTimer.Stop();
                listUpdateTimer.Dispose();
            }
        }
        void UpdateList(object s, EventArgs e) // updates list and images on timer
        {
            if (ImageCollection == null)
                return;
            if (!ImageCollection.ValidDirectory)
                ImageCollection.Clear();
            if (viewForm != null)
            {
                if (imageListView.VirtualListSize < ImageCollection.Count)
                {
                    if(ImageCollection.IsAdded)
                        ViewImage(ImageCollection.Added.FSPath);
                }
                else if (ImageCollection.ActiveFileFSPath != "")
                {
                    FileInfo fi = new FileInfo(ImageCollection.ActiveFileFSPath);
                    if (!fi.Exists && ImageCollection.Last != null)
                        ViewImage(ImageCollection.Last.FSPath);
                }
            }
            if (imageListView != null && imageListView.VirtualListSize != ImageCollection.Count)
            {
                imageListView.VirtualListSize = ImageCollection.Count;
                Text = sourceDir.RealPath + ": " + ImageCollection.Count + (ImageCollection.DirMode ? " directories " : " images");
            }
            UpdateImages();
        }
        void UpdateImages()                     // updates visible images
        {
            if (imageListView.VirtualListSize == 0)
                return;
            try
            {
                redrawRequest = false;
                int firstVisible = -1;
                int lastVisible = -1;
                Rectangle rFirst = imageListView.GetItemRect(0);
                if (rFirst.IntersectsWith(imageListView.ClientRectangle))
                    firstVisible = 0;
                Rectangle rLast = imageListView.GetItemRect(imageListView.VirtualListSize - 1);
                if (rLast.IntersectsWith(imageListView.ClientRectangle))
                    lastVisible = imageListView.VirtualListSize - 1;
                int totalHeight = rLast.Y + rLast.Height - rFirst.Y;
                int seed = (int)(imageListView.VirtualListSize * (imageListView.ClientRectangle.Height / 2f - rFirst.Y) / totalHeight);
                if (firstVisible < 0)
                {
                    firstVisible = seed;
                    while (--firstVisible >= 0)
                        if (!imageListView.GetItemRect(firstVisible).IntersectsWith(imageListView.ClientRectangle))
                            break;
                    firstVisible++;
                }
                if (lastVisible < 0)
                {
                    lastVisible = seed;
                    while (++lastVisible < imageListView.VirtualListSize)
                        if (!IsItemVisible(lastVisible))
                            break;
                    lastVisible--;
                }
                redrawRequest = true;
                for (int i = firstVisible; i <= lastVisible; i++)
                {
                    ImageFileInfo f = ImageCollection[i];
                    f.CheckUpdate();
                    if (f.Modified)
                        imageListView.Invalidate(imageListView.GetItemRect(i));
                }
            }
            catch { }
        }
        void FormResized(object s, System.EventArgs e)
		{
			imageListView.Size=new Size(ClientSize.Width, ClientSize.Height-imageListView.Location.Y);
		}
		void DeleteSelected(object s, System.EventArgs e)
		{
			int nDeleted=imageListView.SelectedIndices.Count;
			if(nDeleted==0)
				return;
            if (ImageCollection.DirMode)
			{
                ListViewItem lvi = s as ListViewItem;
                MessageBox.Show("Image " + lvi?.Text + " is a directory. Deleting directories not supported");
				return;
			}
			DialogResult res;
			if(nDeleted>1)
				res=MessageBox.Show(this, "Are you sure you want to delete "+nDeleted+" image?", 
					"Delete images warning", MessageBoxButtons.YesNo);
			else
				res=MessageBox.Show(this, "Are you sure you want to delete "+SelectedItemFileName()+"?", 
					"Delete images warning", MessageBoxButtons.YesNo);
			if(res==DialogResult.Yes)
			{
				ArrayList deleteFileList=new ArrayList();
				Cursor=Cursors.WaitCursor;
				for(int i=0; i<imageListView.SelectedIndices.Count; i++)
                    deleteFileList.Add((ImageFileInfo)imageListView.Items[imageListView.SelectedIndices[i]].Tag);
                MoveFilesTo((ImageFileInfo[])deleteFileList.ToArray(typeof(ImageFileInfo)), null);
                imageListView.SelectedIndices.Clear();
                Cursor = Cursors.Default;
			}
		}
		void ActivateSelectedItem(object s, System.EventArgs e)
		{
            ImageFileInfo d = SelectedImageFile();
            if(d==null)
                return;
			try
			{
                if (!d.IsDirHeader)
				{
                    if (d.IsImage || d.IsMultiLayer)
                    {
                        ViewImage(d.FSPath);
                        associatedPath.ActiveImageName = d.FSPath;
                    }
                    else if (d.IsUnencryptedVideo)
                        Process.Start(associatedPath.MediaExe, '\"' + d.FSPath + '\"');
                    else if (d.IsEncryptedVideo)
                    {
                        try
                        {
                            Cursor = Cursors.WaitCursor;
                            DataAccess.DecryptToFile(associatedPath.MediaTmpLocation, d.FSPath);
                            Process.Start(associatedPath.MediaExe, associatedPath.MediaTmpLocation);
                        }
                        finally { Cursor = Cursors.Default; }
                    }
				}
				else
				{
                    DirectoryInfo di = d.IsDir ? new DirectoryInfo(d.FSPath) : new DirectoryInfo(Path.GetDirectoryName(d.FSPath));
                    if (di.Exists)
                    {
                        ImageListForm sif = new ImageListForm(di, d.IsLocalImages, associatedPath);
                        sif.Show();
                    }
				}
			}
            catch(Exception ex)
            {
                string mes = ex.Message;
            }
            if (imageListView.SelectedIndices.Count == 1)
                imageListView.SelectedIndices.Clear();
        }
        public void ViewImage(string filePath)	
		{
            if (filePath == null || filePath.Length == 0)
                return;
			if(viewForm==null || viewForm.IsDisposed)
                viewForm = new ImageViewForm(this);
            if (ImageCollection.SetActiveFile(filePath) != null)
                viewForm.ShowNewImage(filePath);
		}
        ImageFileInfo SelectedImageFile()       
        {
            if (imageListView.SelectedIndices.Count == 0)
                return null;
            int ind=imageListView.SelectedIndices[0];
            return (ImageFileInfo)imageListView.Items[ind].Tag;
        }
        string SelectedItemFileName()           
        {
            ImageFileInfo d = SelectedImageFile();
            if(d!=null)
                return d.FSPath; 
           return "";
        }
        public void DeleteActiveImage()			
		{
            MoveFilesTo(new ImageFileInfo[] { ImageCollection.ActiveFile }, null);
		}
        void EmptyDirHandler()
        {
            DirectoryInfo directory = sourceDir.DirInfo;
            if (!directory.Exists || directory.GetDirectories().Length > 0)
                return;
            FileInfo[] files = directory.GetFiles();
            foreach(FileInfo fi in files)
                if ((new ImageFileInfo(fi)).IsImage)
                    return;
            int items = files.Length;
            if (items > 0)
            {
                DialogResult res = items > 1 ? MessageBox.Show(sourceDir.RealPath + " contains no images" + Environment.NewLine +
                                                    "Directory contains " + items.ToString() + " items" + Environment.NewLine +
                                                    "Do you want to delete directory?",
                                                    "Delete directory warning", MessageBoxButtons.YesNo) : DialogResult.Yes;
                if (res == DialogResult.Yes)
                {
                    foreach (var f in files)
                        f.Delete();
                    items = 0;
                }
            }
            Close();
            try
            {
                if (items == 0 && !Navigator.IsSpecDir(directory))
                    directory.Delete();
            }
            finally { }
        }
        void MoveAll(object s, System.EventArgs e) { MoveOrCopySelected(false, false); }
		void MoveSelected(object s, System.EventArgs e) { MoveOrCopySelected(false, true); }
        void CopySelected(object s, System.EventArgs e) { MoveOrCopySelected(true, true); }
        void MoveOrCopySelected(bool copy, bool selection)
        {
            DirectoryInfo toDirectory = DirectorySelectionForm.GetDirectory();
            if (toDirectory == null)
                return;
            if (!toDirectory.Exists)
            {
                MessageBox.Show(toDirectory.FullName + " does not exist");
                return;
            }
            Cursor = Cursors.WaitCursor;
            ImageFileInfo[] fileList = null;
            ArrayList moveFileList = new ArrayList();
            if (selection)
            {
                for (int i = 0; i < imageListView.SelectedIndices.Count; i++)
                    moveFileList.Add((ImageFileInfo)imageListView.Items[imageListView.SelectedIndices[i]].Tag);
                fileList = (ImageFileInfo[])moveFileList.ToArray(typeof(ImageFileInfo));
                imageListView.SelectedIndices.Clear();
            }
            string msg = copy ? CopyFilesTo(fileList, toDirectory) : MoveFilesTo(fileList, toDirectory);
            ImageListForm sif = new ImageListForm(toDirectory, false, associatedPath);
            sif.Show();
            if (msg != null && msg.Length > 0)
                MessageBox.Show(msg);
            Cursor = Cursors.Default;
        }
        string MoveFilesTo(ImageFileInfo[] fileList, DirectoryInfo to)  // fileList == null means all
        {
            string msg = "";
            try
            {
                msg = ImageCollection.MoveFiles(fileList, to);
                imageListView.ArrangeIcons(ListViewAlignment.SnapToGrid);
                if (viewForm != null)
                {
                    int newActive = ImageCollection.ImageFileIndex(ImageCollection.ActiveFileFSPath);
                    if (newActive < 0)
                        return msg;
                    if (tempStore && newActive == 0)
                    {
                        newActive = ImageCollection.Count - 1;
                        ImageCollection.SetActiveFile(ImageCollection[newActive].FSPath);
                    }
                    viewForm.ShowNewImage(ImageCollection.ActiveFileFSPath);
                    imageListView.EnsureVisible(newActive);
                }
                if (ImageCollection.Count > 0)
                    Text = sourceDir.RealPath + ": " + ImageCollection.Count + " images";
            }
            catch { }
            return msg;
        }
        string CopyFilesTo(ImageFileInfo[] fileList, DirectoryInfo to)
        {
            string msg = "";
            foreach (ImageFileInfo d in fileList)
                if (d != null)
                {
                    try { File.Copy(d.FSPath, Path.Combine(to.FullName, Path.GetFileName(d.FSName) + Path.GetExtension(d.FSPath))); }
                    catch (Exception ex) { msg += d.FSPath + " was not copied: " + ex.Message + "  "; }    // legal exception
                }
            return msg;
        }
        void FindFiles(object s, System.EventArgs e)
		{
			string pattern=findPatternBox.Text;
            if (ImageCollection != null)
            {
                ImageCollection.FilterFiles(pattern);
                imageListView.VirtualListSize = 0;
            }
		}
		void SortByName(object s, System.EventArgs e)
		{
            ImageCollection.Sort(new ImageFileInfo.Collection.RealNameComparer());
            imageListView.VirtualListSize = 0;
		}
		void OpenPaint(object s, System.EventArgs e)
		{
			string fileName=SelectedItemFileName();
			if(fileName.Length>0)
                Process.Start(associatedPath.PaintExe, '\"' + fileName + '\"');
		}
        void ShowImages()
        {
            try
            {
                InfoType infoType = (InfoType)Enum.Parse(typeof(InfoType), (string)infoModeBox.SelectedItem);
                double scale = (int)Enum.Parse(typeof(InfoSize), (string)sizeBox.SelectedItem) / 10.0;
                IntSize si = ImageFileInfo.PixelSize(infoType);
                if (si.Height * scale > 255)
                    scale = 255.0 / si.Height;
                thumbnails.ImageSize = new Size((int)(si.Width * scale), (int)(si.Height * scale));
                ImageCollection = extList != null ? new ImageFileInfo.Collection(sourceDir, extList) :
                    subdirListMode ? new ImageFileInfo.Collection(sourceDir, infoType, tempStore) : new ImageFileInfo.Collection(sourceDir, tempStore);
                ImageCollection.notifyEmptyDir += EmptyDirHandler;
                imageListView.VirtualListSize = 0;
                imageListView.ArrangeIcons(ListViewAlignment.SnapToGrid);
            }
            catch { }
       }
        void imageListView_Click(object s, System.EventArgs e)
		{
			BringToFront();
		}
		void imageListView_AfterLabelEdit(object s, System.Windows.Forms.LabelEditEventArgs e)
		{
            ImageFileInfo file = SelectedImageFile();
            ImageCollection.Rename(file, e.Label);
		}
        void imageListView_RetrieveVirtualItem(object s, RetrieveVirtualItemEventArgs e)
        {
            try
            {
                ImageFileInfo f = ImageCollection[e.ItemIndex];
                e.Item = new ListViewItem(subdirListMode ? f.GetDirInfoName() : f.RealName);
                FontStyle fs = f.IsMultiLayer ? FontStyle.Underline : f.IsExact ? FontStyle.Italic : FontStyle.Regular;
                e.Item.Font = new Font("Arial", 10, fs);
                e.Item.Tag = f;
            }
            catch (Exception)
            {
                e.Item = new ListViewItem("......");
                e.Item.Tag = null;
            }
        }
        void imageListView_DrawItem(object s, DrawListViewItemEventArgs e)
        {
            try
            {
                if (!redrawRequest)
                    return;
                if (!IsItemVisible(e.ItemIndex))
                    return;
                Image im = ImageCollection[e.ItemIndex].GetThumbnail();
                float rw = e.Bounds.Width;
                float rh = e.Bounds.Height - 13 * dpiScaleY;
                float scale = Math.Min(rw / im.Width, rh / im.Height);
                float iw = im.Width * scale;
                float ih = im.Height * scale;
                if (imageListView.SelectedIndices.Contains(e.ItemIndex))
                {
                    var bm = new Bitmap((int)rw, (int)rh);
                    var g = Graphics.FromImage(bm);
                    g.FillRectangle(Brushes.Cyan, 0, 0, rw, e.Bounds.Height);
                    g.DrawImage(im, (rw - iw) / 2, (rh - ih) / 2, iw, ih);
                    e.Graphics.DrawImage(bm, e.Bounds.X, e.Bounds.Y, rw, rh);
                }
                else
                    e.Graphics.DrawImage(im, e.Bounds.X + (rw - iw) / 2, e.Bounds.Y + (rh - ih) / 2, iw, ih);
                e.DrawText(TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom);
                //e.DrawText(TextFormatFlags.Left | TextFormatFlags.Bottom);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + " ind=" + e.ItemIndex + " e.Bounds=" + e.Bounds);
            }
        }
        internal void nextSetButton_Click(object sender, EventArgs e)
        {
            sourceDir = new ImageDirInfo(NavigateGroup(1));
            ShowImages();
        }
        internal void previousSetButton_Click(object sender, EventArgs e)
        {
            sourceDir = new ImageDirInfo(NavigateGroup(-1));
            ShowImages();
        }
        DirectoryInfo NavigateGroup(int delta)
        {
            DirectoryInfo di = sourceDir.DirInfo;
            DirectoryInfo patent = di.Parent;
            DirectoryInfo[] siblings = patent.GetDirectories();
            int i=0;
            for (; i < siblings.Length; i++)
                if (siblings[i].Name == di.Name)
                    break;
            i += delta;
            if (i >= 0 && i < siblings.Length)
                return siblings[i];
            return di;
        }
    }
}
