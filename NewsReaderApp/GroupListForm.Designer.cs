namespace NNTP
{
    partial class GroupListForm
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
            this.groupListView = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.allGroupListView = new System.Windows.Forms.ListView();
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label1 = new System.Windows.Forms.Label();
            this.deleteButton = new System.Windows.Forms.Button();
            this.addButton = new System.Windows.Forms.Button();
            this.prefixLabel = new System.Windows.Forms.Label();
            this.saveButton = new System.Windows.Forms.Button();
            this.filterBox = new System.Windows.Forms.TextBox();
            this.filterButton = new System.Windows.Forms.Button();
            this.reset = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // groupListView
            // 
            this.groupListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.groupListView.Location = new System.Drawing.Point(2, 25);
            this.groupListView.Name = "groupListView";
            this.groupListView.Size = new System.Drawing.Size(502, 143);
            this.groupListView.TabIndex = 0;
            this.groupListView.UseCompatibleStateImageBehavior = false;
            this.groupListView.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            this.columnHeader1.Width = 320;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Max ID";
            this.columnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader2.Width = 90;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Count";
            this.columnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader3.Width = 70;
            // 
            // allGroupListView
            // 
            this.allGroupListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
            this.allGroupListView.Location = new System.Drawing.Point(2, 199);
            this.allGroupListView.Name = "allGroupListView";
            this.allGroupListView.Size = new System.Drawing.Size(502, 507);
            this.allGroupListView.TabIndex = 1;
            this.allGroupListView.UseCompatibleStateImageBehavior = false;
            this.allGroupListView.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Name";
            this.columnHeader4.Width = 320;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Max ID";
            this.columnHeader5.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader5.Width = 90;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Count";
            this.columnHeader6.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader6.Width = 70;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(6, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(122, 16);
            this.label1.TabIndex = 2;
            this.label1.Text = "Subscribed groups";
            // 
            // deleteButton
            // 
            this.deleteButton.Location = new System.Drawing.Point(374, 2);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(129, 22);
            this.deleteButton.TabIndex = 3;
            this.deleteButton.Text = "Unsubscribe selected";
            this.deleteButton.UseVisualStyleBackColor = true;
            this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
            // 
            // addButton
            // 
            this.addButton.Location = new System.Drawing.Point(374, 174);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(129, 22);
            this.addButton.TabIndex = 4;
            this.addButton.Text = "Subscribe selected";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            // 
            // prefixLabel
            // 
            this.prefixLabel.AutoSize = true;
            this.prefixLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.prefixLabel.Location = new System.Drawing.Point(6, 176);
            this.prefixLabel.Name = "prefixLabel";
            this.prefixLabel.Size = new System.Drawing.Size(68, 16);
            this.prefixLabel.TabIndex = 5;
            this.prefixLabel.Text = "All groups";
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(181, 712);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(98, 23);
            this.saveButton.TabIndex = 6;
            this.saveButton.Text = "Save && Close";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // filterBox
            // 
            this.filterBox.Location = new System.Drawing.Point(230, 175);
            this.filterBox.Name = "filterBox";
            this.filterBox.Size = new System.Drawing.Size(138, 20);
            this.filterBox.TabIndex = 7;
            // 
            // filterButton
            // 
            this.filterButton.Location = new System.Drawing.Point(190, 174);
            this.filterButton.Name = "filterButton";
            this.filterButton.Size = new System.Drawing.Size(44, 22);
            this.filterButton.TabIndex = 9;
            this.filterButton.Text = "Filter";
            this.filterButton.UseVisualStyleBackColor = true;
            this.filterButton.Click += new System.EventHandler(this.acceptButton_Click);
            // 
            // reset
            // 
            this.reset.Location = new System.Drawing.Point(230, 2);
            this.reset.Name = "reset";
            this.reset.Size = new System.Drawing.Size(123, 23);
            this.reset.TabIndex = 10;
            this.reset.Text = "Reset selected";
            this.reset.UseVisualStyleBackColor = true;
            this.reset.Click += new System.EventHandler(this.reset_Click);
            // 
            // GroupListForm
            // 
            this.AcceptButton = this.filterButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(506, 737);
            this.Controls.Add(this.reset);
            this.Controls.Add(this.filterButton);
            this.Controls.Add(this.filterBox);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.prefixLabel);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.allGroupListView);
            this.Controls.Add(this.groupListView);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GroupListForm";
            this.Text = "GroupListForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView groupListView;
        private System.Windows.Forms.ListView allGroupListView;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button deleteButton;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Label prefixLabel;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.TextBox filterBox;
        private System.Windows.Forms.Button filterButton;
        private System.Windows.Forms.Button reset;
    }
}