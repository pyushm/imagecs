using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Threading;

namespace ImageProcessor
{
    public delegate void VoidNoArg();
    public enum DirShowMode
    {
        Detail=1,   
        Preview, 
        Sys,
        Vag
    }
    public enum Direction
    {
        current = -1,
        Next,
        Prev,
        NextGroup,
        PrevGroup,
        NextName,
        PrevName,
    }
    public static class Scramble
    {   // Scramble: RawMangle ASCI characters by ROT13 and prepend mangleChar to indicate scrambled name
        public const char mangleChar = '\uAB87'; // lowercase of original mangle character \u13B7 
        public static bool IsMangled(string text) { return text != null && text.Length > 0 && char.ToLowerInvariant(text[0]) == mangleChar; }
        static bool allowMangle(string text) { return !string.IsNullOrEmpty(text) && !IsMangled(text) && text[0] != ImageFileName.infoFileChar; }    
        public static string UnMangleFile(string filePath) // returns path with last component of path (dir or file) replaced by human readable name
        {
            if (filePath == null || filePath.Length == 0)
                return filePath;
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            return Path.Combine(Path.GetDirectoryName(filePath), UnMangle(fileName) + Path.GetExtension(filePath));
        }
        public static string MangleFile(string filePath) // returns path with last component of path (dir or file) replaced by scrambled name
        {
            if (!DataAccess.Private || filePath == null || filePath.Length == 0)
                return filePath;
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            return Path.Combine(Path.GetDirectoryName(filePath), MangleForced(fileName) + Path.GetExtension(filePath));
        }
        public static string UnMangle(string src) { return !IsMangled(src)? src : RawMangle(src.Substring(1)); }
        public static string ManglePrivate(string src) { return DataAccess.Private ? MangleForced(src) : src; }
        public static string MangleForced(string src) { return allowMangle(src) ? mangleChar + RawMangle(src) : src; }
        static string RawMangle(string src)  // returns scrambled src if src not scrambled; otherwise returns src
        {
            char[] res = new char[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                if ((src[i] >= 'A' && src[i] <= 'M') || (src[i] >= 'a' && src[i] <= 'm'))
                    res[i] = (char)(src[i] + 13);
                else if ((src[i] >= 'N' && src[i] <= 'Z') || (src[i] >= 'n' && src[i] <= 'z'))
                    res[i] = (char)(src[i] - 13);
                else res[i] = src[i];
            }
            return new string(res);
        }
    }
    public class ImageFileName
    {   // ImageFileName has file type and name conversion data
        protected enum DataType
        {
            JPG,        // compressed heic, webp, jpg, avif image
            GIF,
            PNG,        // exact image
            MLI,        // unencrypted drawing
            MOV,        // any unencrypted video 
                        //Animation,
            EncPNG,     // encrypted png image
            EncJPG,     // encrypted jpg image
            EncMLI,     // encrypted exact layers
            EncMOV,     // encrypted Movie
            Unknown
        }
        static Comparison<FileInfo> FileInfoComparison = delegate (FileInfo p1, FileInfo p2)
        {
            string n1 = Scramble.UnMangle(p1.Name);
            string n2 = Scramble.UnMangle(p2.Name);
            return string.Compare(n1, n2, StringComparison.OrdinalIgnoreCase);
        };
        public const string DeletedFile = "deletedImage";
        static Hashtable knownExtensions = new Hashtable();
        static Hashtable storeTypeString = new Hashtable();
        public const char synonymChar = '=';
        public const char multiNameChar = '+';
        public static string NameWithoutTempPrefix(string name)
        {   // consitent with temp prefix set in GroupManager
            int ind = name.IndexOf('.');
            if (ind <= 0 || name.IndexOf('.', ind + 1) <= 0)
                return name;
            if (!char.IsUpper(name[0]))
                return name;
            for (int i = 0; i < ind; i++)
                if (!char.IsUpper(name[0]) && !char.IsDigit(name[0]))
                    return name;
            return name.Substring(ind + 1);
        }
        public const char infoFileChar = '@';
        const string infoImageSuffix = ".exa";
        static public readonly DirShowMode[] InfoTypes;
        static ImageFileName()
        {
            InfoTypes = (DirShowMode[])Enum.GetValues(typeof(DirShowMode));
            knownExtensions.Add(".avif", DataType.JPG);
            knownExtensions.Add(".webp", DataType.JPG);
            knownExtensions.Add(".heic", DataType.JPG);
            knownExtensions.Add(".jpg", DataType.JPG);
            knownExtensions.Add(".jpeg", DataType.JPG);
            knownExtensions.Add(".gif", DataType.GIF);
            knownExtensions.Add(".bmp", DataType.PNG);
            knownExtensions.Add(".png", DataType.PNG);
            knownExtensions.Add(".MLI", DataType.MLI);
            knownExtensions.Add(".exa", DataType.EncPNG);
            knownExtensions.Add(".jpe", DataType.EncJPG);
            knownExtensions.Add(".drw", DataType.EncMLI);
            knownExtensions.Add(".mpg", DataType.MOV);
            knownExtensions.Add(".mpeg", DataType.MOV);
            knownExtensions.Add(".avi", DataType.MOV);
            knownExtensions.Add(".wmv", DataType.MOV);
            knownExtensions.Add(".mov", DataType.MOV);
            knownExtensions.Add(".mp4", DataType.MOV);
            knownExtensions.Add(".asf", DataType.MOV);
            knownExtensions.Add(".mkv", DataType.MOV);
            knownExtensions.Add(".flv", DataType.MOV);
            knownExtensions.Add(".vid", DataType.EncMOV);
            storeTypeString.Add(DataType.JPG, " JPG ");
            storeTypeString.Add(DataType.PNG, " PNG ");
            storeTypeString.Add(DataType.GIF, " GIF ");
            storeTypeString.Add(DataType.MLI, " Draw");
            storeTypeString.Add(DataType.MOV, "Movie");
            //storeTypeString.Add(DataType.SubDirs, " Dir ");
            storeTypeString.Add(DataType.EncJPG, "<JPG>");
            storeTypeString.Add(DataType.EncPNG, "<PNG>");
            storeTypeString.Add(DataType.EncMLI, "<MLI>");
            storeTypeString.Add(DataType.EncMOV, "<VID>");
        }
        static public DirShowMode? InfoType(string fileName)
        {
            string name = Path.GetFileName(fileName);
            foreach (DirShowMode m in InfoTypes)
                if (name == InfoFileName(m))
                    return m;
            return null;
        }
        protected static DataType FileType(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();
            object o = knownExtensions[ext];
            return o == null ? DataType.Unknown : (DataType)o;
        }
        static public string InfoFileName(DirShowMode m) => infoFileChar + m.ToString(); 
        static public string InfoFileWithExtension(DirShowMode m) => InfoFileName(m) + infoImageSuffix; 
        static public Image[] InfoImages(DirectoryInfo di)
        {   // to show image ingo files for directory selection or extended info view
            List<Image> all = new List<Image>();
            FileInfo[] fia = di.GetFiles("*@*");
            if (fia.Length == 0)
                return new Image[0];
            Array.Sort(fia, FileInfoComparison);
            foreach (FileInfo fi in fia)
            {
                if(fi.Name[0] == infoFileChar || fi.Name[1] == infoFileChar)
                try
                {
                    ImageFileName dt = new ImageFileName(fi.Name);
                    if (!dt.IsImage)
                        continue;
                    byte[] imageBytes = DataAccess.ReadFile(fi.FullName, dt.IsEncrypted);
                    if (imageBytes.Length > 0)
                    {
                        MemoryStream ms = new MemoryStream(imageBytes.Length);
                        ms.Write(imageBytes, 0, imageBytes.Length);
                        Image im = System.Drawing.Image.FromStream(ms, true);//Exception occurs here
                        all.Add(im);
                    }
                }
                finally { }
            }
            return all.ToArray();
        }
        protected readonly DataType data;               // type associated with image 
        public string Name { get; protected set; }      // file name in file system (name+extention, no directory)
        public string FSName { get; protected set; }    // FS (mangled) file name without extention
        public string RealName { get; protected set; }  // real name without extention
        public virtual string ShortName => RealName;
        public bool IsInfoImage { get; private set; }   // each image directory contains {DirShowMode} info images to be shown in parent directory list
        bool Is(DataType dt) => data == dt; 
        public bool IsMovie => Is(DataType.EncMOV) || Is(DataType.MOV);
        public bool IsUnencryptedImage => Is(DataType.GIF) || Is(DataType.JPG) || Is(DataType.PNG); // unencrypted image of any format
        public bool IsEncryptedImage => Is(DataType.EncPNG) || Is(DataType.EncJPG); // any encrypted image
        public bool IsImage => IsUnencryptedImage || IsEncryptedImage; // single layer
        public bool IsMultiLayer => Is(DataType.EncMLI) || Is(DataType.MLI);  
        public bool IsEncrypted => Is(DataType.EncMLI) || IsEncryptedImage || Is(DataType.EncMOV); // any encrypted file
        public bool IsExact => Is(DataType.EncPNG) || Is(DataType.PNG); // encrypted or unencrypted exact bitmap image
        public string StoreTypeString { get { object o = storeTypeString[data]; return o == null ? " ??? " : (string)o; } }
        public bool IsKnown => !Is(DataType.Unknown);
        public bool IsLowQuality { get; protected set; } = false;
        public int SmallFile = 80000;
        public ImageFileName(string fileName) // name with extension
        {
            data = FileType(fileName);
            Name = fileName;
            FSName = Path.GetFileNameWithoutExtension(fileName);
            var fi = new FileInfo(fileName);
            var di = fi.Directory;
            RealName = Scramble.UnMangle(FSName);
            if (Navigator.IsSpecDir(di, SpecName.NewArticles))
                RealName = NameWithoutTempPrefix(RealName);
            IsInfoImage = InfoType(RealName) != null;
            if (IsInfoImage)
                RealName = RealName.Substring(1);
        }
    }
    public class ImageDirInfo : ImageFileName
    {
        public DirectoryInfo DirInfo { get; private set; }
        public ImageDirInfo(DirectoryInfo di) : base(di.Name) { DirInfo = di; SetImageCount(); }
        public string FSPath            => !IsValid ? "" : DirInfo.FullName; // complete path of child directory
        public string RealPath          => !IsValid ? "" : Path.Combine(DirInfo.Parent.FullName, RealName);  // complete path of image object
        public int DirCount()           => IsValid ? DirInfo.GetDirectories().Length : 0;
        public bool IsValid             => DirInfo != null && DirInfo.Exists;
        public void ClearDirectory() { DirInfo = null; }
        DateTime updated;           // last access time of underlying directory
        public int imageCount;
        public int ImageCount { get { if (updated < DirInfo.LastWriteTime) SetImageCount(); return imageCount; } }
        public void SetImageCount()
        {
            int imCount = 0;
            int infoCount = 0;
            int smallCount = 0;
            FileInfo[] files = IsValid ? DirInfo.GetFiles() : new FileInfo[0];
            foreach (var f in files)
            {
                var ifi = new ImageFileInfo(f);
                if (ifi.IsImage)
                    imCount++;
                if (ifi.IsInfoImage)
                    infoCount++;
                else if (ifi.FileInfo.Length < SmallFile)
                    smallCount++;
            }
            imageCount = imCount - infoCount;
            IsLowQuality = smallCount > 0.7 * imageCount;
            updated = DateTime.Now;
        }
    }
    public class ImageFileInfo : ImageFileName
    {
        public class NameComparer : IComparer<string>
        {
            public int Compare(string l1, string l2)
            {
                if (l1.IndexOf(multiNameChar) < 0 && l2.IndexOf(multiNameChar) >= 0)
                    return -1;
                else if (l1.IndexOf(multiNameChar) >= 0 && l2.IndexOf(multiNameChar) < 0)
                    return 1;
                return string.Compare(l1, l2, StringComparison.OrdinalIgnoreCase);
            }
        }
        static Image failedImage = LoadSpecialImage("failedImage.png");
        static Image mediaImage = LoadSpecialImage("mediaImage.png");
        static Image localFilesImage = LoadSpecialImage("localImage.png");
        static Image multiLayerImage = LoadSpecialImage("multiLayerImage.png");
        static public Image notLoadedImage = LoadSpecialImage("notLoadedImage.png");
        public const int infoImageWidth = 144;
        public const int infoImageHeight = 208;
        static Image LoadSpecialImage(string fileName)
        {
            FileStream fs = null;
            Image image;
            try
            {
                fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                image = System.Drawing.Image.FromStream(fs);
            }
            catch    // legal exception - update may fail if file is being modified
            {
                image = failedImage;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            return image;
        }
        internal static string Delete(string fileName)
        {
            bool removed = false;
            int attempts=3;
            string message = "";
            while (!removed && attempts-- > 0)
            {
                try
                {
                    File.Delete(fileName);
                    removed = true;
                }
                catch(Exception ex)    // legal exception - may failed if in use or RO
                {
                    if (attempts > 0)
                        Thread.Sleep(500);
                    else
                        message = fileName + " was not deleted: " + ex.Message;
                }
            }
            return message;
        }
        static public IntSize ThumbnailSize() { return new IntSize(infoImageHeight, infoImageHeight); }
        static public IntSize PixelSize(DirShowMode it) { return it == DirShowMode.Detail || it == DirShowMode.Preview ? new IntSize(infoImageWidth, infoImageHeight) : new IntSize(infoImageWidth / 2, infoImageWidth / 2); }
        public bool NeedThumbnail { get; private set; } = true; // true if thumbnail == null || image modifiled
        string dirSuffix = "";
        internal bool priority = false;     // true indicatates need for priority loading of visible image
        Image thumbnail;                    // image displayed in preview mode
        DateTime modifiedTime;              // update time of the image 
        public FileInfo FileInfo            { get; private set; }
        public ImageGroup Group             { get; set; }  // != null when fisrt member of a group
        public bool IsGroupHead             => Group != null; // indicates first image in group
        public string FSPath                => FileInfo == null ? "" : FileInfo.FullName; // system path of image file
        public string RealPath              => FileInfo == null ? "" : Path.Combine(FileInfo.Directory.Parent.FullName, Scramble.UnMangle(FileInfo.Directory.Name), RealName); // complete path of image object
        public override string ShortName    { get
            {
                if (IsDirInfo)
                {
                    string n = Scramble.UnMangle(FileInfo.Directory.Name);
                    string[] fields = n.Split(new char[] { multiNameChar, synonymChar });
                    string sn = fields.Length > 1 ? n.Substring(0, fields[0].Length + 1) : n;
                    return sn + dirSuffix;
                }
                return base.ShortName;
            }
        }
        public bool IsDirInfo               { get; private set; } // image representing child directory in image list - info image show with child directory name
        public int DisplayListIndex         { get; internal set; } = -1; // >=0 when in list
        public ImageFileInfo(FileInfo fi, bool header = false) : base(fi.Name) 
        { 
            FileInfo = fi;
            IsDirInfo = header;
            if (IsDirInfo)
            {
                int dirCount = FileInfo.Directory.GetDirectories().Length;
                var idi = new ImageDirInfo(FileInfo.Directory);
                dirSuffix = "\u25CF" + idi.ImageCount + (dirCount == 0 ? "" : "-" + dirCount);
                RealName = Scramble.UnMangle(FileInfo.Directory.Name) + dirSuffix;
                Name = FileInfo.Directory.Name;
                IsLowQuality= idi.IsLowQuality;
            }
        }
        public bool CheckExistsSetUpdate()  // false if file does not exists
        { 
            FileInfo fi = new FileInfo(FSPath);
            if (!fi.Exists || fi.Length == 0)
                return false; // no data exists
            if (fi.LastWriteTime > modifiedTime)
                NeedThumbnail = true;// image updated
            return true;
        }
        void SetCynchronized(DateTime dt)   
        {
            modifiedTime = dt;
            NeedThumbnail = false;
            priority = false;
        }
        Image CreateThumbnail(Image image)
        {
            IntSize size = ThumbnailSize();
            float scale = Math.Min((float)size.Width / image.Width, (float)size.Height / image.Height);
            int w = (int)(image.Width * scale);
            int h = (int)(image.Height * scale);
            return image.GetThumbnailImage(w, h, new Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
        }
        public Image UpdateThumbnail()                
        {
            switch (data)
            {
                case DataType.GIF:
                case DataType.JPG:
                case DataType.PNG:
                case DataType.EncPNG:
                case DataType.EncJPG:
                    try
                    {
                        BitmapAccess ba = BitmapAccess.LoadImageWithSize(FSPath, IsEncrypted, out int fileSize, 200);
                        if (ba == null)
                            return null;
                        if (!IsDirInfo && fileSize < SmallFile) 
                            IsLowQuality = true;
                        Bitmap bm = ba.CreateBitmapImage();
                        thumbnail = CreateThumbnail(bm);
                        thumbnail.Tag = FSPath;
                        FileInfo fi = new FileInfo(FSPath);   // FileInfo recreated to get new LastWriteTime
                        SetCynchronized(DateTime.Now);
                    }
                    catch (Exception)   // legal exception - update may fail if file is being modified
                    {   // TODO show exception
                        thumbnail = failedImage;
                    }
                    break;
                case DataType.MOV:
                case DataType.EncMOV:
                    thumbnail = mediaImage;
                    SetCynchronized(DateTime.Now);
                    break;
                //case DataType.EncMLI:
                //    byte[] ta = VisualLayerData.LoadSerializedThumbnail(FSPath, IsEncrypted);
                //    thumbnail = ta == null ? multiLayerImage : CreateThumbnail(new Bitmap(new MemoryStream(ta)));
                //    SetCynchronized(DateTime.Now);
                //    break;
                default:
                    thumbnail = localFilesImage;
                    SetCynchronized(DateTime.Now);
                    break;
            }
            return thumbnail;
        }
        public bool ThumbnailCallback()     { return false; }
        public Image GetThumbnail()         // called by the clent if modified=true
        {
            if (NeedThumbnail)
            {
                priority = true;
                return null;
            }
            return thumbnail;
        }
        public string FileRename(string newName) // returns new full name
        {
            if (IsInfoImage || string.IsNullOrEmpty(newName))
                return null;
            try
            {
                FileInfo.Refresh();
                string ext = Path.GetExtension(FSPath);
                RealName = newName;
                FSName = Scramble.ManglePrivate(RealName);
                string newFullPath = Path.Combine(FileInfo.Directory.FullName, FSName + ext);
                FileInfo.MoveTo(newFullPath);
                NeedThumbnail = true;
                FileInfo = new FileInfo(newFullPath);
                Name = FileInfo.Name;
            }
            catch (Exception e) { return e.Message+Environment.NewLine+"Can't rename file"; }
            return null;
        }
    }
}
