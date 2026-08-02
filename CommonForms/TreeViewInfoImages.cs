using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace ImageProcessor
{
    public class DirectoryInfoImages
    {   // visual reprentation of selected item
        Image[] infoImages;
        Control displayArea;
        int dx, dy; 
        public DirectoryInfoImages(Control displayArea_)
        {
            displayArea = displayArea_;
            displayArea.Paint += new PaintEventHandler(DrawInfoImages);
            dx = displayArea.Width / 2;
            dy = (int)(ImageFileInfo.infoImageHeight / (float)ImageFileInfo.infoImageWidth * displayArea.Width);
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
            int y = 0, x = 0;
            foreach (Image im in infoImages)
                if (im != null)
                {
                    if (im.Width > 100) { e.Graphics.DrawImage(im, x, y, displayArea.Width, dy); y += dy; }
                    else { e.Graphics.DrawImage(im, x, y, dx, dx); x += dx; }
                }
        }
    }
}
