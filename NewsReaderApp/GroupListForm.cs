using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NNTP
{
    public partial class GroupListForm : Form
    {
        GroupManager manager;
        GroupInfo[] serverGroups;
        List<Group> deletedSubscribedGroups;
        public GroupListForm(GroupManager manager_) 
        {
            manager = manager_;
            InitializeComponent();
            prefixLabel.Text = "Groups starting with " + Group.GroupNamePrefix;
            serverGroups = manager.CheckServerGroups(out deletedSubscribedGroups);
            UpdateSubscribedList();
            UpdateGroupList();
        }
        void UpdateGroupList()
        {
            allGroupListView.Items.Clear();
            foreach (GroupInfo gi in serverGroups)
            {
                if (filterBox.Text.Length > 0 && gi.Name.IndexOf(filterBox.Text) < 0)
                    continue;
                ListViewItem lvi = new ListViewItem(gi.Fields);
                lvi.Tag = gi;
                if (gi.State < 0)
                    lvi.ForeColor = Color.DarkRed;
                else if (gi.State > 0)
                    lvi.ForeColor = Color.DarkGreen;
                allGroupListView.Items.Add(lvi);
            }
        }
        void UpdateSubscribedList()                 
        {
            groupListView.Items.Clear();
            foreach (Group g in manager.SubscribedGroups)
            {
                ListViewItem lvi=groupListView.Items.Add(new ListViewItem(g.Fields));
                foreach (Group dg in deletedSubscribedGroups)
                    if (dg.Name == g.Name)
                        lvi.ForeColor = Color.DarkRed;
            }
        }
        void deleteButton_Click(object s, EventArgs e)
        {
            int nu = groupListView.SelectedIndices.Count;
            if (nu == 0)
                return;
            if (DialogResult.Yes != MessageBox.Show("Are you sure you want to unsubscribe from " + nu + " selected groups?", "Warning", MessageBoxButtons.YesNo))
                return;
            string[] deleteGroupList=new string[nu];
            int i = 0;
            foreach(int ind in groupListView.SelectedIndices)
                deleteGroupList[i++] = groupListView.Items[ind].Text;
            manager.DeleteSubscribedGroups(deleteGroupList);
            UpdateSubscribedList();
        }
        void addButton_Click(object s, EventArgs e) 
        {
            Group[] nga = new Group[allGroupListView.SelectedIndices.Count];
            int i = 0;
            foreach (int ind in allGroupListView.SelectedIndices)
            {
                GroupInfo gi=(GroupInfo)allGroupListView.Items[ind].Tag;
                nga[i++] = new Group(gi.Name, gi.MinArticleId);
            }
            manager.AddSubscribedDroups(nga);
            UpdateSubscribedList();
        }
        void saveButton_Click(object s, EventArgs e)
        {
            manager.StoreServerGroups(serverGroups);
            Close();
        }
        private void acceptButton_Click(object sender, EventArgs e)
        {
            UpdateGroupList();
        }

        private void reset_Click(object sender, EventArgs e)
        {
            int nu = groupListView.SelectedIndices.Count;
            if (nu == 0)
                return;
            if (DialogResult.Yes != MessageBox.Show("Are you sure you want to reset " + nu + " selected groups?", "Warning", MessageBoxButtons.YesNo))
                return;
            string[] resetGroupList = new string[nu];
            int i = 0;
            foreach (int ind in groupListView.SelectedIndices)
                resetGroupList[i++] = groupListView.Items[ind].Text;
            manager.ResetGroups(resetGroupList);
            UpdateSubscribedList();
        }
    }
}
