namespace NNTP
{
    partial class ServerBrowserWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this.statusBox = new System.Windows.Forms.TextBox();
            this.checkServerBtn = new System.Windows.Forms.Button();
            this.newServerBox = new System.Windows.Forms.TextBox();
            this.serverList = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupListlabel = new System.Windows.Forms.Label();
            this.groupList = new System.Windows.Forms.ListView();
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SuspendLayout();
            // 
            // statusBox
            // 
            this.statusBox.Location = new System.Drawing.Point(12, 3);
            this.statusBox.Multiline = true;
            this.statusBox.Name = "statusBox";
            this.statusBox.ReadOnly = true;
            this.statusBox.Size = new System.Drawing.Size(354, 36);
            this.statusBox.TabIndex = 8;
            // 
            // checkServerBtn
            // 
            this.checkServerBtn.Location = new System.Drawing.Point(12, 43);
            this.checkServerBtn.Name = "checkServerBtn";
            this.checkServerBtn.Size = new System.Drawing.Size(122, 22);
            this.checkServerBtn.TabIndex = 0;
            this.checkServerBtn.Text = "Check Server";
            this.checkServerBtn.UseVisualStyleBackColor = true;
            this.checkServerBtn.Click += new System.EventHandler(this.checkServerBtn_Click);
            // 
            // newServerBox
            // 
            this.newServerBox.Location = new System.Drawing.Point(140, 45);
            this.newServerBox.Name = "newServerBox";
            this.newServerBox.Size = new System.Drawing.Size(227, 20);
            this.newServerBox.TabIndex = 1;
            // 
            // serverList
            // 
            this.serverList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader5});
            this.serverList.HideSelection = false;
            this.serverList.Location = new System.Drawing.Point(12, 71);
            this.serverList.Name = "serverList";
            this.serverList.Size = new System.Drawing.Size(354, 136);
            this.serverList.TabIndex = 2;
            this.serverList.UseCompatibleStateImageBehavior = false;
            this.serverList.View = System.Windows.Forms.View.Details;
            this.serverList.SelectedIndexChanged += new System.EventHandler(this.serverList_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Server";
            this.columnHeader1.Width = 180;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Result";
            this.columnHeader2.Width = 100;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Headers";
            this.columnHeader5.Width = 52;
            // 
            // groupListlabel
            // 
            this.groupListlabel.AutoSize = true;
            this.groupListlabel.Location = new System.Drawing.Point(14, 218);
            this.groupListlabel.Name = "groupListlabel";
            this.groupListlabel.Size = new System.Drawing.Size(55, 13);
            this.groupListlabel.TabIndex = 3;
            this.groupListlabel.Text = "Group List";
            // 
            // groupList
            // 
            this.groupList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader6});
            this.groupList.HideSelection = false;
            this.groupList.Location = new System.Drawing.Point(12, 241);
            this.groupList.Name = "groupList";
            this.groupList.Size = new System.Drawing.Size(355, 646);
            this.groupList.TabIndex = 5;
            this.groupList.UseCompatibleStateImageBehavior = false;
            this.groupList.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Group";
            this.columnHeader3.Width = 180;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Max ID";
            this.columnHeader4.Width = 100;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Articles";
            this.columnHeader6.Width = 52;
            // 
            // ServerBrowserWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(379, 899);
            this.Controls.Add(this.statusBox);
            this.Controls.Add(this.groupList);
            this.Controls.Add(this.groupListlabel);
            this.Controls.Add(this.serverList);
            this.Controls.Add(this.newServerBox);
            this.Controls.Add(this.checkServerBtn);
            this.MaximumSize = new System.Drawing.Size(395, 938);
            this.MinimumSize = new System.Drawing.Size(395, 938);
            this.Name = "ServerBrowserWindow";
            this.Text = "Server Browser";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button checkServerBtn;
        private System.Windows.Forms.TextBox newServerBox;
        private System.Windows.Forms.ListView serverList;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.Label groupListlabel;
        private System.Windows.Forms.ListView groupList;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.TextBox statusBox;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
    }
}