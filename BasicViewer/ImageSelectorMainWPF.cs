using System;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace ImageProcessor
{
	public class ImageSelectorApp : System.Windows.Application

    {
        [STAThread]
        static void Main()
        {
            try
            {
                var app = new ImageSelectorApp();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                app.Resources[typeof(ListBoxItem)] = CreateCompactStyle(typeof(ListBoxItem));
                app.Resources[typeof(TreeViewItem)] = CreateCompactStyle(typeof(TreeViewItem));
                string[] args = Environment.GetCommandLineArgs();
                NavigatorForm nf = (args.Length>1 && args[1] == "-privateaccess") ? new NavigatorForm(true) : new NavigatorForm();
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
        private static Style CreateCompactStyle(Type itemType)
        {
            var style = new Style(itemType);
            style.Setters.Add(new Setter(System.Windows.Controls.Control.PaddingProperty, new Thickness(2, 0, 2, 0)));
            style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 0.0));
            style.Setters.Add(new Setter(System.Windows.Controls.Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            return style;
        }
    }
    public static class WpfAppearance
    {
        public static void ApplyCompactItemSpacing()
        {
            var app = System.Windows.Application.Current;
            if (app is null)
                return;
        }
    }
}
