using System;
using System.Windows;
using System.Diagnostics;

namespace ImageProcessor
{
	public class ImageSelectorApp : Application
	{
        [STAThread]
        static void Main()
        {
            try
            {
                var app = new ImageSelectorApp();
                app.ShutdownMode= ShutdownMode.OnExplicitShutdown;
                NavigatorForm nf = new NavigatorForm();
                nf.Show();
                nf.FormClosing += new System.Windows.Forms.FormClosingEventHandler(NavigatorFormClosing);
                app.Run();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }
        static private void NavigatorFormClosing(object sender, EventArgs e)
        {
            Current.Shutdown();
        }
    }
}
