using System;
using System.Drawing;
using System.Windows.Forms;

namespace NNTP
{
    public partial class ServerBrowserWindow : Form
    {
        HostManager manager;
        bool groupListUpdated = false;
        System.Windows.Forms.Timer updateTimer;
        public ServerBrowserWindow()
        {
            InitializeComponent();
            groupListlabel.Text += " (starts with "+Group.GroupNamePrefix+')';
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 1000;
            updateTimer.Tick += new EventHandler(UpdateState);
            updateTimer.Start();
        }
        void UpdateGroupList(Group[] groups)
        {
            if (groupListUpdated)
                return;
            serverList.Items.Add(new ListViewItem(manager.Fields));
            groupList.Items.Clear();
            foreach (GroupInfo gi in groups)
            {
                //if (filterBox.Text.Length > 0 && gi.Name.IndexOf(filterBox.Text) < 0)
                //    continue;
                ListViewItem lvi = new ListViewItem(gi.Fields);
                lvi.Tag = gi;
                groupList.Items.Add(lvi);
            }
            groupListUpdated = true;
        }
        void UpdateState(object s, System.EventArgs e)
        {
            if (manager == null)
                return;
            Common.LogEntry[] entry = manager.Messages.Retrieve();
            if (entry.Length > 0)
                ShowMessage(entry[entry.Length - 1].Message, entry[entry.Length - 1].Type);
            else
                ShowMessage(manager.ProcessState.ToString(), Common.MessageType.Info);
            if (manager.ProcessState == ProcessState.Disconnected)
                manager.StartConnectHost();
            else if (manager.ProcessState == ProcessState.Connected)
                manager.StartGetServerGroups();
            else if (manager.ProcessState == ProcessState.GotGroups)
                UpdateGroupList(manager.HostGroups);
            else if (manager.ProcessState == ProcessState.FailedConnect || manager.ProcessState == ProcessState.FailedGetGroups)
                FinalResult();
        }
        void FinalResult()
        {
            serverList.Items.Add(new ListViewItem(manager.Fields));
            ClearManager();
        }
        void ShowMessage(string message, Common.MessageType level)
        {
            statusBox.Text = message;
            statusBox.ForeColor = level == Common.MessageType.Info ? Color.Black : level == Common.MessageType.Warning ? Color.Yellow : Color.Red;
        }
        void CleateManager()
        {
            ClearManager();
            string hn=newServerBox.Text.Trim();
            if (hn.Length > 0)
                manager = new HostManager(hn);
            else
                manager = null;
            groupListUpdated = false;
            groupList.Items.Clear();
        }
        void ClearManager()
        {
            if (manager != null)
            {
                manager.Stop();
                manager = null;
            }
        }
        private void serverList_SelectedIndexChanged(object sender, EventArgs e)
        { }
        private void checkServerBtn_Click(object s, EventArgs e) { CleateManager(); }
    }
}
