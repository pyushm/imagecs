using System;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Windows.Input;

namespace ImageProcessor
{
    public partial class DirectorySelectionForm : Form
    {
        Navigator navigator;				// object handling directory tree
        DirectoryInfo selectedNode = null;	// currently selected directory
        DirectoryInfoImages itemInfoImages;	// emulator of hover over TreeView, shows info
        DirectoryInfo dirSelection;
        DirectorySelectionForm()
        {
            navigator = new Navigator();
            InitializeComponent();
            locationTreeView.ItemHeight = 22;
            TreeNode node = locationTreeView.Nodes.Add(Navigator.Root.Name);
            node.Tag = Navigator.Root;
            node.Nodes.Add("fake");
            itemInfoImages = new DirectoryInfoImages(infoImagePanel);
        }
        public static DirectoryInfo GetDirectory()
        {
            DirectorySelectionForm selector = new DirectorySelectionForm();
            DialogResult res = selector.ShowDialog();
            return selector.dirSelection;
        }
        void RetrievNodes(object sender, TreeViewCancelEventArgs e)
        {
            Cursor = System.Windows.Forms.Cursors.WaitCursor;
            TreeNode node = e.Node;
            node.Nodes.Clear();
            DirectoryInfo[] dia = navigator.GetDirectories(((DirectoryInfo)node.Tag));
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
            Cursor = System.Windows.Forms.Cursors.Default;
        }
        void DisplaySelectedNode(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
            if (e.Node == null || e.Node.Tag == null)
                return;
            selectedNode = (DirectoryInfo)e.Node.Tag;
            if (selectedNode.Exists)
            {
                itemInfoImages.ReDrawInfoImages(selectedNode);
                inputOutputBox.Text = Path.Combine(selectedNode.Parent.FullName, Scramble.UnMangle(selectedNode.Name));
                if (Navigator.IsSpecDir(selectedNode.Parent, SpecName.AllDevicy))
                    inputOutputBox.Text += '/';
            }
        }
        void moveToButton_Click(object sender, System.EventArgs e)
        {   // sets 'dirSelection' and closes dialog
            if (inputOutputBox.Text.Length == 0)
            {
                MessageBox.Show("No directory selected");
                return;
            }
            var realDir = new DirectoryInfo(inputOutputBox.Text); // unmangled full path of destination dir
            var pp = realDir.Parent != null && realDir.Parent.Parent != null ? realDir.Parent.Parent : null;
            if (pp == null || !Navigator.IsSpecDir(pp, SpecName.AllDevicy)) 
                dirSelection = realDir;
            else
            {
                if (DataAccess.Private)
                {
                    dirSelection = new DirectoryInfo(Path.Combine(realDir.Parent.FullName, Scramble.MangleForced(realDir.Name))); // scrambled
                    if (realDir.Exists)
                    {   // prevents creating directory with same real name
                        MessageBox.Show("Unmangled dir " + realDir.Name + " exists", "Can't create directory: duplicate name detected");
                        return;
                    }
                }
                if (!dirSelection.Exists)
                {
                    int c = 3;
                    do
                    {
                        try
                        {
                            dirSelection.Create();
                            Thread.Sleep(300);
                            dirSelection = new DirectoryInfo(dirSelection.FullName);
                            Close();
                            return;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Attempt " + c + " to create directory " + dirSelection.FullName, ex.Message);
                        }
                    } while (!dirSelection.Exists && c-- > 0);
                    MessageBox.Show("Directory " + dirSelection.FullName + " was NOT created after " + c + " attempts");
                    dirSelection = null;
                }
            }
            Close();
        }
    }
}
