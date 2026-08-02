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
            locationTreeView = new TreeView();
            infoImagePanel = new Panel();
            moveToButton = new Button();
            inputOutputBox = new TextBox();
            SuspendLayout();
            // 
            // locationTreeView
            // 
            locationTreeView.Location = new Point(0, 54);
            locationTreeView.Margin = new Padding(5, 6, 5, 6);
            locationTreeView.Name = "locationTreeView";
            locationTreeView.Size = new Size(371, 875);
            locationTreeView.TabIndex = 11;
            locationTreeView.BeforeExpand += RetrievNodes;
            locationTreeView.AfterSelect += DisplaySelectedNode;
            // 
            // infoImagePanel
            // 
            infoImagePanel.BorderStyle = BorderStyle.FixedSingle;
            infoImagePanel.Location = new Point(383, 54);
            infoImagePanel.Margin = new Padding(5, 6, 5, 6);
            infoImagePanel.Name = "infoImagePanel";
            infoImagePanel.Size = new Size(258, 875);
            infoImagePanel.TabIndex = 12;
            // 
            // moveToButton
            // 
            moveToButton.Location = new Point(531, 6);
            moveToButton.Margin = new Padding(5, 6, 5, 6);
            moveToButton.Name = "moveToButton";
            moveToButton.Size = new Size(110, 42);
            moveToButton.TabIndex = 22;
            moveToButton.Text = "Move";
            moveToButton.Click += moveToButton_Click;
            // 
            // inputOutputBox
            // 
            inputOutputBox.Location = new Point(0, 11);
            inputOutputBox.Margin = new Padding(5, 6, 5, 6);
            inputOutputBox.Name = "inputOutputBox";
            inputOutputBox.Size = new Size(521, 31);
            inputOutputBox.TabIndex = 23;
            // 
            // DirectorySelectionForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(651, 934);
            Controls.Add(inputOutputBox);
            Controls.Add(moveToButton);
            Controls.Add(infoImagePanel);
            Controls.Add(locationTreeView);
            Margin = new Padding(5, 6, 5, 6);
            Name = "DirectorySelectionForm";
            Text = "DirectorySelectionForm";
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView locationTreeView;
        private System.Windows.Forms.Panel infoImagePanel;
        private System.Windows.Forms.Button moveToButton;
        private System.Windows.Forms.TextBox inputOutputBox;
    }
}