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
    public enum InfoType
    {
        Detail=1,   
        Preview, 
        Sys,
        Vag
    }
    public enum DataType
    {
        JPG,        //
        GIF,
        PNG,
        MLI,        // encripted exact layers
        Movie,
        Animation,
        Dir,
        LocalImages,
        Exact,      // encripted png image
        Regular,    // encripted jpg image
        Video,      // encripted video
        Unknown
    }
    public class ImageFileName
    {   // ImageFileNam has file type and name conversion data
        static Comparison<FileInfo> FileInfoComparison = delegate (FileInfo p1, FileInfo p2)
        {
            string n1 = IsMangled(p1.Name) ? FSUnMangle(p1.Name) : p1.Name;
            string n2 = IsMangled(p2.Name) ? FSUnMangle(p2.Name) : p2.Name;
            return string.Compare(n1, n2);
        };
        static Hashtable knownExtensions = new Hashtable();
        static Hashtable storeTypeString = new Hashtable();
        public const char mangleChar = '\u13B7';
        public static bool IsMangled(string text) { return text != null && text.Length > 0 && text[0] == mangleChar; }
        public static string FSUnMangle(string filePath) // returns path with last component of path (dir or file) replaced by human readable name
        {
            if (filePath == null || filePath.Length == 0)
                return filePath;
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            return Path.Combine(Path.GetDirectoryName(filePath), UnMangleText(fileName) + Path.GetExtension(filePath));
        }
        public static string FSMangle(string filePath) // returns path with last component of path (dir or file) replaced by scrambled name
        {
            if (filePath == null || filePath.Length == 0)
                return filePath;
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            return Path.Combine(Path.GetDirectoryName(filePath), MangleText(fileName) + Path.GetExtension(filePath));
        }
        public static string UnMangleText(string src)   // returns unscrambled src if src scrambled; otherwise returns src
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
        public static string MangleText(string src)  // return scrambled src if src not scrambled; otherwise returns src
        {
            if (src == null || src.Length == 0 || src[0] == mangleChar)
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
        static public readonly InfoType[] InfoModes;
        static ImageFileName()
        {
            InfoModes = (InfoType[])Enum.GetValues(typeof(InfoType));
            knownExtensions.Add(".jpg", DataType.JPG);
            knownExtensions.Add(".jpeg", DataType.JPG);
            knownExtensions.Add(".gif", DataType.GIF);
            knownExtensions.Add(".bmp", DataType.PNG);
            knownExtensions.Add(".png", DataType.PNG);
            knownExtensions.Add(".exa", DataType.Exact);
            knownExtensions.Add(".jpe", DataType.Regular);
            knownExtensions.Add(".drw", DataType.MLI);
            knownExtensions.Add(".mpg", DataType.Movie);
            knownExtensions.Add(".mpeg", DataType.Movie);
            knownExtensions.Add(".avi", DataType.Movie);
            knownExtensions.Add(".wmv", DataType.Movie);
            knownExtensions.Add(".mov", DataType.Movie);
            knownExtensions.Add(".mp4", DataType.Movie);
            knownExtensions.Add(".asf", DataType.Movie);
            knownExtensions.Add(".mkv", DataType.Movie);
            knownExtensions.Add(".flv", DataType.Movie);
            knownExtensions.Add(".vid", DataType.Video);
            storeTypeString.Add(DataType.JPG, " JPG ");
            storeTypeString.Add(DataType.PNG, " PNG ");
            storeTypeString.Add(DataType.Regular, "<JPG>");
            storeTypeString.Add(DataType.Exact, "<PNG>");
            storeTypeString.Add(DataType.MLI, "<MLI>");
            storeTypeString.Add(DataType.GIF, " GIF ");
            storeTypeString.Add(DataType.Movie, "Movie");
            storeTypeString.Add(DataType.Video, "<VID>");
        }
        static public InfoType? InfoMode(string fileName)
        {
            string name = Path.GetFileName(fileName);
            foreach (InfoType m in InfoModes)
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
        static public string InfoFileName(InfoType m) { return infoImagePrefix + m; }
        static public string InfoFileWithExtension(InfoType m) { return InfoFileName(m) + infoImageSuffix; }
        static public Image[] InfoImages(DirectoryInfo di)
        {
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
        public readonly DataType Type;      // type associated with image 
        public string FSName { get; protected set; }    // FS (mangled) file name without extention
        public string RealName { get; protected set; }  // real name without extention
        public bool IsInfoImage { get; private set; }   // true if it is a directory info image
        public bool KnownType { get { return Type != DataType.Unknown; } }
        public bool IsDir { get { return Type == DataType.Dir; } }
        public bool IsLocalImages { get { return Type == DataType.LocalImages; } }
        public bool IsUnencryptedImage { get { return Type == DataType.GIF || Type == DataType.JPG || Type == DataType.PNG; } }  // unencrypted image of any format
        public bool IsEncryptedImage { get { return Type == DataType.Exact || Type == DataType.Regular; } }  // any encrypted image
        public bool IsRegularImage { get { return IsImage && !RealName.StartsWith(infoImagePrefix); } }
        public bool IsImage { get { return IsUnencryptedImage || IsEncryptedImage; } }
        public bool IsMultiLayer { get { return Type == DataType.MLI; } }    // always encrypted
        public bool IsUnencryptedVideo { get { return Type == DataType.Movie; } }  // unencrypted video of any format
        public bool IsEncryptedVideo { get { return Type == DataType.Video; } }  // encrypted video
        public bool IsEncrypted { get { return IsMultiLayer || IsEncryptedImage || IsEncryptedVideo; } } // any encrypted file
        public bool IsExact { get { return Type == DataType.Exact || Type == DataType.PNG; } } // encrypted or unencrypted exact bitmap image
        public string StoreTypeString { get { object o = storeTypeString[Type]; return o == null ? " ??? " : (string)o; } }
        public ImageFileName(string fileName) // name with extension
        {
            Type = FileType(fileName);
            FSName = Path.GetFileNameWithoutExtension(fileName);
            RealName = UnMangleText(FSName);
            IsInfoImage = InfoMode(RealName) != null;
        }
        public ImageFileName(DataType dt, FileInfo fsi, bool temp, bool header)
        {
            Type = dt;
            if (fsi == null)
                FSName = RealName = "";
            else
            {
                FSName = Path.GetFileNameWithoutExtension(fsi.Name);
                RealName = UnMangleText(FSName);
                IsInfoImage = InfoMode(RealName) != null;
                if (dt == DataType.LocalImages)
                    RealName = "LocalImages";
                else if (temp)
                    RealName = NameWithoutTempPrefix(RealName);
                else if (IsInfoImage)
                    RealName = UnMangleText(fsi.Directory.Name) + '_' + RealName[1];
                    //RealName = UnMangleText(fsi.Directory.Name) + '_' + RealName[1];
                else if (header)
                    RealName = Path.GetFileName("dir_" + fsi.DirectoryName);
            }
        }
    }
    public class ImageDirInfo : ImageFileName
    {
        public DirectoryInfo DirInfo { get; private set; }
        public ImageDirInfo(DirectoryInfo di) : base(di.Name) { DirInfo = di; }
        public string FSPath { get { return DirInfo == null ? "" : DirInfo.FullName; } } // complete path of image object
        public string RealPath { get { return DirInfo == null ? "" : Path.Combine(DirInfo.Parent.FullName, RealName); } } // complete path of image object
    }
    public class ImageFileInfo : ImageFileName
    {
        static Image failedImage;
        static Image mediaImage;
        static Image dirImage;
        static Image localImage;
        static Image multiLayerImage;
        static Image notLoadedImage;
        public const char synonymChar = '=';
        public const char multiNameChar = '+';
        const int infoImageWidth = 142;
        const int infoImageHeight = 207;
        static ImageFileInfo()              
        {
            failedImage = LoadSpecialImage("failedImage.png");
            mediaImage = LoadSpecialImage("mediaImage.png");
            dirImage = LoadSpecialImage("dirImage.png");
            localImage = LoadSpecialImage("localImage.png");
            multiLayerImage = LoadSpecialImage("multiLayerImage.png");
            notLoadedImage = LoadSpecialImage("notLoadedImage.png");
        }
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
        static public IntSize PixelSize(InfoType it) { return it == InfoType.Detail || it == InfoType.Preview ? new IntSize(infoImageWidth, infoImageHeight) : new IntSize(infoImageWidth / 2, infoImageWidth / 2); }
        static public FileInfo GetFirstImageName(FileInfo[] files)
        {
            foreach (var f in files)
                if ((new ImageFileName(f.Name)).IsImage)
                    return f;
            return null;
        }
        bool modified = true;               // true indicatates for client to get new image 
        bool cynchronized = false;          // false indicatates need to load image
        bool priority = false;              // true indicatates need for priority loading of visible image
        bool dirHeader;                     // indicate file representing directory
        Image thumbnail;                    // image displayed in preview mode
        DateTime modifiedTime;              // update time of the image 
        public FileInfo FileInfo            { get; private set; }
        public string FSPath { get { return FileInfo == null ? "" : FileInfo.FullName; } } // complete path of image object
        public string RealPath { get { return FileInfo == null ? "" : Path.Combine(FileInfo.Directory.Parent.FullName, UnMangleText(FileInfo.Directory.Name), RealName); } } // complete path of image object
        public bool Modified                { get { return modified; } }
        public bool IsDirHeader             { get { return dirHeader; } }
        public ImageFileInfo(FileInfo fi) : base(fi.Name) { FileInfo = fi; }
        public ImageFileInfo(DataType dt, FileInfo fsi, bool temp, bool header) : base(dt, fsi, temp, header)
        {
            FileInfo = fsi;
            thumbnail = notLoadedImage;
            dirHeader = header;
            modifiedTime = new DateTime();
        }
        public bool CheckFile()             
        {
            FileInfo fi = new FileInfo(FSPath);
            if (!fi.Exists || fi.Length == 0)
                return false; // no data exists
            if (fi.LastWriteTime <= modifiedTime)
                return true;// existing image not updated
            cynchronized = false;
            return true;
        }
        void SetCynchronized(DateTime dt)   
        {
            modifiedTime = dt;
            cynchronized = true;
            modified = true;
            priority = false;
        }
        Image CreateThumbnail(byte[] ba)    
        {
            Image image = new Bitmap(new MemoryStream(ba));
            IntSize size = ThumbnailSize();
            float scale = Math.Min((float)size.Width / image.Width, (float)size.Height / image.Height);
            int w = (int)(image.Width * scale);
            int h = (int)(image.Height * scale);
            return image.GetThumbnailImage(w, h, new Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
        }
        public Image SynchronizeThumbnail()                
        {
            switch (Type)
            {
                case DataType.GIF:
                case DataType.JPG:
                case DataType.PNG:
                case DataType.Exact:
                case DataType.Regular:
                    try
                    {
                        byte[] ba = DataAccess.ReadFile(FSPath, IsEncrypted);
                        thumbnail = CreateThumbnail(ba);
                        thumbnail.Tag = FSPath;
                        FileInfo fi = new FileInfo(FSPath);   // FileInfo recreated to get new LastWriteTime
                        SetCynchronized(fi.LastWriteTime);
                    }
                    catch    // legal exception - update may fail if file is being modified
                    {
                        thumbnail = failedImage;
                        //Debug.WriteLine("loading thumbnail '"+ FullPath + "' failed");
                    }
                    break;
                case DataType.Movie:
                case DataType.Video:
                    thumbnail = mediaImage;
                    SetCynchronized(DateTime.Now);
                    break;
                case DataType.MLI:
                    byte[] ta = VisualLayerData.LoadThumbnail(FSPath, IsEncrypted);
                    thumbnail = ta==null ? multiLayerImage : CreateThumbnail(ta);
                    SetCynchronized(DateTime.Now);
                    break;
                case DataType.Dir:
                    thumbnail = dirImage;
                    SetCynchronized(DateTime.Now);
                    break;
                case DataType.LocalImages:
                    thumbnail = localImage;
                    SetCynchronized(DateTime.Now);
                    break;
            }
            return thumbnail;
        }
        public bool ThumbnailCallback()     { return false; }
        public Image GetThumbnail()         // called by the clent if modified=true
        {
            if(!cynchronized)
                priority=true;
            modified = false;
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
                FSName = MangleText(RealName);
                newName = FSName + ext;
                string newFullPath;
                //newFullPath = Path.Combine(info.Directory.FullName, newName);
                //info.MoveTo(newFullPath);
                newFullPath = Path.Combine(FileInfo.Directory.FullName, newName);
                FileInfo.MoveTo(newFullPath);
                modified = true;
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
        public class Collection             
        {
            static ImageFileInfo emptyImage = new ImageFileInfo(DataType.Unknown, null, false, false);
            static int synchronizationDelay = 300; // synchronization delay between directory and image collection
            public class RealNameComparer : IComparer<ImageFileInfo>
            {
                IComparer ifhc = new NameComparer();
                int IComparer<ImageFileInfo>.Compare(ImageFileInfo l1, ImageFileInfo l2)
                {
                    return ifhc.Compare(l1.RealName, l2.RealName);
                    //return ifhc.Compare(UnMangleText(l1.RealName), UnMangleText(l2.RealName));
                }
            }
            bool isTemp;                    // true if directory is a temp store of new articles
            //bool isLocked = false;
            DirectoryInfo directory = null; // underlying directory; set up at construction and not changed
            string[] extList = null;        // if specified contains list to display (alternative to directory)
            DateTime lastUpdated;           // last access time of underlying directory
            bool dirMode;                   // true - shows dir info images, false - shows images 
            InfoType infoType;              // type of info if view mode is info 
            ImageFileInfo activeFile;       // current file name
            Dictionary<string, int> indexTable = new Dictionary<string, int>(); // FSPath to index table
            List<ImageFileInfo> imageFileList = new List<ImageFileInfo>();// collection of ImageFile
            BackgroundWorker synchronization; // keeping synchronization between list and directory
            public event VoidNoArg notifyEmptyDir = null;
            public ImageFileInfo added = null;
            int thumbnailUpdateIndex = 0;
            bool abortSynchronization = false;
            public string RealName          { get { return directory == null ? "" : UnMangleText(directory.Name); } }
            public int Count                { get { return imageFileList.Count; } }
            bool IsUpdating                 { get { return synchronization != null && synchronization.IsBusy; } }
            public bool DirMode             { get { return dirMode; } }
            public ImageFileInfo ActiveFile { get { return activeFile; } }
            public string ActiveFileFSPath  { get { return activeFile == null ? "" : activeFile.FSPath; } }
            public bool IsFirst             { get { return ImageFileIndex(ActiveFileFSPath) == 0; } }
            public bool IsLast              { get { return ImageFileIndex(ActiveFileFSPath) == Count - 1; } }
            public ImageFileInfo Last       { get { return Count>0 ? imageFileList[Count - 1] : null; } }
            //public bool IsSubDirs           { get { return subDirList != null; } }
            public bool IsAdded             { get { return added != null; } }
            public ImageFileInfo Added { get { ImageFileInfo ret = added; added = null; return ret; } }
            public bool ValidDirectory { get { return directory!=null && directory.Exists; } }
            public ImageFileInfo this[int i]
            {
                get
                {
                    if (i < 0 && i >= Count)
                        return emptyImage;
                    try { return imageFileList[i]; }
                    catch { return emptyImage; }
                }
            }
            public Collection(ImageDirInfo dir, InfoType it, bool temp) { Initialize(dir, true, it, null, temp); }
            public Collection(ImageDirInfo dir, bool temp) { Initialize(dir, false, InfoType.Detail, null, temp); }
            public Collection(ImageDirInfo dir, string[] extList) { Initialize(dir, true, InfoType.Detail, extList, false); }
            void Initialize(ImageDirInfo dir, bool dirm, InfoType it, string[] list, bool tempStore_)
            {
                directory = dir.DirInfo;
                if (!ValidDirectory)
                    throw new Exception("Directory '"+dir.FSPath+"' does not exists");
                dirMode = dirm;
                infoType = it;
                extList = list;
                isTemp = tempStore_;
                activeFile = null;
                synchronization = new BackgroundWorker();
                synchronization.DoWork += Synchronization_DoWork;
                synchronization.RunWorkerCompleted += Synchronization_RunWorkerCompleted;
                AppendFiles();
                if(extList == null)
                    Sort(new RealNameComparer());
                SynchronizeDirectory();
            }
            ~Collection()
            {
                Clear();
                if (synchronization != null)
                    synchronization.Dispose();
            }                
            public void Clear()
            {
                abortSynchronization = true;
                notifyEmptyDir = null;
                directory = null;
                extList = null;
                added = null;
                imageFileList.Clear();
                indexTable.Clear();
            }
            public void Sort(IComparer<ImageFileInfo> c) 
            {
                lock (this)
                {
                    imageFileList.Sort(c);
                    RebuildIndexTable();
                }
            }
            public int ImageFileIndex(string fileName) { int ind; if(indexTable.TryGetValue(fileName, out ind)) return ind; return -1; }
            public string MoveFiles(ImageFileInfo[] filesToMove, DirectoryInfo toDirectory)
            {
                if (toDirectory != null && !Directory.Exists(toDirectory.FullName))
                    return "Destination directory '" + toDirectory + "' does not exist";
                //while (isLocked)
                //    Thread.Sleep(300);
                lock (this)
                {
                    //isLocked = true;
                    string warnings = "";
                    bool delete = toDirectory == null;
                    bool navigate = true;
                    if (filesToMove == null)
                    {
                        filesToMove = imageFileList.ToArray();
                        navigate = false;
                    }
                    foreach (ImageFileInfo ifi in filesToMove)
                    {
                        if (ifi == null || ifi.dirHeader)
                            continue;
                        try
                        {
                            if (activeFile != null && navigate && ifi.FSPath == activeFile.FSPath)
                                NavigateTo(true);
                            if (delete)
                                warnings += Delete(ifi.FSPath);
                            else if (ifi.IsEncrypted)
                            {
                                string dest = Path.Combine(toDirectory.FullName, FSMangle(Path.GetFileName(ifi.FSPath)));
                                File.Move(ifi.FSPath, dest);
                            }
                            else if (DataAccess.PrivateAccessAllowed && (ifi.IsUnencryptedImage || ifi.IsUnencryptedVideo))
                            {   // when PrivateAccessAllowed move images with encription and name mangling
                                string name = ifi.IsUnencryptedVideo ? ifi.FSName + ".vid" : ifi.IsExact ? ifi.FSName + ".exa" : ifi.FSName + ".jpe";
                                byte[] src = File.ReadAllBytes(ifi.FSPath);
                                if (!DataAccess.WriteFile(Path.Combine(toDirectory.FullName, FSMangle(name)), src, true))
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
                    //isLocked = false;
                    return warnings;
                }
            }
            public void Rename(ImageFileInfo ifi, string newName)
            {
                //while (isLocked)
                //    Thread.Sleep(300);
                lock (this)
                {
                    //isLocked = true;
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
                    //isLocked = false;
                }
            }
            int NextInd(bool up)
            {
                int ind;
                if (activeFile == null)
                    ind = 0;
                else
                {
                    ind = Math.Max(0, ImageFileIndex(activeFile.FSPath));
                    ind = up ? ind + 1 : ind - 1;
                    if (ind < 0)
                        ind = Count - 1;
                    else if (ind >= Count)
                        ind = 0;
                }
                return ind;
            }
            public string NavigateTo(bool up)
            {
                int ind = NextInd(up);
                activeFile = ind >=0 ? this[ind] : null; 
                return ActiveFileFSPath;
            }
            public string NavigateToGroup(bool up)
            {
                string n = activeFile == null ? "" : activeFile.RealName;
                int ind = NextInd(up);
                if (ind < 0)
                    return "";
                int i = 0;
                string nn = this[ind].RealName;
                int len = Math.Min(n.Length, nn.Length);
                for (; i<len; i++)
                {
                    if (!char.IsLetterOrDigit(n[i]) || n[i] != nn[i])
                        break;
                }
                if (i == 0)
                {
                    activeFile = this[ind];
                    return ActiveFileFSPath;
                }
                while (len - i < 3 && char.IsDigit(n[i]) && i > 1)
                    i--;
                string common = n.Substring(0, i);
                if (up)
                {
                    for (i = ind; i < Count; i++)
                        if (!this[i].RealName.StartsWith(common))
                            break;
                }
                else
                {
                    for (i = ind; i >= 0; i--)
                        if (!this[i].RealName.StartsWith(common))
                            break;
                }
                if (ind < 0)
                    ind = Count - 1;
                else if (ind >= Count)
                    ind = 0;
                activeFile = this[i];
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
                    RebuildIndexTable();
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
                    synchronization.RunWorkerAsync();
                }
                catch { }
            }
            private void Synchronization_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
            {
                try
                {
                if(!dirMode && imageFileList.Count == 0)
                    notifyEmptyDir?.Invoke();
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
                //while (isLocked)
                //    Thread.Sleep(300);
                if (directory != null && directory.Exists)
                    directory = new DirectoryInfo(directory.FullName);  // updates info
                if (directory == null || !directory.Exists)
                    return false;
                lock (this)
                {
                    //isLocked = true;
                    //DateTime lat = directory.LastAccessTime;
                    DateTime lat = directory.LastWriteTime;
                    if (lastUpdated < lat)
                    {   // rebuild collection if directory content changed
                        //Debug.WriteLine("updating directory "+ directory.Name + (dirMode ? " subDirs" : " images"));
                        AppendFiles();           // appending new files to the list
                        List<string> deletedFiles = new List<string>();
                        foreach (ImageFileInfo d in imageFileList)
                            if (!d.CheckFile())
                                deletedFiles.Add(d.FSPath);            
                        if (deletedFiles.Count > 0)
                        {   // removing deleted files from the list
                            int[] indexes = new int[deletedFiles.Count];
                            for (int i = 0; i < indexes.Length; i++)
                                indexes[i] = ImageFileIndex(deletedFiles[i]);
                            Array.Sort(indexes);
                            for (int i = indexes.Length - 1; i >= 0; i--)
                                if(indexes[i]>=0)
                                    imageFileList.RemoveAt(indexes[i]);
                            //Debug.WriteLine(imageFileList.Count.ToString() + (dirMode ? " subDirs" : " images") + " left");
                            RebuildIndexTable();
                            if (imageFileList.Count == 0)
                            {
                                abortSynchronization = true;
                                //isLocked = false;
                                return false;
                            }
                        }
                        lastUpdated = lat;
                    }
                    int loaded = 0;
                    foreach (ImageFileInfo d in imageFileList)
                    {
                        if (abortSynchronization)
                            break;
                        if (!d.priority)
                            continue;
                        d.SynchronizeThumbnail();
                        loaded++;
                        //Debug.WriteLine("priority " + d.Name + " updated");
                    }
                    if (loaded == 0)
                        UpdateHiddenThumbnails(3);
                    //isLocked = false;
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
                    ImageFileInfo d = imageFileList[thumbnailUpdateIndex];
                    if (!d.cynchronized && !d.priority)
                    {
                        //Debug.WriteLine("hidden " + d.Name + " updated " + max);
                        if (abortSynchronization)
                            break;
                        d.SynchronizeThumbnail();
                        if(max-- <0)
                            return;
                    }
                    thumbnailUpdateIndex++;
                }
            }
            void AppendFiles()              
            {
                if (dirMode)
                {
                    DirectoryInfo[] directories;
                    if (extList != null)
                    {
                        directories = new DirectoryInfo[extList.Length];
                        for (int i = 0; i < extList.Length; i++)
                        {
                            string dirFullName = Path.Combine(directory.FullName, extList[i]);
                            if (Directory.Exists(dirFullName))   // unmangled name
                                directories[i] = new DirectoryInfo(dirFullName);
                            else
                            {
                                dirFullName = Path.Combine(directory.FullName, FSMangle(extList[i]));
                                directories[i] = Directory.Exists(dirFullName) ? new DirectoryInfo(dirFullName) : null;
                            }
                        }
                    }
                    else
                        directories = directory.GetDirectories();
                    FileInfo fsf = GetFirstImageName(directory.GetFiles(InfoFileName(infoType) + '*'));
                    if (fsf == null)
                        fsf = GetFirstImageName(directory.GetFiles());
                    if (fsf != null)
                        AppendImageFile(DataType.LocalImages, fsf, isTemp, true);
                    foreach (DirectoryInfo di in directories)
                    {
                        if (di == null)
                            continue;
                        try
                        {
                            string pattern = IsMangled(di.Name) ? FSMangle(InfoFileName(infoType)) : InfoFileName(infoType);
                            fsf = GetFirstImageName(di.GetFiles(pattern + '*'));
                            if (fsf == null)
                                fsf = GetFirstImageName(di.GetFiles());
                            if (fsf != null)
                                AppendImageFile(FileType(fsf.Name), fsf, isTemp, true);
                            else if (Directory.GetDirectories(di.FullName).Length > 0)
                                AppendImageFile(DataType.Dir, new FileInfo(di.FullName), isTemp, true);
                        }
                        finally { }
                    }
                }
                else
                {
                    FileInfo[] files = directory.GetFiles();
                    foreach (var fn in files)
                        AppendImageFile(FileType(fn.Name), fn, isTemp, false);
                }
                lastUpdated = directory.LastAccessTime;
            }
            void AppendImageFile(DataType dt, FileInfo fn, bool temp, bool header)
            {
                if (indexTable.ContainsKey(fn.FullName) || dt == DataType.Unknown)
                    return;
                //while (isLocked)
                //    Thread.Sleep(300);
                lock (this)
                {
                    //isLocked = true;
                    added = new ImageFileInfo(dt, fn, temp, header);
                    imageFileList.Add(added);
                    indexTable.Add(fn.FullName, imageFileList.Count - 1);
                    //isLocked = false;
                }
            }
            void RebuildIndexTable()        
            {
                indexTable.Clear();
                int i=0;
                try
                {
                    for (; i < Count; i++)     // complete rebuild of index list
                        indexTable.Add(this[i].FSPath, i);
                }
                catch
                {
                    string s = this[i].FSPath;
                }
            }
        }
    }
}
