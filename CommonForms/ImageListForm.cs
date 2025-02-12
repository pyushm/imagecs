using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;

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
		ComboBox infoModeBox;
		private System.ComponentModel.Container components = null;
        IAssociatedPath associatedPath;
        ImageDirInfo sourceDir;	                // direcory to build sourceCollection from
        string[] searchList;                    // list of items from search
        bool srcNewArticles;                    // source is NewArticles
        public ImageFileInfo.ImageList Images    { get; private set; } = null; // images to be displayed 
        ImageList thumbnails;                   // currently displayed thumbnailes
        ImageViewForm viewForm;                 // form displaying active image of ImageCollection
        System.Windows.Forms.Timer listUpdateTimer;
        int updateListFrequency = 300;          // update frequency of list change, ms
        bool redrawRequest = true;
        private Button previousSetButton;
        private Button nextSetButton;
        private RadioButton listButton;
        private RadioButton groupButton;
        private RadioButton autoButton;
        private ComboBox sizeBox;
        protected override void Dispose(bool disposing)
		{
            Images?.Clear();
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
		void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImageListForm));
            this.imageListView = new System.Windows.Forms.ListView();
            this.sortNameButton = new System.Windows.Forms.Button();
            this.moveAllButton = new System.Windows.Forms.Button();
            this.infoModeBox = new System.Windows.Forms.ComboBox();
            this.previousSetButton = new System.Windows.Forms.Button();
            this.nextSetButton = new System.Windows.Forms.Button();
            this.sizeBox = new System.Windows.Forms.ComboBox();
            this.listButton = new System.Windows.Forms.RadioButton();
            this.groupButton = new System.Windows.Forms.RadioButton();
            this.autoButton = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // imageListView
            // 
            this.imageListView.BackColor = System.Drawing.SystemColors.Control;
            this.imageListView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.imageListView.GridLines = true;
            this.imageListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.imageListView.HideSelection = false;
            this.imageListView.LabelEdit = true;
            this.imageListView.Location = new System.Drawing.Point(0, 60);
            this.imageListView.Margin = new System.Windows.Forms.Padding(6);
            this.imageListView.Name = "imageListView";
            this.imageListView.OwnerDraw = true;
            this.imageListView.Size = new System.Drawing.Size(1919, 477);
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
            this.sortNameButton.Location = new System.Drawing.Point(138, 4);
            this.sortNameButton.Margin = new System.Windows.Forms.Padding(6);
            this.sortNameButton.Name = "sortNameButton";
            this.sortNameButton.Size = new System.Drawing.Size(84, 46);
            this.sortNameButton.TabIndex = 2;
            this.sortNameButton.Text = "Sort";
            this.sortNameButton.Click += new System.EventHandler(this.SortByName);
            // 
            // moveAllButton
            // 
            this.moveAllButton.Location = new System.Drawing.Point(598, 4);
            this.moveAllButton.Margin = new System.Windows.Forms.Padding(6);
            this.moveAllButton.Name = "moveAllButton";
            this.moveAllButton.Size = new System.Drawing.Size(176, 46);
            this.moveAllButton.TabIndex = 9;
            this.moveAllButton.Text = "Move All To...";
            this.moveAllButton.Click += new System.EventHandler(this.MoveAll);
            // 
            // infoModeBox
            // 
            this.infoModeBox.Location = new System.Drawing.Point(942, 8);
            this.infoModeBox.Margin = new System.Windows.Forms.Padding(6);
            this.infoModeBox.Name = "infoModeBox";
            this.infoModeBox.Size = new System.Drawing.Size(140, 33);
            this.infoModeBox.TabIndex = 10;
            // 
            // previousSetButton
            // 
            this.previousSetButton.Font = new System.Drawing.Font("Webdings", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.previousSetButton.Image = ((System.Drawing.Image)(resources.GetObject("previousSetButton.Image")));
            this.previousSetButton.Location = new System.Drawing.Point(0, 4);
            this.previousSetButton.Margin = new System.Windows.Forms.Padding(6);
            this.previousSetButton.Name = "previousSetButton";
            this.previousSetButton.Size = new System.Drawing.Size(52, 42);
            this.previousSetButton.TabIndex = 40;
            this.previousSetButton.Click += new System.EventHandler(this.previousSetButton_Click);
            // 
            // nextSetButton
            // 
            this.nextSetButton.Font = new System.Drawing.Font("Webdings", 12F);
            this.nextSetButton.Image = ((System.Drawing.Image)(resources.GetObject("nextSetButton.Image")));
            this.nextSetButton.Location = new System.Drawing.Point(60, 4);
            this.nextSetButton.Margin = new System.Windows.Forms.Padding(6);
            this.nextSetButton.Name = "nextSetButton";
            this.nextSetButton.Size = new System.Drawing.Size(52, 42);
            this.nextSetButton.TabIndex = 41;
            this.nextSetButton.Click += new System.EventHandler(this.nextSetButton_Click);
            // 
            // sizeBox
            // 
            this.sizeBox.Location = new System.Drawing.Point(786, 8);
            this.sizeBox.Margin = new System.Windows.Forms.Padding(6);
            this.sizeBox.Name = "sizeBox";
            this.sizeBox.Size = new System.Drawing.Size(140, 33);
            this.sizeBox.TabIndex = 42;
            // 
            // listButton
            // 
            this.listButton.AutoSize = true;
            this.listButton.Location = new System.Drawing.Point(367, 12);
            this.listButton.Name = "listButton";
            this.listButton.Size = new System.Drawing.Size(77, 29);
            this.listButton.TabIndex = 43;
            this.listButton.Text = "List";
            this.listButton.UseVisualStyleBackColor = true;
            this.listButton.CheckedChanged += new System.EventHandler(this.listViewselectionChanged);
            // 
            // groupButton
            // 
            this.groupButton.AutoSize = true;
            this.groupButton.Location = new System.Drawing.Point(462, 12);
            this.groupButton.Name = "groupButton";
            this.groupButton.Size = new System.Drawing.Size(113, 29);
            this.groupButton.TabIndex = 45;
            this.groupButton.Text = "Groups";
            this.groupButton.UseVisualStyleBackColor = true;
            this.groupButton.CheckedChanged += new System.EventHandler(this.listViewselectionChanged);
            // 
            // autoButton
            // 
            this.autoButton.AutoSize = true;
            this.autoButton.Checked = true;
            this.autoButton.Location = new System.Drawing.Point(253, 12);
            this.autoButton.Name = "autoButton";
            this.autoButton.Size = new System.Drawing.Size(87, 29);
            this.autoButton.TabIndex = 46;
            this.autoButton.TabStop = true;
            this.autoButton.Text = "Auto";
            this.autoButton.UseVisualStyleBackColor = true;
            this.autoButton.CheckedChanged += new System.EventHandler(this.listViewselectionChanged);
            // 
            // ImageListForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1940, 570);
            this.Controls.Add(this.autoButton);
            this.Controls.Add(this.groupButton);
            this.Controls.Add(this.listButton);
            this.Controls.Add(this.sizeBox);
            this.Controls.Add(this.nextSetButton);
            this.Controls.Add(this.previousSetButton);
            this.Controls.Add(this.infoModeBox);
            this.Controls.Add(this.moveAllButton);
            this.Controls.Add(this.sortNameButton);
            this.Controls.Add(this.imageListView);
            this.Margin = new System.Windows.Forms.Padding(6);
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
        public ImageListForm(DirectoryInfo di, string[] list, IAssociatedPath paths) { Initialize(di, list, paths); } // call from found matches
        public ImageListForm(DirectoryInfo di, IAssociatedPath paths) { Initialize(di, null, paths); } // call from News Reader
        void Initialize(DirectoryInfo di, string[] list, IAssociatedPath paths)
		{
            if (di == null)
                return;
            if (!di.Exists)
                MessageBox.Show(di.Name + " does not exist", "Can't open directory");
            try
            {
                InitializeComponent();
                srcNewArticles = list == null && Navigator.IsSpecDir(di, SpecName.NewArticles);
                sourceDir = new ImageDirInfo(di);
                if (Navigator.IsSpecDir(di, SpecName.Downloaded))
                {
                    autoButton.Checked = groupButton.Checked = false;
                    autoButton.Enabled = groupButton.Enabled = listButton.Enabled = false;
                    listButton.Checked = true;
                }
                searchList = list;
                associatedPath = paths;
                nextSetButton.Visible = previousSetButton.Visible = !srcNewArticles;
                imageListView.VirtualMode = true;
                Text = sourceDir.RealPath;
                infoModeBox.Items.AddRange(Enum.GetNames(typeof(DirShowMode)));
                infoModeBox.SelectedIndex = 1;  // calls ModeChanged
                sizeBox.Items.AddRange(Enum.GetNames(typeof(InfoSize)));
                sizeBox.SelectedIndex = 1;  // calls ModeChanged
                thumbnails = new ImageList();
                thumbnails.ColorDepth = ColorDepth.Depth16Bit;
                imageListView.LargeImageList = thumbnails;
                infoModeBox.SelectedIndexChanged += delegate (object s, System.EventArgs e) { ShowImages(); };
                sizeBox.SelectedIndexChanged += delegate (object s, System.EventArgs e) { ShowImages(); };
                FormResized(null, null);
                ContextMenu selectMenu = new ContextMenu();
                selectMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                new MenuItem("Open with Paint", new EventHandler(OpenPaint)),
                new MenuItem("Move to ...", new EventHandler(MoveSelected)),
                new MenuItem("Copy to ...", new EventHandler(CopySelected)),
                new MenuItem("Delete", new EventHandler(DeleteSelected)) });
                imageListView.ContextMenu = selectMenu;
                listUpdateTimer = new System.Windows.Forms.Timer();
                listUpdateTimer.Interval = updateListFrequency;
                listUpdateTimer.Tick += new EventHandler(UpdateList);
                listUpdateTimer.Start();
                infoModeBox.Visible = sourceDir.DirInfo.GetDirectories().Length > 7;
                ShowImages();
                Load += ImageViewForm_Load;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Can't open directory"); }
        }
        private void ImageViewForm_Load(object sender, EventArgs e)
        {
            Graphics g = CreateGraphics();
            if (g != null)
            {
                dpiScaleY = g.DpiY / 96;
                Height += (int)((ImageFileInfo.ThumbnailSize().Height - 7) * dpiScaleY) - ClientSize.Height;
                g.Dispose();
            }
        }
        void ImageListForm_FormClosing(object s, FormClosingEventArgs e)
        {
            Images?.Clear();
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
            try
            {
                if (Images == null || imageListView == null)
                    return;
                if (!Images.ValidDirectory)
                    Images.Clear();
                if (imageListView.VirtualListSize != Images.Count)
                {
                    if (viewForm != null)
                        ViewImage(Images.LastAdded);
                    imageListView.VirtualListSize = Images.Count;
                    int dc = sourceDir.DirCount();
                    Text = sourceDir.RealPath + ": " + sourceDir.ImageCount() + " images " + Images.GroupCount + " groups " + (dc == 0 ? "" : ", " + dc + " directories ");
                }
                UpdateThumbnail();
            }
            catch (Exception) { }
        }
        void UpdateThumbnail()                     // updates visible images
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
                    ImageFileInfo f = Images[i];
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
        void imageListView_Click(object s, System.EventArgs e)
        {
            BringToFront();
            ImageFileInfo d = SelectedImageFile();
            if (d != null && d.IsGroupHead)
                d.Group.Expanded = !d.Group.Expanded;
            Images.RebuildDisplayedList();
        }
        void ActivateSelectedItem(object s, System.EventArgs e)
		{
            ImageFileInfo d = SelectedImageFile();
            if(d==null)
                return;
            if (d != null && d.IsGroupHead)
                d.Group.Expanded = !d.Group.Expanded;
            Images.RebuildDisplayedList();
            try
            {
                if (!d.IsHeader)
				{
                    if (d.IsImage || d.IsMultiLayer)
                    {
                        ViewImage(d);
                        associatedPath.ActiveImageName = d.FSPath;
                    }
                    else if (d.IsMovie)
                    {
                        associatedPath.RunVideoFile = d;
                    }
                }
				else
				{
                    DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(d.FSPath));
                    if (di.Exists)
                    {
                        ImageListForm sif = new ImageListForm(di, associatedPath);
                        sif.Show();
                    }
				}
			}
            catch(Exception ex)
            {
                string mes = ex.Message;
            }
            imageListView.SelectedIndices.Clear();
        }
        void ViewImage(ImageFileInfo ifi)	
		{
            if (ifi == null || string.IsNullOrEmpty(ifi.FSPath))
                return;
			if(viewForm==null || viewForm.IsDisposed)
                viewForm = new ImageViewForm(this);
            viewForm.ShowNewImage(Images.SetActiveFile(ifi));
		}
        ImageFileInfo SelectedImageFile()       
        {
            if (imageListView.SelectedIndices.Count == 0)
                return null;
            int ind=imageListView.SelectedIndices[0];
            return (ImageFileInfo)imageListView.Items[ind].Tag;
        }
        string SelectedItemFullName()           
        {
            ImageFileInfo d = SelectedImageFile();
            if(d!=null)
                return d.FSPath; 
           return "";
        }
        void EmptyDirHandler()
        {
            DirectoryInfo directory = sourceDir.DirInfo;
            if (!directory.Exists || directory.GetDirectories().Length > 0 || Navigator.IsSpecDir(directory))
                return;
            FileInfo[] files = directory.GetFiles();
            foreach(FileInfo fi in files)
                if ((new ImageFileInfo(fi)).IsKnown)
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
                if (items == 0)
                    directory.Delete();
            }
            finally { }
        }
        public void DeleteActiveImage()
        {   // called by child image window
            MoveFilesTo(new ImageFileInfo[] { Images.ActiveFile }, null);
        }
        void DeleteSelected(object s, System.EventArgs e)
        {
            int nDeleted = imageListView.SelectedIndices.Count;
            if (nDeleted == 0)
                return;
            DialogResult res;
            if (nDeleted > 1)
                res = MessageBox.Show(this, "Are you sure you want to delete " + nDeleted + " image?",
                    "Delete images warning", MessageBoxButtons.YesNo);
            else
                res = MessageBox.Show(this, "Are you sure you want to delete " + SelectedItemFullName() + "?",
                    "Delete images warning", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
            {
                ArrayList deleteFileList = new ArrayList();
                Cursor = Cursors.WaitCursor;
                bool hasDirs = false;
                for (int i = 0; i < imageListView.SelectedIndices.Count; i++)
                {
                    var ifi = (ImageFileInfo)imageListView.Items[imageListView.SelectedIndices[i]].Tag;
                    if (ifi != null && !ifi.IsHeader)
                        deleteFileList.Add(ifi);
                    else if (ifi != null)
                        hasDirs = true;
                }
                MoveFilesTo((ImageFileInfo[])deleteFileList.ToArray(typeof(ImageFileInfo)), null);
                if (hasDirs)
                    MessageBox.Show("Deleting directories not supported; " + deleteFileList.Count + " files deleted");
                imageListView.SelectedIndices.Clear();
                Cursor = Cursors.Default;
            }
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
            ImageListForm sif = new ImageListForm(toDirectory, associatedPath);
            sif.Show();
            if (msg != null && msg.Length > 0)
                MessageBox.Show(msg);
            Cursor = Cursors.Default;
        }
        string MoveFilesTo(ImageFileInfo[] fileListToMove, DirectoryInfo to)  // fileList == null means all content of Images
        {
            string msg = "";
            try
            {
                int lastDeleted = -1;
                if (fileListToMove != null)
                    foreach (var ifi in fileListToMove)
                    {
                        if (lastDeleted < ifi.DisplayListIndex)
                            lastDeleted = ifi.DisplayListIndex;
                    }
                msg = Images.MoveFiles(fileListToMove, to);
                imageListView.VirtualListSize = 0;
                imageListView.ArrangeIcons(ListViewAlignment.SnapToGrid);
                int newInd = Images.Count == 0 || lastDeleted == -1 ? -1 : Math.Min(lastDeleted + 1, Images.Count - 1);
                if (newInd >= 0)
                {
                    if (viewForm != null)
                        viewForm.ShowNewImage(Images[newInd]);
                    imageListView.EnsureVisible(newInd);
                }
                Text = sourceDir.RealPath + ": " + Images.Count + " images";
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
        void SortByName(object s, System.EventArgs e)
		{
            Images.SortFileListByRealName();
            imageListView.VirtualListSize = 0;
		}
		void OpenPaint(object s, System.EventArgs e)
		{
			string fileName=SelectedItemFullName();
			if(fileName.Length>0)
                Process.Start(associatedPath.PaintExe, '\"' + fileName + '\"');
		}
        void ShowImages()
        {
            try
            {
                DirShowMode infoType = infoModeBox.Visible ? (DirShowMode)Enum.Parse(typeof(DirShowMode), (string)infoModeBox.SelectedItem) : DirShowMode.Detail;
                double scale = (int)Enum.Parse(typeof(InfoSize), (string)sizeBox.SelectedItem) / 10.0;
                IntSize si = ImageFileInfo.PixelSize(infoType);
                if (si.Height * scale > 255)
                    scale = 255.0 / si.Height;
                thumbnails.ImageSize = new Size((int)(si.Width * scale), (int)(si.Height * scale));
                var mode = Navigator.IsSpecDir(sourceDir.DirInfo, SpecName.Downloaded) ? listButton.Text : autoButton.Text;
                Images = searchList != null ? new ImageFileInfo.ImageList(sourceDir, mode, searchList) : new ImageFileInfo.ImageList(sourceDir, infoType, mode);
                Images.notifyEmptyDir += EmptyDirHandler;
                imageListView.VirtualListSize = 0;
                imageListView.ArrangeIcons(ListViewAlignment.SnapToGrid);
            }
            catch { }
       }
		void imageListView_AfterLabelEdit(object s, System.Windows.Forms.LabelEditEventArgs e)
		{
            ImageFileInfo file = SelectedImageFile();
            Images.Rename(file, e.Label);
            imageListView.SelectedIndices.Clear();
        }
        void imageListView_RetrieveVirtualItem(object s, RetrieveVirtualItemEventArgs e)
        {
            try
            {
                ImageFileInfo f = Images[e.ItemIndex];
                if (f != null)
                {
                    //e.Item = new ListViewItem(f.IsHeader ? f.GetDirInfo() : f.RealName);
                    e.Item = new ListViewItem(f.RealName);
                    FontStyle fs = f.IsMultiLayer ? FontStyle.Underline : f.IsExact ? FontStyle.Italic : FontStyle.Regular;
                    e.Item.Font = new Font("Arial", 10, fs);
                    //if (f.IsHeader)
                    //    e.Item.ForeColor = Color.BlueViolet;
                }
                if (e.Item == null)
                {
                    e.Item = new ListViewItem("......");
                    e.Item.Tag = null;
                }
                else
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
                if (!redrawRequest || Images[e.ItemIndex] == null || !IsItemVisible(e.ItemIndex))
                    return;
                Image im = Images[e.ItemIndex].GetThumbnail();
                if(im==null) 
                    im = ImageFileInfo.notLoadedImage;
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
        private void listViewselectionChanged(object sender, EventArgs e) 
        { 
            var b = (RadioButton)sender;
            if(b.Checked && Images != null)
                Images.SetViewMode(b.Text); 
        }
    }
}
