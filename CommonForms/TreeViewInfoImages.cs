using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace ImageProcessor
{
    public class DirectoryInfoImages
    {   // visual reprentation of selected item
        Image[] infoImages;
        Control displayArea;
        public DirectoryInfoImages(Control displayArea_)
        {
            displayArea = displayArea_;
            displayArea.Paint += new PaintEventHandler(DrawInfoImages);
        }
        ~DirectoryInfoImages() { DisposeInfoImages(); }
        public DirectoryInfo ReDrawInfoImages(DirectoryInfo di = null)
        {
            try
            {
                bool valid = di != null && di.Exists;
                DisposeInfoImages();
                infoImages = valid ? ImageFileName.InfoImages(di) : new Image[0];
                displayArea.Invalidate();
                return valid ? di : null;
            }
            catch { return null; }
        }
        void DisposeInfoImages()
        {
            if (infoImages == null)
                return;
            foreach (Image im in infoImages)
                if (im != null)
                    im.Dispose();
        }
        void DrawInfoImages(object sender, PaintEventArgs e)
        {
            if (infoImages == null)
                return;
            int y = 0;
            int x = 0;
            float dpiScale = e.Graphics.DpiY/96;
            foreach (Image im in infoImages)
            {
                if (im != null)
                {
                    e.Graphics.DrawImage(im, x, y);
                    if (im.Width > 100)
                        y += (int)(im.Height * dpiScale);
                    else
                        x += (int)(im.Width * dpiScale);
                }
            }
        }
    }
}
