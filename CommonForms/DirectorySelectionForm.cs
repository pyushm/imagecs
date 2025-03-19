using System;
using System.Windows.Forms;
using System.IO;
using System.Threading;

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
            TreeNode node = locationTreeView.Nodes.Add(Navigator.Root.Name);
            node.Tag = Navigator.Root;
            node.Nodes.Add("fake");
            itemInfoImages = new DirectoryInfoImages(locationTreeView, infoImagePanel);
        }
        public static DirectoryInfo GetDirectory()
        {
            DirectorySelectionForm selector = new DirectorySelectionForm();
            DialogResult res = selector.ShowDialog();
            return selector.dirSelection;
        }
        void RetrievNodes(object sender, TreeViewCancelEventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            TreeNode node = e.Node;
            node.Nodes.Clear();
            DirectoryInfo[] dia = navigator.GetDirectories(((DirectoryInfo)node.Tag));
            string[] fna = new string[dia.Length];
            for (int i = 0; i < dia.Length; i++)
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
        void DisplaySelectedNode(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
            if (e.Node == null || e.Node.Tag == null)
                return;
            selectedNode = (DirectoryInfo)e.Node.Tag;
            if (selectedNode.Exists)
            {
                itemInfoImages.ShowInfoImages(selectedNode);
                inputOutputBox.Text = Path.Combine(selectedNode.Parent.FullName, FileName.UnMangle(selectedNode.Name));
                if (Navigator.IsSpecDir(selectedNode.Parent, SpecName.AllDevicy))
                    inputOutputBox.Text += '/';
            }
        }
        void moveToButton_Click(object sender, System.EventArgs e)
        {
            if (inputOutputBox.Text.Length == 0)
            {
                MessageBox.Show("No directory selected");
                return;
            }
            var realDir = new DirectoryInfo(inputOutputBox.Text); // unmangled dir name 
            var FSdir = new DirectoryInfo(Path.Combine(realDir.Parent.FullName, FileName.RawMangle(realDir.Name))); // scrambled
            dirSelection = DataAccess.PrivateAccessEnforced ? FSdir : realDir;
            var otherDir = DataAccess.PrivateAccessEnforced ? realDir : FSdir;
            if(otherDir.Exists)
            {   // prevents creating directory with same real name
                string msg = DataAccess.PrivateAccessEnforced ? "Unmangled dir " + realDir.Name + " exists" : "Mangled dir " + FSdir.Name + " exists";
                MessageBox.Show(msg, "Can't create directory: duplicate name detected");
                return;
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
                        MessageBox.Show("Attempt " + c+" to create directory " + dirSelection.FullName, ex.Message);
                    }
                } while(!dirSelection.Exists && c-- > 0);
                MessageBox.Show("Directory " + dirSelection.FullName + " was NOT created after " + c +" attempts");
                dirSelection = null;
            }
            Close();
        }
    }
}
