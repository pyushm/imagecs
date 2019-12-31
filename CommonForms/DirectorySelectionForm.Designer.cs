namespace ImageProcessor
{
    partial class DirectorySelectionForm
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
            this.locationTreeView = new System.Windows.Forms.TreeView();
            this.infoImagePanel = new System.Windows.Forms.Panel();
            this.moveToButton = new System.Windows.Forms.Button();
            this.inputOutputBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // locationTreeView
            // 
            this.locationTreeView.Location = new System.Drawing.Point(0, 28);
            this.locationTreeView.Name = "locationTreeView";
            this.locationTreeView.Size = new System.Drawing.Size(224, 485);
            this.locationTreeView.TabIndex = 11;
            this.locationTreeView.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.RetrievNodes);
            this.locationTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.DisplaySelectedNode);
            // 
            // infoImagePanel
            // 
            this.infoImagePanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.infoImagePanel.Location = new System.Drawing.Point(230, 28);
            this.infoImagePanel.Name = "infoImagePanel";
            this.infoImagePanel.Size = new System.Drawing.Size(142, 485);
            this.infoImagePanel.TabIndex = 12;
            // 
            // moveToButton
            // 
            this.moveToButton.Location = new System.Drawing.Point(317, 2);
            this.moveToButton.Name = "moveToButton";
            this.moveToButton.Size = new System.Drawing.Size(55, 22);
            this.moveToButton.TabIndex = 22;
            this.moveToButton.Text = "Move";
            this.moveToButton.Click += new System.EventHandler(this.moveToButton_Click);
            // 
            // inputOutputBox
            // 
            this.inputOutputBox.Location = new System.Drawing.Point(0, 3);
            this.inputOutputBox.Name = "inputOutputBox";
            this.inputOutputBox.Size = new System.Drawing.Size(314, 20);
            this.inputOutputBox.TabIndex = 23;
            // 
            // DirectorySelectionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(374, 514);
            this.Controls.Add(this.inputOutputBox);
            this.Controls.Add(this.moveToButton);
            this.Controls.Add(this.infoImagePanel);
            this.Controls.Add(this.locationTreeView);
            this.Name = "DirectorySelectionForm";
            this.Text = "DirectorySelectionForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView locationTreeView;
        private System.Windows.Forms.Panel infoImagePanel;
        private System.Windows.Forms.Button moveToButton;
        private System.Windows.Forms.TextBox inputOutputBox;
    }
}