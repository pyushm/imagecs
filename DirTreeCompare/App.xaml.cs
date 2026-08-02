using System.Windows;
using System.Windows.Controls;

namespace DirTreeCompare
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Resources[typeof(ListBoxItem)] = CreateCompactStyle(typeof(ListBoxItem));
            Resources[typeof(TreeViewItem)] = CreateCompactStyle(typeof(TreeViewItem)); 
            base.OnStartup(e);
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
}
