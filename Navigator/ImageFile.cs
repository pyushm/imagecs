using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Threading;
using System.ComponentModel;

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
    public enum ListShowMode
    {
        Auto,
        List,
        Groups
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
    public static class FileName
    {
        const char mangleChar = '\u13B7';
        public static bool IsMangled(string text) { return text != null && text.Length > 0 && text[0] == mangleChar; }
        public static string UnMangleFile(string filePath) // returns path with last component of path (dir or file) replaced by human readable name
        {
            if (filePath == null || filePath.Length == 0)
                return filePath;
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            return Path.Combine(Path.GetDirectoryName(filePath), UnMangle(fileName) + Path.GetExtension(filePath));
        }
        public static string MangleFile(string filePath) // returns path with last component of path (dir or file) replaced by scrambled name
        {
            if (!DataAccess.PrivateAccessEnforced || filePath == null || filePath.Length == 0)
                return filePath;
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            return Path.Combine(Path.GetDirectoryName(filePath), Mangle(fileName) + Path.GetExtension(filePath));
        }
        public static string UnMangle(string src)   // returns unscrambled src if src scrambled; otherwise returns src
        {
            if (!IsMangled(src))
                return src;
            char[] res = new char[src.Length - 1];
            for (int i = 1; i < src.Length; i++)
            {
                if ((src[i] >= 'A' && src[i] <= 'M') || (src[i] >= 'a' && src[i] <= 'm'))
                    res[i - 1] = (char)(src[i] + 13);
                else if ((src[i] >= 'N' && src[i] <= 'Z') || (src[i] >= 'n' && src[i] <= 'z'))
                    res[i - 1] = (char)(src[i] - 13);
                else res[i - 1] = src[i];
            }
            return new string(res);
        }
        public static string Mangle(string src)  // return scrambled src if src not scrambled; otherwise returns src
        {
            if (!DataAccess.PrivateAccessEnforced || src == null || src.Length == 0 || src[0] == mangleChar)
                return src;
            char[] res = new char[src.Length + 1];
            res[0] = mangleChar;
            for (int i = 0; i < src.Length; i++)
            {
                if ((src[i] >= 'A' && src[i] <= 'M') || (src[i] >= 'a' && src[i] <= 'm'))
                    res[i + 1] = (char)(src[i] + 13);
                else if ((src[i] >= 'N' && src[i] <= 'Z') || (src[i] >= 'n' && src[i] <= 'z'))
                    res[i + 1] = (char)(src[i] - 13);
                else res[i + 1] = src[i];
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
            string n1 = FileName.UnMangle(p1.Name);
            string n2 = FileName.UnMangle(p2.Name);
            return string.Compare(n1, n2);
        };
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
        const string infoImagePrefix = "@";
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
        static public string InfoFileName(DirShowMode m) { return infoImagePrefix + m; }
        static public string InfoFileWithExtension(DirShowMode m) { return InfoFileName(m) + infoImageSuffix; }
        static public Image[] InfoImages(DirectoryInfo di)
        {   // to show image ingo files for directory selection or extended info view
            List<Image> all = new List<Image>();
            FileInfo[] fia = di.GetFiles("*@*");
            if (fia.Length == 0)
                return new Image[0];
            Array.Sort(fia, FileInfoComparison);
            foreach (FileInfo fi in fia)
            {
                if(fi.Name[0] == infoImagePrefix[0] || fi.Name[1] == infoImagePrefix[0])
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
        public bool IsInfoImage { get; private set; }   // each image directory contains {DirShowMode} info images to be shown in parent directory list
        bool Is(DataType dt) { return data == dt; }
        public bool IsMovie { get { return Is(DataType.EncMOV) || Is(DataType.MOV); } }
        public bool IsUnencryptedImage { get { return Is(DataType.GIF) || Is(DataType.JPG) || Is(DataType.PNG); } } // unencrypted image of any format
        public bool IsEncryptedImage { get { return Is(DataType.EncPNG) || Is(DataType.EncJPG); } } // any encrypted image
        public bool IsImage { get { return IsUnencryptedImage || IsEncryptedImage; } }  // single layer
        public bool IsMultiLayer { get { return Is(DataType.EncMLI) || Is(DataType.MLI); } }    
        public bool IsEncrypted { get { return Is(DataType.EncMLI) || IsEncryptedImage || Is(DataType.EncMOV); } } // any encrypted file
        public bool IsExact { get { return Is(DataType.EncPNG) || Is(DataType.PNG); } } // encrypted or unencrypted exact bitmap image
        public string StoreTypeString { get { object o = storeTypeString[data]; return o == null ? " ??? " : (string)o; } }
        public bool IsKnown { get { return !Is(DataType.Unknown); } }
        public ImageFileName(string fileName) // name with extension
        {
            data = FileType(fileName);
            Name = fileName;
            FSName = Path.GetFileNameWithoutExtension(fileName);
            var fi = new FileInfo(fileName);
            var di = fi.Directory;
            RealName = FileName.UnMangle(FSName);
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
        public ImageDirInfo(DirectoryInfo di) : base(di.Name) { DirInfo = di; }
        public string FSPath { get { return !IsValid ? "" : DirInfo.FullName; } } // complete path of child directory
        public string RealPath { get { return !IsValid ? "" : Path.Combine(DirInfo.Parent.FullName, RealName); } } // complete path of image object
        public int DirCount() { return IsValid ? DirInfo.GetDirectories().Length : 0; }
        public bool IsValid { get { return DirInfo != null && DirInfo.Exists; } }
        public int ImageCount()
        {
            int imCount = 0;
            int infoCount = 0;
            FileInfo[] files = IsValid ? DirInfo.GetFiles() : new FileInfo[0];
            foreach (var f in files)
            {
                var inf = new ImageFileName(f.FullName);
                if (inf.IsImage)
                    imCount++;
                if (inf.IsInfoImage)
                    infoCount++;
            }
            return imCount - infoCount;
        }
    }
    public class ImageFileInfo : ImageFileName
    {
        static Image failedImage = LoadSpecialImage("failedImage.png");
        static Image mediaImage = LoadSpecialImage("mediaImage.png");
        static Image localFilesImage = LoadSpecialImage("localImage.png");
        static Image multiLayerImage = LoadSpecialImage("multiLayerImage.png");
        static public Image notLoadedImage = LoadSpecialImage("notLoadedImage.png");
        const int infoImageWidth = 144;
        const int infoImageHeight = 208;
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
        public bool Modified { get; private set; } // true indicatates for client to get new image 
        bool cynchronized = false;          // false indicatates need to load image
        bool priority = false;              // true indicatates need for priority loading of visible image
        Image thumbnail;                    // image displayed in preview mode
        DateTime modifiedTime;              // update time of the image 
        public FileInfo FileInfo { get; private set; }
        public ImageGroup Group { get; set; }  // != null when fisrt member of a group
        public bool IsGroupHead { get { return Group != null; } } // indicates first image in group
        public string FSPath { get { return FileInfo == null ? "" : FileInfo.FullName; } } // system path of image file
        public string RealPath { get { return FileInfo == null ? "" : Path.Combine(FileInfo.Directory.Parent.FullName, FileName.UnMangle(FileInfo.Directory.Name), RealName); } } // complete path of image object
        public bool IsHeader { get; private set; } // image representing child directory in image list - info image show with child directory name
        public int DisplayListIndex { get; private set; } = -1; // >=0 when in list
        public ImageFileInfo(FileInfo fi, bool header = false) : base(fi.Name) 
        { 
            FileInfo = fi;
            IsHeader = header;
            if (IsHeader)
            {
                int dirCount = FileInfo.Directory.GetDirectories().Length;
                var idi = new ImageDirInfo(FileInfo.Directory);
                RealName = FileName.UnMangle(FileInfo.Directory.Name) + '\u25CF' + idi.ImageCount() + (dirCount == 0 ? "" : "-" + dirCount);
                Name = FileInfo.Directory.Name;
            }
        }
        public bool CheckUpdate()  // false if file does not exists
        { 
            FileInfo fi = new FileInfo(FSPath);
            if (!fi.Exists || fi.Length == 0)
                return false; // no data exists
            if (fi.LastWriteTime > modifiedTime)
                cynchronized = false;// image updated
            return true;
        }
        void SetCynchronized(DateTime dt)   
        {
            modifiedTime = dt;
            cynchronized = true;
            Modified = true;
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
        public Image SynchronizeThumbnail()                
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
                        BitmapAccess ba = BitmapAccess.LoadImage(FSPath, IsEncrypted, 200);
                        if (ba == null)
                            return null;
                        Bitmap bm = ba.CreateBitmapImage();
                        thumbnail = CreateThumbnail(bm);
                        thumbnail.Tag = FSPath;
                        FileInfo fi = new FileInfo(FSPath);   // FileInfo recreated to get new LastWriteTime
                        SetCynchronized(fi.LastWriteTime);
                    }
                    catch (Exception)   // legal exception - update may fail if file is being modified
                    {
                        thumbnail = failedImage;
                    }
                    break;
                case DataType.MOV:
                case DataType.EncMOV:
                    thumbnail = mediaImage;
                    SetCynchronized(DateTime.Now);
                    break;
                case DataType.EncMLI:
                    byte[] ta = VisualLayerData.LoadSerializedThumbnail(FSPath, IsEncrypted);
                    thumbnail = ta == null ? multiLayerImage : CreateThumbnail(new Bitmap(new MemoryStream(ta)));
                    SetCynchronized(DateTime.Now);
                    break;
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
            if (!cynchronized)
            {
                priority = true;
                return null;
            }
            Modified = false;
            return thumbnail;
        }
        string Rename(string newName)       // returns new full name
        {
            try
            {
                if (IsInfoImage)
                    return FSPath;
                FileInfo.Refresh();
                string ext = Path.GetExtension(FSPath);
                RealName = newName;
                FSName = FileName.UnMangle(RealName);
                newName = FSName + ext;
                string newFullPath = Path.Combine(FileInfo.Directory.FullName, newName);
                FileInfo.MoveTo(newFullPath);
                Modified = true;
                FileInfo = new FileInfo(newFullPath);
                Name = FileInfo.Name;
                return newFullPath;
            }
            catch { return null; }
        }
        public class NameComparer : IComparer
        {
            int IComparer.Compare(object l1, object l2)
            {
                string if1 = (string)l1;
                string if2 = (string)l2;
                if (if1.IndexOf(multiNameChar) < 0 && if2.IndexOf(multiNameChar) >= 0)
                    return -1;
                else if (if1.IndexOf(multiNameChar) >= 0 && if2.IndexOf(multiNameChar) < 0)
                    return 1;
                return string.Compare(if1, if2, true);
            }
        }
        public class RealNameComparer : IComparer<ImageFileInfo>
        {
            IComparer ifhc = new NameComparer();
            int IComparer<ImageFileInfo>.Compare(ImageFileInfo l1, ImageFileInfo l2)
            {
                if (l1.IsHeader && !l2.IsHeader)
                    return -1;
                if (!l1.IsHeader && l2.IsHeader)
                    return 1;
                if (l1.IsInfoImage && !l2.IsInfoImage)
                    return -1;
                if (!l1.IsInfoImage && l2.IsInfoImage)
                    return 1;
                return ifhc.Compare(l1.RealName, l2.RealName);
            }
        }
        public class ImageGroup
        {
            public string Name { get; private set; }
            public int First { get; private set; }
            public int Last { get; private set; }
            public bool Expanded { get; set; }
            public ImageGroup(List<ImageFileInfo> fileInfos, int ind)
            {
                var fileName = fileInfos[ind].RealName;
                int len = fileName.Length;
                int dm = Math.Min(len, 3);
                int digits = 0;
                for (; digits < dm; digits++)
                    if (!char.IsDigit(fileName[len - 1 - digits]))
                        break;
                First = ind;
                Last = 0;
                Name = len == digits ? "" : len == digits + 1 ? fileName.Substring(0, 1) : null;
                if (Name == null)
                {
                    char last = fileName[len - 1 - digits];
                    Name = IsSeparator(last) ? fileName.Substring(0, len - digits - 1) : fileName.Substring(0, len - digits);
                }
                Expanded = false;
                return;
            }
            public void SetLast(int li) { if (Last == 0) Last = li; }
            public override string ToString() { return Name + " [" + First + '-' + Last + ']'; }
            bool IsSeparator(char c) { return c == '-' || c == '_'; }
            internal bool NameMatches(ImageFileInfo ifi)
            {
                var fileName = ifi.RealName;
                if (!fileName.StartsWith(Name))     // has starting with Name
                    return false;
                var len = fileName.Length;
                if (len != Name.Length)             // allowed no separator or digit after Name
                {
                    int s = IsSeparator(fileName[Name.Length]) ? 1 : 0; // can have separator after Name
                    int ds = Name.Length + s;
                    if (len - ds > 3)               // not more than 3 digits
                        return false;
                    for (int i = ds; i < len; i++)  // only digits allowed at the end
                        if (!char.IsDigit(fileName[i]))
                            return false;
                }
                return true;
            }
            internal bool Contains(int ind) { return ind >= First && ind <= Last; }
        }
        public class ImageList
        {   // sortable list of 'ImageFileInfo' accessible by key, index or Group of similar
            static int synchronizationDelay = 300; // synchronization delay between directory and image collection
            ListShowMode viewMode = ListShowMode.Auto;         
            DirectoryInfo directory = null; // source directory (search path of dirList or image and subDir source)
            bool dirModified = false;
            string[] dirList = null;        // if(dirList!=null) list of subDirs of the directory matching search criteria else => all images and subDirs of the directory will be shown
            DateTime lastUpdated;           // last access time of underlying directory
            DirShowMode viewInfoType;       // type of info if view mode is info     
            Dictionary<string, int> indexTable = new Dictionary<string, int>(); // stores system file name and fileList index pairs
            List<ImageFileInfo> fileList = new List<ImageFileInfo>();   // holds both local image files and subdir header files
            List<ImageGroup> groupList = new List<ImageGroup>();        // header file names of image groups (same StartsWith). Applies only to directory source
            List<ImageFileInfo> displayed = new List<ImageFileInfo>();  // image list displayed by list view window
            public int Count { get { return displayed.Count; } }
            public int GroupCount { get { return groupList.Count; } }
            public int ImageCount { get { return fileList.Count; } }
            int prevImageCount = 0;         // image count in directory before update
            BackgroundWorker fileSyncWorker;// keeping synchronization between list and directory
            public event VoidNoArg notifyEmptyDir = null;
            int thumbnailUpdateIndex = 0;
            bool abortSynchronization = false;
            public string DirRealName       { get { return directory == null ? "" : FileName.UnMangle(directory.Name); } }
            bool IsUpdating                 { get { return fileSyncWorker != null && fileSyncWorker.IsBusy; } }
            public ImageFileInfo ActiveFile { get; private set; } // current file name
            public string ActiveFileFSPath  { get { return ActiveFile == null ? "" : ActiveFile.FSPath; } }
            ImageFileInfo lastAdded;        // file added to a directory
            public ImageFileInfo LastAdded  { get { bool show = ImageCount > prevImageCount; prevImageCount = ImageCount; return show ? lastAdded : null; } } 
            public bool ValidDirectory      { get { return directory!=null && directory.Exists; } }
            public ImageFileInfo this[int i]
            {
                get
                {
                    if (i < 0 && i >= Count)
                        return null;
                    try { return displayed[i]; }
                    catch { return null; }
                }
            }
            public ImageList(ImageDirInfo dir, DirShowMode it, string mode) { Initialize(dir, it, mode, null); }
            public ImageList(ImageDirInfo dir, string mode, string[] list) { Initialize(dir, ImageProcessor.DirShowMode.Detail, mode, list); }
            void Initialize(ImageDirInfo dir, DirShowMode it, string mode, string[] list)
            {
                directory = dir.DirInfo;
                if (!ValidDirectory)
                    throw new Exception("Directory '"+dir.FSPath+"' does not exists");
                viewInfoType = it;
                SetViewMode(mode);
                dirList = list;
                //isTemp = list == null && Navigator.IsSpecDir(dir.DirInfo, SpecName.NewArticles);
                ActiveFile = null;
                fileSyncWorker = new BackgroundWorker();
                fileSyncWorker.DoWork += Synchronization_DoWork;
                fileSyncWorker.RunWorkerCompleted += Synchronization_RunWorkerCompleted;
                SynchronizeDirectory();
            }
            ~ImageList()
            {
                Clear();
                if (fileSyncWorker != null)
                    fileSyncWorker.Dispose();
            }
            public void Clear()
            {
                abortSynchronization = true;
                notifyEmptyDir = null;
                directory = null;
                dirList = null;
                fileList.Clear();
                prevImageCount = 0;
                indexTable.Clear();
            }
            #region fileList maintenance and thumbnail sinchronization
            void Synchronization_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
            {
                try
                {
                    if (ImageCount == 0)
                        notifyEmptyDir?.Invoke(); // sends empty dir notification
                }
                finally { }
            }
            void Synchronization_DoWork(object sender, DoWorkEventArgs e)
            {
                //Debug.WriteLine("RunSynchronization " + directory.Name + " thread "+Thread.CurrentThread.Name);
                while (true)
                {
                    try
                    {
                        if (!UpdateImageList())
                            break;
                    }
                    catch   //(Exception ex)
                    {
                        if (abortSynchronization || !IsUpdating)
                            break;
                        //Debug.WriteLine("RunSynchronization: " + ex.Message);
                        if (directory != null)
                            directory = new DirectoryInfo(directory.FullName); // needed to keep dir info up-to-date
                        if (directory == null || !directory.Exists)
                            break;
                    }
                    Thread.Sleep(synchronizationDelay);
                }
            }
            bool UpdateImageList()
            {
                try
                {
                    if (ValidDirectory)
                        directory = new DirectoryInfo(directory.FullName);  // updates info
                    if (!ValidDirectory)
                        return false;
                    var downloadedDir = Navigator.IsSpecDir(directory, SpecName.Downloaded);
                    lock (this)
                    {
                        if (dirModified || lastUpdated < directory.LastWriteTime)
                        {   // rebuild collection if directory content changed
                            prevImageCount = ImageCount;
                            lastAdded = null;
                            AppendFiles(downloadedDir);           // appending new files to the list
                            List<string> deletedFiles = new List<string>(); // names of deleted, moved, or renamed files
                            foreach (ImageFileInfo d in fileList)
                                if (!d.CheckUpdate())
                                    deletedFiles.Add(d.Name);
                            if (deletedFiles.Count > 0)
                            {
                                int[] indexes = new int[deletedFiles.Count]; // indexes of deleted, moved, or renamed files
                                for (int i = 0; i < indexes.Length; i++)
                                    indexes[i] = FileListIndex(deletedFiles[i]);
                                Array.Sort(indexes);
                                for (int i = indexes.Length - 1; i >= 0; i--)  // removing deleted files from the list
                                    if (indexes[i] >= 0)
                                        fileList.RemoveAt(indexes[i]);
                                if (ImageCount == 0)
                                {
                                    abortSynchronization = true;
                                    return false;
                                }
                            }
                            if (downloadedDir && prevImageCount > 0)
                                RebuildIndexesNoSoting();
                            else
                                RebuildIndexesAndGroups();
                            if(prevImageCount == 0)
                                lastAdded = null;
                            dirModified = false;
                        }
                        int loaded = 0;
                        foreach (ImageFileInfo ifo in fileList)
                        {
                            if (abortSynchronization)
                                break;
                            if (!ifo.priority)
                                continue;
                            ifo.SynchronizeThumbnail();
                            loaded++;
                            //Debug.WriteLine("priority " + d.Name + " updated");
                        }
                        if (loaded == 0)
                            UpdateHiddenThumbnails(3);
                        return !abortSynchronization;
                    }
                }
                catch (Exception ex) 
                { 
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                    return false;
                }
            }
            void AppendFiles(bool downloadedDir)
            {
                DirectoryInfo[] directories;
                if (dirList == null) // so far only directories passes => all entries treated as directories
                    directories = directory.GetDirectories();
                else
                {
                    directories = new DirectoryInfo[dirList.Length];
                    for (int i = 0; i < dirList.Length; i++)
                    {
                        string dirFullName = Path.Combine(directory.FullName, dirList[i]);
                        directories[i] = Directory.Exists(dirFullName) ? new DirectoryInfo(dirFullName) : null;
                        if (DataAccess.PrivateAccessEnforced && directories[i] == null)
                        {
                            dirFullName = Path.Combine(directory.FullName, FileName.MangleFile(dirList[i]));
                            directories[i] = Directory.Exists(dirFullName) ? new DirectoryInfo(dirFullName) : null;
                        }
                    }
                }
                foreach (DirectoryInfo di in directories)
                {
                    if (di == null)
                        continue;
                    try
                    {
                        FileInfo fsf = GetInfoFile(di);
                        if (fsf != null)
                        {
                            if (downloadedDir && prevImageCount > 0)
                                AddImageFileToFront(fsf, true);
                            else
                                AppendImageFile(fsf, true);
                        }
                        //AppendImageFile(fsf, true);   // adds info of subdirectories
                    }
                    finally { }
                }
                FileInfo[] files;
                if (dirList == null)
                    files = directory.GetFiles();
                else
                    files = new FileInfo[0]; // all entries treated as directories
                foreach (var fi in files)
                {
                    if (downloadedDir && prevImageCount > 0)
                        AddImageFileToFront(fi);
                    else
                        AppendImageFile(fi);
                }
                lastUpdated = directory.LastAccessTime;
                //if (ImageCount > prevImageCount)
                //    Debug.WriteLine(" *AppendFiles "+prevImageCount.ToString() + " -> " + ImageCount + " first added " + fileList[prevImageCount].RealName);
            }
            void AddImageFileToFront(FileInfo fi, bool header = false)
            {   // insert new item to the front of list and indexTable
                var ifi = new ImageFileInfo(fi, header);
                try
                {
                    if (indexTable.ContainsKey(ifi.Name))
                        return;
                    //Debug.WriteLine("Appenging to front: " + ifi.Name);
                    if (!header && !ifi.IsKnown)
                        return;
                    lock (this)
                    {
                        lastAdded = ifi;
                        fileList.Insert(0, lastAdded);
                    }
                }
                catch
                {
                    //Debug.WriteLine("FAILED @: " + ifi.Name);
                }
            }
            void AppendImageFile(FileInfo fi, bool header = false)
            {   // append new item to the list and indexTable
                var ifi = new ImageFileInfo(fi, header);
                try
                {
                    if (indexTable.ContainsKey(ifi.Name))
                        return;
                    //Debug.WriteLine("Appenging to list: " + ifi.Name);
                    if (!header && !ifi.IsKnown)
                        return;
                    lock (this)
                    {
                        lastAdded = ifi;
                        fileList.Add(lastAdded);
                    }
                }
                catch
                {
                    //Debug.WriteLine("FAILED @: " + ifi.Name);
                }
            }
            void RebuildIndexesNoSoting() 
            {   // rebuilds indexes witout sorting
                indexTable.Clear();
                int i = 0;
                try
                {
                    for (; i < ImageCount; i++)
                    {
                        fileList[i].DisplayListIndex = -1;
                        indexTable.Add(fileList[i].Name, i);
                        //Debug.WriteLine("Appenging to table: " + fileList[i].Name);
                    }
                }
                catch
                {
                    //Debug.WriteLine("FAILED table @: " + fileList[i].Name);
                }
                RebuildDisplayedList();
            }
            void RebuildIndexesAndGroups()
            {   // applied when fileList changed
                indexTable.Clear();
                var newGL = new List<ImageGroup>();
                int i = 0;
                try
                {
                    lock (this)
                    {
                        fileList.Sort(new ImageFileInfo.RealNameComparer());
                        for (; i < ImageCount; i++)    
                        {
                            var ifi = fileList[i];
                            //ifi.SetListIndex();
                            indexTable.Add(ifi.Name, i); // complete rebuild of index list
                            int n = newGL.Count;
                            if (ifi.IsHeader || ifi.IsInfoImage)
                            {
                                if (n > 0)
                                    newGL[n - 1].SetLast(i - 1);
                                continue;
                            }
                            if (n == 0 || !newGL[n - 1].NameMatches(ifi))
                            {
                                newGL.Add(new ImageGroup(fileList, i));
                                ifi.Group = newGL[newGL.Count-1];
                                if (n > 0)
                                    newGL[n - 1].SetLast(i - 1);
                            }
                        }
                        if (newGL.Count > 0)
                            newGL[newGL.Count - 1].SetLast(i - 1);
                        if (newGL.Count == GroupCount)
                            for (i = 0; i < newGL.Count; i++)
                                if (newGL[i].Name == groupList[i].Name)
                                    newGL[i].Expanded = groupList[i].Expanded;
                        groupList = newGL;
                    }
                }
                catch
                {
                    string s = fileList[i].FSPath;
                }
                //Debug.WriteLine(" *RebuildIndexesAndGroups list=" + ImageCount + " groups=" + GroupCount);
                RebuildDisplayedList();
                //foreach (var gr in groupList)
                //    Debug.WriteLine(gr.ToString());
            }
            public void RebuildDisplayedList()
            {
                Thread.Sleep(150);
                displayed.Clear();
                int fc = ImageCount;
                int gc = GroupCount;
                bool groupsHeadsOnly = true;
                switch (viewMode)
                {
                    case ListShowMode.Auto: groupsHeadsOnly = gc > 1 && fc > 200 || gc > 3 && fc > 100 || gc > 10 && gc < fc / 3; break;
                    case ListShowMode.List: groupsHeadsOnly = false; break;
                    case ListShowMode.Groups: groupsHeadsOnly = true; break;
                }
                lock (this)
                {
                    int gInd = 0;
                    int ind = 0;
                    for (int i = 0; i < ImageCount; i++)
                    {   // complete rebuild of index list
                        var ifi = fileList[i];
                        if(!groupsHeadsOnly || ifi.IsHeader || ifi.IsInfoImage) // !groupsHeadsOnly showing all images
                        {   // !groupsHeadsOnly showing all images
                            ifi.DisplayListIndex = ind++;
                            displayed.Add(ifi);
                            continue;
                        }
                        if (!groupList[gInd].NameMatches(ifi))
                        {
                            gInd++;
                            if (gInd >= GroupCount)
                                continue;
                        }
                        if (groupList[gInd].NameMatches(ifi) && (ifi.IsGroupHead || groupList[gInd].Expanded))
                        {
                            ifi.DisplayListIndex = ind++;
                            displayed.Add(ifi);
                        }
                    }
                    //Debug.Assert(displayed.Count == ind);
                }
                //Debug.WriteLine(" *RebuildDisplayedList displayed=" + displayed.Count);
            }
            #endregion
            public void SetViewMode(string modeName)
            {
                string[] names = Enum.GetNames(typeof(ListShowMode));
                for (int i = 0; i < names.Length; i++)
                    if (names[i] == modeName)
                        viewMode = (ListShowMode)i;
                bool exp = viewMode == ListShowMode.List;
                if (viewMode != ListShowMode.Auto)
                    foreach (var gr in groupList)
                        gr.Expanded = exp;
                RebuildDisplayedList();
            }
            public void SortFileListByRealName() { RebuildIndexesAndGroups(); }
            public int FileListIndex(string name) { if(indexTable.TryGetValue(name, out int ind)) return ind; return -1; }
            public string MoveFiles(ImageFileInfo[] filesToMove, DirectoryInfo toDirectory)
            {
                if (toDirectory != null && !Directory.Exists(toDirectory.FullName))
                    return "Destination directory '" + toDirectory + "' does not exist";
                string warnings = "";
                lock (this)
                {
                    filesToMove = filesToMove == null ? fileList.ToArray() : filesToMove;
                    bool delete = toDirectory == null;  // deleting files from filesToMove List
                    foreach (ImageFileInfo ifi in filesToMove)
                    {
                        if (ifi == null || ifi.IsHeader) 
                            continue;   // files representing child directories can't be moved
                        try
                        {
                            if (delete)
                            {
                                var er = Delete(ifi.FSPath);
                                if (!string.IsNullOrEmpty(er))
                                    warnings += er + Environment.NewLine;
                            }
                            else if (ifi.IsEncrypted)
                            {
                                string dest = Path.Combine(toDirectory.FullName, FileName.MangleFile(Path.GetFileName(ifi.FSPath)));
                                File.Move(ifi.FSPath, dest);
                            }
                            else if (DataAccess.PrivateAccessEnforced && !ifi.IsEncrypted)
                            {   // when PrivateAccessAllowed move images with encription and name mangling
                                string name = ifi.IsMovie ? ifi.FSName + ".vid" : ifi.IsExact ? ifi.FSName + ".exa" : ifi.FSName + ".jpe";
                                byte[] src = File.ReadAllBytes(ifi.FSPath);
                                if (!DataAccess.WriteFile(Path.Combine(toDirectory.FullName, FileName.MangleFile(name)), src, true))
                                    warnings += ifi.FSName + ": " + DataAccess.Warning + Environment.NewLine;
                                else
                                {
                                    var er = Delete(ifi.FSPath);
                                    if (!string.IsNullOrEmpty(er))
                                        warnings += er + Environment.NewLine;
                                }
                            }
                            else
                            {
                                string dest = Path.Combine(toDirectory.FullName, Path.GetFileName(ifi.FSPath));
                                File.Move(ifi.FSPath, dest);
                            }
                        }
                        catch (Exception ex)        // legal exception
                        {
                            warnings += ifi.FSPath + " was not moved: " + ex.Message + "  ";
                        }
                    }
                }
                dirModified = true;
                //RebuildIndexesAndGroups();
                return warnings;
            }
            public void Rename(ImageFileInfo ifi, string newName)
            {
                if (string.IsNullOrEmpty(newName))
                    return;

                lock (this)
                {
                    int ind = indexTable[ifi.Name];
                    ifi.Rename(newName);
                    dirModified = true;
                    fileList.RemoveAt(ind);
                }
            }
            public ImageFileInfo SetActiveFile(ImageFileInfo ifi) { return ActiveFile = ifi; }
            public ImageFileInfo SetActiveFile(Direction dest) { int ind = NewDisplayIndex(dest); ActiveFile = ind < 0 ? null : displayed[ind]; return ActiveFile; }
            public int NewDisplayIndex(Direction dest) // only predicts index of the  next image
            {
                if (ActiveFile != null && dest != Direction.current)
                {
                    int ind = ActiveFile.DisplayListIndex;
                    int del = dest == Direction.Next || dest == Direction.NextGroup ? 1 : -1;
                    ind += del;
                    if (dest == Direction.Next)
                        return ind < 0 ? displayed.Count - 1 : ind >= displayed.Count ? 0 : ind;
                    for (int i = ind; i < displayed.Count && i >= 0; i += del)
                        if (displayed[i].IsGroupHead)
                            return i;
                }
                return -1;
            }
            void SynchronizeDirectory()     
            {
                if (IsUpdating)
                    return;
                try
                {
                    abortSynchronization = false;
                    fileSyncWorker.RunWorkerAsync();
                }
                catch { }
            }
            void UpdateHiddenThumbnails(int max)
            {
                if (thumbnailUpdateIndex >= ImageCount)
                {
                    thumbnailUpdateIndex = 0;
                    return;
                }
                while (thumbnailUpdateIndex < ImageCount)
                {
                    ImageFileInfo ifi = fileList[thumbnailUpdateIndex];
                    if (!ifi.cynchronized && !ifi.priority)
                    {
                        //Debug.WriteLine("hidden " + d.Name + " updated " + max);
                        if (abortSynchronization)
                            break;
                        ifi.SynchronizeThumbnail();
                        if(max-- <0)
                            return;
                    }
                    thumbnailUpdateIndex++;
                }
            }
            FileInfo GetInfoFile(DirectoryInfo di)
            {
                bool useMangled = DataAccess.PrivateAccessEnforced;
                string nInfo = InfoFileName(viewInfoType);
                string nMangled = FileName.MangleFile(nInfo);
                FileInfo info = null;
                FileInfo img = null;
                foreach (var f in di.GetFiles())
                {
                    if (useMangled && f.Name.StartsWith(nMangled))
                        return f;
                    if (info == null && f.Name.StartsWith(nInfo))
                    {
                        if(!useMangled)
                            return f;
                        info = f;
                    }
                    var ft = new ImageFileName(f.Name);
                    if (img == null && ft.IsImage)
                        img = f;
                }
                return info != null ? info : img;
            }
        }
    }
}
