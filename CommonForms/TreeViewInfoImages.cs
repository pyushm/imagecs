using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace ImageProcessor
{
    public class DirectoryInfoImages
    {   // visual reprentation of selected item
        Image[] infoImages;
        TreeView tree;
        Control displayArea;
        public DirectoryInfoImages(TreeView tree_, Control displayArea_)
        {
            tree = tree_;
            displayArea = displayArea_;
            displayArea.Paint += new PaintEventHandler(DrawInfoImages);
        }
        ~DirectoryInfoImages() { DisposeImages(); }
        public int ShowInfoImages(DirectoryInfo di)
        {
            try
            {
                FileInfo[] files = di.GetFiles();
                DisposeImages();
                infoImages = ImageFileName.InfoImages(di);
                displayArea.Invalidate();
                return files.Length - infoImages.Length;
            }
            catch { return 0; }
        }
        public void HideInfoImages()
        {
            DisposeImages();
            infoImages = new Image[0];
            displayArea.Invalidate();
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
        void DisposeImages()
        {
            if (infoImages == null)
                return;
            foreach (Image im in infoImages)
                if (im != null)
                    im.Dispose();
        }
    }
}
