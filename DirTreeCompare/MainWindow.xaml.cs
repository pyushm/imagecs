using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using ImageProcessor;
using System.ComponentModel;
using System.Threading;

namespace DirTreeCompare
{
    internal class DifferenceInfo
    {
        public readonly string Dir;
        public readonly string CompareDir;
        public readonly string Name;
        public readonly Relation reason;
        public readonly bool IsDirectory;
        public bool IsHeader { get { return reason == Relation.Header; } }
        public bool IsProcessed = false;
        public DifferenceInfo(string title, string path, string comparePath, Relation rel)
        {
            Dir = path;
            CompareDir = comparePath;
            reason = rel;
            IsDirectory = title.Length > 0 && title[0] == DirDifference.dirPrefix;
            Name = IsDirectory ? title.Substring(1) : title;
        }
    }
    internal class SelectableTreeItem : TreeViewItem
    {
        System.Windows.Controls.CheckBox cb;
        internal SelectableTreeItem(DifferenceInfo info, Color color)
        {
            DockPanel dp = new DockPanel();
            cb = new System.Windows.Controls.CheckBox();
            TextBlock tb = new TextBlock();
            tb.Text = FileName.UnMangleFile(info.Name);
            if(info.IsDirectory)
                tb.TextDecorations = TextDecorations.Underline;
            tb.Background = new SolidColorBrush(color);
            dp.Children.Add(cb);
            dp.Children.Add(tb);
            IsExpanded = info.Dir.Length == 0;
            Header = dp;
            IsEnabled = true;
            cb.Checked += delegate (object s, RoutedEventArgs e) { SetSelection(true); };
            cb.Unchecked += delegate (object s, RoutedEventArgs e) { SetSelection(false); };
            Tag = info;
        }
        public void SetSelection(bool state)
        {
            cb.IsChecked = state;
            foreach (var si in Items)
                (si as SelectableTreeItem)?.SetSelection(state);
        }
        public bool IsChecked { get { return (bool)cb.IsChecked; } }
    }
    public partial class MainWindow : Window
    {
        delegate void Operation();
        Navigator navigator = new Navigator();
        List<Form> invoked = new List<Form>();
        List<DifferenceInfo> selectedItems = new List<DifferenceInfo>();
        BackgroundWorker backgroundWorker;
        Operation workerOperation = null;
        System.Windows.Controls.TreeView list = null;
        double progressMax = 100;
        List<string> errors = new List<string>();
        public MainWindow()
        {
            InitializeComponent();
            Closing += CompareWindow_Closing;
            leftList.SelectedItemChanged += ActivateSelection;
            rightList.SelectedItemChanged += ActivateSelection;
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += delegate (object sender, DoWorkEventArgs e) { workerOperation?.Invoke(); };
            backgroundWorker.RunWorkerCompleted += RunWorkerCompleted;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.ProgressChanged += delegate (object sender, ProgressChangedEventArgs e) { opProgress.Value = e.ProgressPercentage; };
            textBox1.Text = @"C:\data\OldC\stuff";
            textBox2.Text = @"\\MSI\OldC\stuff";
        }
        void PerformBackgroundOperation(Operation op, System.Windows.Controls.TreeView listBox)
        {
            SetWorkerOperation(op);
            list = listBox;
            selectedItems.Clear();
            SetSelectedItems(list);
            if (selectedItems.Count == 0)
            {
                System.Windows.MessageBox.Show("Nothing selected");
                SetWorkerOperation(null);
                return;
            }
            if (workerOperation == DeleteFrom)
            {
                string q = "Deleting " + selectedItems.Count + " files from " + list.Name + ". Are you sure?";
                if(System.Windows.MessageBox.Show(q, "", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    SetWorkerOperation(null);
                    return;
                }
            }
            backgroundWorker.RunWorkerAsync();
        }
        void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                RemovedProcessedItems(list);
                if (errors.Count > 0)
                {
                    string str = "";
                    foreach (var s in errors)
                    {
                        str += s;
                        if (s.Length > 0 && s[s.Length - 1] != Environment.NewLine[Environment.NewLine.Length - 1])
                            str += Environment.NewLine;
                    }
                    System.Windows.MessageBox.Show(str, "Errors");
                }
            }
            finally
            {
                list = null;
                selectedItems.Clear();
                SetWorkerOperation(null);
            }
        }
        void SetWorkerOperation(Operation op)
        {
            bool state = op == null;
            compareBtn.IsEnabled = state;
            copyFromLeftBtn.IsEnabled = state;
            copyFromRightBtn.IsEnabled = state;
            deleteFromRightBtn.IsEnabled = state;
            deleteFromLeftBtn.IsEnabled = state;
            workerOperation = op;
        }
        void ActivateSelection(object sender, RoutedPropertyChangedEventArgs<object> e) { ActivateItem((TreeViewItem)e.NewValue); e.Handled = true; }
        void Compare_Click(object sender, RoutedEventArgs e)
        {
            opProgress.Value = 0;
            Cursor = System.Windows.Input.Cursors.Wait;
            Thread.Sleep(100);
            DirectoryInfo d1 = null;
            try { d1 = new DirectoryInfo(textBox1.Text); }
            catch { System.Windows.MessageBox.Show(textBox1.Text + " is not a valid path", "Directory 1 failure"); }
            DirectoryInfo d2 = null;
            try { d2 = new DirectoryInfo(textBox2.Text); }
            catch { System.Windows.MessageBox.Show(textBox2.Text + " is not a valid path", "Directory 2 failure"); }
            string errors = "";
            if (d1 != null && d2 != null)
            {
                List<DirDifference> res = navigator.CompareDirectoryTree(d1, d2);
                leftList.Items.Clear();
                rightList.Items.Clear();
                foreach (var dd in res)
                {
                    if (dd.CompareError != null)
                        errors += dd.CompareError;
                    if (dd.Count1 > 0)
                    {
                        TreeViewItem hi = new SelectableTreeItem(new DifferenceInfo(dd.Path1, dd.Path1, dd.Path2, Relation.Header), Colors.White);
                        leftList.Items.Add(hi);
                        foreach (var s in dd.List(Relation.Only1))
                            hi.Items.Add(new SelectableTreeItem(new DifferenceInfo(s, dd.Path1, dd.Path2, Relation.Only1), Colors.White));
                        foreach (var s in dd.List(Relation.Newer1))
                            hi.Items.Add(new SelectableTreeItem(new DifferenceInfo(s, dd.Path1, dd.Path2, Relation.Newer1), Colors.LightGreen));
                        foreach (var s in dd.List(Relation.Newer2))
                            hi.Items.Add(new SelectableTreeItem(new DifferenceInfo(s, dd.Path1, dd.Path2, Relation.Newer2), Colors.LightSalmon));
                        foreach (var s in dd.List(Relation.DifferentLength))
                            hi.Items.Add(new SelectableTreeItem(new DifferenceInfo(s, dd.Path1, dd.Path2, Relation.DifferentLength), Colors.LightSkyBlue));
                    }
                    if (dd.Count2 > 0)
                    {
                        TreeViewItem hi = new SelectableTreeItem(new DifferenceInfo(dd.Path2, dd.Path2, dd.Path1, Relation.Header), Colors.White);
                        rightList.Items.Add(hi);
                        foreach (var s in dd.List(Relation.Only2))
                            hi.Items.Add(new SelectableTreeItem(new DifferenceInfo(s, dd.Path2, dd.Path1, Relation.Only2), Colors.White));
                        foreach (var s in dd.List(Relation.Newer2))
                            hi.Items.Add(new SelectableTreeItem(new DifferenceInfo(s, dd.Path2, dd.Path1, Relation.Newer2), Colors.LightGreen));
                        foreach (var s in dd.List(Relation.Newer1))
                            hi.Items.Add(new SelectableTreeItem(new DifferenceInfo(s, dd.Path2, dd.Path1, Relation.Newer1), Colors.LightSalmon));
                        foreach (var s in dd.List(Relation.DifferentLength))
                            hi.Items.Add(new SelectableTreeItem(new DifferenceInfo(s, dd.Path2, dd.Path1, Relation.DifferentLength), Colors.LightSkyBlue));
                    }
                }
            }
            Cursor = System.Windows.Input.Cursors.Arrow;
            if (errors.Length > 0)
                System.Windows.MessageBox.Show(errors, "Comparison errors");
        }
        void Select_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = ((System.Windows.Controls.Button)sender).Name == "b1" ? textBox1.Text : textBox2.Text;
                DialogResult result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    if (((System.Windows.Controls.Button)sender).Name == "b1")
                        textBox1.Text = fbd.SelectedPath;
                    else if (((System.Windows.Controls.Button)sender).Name == "b2")
                        textBox2.Text = fbd.SelectedPath;
                }
            }
        }
        void SetSelectedItems(ItemsControl ic)
        {
            foreach (SelectableTreeItem v in ic.Items)
            {
                if (v.Items.Count > 0)
                    SetSelectedItems(v);
                else if (v.IsChecked)
                    selectedItems.Add(v.Tag as DifferenceInfo);
            }
        }
        void CopyFromLeft(object sender, RoutedEventArgs e) { PerformBackgroundOperation(CopyFrom, leftList); } 
        void CopyFromRight(object sender, RoutedEventArgs e) { PerformBackgroundOperation(CopyFrom, rightList); }
        void DeleteFromLeft(object sender, RoutedEventArgs e) { PerformBackgroundOperation(DeleteFrom, leftList); }
        void DeleteFromRight(object sender, RoutedEventArgs e) { PerformBackgroundOperation(DeleteFrom, rightList); }
        void CopyFrom()
        {
            errors.Clear();
            double progress = 0;
            int total = selectedItems.Count;
            foreach (var v in selectedItems)
                if (v.IsDirectory)
                    total += Directory.GetFiles(Path.Combine(v.Dir, v.Name)).Length - 1;
            double delta = progressMax / total;
            try
            {
                foreach (var v in selectedItems)
                {
                    try
                    {
                        if (v.IsDirectory)
                        {
                            DirectoryInfo di = Directory.CreateDirectory(Path.Combine(v.CompareDir, v.Name));
                            string[] files = Directory.GetFiles(Path.Combine(v.Dir, v.Name));
                            foreach (string f in files)
                            {
                                using (FileStream SourceStream = File.Open(f, FileMode.Open))
                                {
                                    using (FileStream DestinationStream = File.Create(Path.Combine(di.FullName, Path.GetFileName(f))))
                                    {
                                        SourceStream.CopyTo(DestinationStream);
                                        progress += delta;
                                        backgroundWorker.ReportProgress((int)(progress + 0.5));
                                    }
                                }
                            }
                        }
                        else
                        {
                            using (FileStream SourceStream = File.Open(Path.Combine(v.Dir, v.Name), FileMode.Open))
                            {
                                using (FileStream DestinationStream = File.Create(Path.Combine(v.CompareDir, v.Name)))
                                {
                                    SourceStream.CopyTo(DestinationStream);
                                    progress += delta;
                                    backgroundWorker.ReportProgress((int)(progress + 0.5));
                                }
                            }
                        }
                        v.IsProcessed = true;    // marked to be removed
                                                 //Debug.WriteLine(v.Name + " processed");
                    }
                    catch (Exception ex)
                    {
                        errors.Add(v.Name + ": " + ex.Message);
                        //Debug.WriteLine(v.Name + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add("Processing stopped: " + ex.Message);
            }
        }
        void DeleteFrom()
        {
            errors.Clear();
            double progress = 0;
            double delta = progressMax / selectedItems.Count;
            try
            {
                foreach (var v in selectedItems)
                {
                    try
                    {
                        if (v.IsDirectory)
                            Directory.Delete(Path.Combine(v.Dir, v.Name), true);
                        else
                            File.Delete(Path.Combine(v.Dir, v.Name));
                        v.IsProcessed = true;    // marked to be removed
                        progress += delta;
                        backgroundWorker.ReportProgress((int)(progress + 0.5));
                        //Debug.WriteLine(v.Name + " deleted");

                    }
                    catch (Exception ex)
                    {
                        errors.Add(v.Name + ": " + ex.Message);
                        //Debug.WriteLine(v.Name + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add("Processing stopped: " + ex.Message);
            }
        }
        void RemovedProcessedItems(ItemsControl ic)
        {
            (ic as SelectableTreeItem)?.SetSelection(false);
            List<TreeViewItem> toKeep = new List<TreeViewItem>();
            foreach (TreeViewItem v in ic.Items)
            {
                RemovedProcessedItems(v);
                DifferenceInfo di = v.Tag as DifferenceInfo;
                if (di == null || !di.IsProcessed)
                    toKeep.Add(v);
            }
            ic.Items.Clear();
            foreach (var v in toKeep)
                ic.Items.Add(v);
        }
        void CompareWindow_Closing(object sender, CancelEventArgs e)
        {
            foreach (var form in invoked)
            {
                if (form != null && !form.IsDisposed)
                    form.Close();
            }
        }
        void ActivateItem(TreeViewItem tvi)
        {
            if (tvi == null)
                return;
            DifferenceInfo di = tvi.Tag as DifferenceInfo;
            if (di == null || di.IsHeader )
                return;
            string path = Path.Combine(di.Dir, di.Name);
            if (di.IsDirectory)
            {
                if (!Directory.Exists(path))
                {
                    System.Windows.MessageBox.Show("Directory " + path + " doew not exist");
                    return;
                }
                ImageListForm sif = new ImageListForm(new DirectoryInfo(path), navigator);
                invoked.Add(sif);
                try
                {
                    sif.Show();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
            else
            {
                FileInfo fi = new FileInfo(path);
                ImageFileName dt = new ImageFileName(path);
                if (!fi.Exists)
                    System.Windows.MessageBox.Show("File " + path + " does not exist");
                else if (dt.IsImage)
                {
                    ImageViewForm viewForm = new ImageViewForm(null);
                    invoked.Add(viewForm);
                    viewForm.ShowNewImage(path);
                }
                else if (dt.Is(DataType.MOV))
                    Process.Start(navigator.MediaExe, '\"' + path + '\"');
                else if (dt.Is(DataType.EncMOV))
                {
                    try
                    {
                        Cursor = System.Windows.Input.Cursors.Wait;
                        DataAccess.DecryptToFile(navigator.MediaTmpLocation, fi.FullName);
                        Process.Start(navigator.MediaExe, navigator.MediaTmpLocation);
                    }
                    finally { Cursor = System.Windows.Input.Cursors.Arrow; }
                }
                else
                    System.Windows.MessageBox.Show(path + Environment.NewLine + "Length=" + fi.Length + Environment.NewLine + fi.LastWriteTime.ToLongDateString());
            }
        }
    }
}