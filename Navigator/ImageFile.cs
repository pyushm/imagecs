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
        protected readonly DataType data;      // type associated with image 
        public string FSName { get; protected set; }    // FS (mangled) file name without extention
        public string RealName { get; protected set; }  // real name without extention
        public bool IsInfoImage { get; private set; }   // true if it is a directory info image
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
        public string FSPath { get { return DirInfo == null ? "" : DirInfo.FullName; } } // complete path of image object
        public string RealPath { get { return DirInfo == null ? "" : Path.Combine(DirInfo.Parent.FullName, RealName); } } // complete path of image object
        public int DirCount() { return DirInfo.GetDirectories().Length; }
        public int ImageCount()
        {
            int imCount = 0;
            int infoCount = 0;
            FileInfo[] files = DirInfo.GetFiles();
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
        public FileInfo FileInfo            { get; private set; }
        public string FSPath { get { return FileInfo == null ? "" : FileInfo.FullName; } } // complete path of image object
        public string RealPath { get { return FileInfo == null ? "" : Path.Combine(FileInfo.Directory.Parent.FullName, FileName.UnMangle(FileInfo.Directory.Name), RealName); } } // complete path of image object
        public bool IsHeader            { get; private set; } // image representing directory in image list 
        public ImageFileInfo(FileInfo fi, bool header = false) : base(fi.Name) 
        { 
            FileInfo = fi; 
            IsHeader = header;
            if (IsHeader)
            {
                int dirCount = FileInfo.Directory.GetDirectories().Length;
                var idi = new ImageDirInfo(FileInfo.Directory);
                RealName = FileName.UnMangle(FileInfo.Directory.Name) + '\u25CF' + idi.ImageCount() + (dirCount == 0 ? "" : "-" + dirCount);
            }
        }
        public bool CheckUpdate()
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
                    catch (Exception ex)   // legal exception - update may fail if file is being modified
                    {
                        thumbnail = failedImage;
                        Debug.WriteLine(FSPath+": "+ ex.Message);
                        //Debug.WriteLine("loading thumbnail '"+ FullPath + "' failed");
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
                string newFullPath;
                newFullPath = Path.Combine(FileInfo.Directory.FullName, newName);
                FileInfo.MoveTo(newFullPath);
                Modified = true;
                FileInfo = new FileInfo(newFullPath);
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
        public class FileList             
        {
            struct Group
            {
                public string Name { get; private set; }
                public int First { get; private set; }
                public int Length { get; private set; }
                int digits;
                public Group(string fileName, int ind)
                {
                    int len = fileName.Length;
                    int dm = Math.Min(len, 3);
                    int i = 0;
                    for (; i < dm; i++)
                        if (char.IsDigit(fileName[len - 1 - i]))
                            break;
                    digits = i;
                    First = ind;
                    Length = 1;
                    Name = len == digits ? "" : len == digits + 1 ? fileName.Substring(0, 1) : null;
                    if (Name == null)
                    {
                        char last = fileName[len - 1 - i];
                        Name = last == '-' || last == '_' ? fileName.Substring(0, len - digits - 1) : fileName.Substring(0, len - digits);
                    }
                    return;
                }
            }
            static int synchronizationDelay = 300; // synchronization delay between directory and image collection
            bool isTemp;                    // true if directory is a temp store of new articles
            DirectoryInfo directory = null; // source directory (search path of dirList or image and subDir source)
            string[] dirList = null;        // if(dirList!=null) list of subDirs of the directory matching search criteria else => all images and subDirs of the directory will be shown
            DateTime lastUpdated;           // last access time of underlying directory
            DirShowMode viewInfoType;       // type of info if view mode is info 
            ImageFileInfo activeFile;       // current file name
            Dictionary<string, int> indexTable = new Dictionary<string, int>(); // FSPath to index table
            List<ImageFileInfo> imageFileList = new List<ImageFileInfo>();// holds both local files and subdirs
            List<Group> groupList = new List<Group>();  // holds file name (same source) image groups. Applies only to directory source
            BackgroundWorker fileSyncWorker; // keeping synchronization between list and directory
            public event VoidNoArg notifyEmptyDir = null;
            int thumbnailUpdateIndex = 0;
            bool abortSynchronization = false;
            public string RealName          { get { return directory == null ? "" : FileName.UnMangle(directory.Name); } }
            public int Count                { get { return imageFileList.Count; } }
            bool IsUpdating                 { get { return fileSyncWorker != null && fileSyncWorker.IsBusy; } }
            public ImageFileInfo ActiveFile { get { return activeFile; } }
            public string ActiveFileFSPath  { get { return activeFile == null ? "" : activeFile.FSPath; } }
            public bool IsFirst             { get { return ImageFileIndex(ActiveFileFSPath) == 0; } }
            public bool IsLast              { get { return ImageFileIndex(ActiveFileFSPath) == Count - 1; } }
            public ImageFileInfo Last       { get { return Count>0 ? imageFileList[Count - 1] : null; } }
            public ImageFileInfo LastAdded  { get; private set; }
            public bool ValidDirectory      { get { return directory!=null && directory.Exists; } }
            public ImageFileInfo this[int i]
            {
                get
                {
                    if (i < 0 && i >= Count)
                        return null;
                    try { return imageFileList[i]; }
                    catch { return null; }
                }
            }
            public FileList(ImageDirInfo dir, DirShowMode it) { Initialize(dir, it, null); }
            public FileList(ImageDirInfo dir, string[] list) { Initialize(dir, ImageProcessor.DirShowMode.Detail, list); }
            void Initialize(ImageDirInfo dir, DirShowMode it, string[] list)
            {
                directory = dir.DirInfo;
                if (!ValidDirectory)
                    throw new Exception("Directory '"+dir.FSPath+"' does not exists");
                viewInfoType = it;
                dirList = list;
                isTemp = list == null && Navigator.IsSpecDir(dir.DirInfo, SpecName.NewArticles);
                activeFile = null;
                fileSyncWorker = new BackgroundWorker();
                fileSyncWorker.DoWork += Synchronization_DoWork;
                fileSyncWorker.RunWorkerCompleted += Synchronization_RunWorkerCompleted;
                AppendFiles();
                if (dirList == null)
                    Sort(new RealNameComparer());
                SynchronizeDirectory();
            }
            void RebuildIndexing()
            {
                indexTable.Clear();
                int i = 0;
                try
                {
                    for (; i < Count; i++)     // complete rebuild of index list
                        indexTable.Add(this[i].FSPath, i);
                }
                catch
                {
                    string s = this[i].FSPath;
                }
                if (dirList == null)
                    UpdateGroupList();
            }
            void UpdateGroupList()
            {
                Group gr = new Group();
                for(int i=0; i < Count; i++)
                {
                    if (!this[i].IsImage || this[i].IsInfoImage)
                        continue;

                }
            }
            ~FileList()
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
                LastAdded = null;
                imageFileList.Clear();
                indexTable.Clear();
            }
            public void Sort(IComparer<ImageFileInfo> c) 
            {
                lock (this)
                {
                    imageFileList.Sort(c);
                    RebuildIndexing();
                }
            }
            public int ImageFileIndex(string fileName) { if(indexTable.TryGetValue(fileName, out int ind)) return ind; return -1; }
            public string MoveFiles(ImageFileInfo[] filesToMove, DirectoryInfo toDirectory)
            {
                if (toDirectory != null && !Directory.Exists(toDirectory.FullName))
                    return "Destination directory '" + toDirectory + "' does not exist";
                lock (this)
                {
                    string warnings = "";
                    filesToMove = filesToMove == null ? imageFileList.ToArray() : filesToMove;
                    bool delete = toDirectory == null;  // deleting files from filesToMove List
                    foreach (ImageFileInfo ifi in filesToMove)
                    {
                        if (ifi == null || ifi.IsHeader) 
                            continue;   // files representintimc directories can't be moved
                        try
                        {
                            if (activeFile != null && filesToMove != null && ifi.FSPath == activeFile.FSPath)
                                NavigateTo(1);
                            if (delete)
                                warnings += Delete(ifi.FSPath);
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
                                    warnings += ifi.FSName + ": " + DataAccess.Warning;
                                else
                                {
                                    string warning = Delete(ifi.FSPath);
                                    if (warning.Length > 0)
                                        warnings += warning;
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
                    return warnings;
                }
            }
            public void Rename(ImageFileInfo ifi, string newName)
            {
                lock (this)
                {
                    string oldName = ifi.FSPath;
                    if (newName == null || newName.Length == 0)
                        return;
                    int ind = indexTable[oldName];
                    string newFullName = ifi.Rename(newName);
                    if (newFullName != null)
                    {
                        indexTable.Remove(oldName);
                        indexTable.Add(newFullName, ind);
                    }
                }
            }
            public int NewInd(int delta = 0)
            {
                if (activeFile == null)
                    return 0;
                int ind = ImageFileIndex(activeFile.FSPath);
                if (ind < 0)
                    return 0;
                ind += delta;
                if (ind < 0)
                    return Count - 1;
                else if (ind >= Count)
                    return 0;
                return ind;
            }
            public string NavigateTo(int delta)
            {
                int ind = NewInd(delta);
                activeFile = this[ind]; 
                return ActiveFileFSPath;
            }
            public string NavigateToGroup(bool up)
            {
                string n = activeFile == null ? "" : activeFile.RealName;
                int ind = NewInd(up ? 1 : -1);
                if (this[ind] == null)
                    return "";
                string nn = this[ind].RealName;
                int len = n.Length;
                if (len > 3 && len == nn.Length)
                {
                    string common = n.Substring(0, len - 3);
                    if (up)
                    {
                        for (; ind < Count; ind++)
                            if (this[ind] != null && !this[ind].RealName.StartsWith(common))
                                break;
                        if (ind >= Count)
                            ind = 0;
                    }
                    else
                    {
                        for (; ind >= 0; ind--)
                            if (this[ind] != null && !this[ind].RealName.StartsWith(common))
                                break;
                        if (ind < 0)
                            ind = Count - 1;
                    }

                }
                activeFile = this[ind];
                return ActiveFileFSPath;
            }
            public ImageFileInfo SetActiveFile(string filePath)
            {
                int ind = Math.Max(0, ImageFileIndex(filePath));
                try { activeFile = this[ind]; }
                catch { activeFile = null; }
                return activeFile;
            }
            public void FilterFiles(string pattern)
            {
                //while (isLocked)
                //    Thread.Sleep(300);
                lock (this)
                {
                    //isLocked = true;
                    if (pattern.Length == 0)
                        return;
                    string patternl = pattern.ToLower();
                    List<ImageFileInfo> list = new List<ImageFileInfo>();
                    for (int i = 0; i < Count; i++)
                    {
                        if (this[i].RealName.ToLower().IndexOf(pattern) >= 0)
                            list.Add(this[i]);
                    }
                    activeFile = null;
                    imageFileList = list;
                    RebuildIndexing();
                    //isLocked = false;
                }
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
            private void Synchronization_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
            {
                try
                {
                    if(imageFileList.Count == 0)
                        notifyEmptyDir?.Invoke(); // sends empty dir notification
                }
                finally { }
            }
            private void Synchronization_DoWork(object sender, DoWorkEventArgs e)
            {
                //Debug.WriteLine("RunSynchronization " + directory.Name + " thread "+Thread.CurrentThread.Name);
                while (true)
                {
                    try
                    {
                        if(!UpdateImageList())
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
                if (directory != null && directory.Exists)
                    directory = new DirectoryInfo(directory.FullName);  // updates info
                if (directory == null || !directory.Exists)
                    return false;
                lock (this)
                {
                    if (lastUpdated < directory.LastWriteTime)
                    {   // rebuild collection if directory content changed
                        //Debug.WriteLine("updating directory "+ directory.Name + (dirMode ? " subDirs" : " images"));
                        AppendFiles();           // appending new files to the list
                        List<string> deletedFiles = new List<string>();
                        foreach (ImageFileInfo d in imageFileList)
                            if (!d.CheckUpdate())
                                deletedFiles.Add(d.FSPath);            
                        if (deletedFiles.Count > 0)
                        {   // removing deleted files from the list
                            int[] indexes = new int[deletedFiles.Count];
                            for (int i = 0; i < indexes.Length; i++)
                                indexes[i] = ImageFileIndex(deletedFiles[i]);
                            Array.Sort(indexes);
                            for (int i = indexes.Length - 1; i >= 0; i--)
                                if (indexes[i] >= 0)
                                    imageFileList.RemoveAt(indexes[i]);
                            //Debug.WriteLine(imageFileList.Count.ToString() + (dirMode ? " subDirs" : " images") + " left");
                            RebuildIndexing();
                            if (imageFileList.Count == 0)
                            {
                                abortSynchronization = true;
                                return false;
                            }
                        }
                    }
                    int loaded = 0;
                    foreach (ImageFileInfo ifo in imageFileList)
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
            void UpdateHiddenThumbnails(int max)
            {
                if (thumbnailUpdateIndex >= imageFileList.Count)
                {
                    thumbnailUpdateIndex = 0;
                    return;
                }
                while (thumbnailUpdateIndex < imageFileList.Count)
                {
                    ImageFileInfo ifi = imageFileList[thumbnailUpdateIndex];
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
            void AppendFiles()              
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
                            AppendImageFile(fsf, true);   // adds info of subdirectories
                    }
                    finally { }
                }
                FileInfo[] files;
                if (dirList == null)
                    files = directory.GetFiles();
                else
                    files = new FileInfo[0]; // all entries treated as directories
                var downloaded = Navigator.IsSpecDir(directory, SpecName.Downloaded);
                foreach (var fi in files)
                {
                    if (downloaded)
                        AddImageFileToFront(fi);
                    else
                        AppendImageFile(fi);
                }
                lastUpdated = directory.LastAccessTime;
            }
            void AddImageFileToFront(FileInfo fn)
            {   // insert new item to the front of list and indexTable
                if (indexTable.ContainsKey(fn.FullName))
                    return;
                var item = new ImageFileInfo(fn, false);
                if (!item.IsKnown)
                    return;
                lock (this)
                {
                    LastAdded = new ImageFileInfo(fn);
                    imageFileList.Insert(0, LastAdded);
                    RebuildIndexing();
                }
            }
            void AppendImageFile(FileInfo fn, bool header = false)
            {   // append new item to the list and indexTable
                if (fn == null || indexTable.ContainsKey(fn.FullName))
                    return;
                var item = new ImageFileInfo(fn, header);
                if (!header && !item.IsKnown)
                    return;
                lock (this)
                {
                    LastAdded = item;
                    imageFileList.Add(LastAdded);
                    indexTable.Add(fn.FullName, imageFileList.Count - 1);
                }
            }
        }
    }
}
