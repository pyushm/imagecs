using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

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
        public ImageFileInfo.ImageList Images    { get; private set; } = null; // images to be displayed 
        bool listViewOnly;                      // indicate directory with list only viewing
        ImageList displayed;                    // used to set thumbnailes size 
        public List<ImageViewForm> viewForms = new List<ImageViewForm>(); // viewForms[0] displaying active image, others - static images
        System.Windows.Forms.Timer listUpdateTimer;
        int updateListFrequency = 300;          // update frequency of list change, ms
        bool redrawRequest = true;
        private CheckBox groupViewBox;
        private ComboBox infoSizeBox;
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
            this.imageListView = new System.Windows.Forms.ListView();
            this.sortNameButton = new System.Windows.Forms.Button();
            this.moveAllButton = new System.Windows.Forms.Button();
            this.infoModeBox = new System.Windows.Forms.ComboBox();
            this.infoSizeBox = new System.Windows.Forms.ComboBox();
            this.groupViewBox = new System.Windows.Forms.CheckBox();
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
            this.sortNameButton.Location = new System.Drawing.Point(264, -2);
            this.sortNameButton.Margin = new System.Windows.Forms.Padding(6);
            this.sortNameButton.Name = "sortNameButton";
            this.sortNameButton.Size = new System.Drawing.Size(100, 46);
            this.sortNameButton.TabIndex = 2;
            this.sortNameButton.Text = "Sort";
            this.sortNameButton.Click += new System.EventHandler(this.SortByName);
            // 
            // moveAllButton
            // 
            this.moveAllButton.Location = new System.Drawing.Point(15, 2);
            this.moveAllButton.Margin = new System.Windows.Forms.Padding(6);
            this.moveAllButton.Name = "moveAllButton";
            this.moveAllButton.Size = new System.Drawing.Size(200, 46);
            this.moveAllButton.TabIndex = 9;
            this.moveAllButton.Text = "Move All To...";
            this.moveAllButton.Click += new System.EventHandler(this.MoveAll);
            // 
            // infoModeBox
            // 
            this.infoModeBox.Location = new System.Drawing.Point(695, 6);
            this.infoModeBox.Margin = new System.Windows.Forms.Padding(6);
            this.infoModeBox.Name = "infoModeBox";
            this.infoModeBox.Size = new System.Drawing.Size(140, 33);
            this.infoModeBox.TabIndex = 10;
            // 
            // infoSizeBox
            // 
            this.infoSizeBox.Location = new System.Drawing.Point(543, 6);
            this.infoSizeBox.Margin = new System.Windows.Forms.Padding(6);
            this.infoSizeBox.Name = "infoSizeBox";
            this.infoSizeBox.Size = new System.Drawing.Size(140, 33);
            this.infoSizeBox.TabIndex = 42;
            // 
            // groupViewBox
            // 
            this.groupViewBox.AutoSize = true;
            this.groupViewBox.Location = new System.Drawing.Point(373, 6);
            this.groupViewBox.Name = "groupViewBox";
            this.groupViewBox.Size = new System.Drawing.Size(152, 29);
            this.groupViewBox.TabIndex = 43;
            this.groupViewBox.Text = "Group view";
            this.groupViewBox.UseVisualStyleBackColor = true;
            this.groupViewBox.Click += new System.EventHandler(this.groupViewBox_Click);
            // 
            // ImageListForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1940, 570);
            this.Controls.Add(this.groupViewBox);
            this.Controls.Add(this.infoSizeBox);
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
                searchList = list;
                sourceDir = new ImageDirInfo(di);
                listViewOnly = searchList != null || Navigator.IsSpecDir(sourceDir.DirInfo, SpecName.Downloaded) || Navigator.IsSpecDir(sourceDir.DirInfo, SpecName.Work);
                if (!listViewOnly)
                {
                    var par = di.Parent;
                    listViewOnly = par != null && ((Navigator.IsSpecDir(par, SpecName.Work) || par.Parent != null && Navigator.IsSpecDir(par.Parent, SpecName.Work)));
                }
                Images = searchList == null ? new ImageFileInfo.ImageList(sourceDir, listViewOnly) : new ImageFileInfo.ImageList(sourceDir, searchList);
                groupViewBox.Enabled = !listViewOnly;
                groupViewBox.Checked = Images.PreferedGroupView;
                associatedPath = paths;
                imageListView.VirtualMode = true;
                Text = sourceDir.RealPath;
                infoModeBox.Items.AddRange(Enum.GetNames(typeof(DirShowMode)));
                infoModeBox.SelectedIndex = 1;  // calls ModeChanged
                infoSizeBox.Items.AddRange(Enum.GetNames(typeof(InfoSize)));
                infoSizeBox.SelectedIndex = 1;  // calls ModeChanged
                displayed = new ImageList();
                displayed.ColorDepth = ColorDepth.Depth16Bit;
                imageListView.LargeImageList = displayed;
                viewForms.Add(null);
                infoModeBox.SelectedIndexChanged += delegate (object s, System.EventArgs e) { RecreateThumbnails(); };
                infoSizeBox.SelectedIndexChanged += delegate (object s, System.EventArgs e) { RecreateThumbnails(); };
                FormResized(null, null);
                ContextMenu selectMenu = new ContextMenu();
                selectMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                new MenuItem("Open static", new EventHandler(OpenStatic)),
                new MenuItem("Move to ...", new EventHandler(MoveSelected)),
                new MenuItem("Copy to ...", new EventHandler(CopySelected)),
                new MenuItem("Delete", new EventHandler(DeleteSelected)) });
                imageListView.ContextMenu = selectMenu; 
                listUpdateTimer = new System.Windows.Forms.Timer();
                listUpdateTimer.Interval = updateListFrequency;
                listUpdateTimer.Tick += new EventHandler(synchronizeThumbnails);
                listUpdateTimer.Start();
                infoModeBox.Visible = sourceDir.DirInfo.GetDirectories().Length > 7;
                RecreateThumbnails();
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
            for (int i=0; i <viewForms.Count; i++)
            {
                var viewForm = viewForms[i];
                if (viewForm != null && !viewForm.IsDisposed)
                    viewForm.Close();
            }
            viewForms.Clear();
            if(listUpdateTimer != null)
            {
                listUpdateTimer.Stop();
                listUpdateTimer.Dispose();
            }
        }
        void synchronizeThumbnails(object s, EventArgs e) // updates list and images on timer
        {   // synchronizes visible thumbnails with image list
            try
            {
                if (Images == null || imageListView == null)
                    return;
                if (!Images.ValidDirectory)
                    Images.Clear();
                if (imageListView.VirtualListSize != Images.Count)
                {
                    if (viewForms[0] != null)
                        ViewImage(Images.LastAdded);
                    imageListView.VirtualListSize = Images.Count;
                    int dc = sourceDir.DirCount();
                    Text = sourceDir.RealPath + ": " + sourceDir.ImageCount + " images " + Images.GroupCount + " groups " + (dc == 0 ? "" : ", " + dc + " directories ");
                }
                if (imageListView.VirtualListSize == 0)
                    return;
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
                    if (f == null)
                        continue;
                    f.CheckExistsSetUpdate();
                    if (f.NeedThumbnail && f.UpdateThumbnail() != null) 
                        imageListView.Invalidate(imageListView.GetItemRect(i)); // causes redraw request and blinking
                }
            }
            catch (Exception) { }
        }
        void RecreateThumbnails()
        {
            try
            {
                DirShowMode infoType = infoModeBox.Visible ? (DirShowMode)Enum.Parse(typeof(DirShowMode), (string)infoModeBox.SelectedItem) : DirShowMode.Detail;
                Images.SetInfoType(infoType);
                double scale = (int)Enum.Parse(typeof(InfoSize), (string)infoSizeBox.SelectedItem) / 10.0;
                IntSize si = ImageFileInfo.PixelSize(infoType);
                if (si.Height * scale > 255)
                    scale = 255.0 / si.Height;
                displayed.ImageSize = new Size((int)(si.Width * scale), (int)(si.Height * scale));
                Images.notifyEmptyDir += EmptyDirHandler;
                imageListView.VirtualListSize = 0;
                imageListView.ArrangeIcons(ListViewAlignment.SnapToGrid);
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
            if (listViewOnly)
                return;
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
                if (!d.IsDirInfo)
				{
                    if (d.IsImage || d.IsMultiLayer)
                    {
                        ViewImage(d);
                        associatedPath.SetActiveImageName(d.FSPath);
                    }
                    else if (d.IsMovie)
                    {
                        associatedPath.RunVideoFile(d);
                    }
                }
				else
				{
                    DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(d.FSPath));
                    if (di.Exists)
                    {
                        ImageListForm sif = new ImageListForm(di, associatedPath);
                        associatedPath.SetActiveDir(di);
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
            if (viewForms[0] == null || viewForms[0].IsDisposed)
                viewForms[0] = new ImageViewForm(this);
            viewForms[0].ShowNewImage(Images.SetActiveFile(ifi));
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
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions] // added to capture access violation by try block
        void EmptyDirHandler()
        {
            try
            {
                DirectoryInfo directory = sourceDir?.DirInfo;
                if (directory == null || !directory.Exists) { Close(); return; }
                if (directory.GetDirectories().Length > 0 || Navigator.IsSpecDir(directory))
                    return; // keep dirs with subdirs or special dirs
                FileInfo[] files = directory.GetFiles();
                foreach(FileInfo fi in files)
                    if ((new ImageFileInfo(fi)).IsKnown)
                        return; // dir has images
                int items = files.Length;
                if (items > 0)
                {
                    DialogResult res = items > 1 ? MessageBox.Show(sourceDir.RealPath + " contains no images" + Environment.NewLine +
                                                        "Directory contains " + items.ToString() + " items" + Environment.NewLine +
                                                        "Do you want to delete directory?", "Delete directory warning", MessageBoxButtons.YesNo, 
                                                        MessageBoxIcon.None, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0x40000) : DialogResult.Yes;
                    if (res == DialogResult.Yes)
                    {
                        foreach (var f in files)
                            f.Delete();
                        items = 0;
                    }
                }
                if (items == 0) // delete if nothing there
                {
                    directory.Delete();
                    sourceDir.ClearDirectory();
                }
            }
            catch(Exception) { Close(); }
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
                List<ImageFileInfo> deleteFileList = new List<ImageFileInfo>();
                Cursor = Cursors.WaitCursor;
                bool hasDirs = false;
                for (int i = 0; i < imageListView.SelectedIndices.Count; i++)
                {
                    var ifi = (ImageFileInfo)imageListView.Items[imageListView.SelectedIndices[i]].Tag;
                    if (ifi != null && !ifi.IsDirInfo)
                        deleteFileList.Add(ifi);
                    else if (ifi != null)
                        hasDirs = true;
                }
                MoveFilesTo(deleteFileList.ToArray(), null);
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
                var res = MessageBox.Show(toDirectory.FullName + " does not exist", "Create new directory?", MessageBoxButtons.YesNo);
                if (res == DialogResult.No)
                    return;
                Directory.CreateDirectory(toDirectory.FullName);
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
        string MoveFilesTo(ImageFileInfo[] imagesToMove, DirectoryInfo to)  // fileList == null means all content of Images
        {
            string msg = "";
            try
            {
                int lastDeleted = -1;
                if (imagesToMove != null)
                    foreach (var ifi in imagesToMove)
                    {
                        if (lastDeleted < ifi.DisplayListIndex)
                            lastDeleted = ifi.DisplayListIndex;
                    }
                int newInd = Images.Count == 0 || lastDeleted == -1 ? -1 : lastDeleted + 1 > Images.Count - 1 ? 0 : lastDeleted +1;
                var ni = Images[newInd];
                msg = Images.MoveFiles(imagesToMove, to);
                imageListView.VirtualListSize = 0;
                imageListView.ArrangeIcons(ListViewAlignment.SnapToGrid);
                imageListView.Invalidate();
                if (newInd >= 0)
                {
                    if (viewForms[0] != null)
                        viewForms[0].ShowNewImage(ni);
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
		void OpenStatic(object s, System.EventArgs e)
		{
			var ifi = SelectedImageFile();
            var vf = new ImageViewForm(this, viewForms.Count);
            viewForms.Add(vf);
            vf.ShowNewImage(ifi);
        }
		void imageListView_AfterLabelEdit(object s, System.Windows.Forms.LabelEditEventArgs e)
		{
            ImageFileInfo fi = SelectedImageFile();
            if(fi == null) 
                return;
            if (fi.IsDirInfo)
                MessageBox.Show(fi.RealName + " is a directory header: use Navigator to rename");
            Images.Rename(fi, e.Label);
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
                    e.Item = new ListViewItem(f.ShortName);
                    FontStyle fs = f.IsMultiLayer ? FontStyle.Underline : f.IsExact ? FontStyle.Italic : FontStyle.Regular;
                    e.Item.Font = new Font("Arial", 10, fs);
                    if (f.IsLowQuality && (f.IsDirInfo || !f.IsInfoImage))
                        e.Item.ForeColor = Color.Red;
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
                var ifi = Images[e.ItemIndex];
                Image im = ifi?.GetThumbnail();
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
            RecreateThumbnails();
        }
        internal void previousSetButton_Click(object sender, EventArgs e)
        {
            sourceDir = new ImageDirInfo(NavigateGroup(-1));
            RecreateThumbnails();
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
        private void groupViewBox_Click(object sender, EventArgs e)
        {
            Images.ViewList = groupViewBox.Checked;
            Images.RebuildDisplayedList();
        }
    }
}
