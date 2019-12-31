using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageProcessor
{
    public partial class PasswordDialog : Form
    {
        int attempts = 3;
        public PasswordDialog()
        {
            InitializeComponent();
            passBox.KeyDown += new KeyEventHandler(passBox_KeyDown);
            passBox.PasswordChar = '\u25CF';

        }
        void passBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //DataCipher.ChangePassword(outputBox.Text, outputBox.Text);
                bool ok = DataAccess.AllowPrivateAccess(passBox.Text);
                if (!ok && attempts>0)
                {
                    passBox.Text = "Wrong password";
                    passBox.ForeColor = Color.Red;
                    attempts--;
                }
                Close();
            }
        }
    }
}
