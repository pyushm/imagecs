using System;
using System.Windows.Forms;

namespace ImageProcessor
{
	public class ImageSelectorApp
	{
		[STAThread]
		static void Main()					
		{
            try
            {
                Application.Run(new NavigatorForm());
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }
	}
}
