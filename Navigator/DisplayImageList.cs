using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.ComponentModel;
using static ImageProcessor.ImageFileInfo;

namespace ImageProcessor
{
    public class ImageGroup
    {
        public string Name { get; private set; }
        public int First { get; private set; }
        public int Last { get; private set; }
        public bool Expanded { get; set; }
        public ImageGroup(string fileName, int ind)
        {
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
                Name = fileName.Substring(0, len - digits);
            }
            Expanded = false;
            return;
        }
        public void SetLast(int li) { if (Last == 0) Last = li; }
        public override string ToString() => Name + " [" + First + '-' + Last + ']';
        internal bool NameMatches(ImageFileInfo ifi)
        {
            var fileName = ifi.RealName;
            if (!fileName.StartsWith(Name))     // has starting with Name
                return false;
            var len = fileName.Length;
            if (len != Name.Length)             // allowed no separator or digit after Name
            {
                int ds = Name.Length;
                if (len - ds > 3)               // not more than 3 digits
                    return false;
                for (int i = ds; i < len; i++)  // only digits allowed at the end
                    if (!char.IsDigit(fileName[i]))
                        return false;
            }
            return true;
        }
        internal bool Contains(int ind) => ind >= First && ind <= Last;
    }
    public class DisplayImageList
    {   // sortable list of 'ImageFileInfo' accessible by key, index or Group of similar
        public class RealNameComparer : IComparer<ImageFileInfo>
        {
            IComparer<string> ifhc = new NameComparer();
            int IComparer<ImageFileInfo>.Compare(ImageFileInfo l1, ImageFileInfo l2)
            {
                if (l1.IsDirInfo && !l2.IsDirInfo)
                    return -1;
                if (!l1.IsDirInfo && l2.IsDirInfo)
                    return 1;
                if (l1.IsInfoImage && !l2.IsInfoImage)
                    return -1;
                if (!l1.IsInfoImage && l2.IsInfoImage)
                    return 1;
                return ifhc.Compare(l1.RealName, l2.RealName);
            }
        }
        public const int updateListDelay = 300; // synchronization delay [ms] between directory and image collection
        const double mandatoryUpdate = 0.4;     // time between mandatory updates [s] 
        public string DeletedFile { get; set; } = null; // known deletion from ditectory or from srcList
        public bool GroupView { get; set; }
        public bool PreferedGroupView { get; set; } = false;
        DirectoryInfo directory = null; // source directory (search path of srcList or image and subDir source)
        public DirectoryInfo DirInfo { get { return directory; } } // source directory (search path of srcList or image and subDir source)
        string[] srcList = null;        // subDirs of the directory matching search criteria
        bool dirFiles;
        DateTime lastUpdated = DateTime.Now; // last updated display images
        DirShowMode viewInfoType;       // type of info if view mode is info     
        Dictionary<string, int> indexTable = new Dictionary<string, int>(); // stores system file name and fileList index pairs
        List<ImageFileInfo> fileList = new List<ImageFileInfo>();   // holds both local image files and subdir header files
        List<ImageGroup> groupList = new List<ImageGroup>();        // header file names of image groups (same StartsWith). Applies only to directory source
        List<ImageFileInfo> displayed = new List<ImageFileInfo>();  // image list displayed by list view window
        public int DisplayedCount => displayed.Count;
        public int GroupCount => groupList.Count;
        public int ImageCount => fileList.Count;
        int prevImageCount = 0;         // image count in directory before update
        bool isFirst = true;            // indicates that list is created first time at window opening
        bool isDownloadedDir;
        BackgroundWorker fileSyncWorker;// keeping synchronization between list and directory
        public event VoidNoArg notifyEmptyDir = null;
        int thumbnailUpdateIndex = 0;
        bool abortSynchronization = false;
        public string DirRealName => directory == null ? "" : Scramble.UnMangle(directory.Name);
        bool IsUpdating => fileSyncWorker != null && fileSyncWorker.IsBusy;
        public ImageFileInfo ActiveFile { get; private set; } // current file name
        public string ActiveFileFSPath => ActiveFile == null ? "" : ActiveFile.FSPath;
        ImageFileInfo lastAdded;        // file added to a directory
        public ImageFileInfo LastAdded { get { bool show = ImageCount > prevImageCount; prevImageCount = ImageCount; return show ? lastAdded : null; } }
        public bool ValidDirectory => directory != null && directory.Exists;
        public ImageFileInfo this[int i]
        {
            get
            {
                if (i < 0 && i >= DisplayedCount)
                    return null;
                try { return displayed[i]; }
                catch { return null; }
            }
        }
        public DisplayImageList(DirectoryInfo dir, bool listOnly) { Initialize(dir, listOnly, null); }
        public DisplayImageList(DirectoryInfo dir, string[] list) { Initialize(dir, true, list); }
        void Initialize(DirectoryInfo dir, bool listOnly, string[] list)
        {
            directory = dir;
            if (!ValidDirectory)
                throw new Exception("Directory '" + dir.FullName + "' does not exists");
            isDownloadedDir = Navigator.IsSpecDir(directory, SpecName.Downloaded);
            viewInfoType = DirShowMode.Detail;
            srcList = list;
            dirFiles = srcList == null;
            ActiveFile = null;
            GroupView = !listOnly && !isDownloadedDir;
            UpdateImageList();
            if (GroupView)
            {
                int fc = ImageCount;
                int gc = GroupCount;
                PreferedGroupView = gc > 1 && fc > 200 || gc > 3 && fc > 100 || gc > 10 && gc < fc / 3;
            }
            fileSyncWorker = new BackgroundWorker();
            fileSyncWorker.DoWork += Synchronization_DoWork;
            fileSyncWorker.RunWorkerCompleted += Synchronization_RunWorkerCompleted;
            fileSyncWorker.RunWorkerAsync();
        }
        ~DisplayImageList()
        {
            Clear();
            if (fileSyncWorker != null)
                fileSyncWorker.Dispose();
        }
        public void SetInfoType(DirShowMode it) { viewInfoType = it; }
        public void Clear()
        {
            abortSynchronization = true;
            directory = null;
            srcList = null;
            fileList.Clear();
            prevImageCount = 0;
            indexTable.Clear();
        }
        #region fileList maintenance and thumbnail sinchronization
        void Synchronization_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) => notifyEmptyDir?.Invoke();
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
                Thread.Sleep(updateListDelay);
            }
        }
        public bool UpdateImageList()
        {
            try
            {
                if (!ValidDirectory)
                    return false;
                //prevImageCount = ImageCount;
                isFirst = ImageCount == 0;
                lock (this)
                {
                    bool listChanged = false;
                    if (dirFiles && (DeletedFile != null || lastUpdated < Directory.GetLastWriteTime(directory.FullName)))//&& directory != null 
                    {   // rebuild collection if directory content changed
                        List<string> deletedFiles = new List<string>(); // names of deleted, moved, or renamed files
                        foreach (ImageFileInfo d in fileList)
                            if (d != null && !d.CheckExistsSetUpdate())
                                deletedFiles.Add(d.Name);
                        if(DeletedFile != null)
                            deletedFiles.Add(DeletedFile);
                        if (deletedFiles.Count > 0)
                        {
                            int[] indexes = new int[deletedFiles.Count]; // indexes of deleted, moved, or renamed files
                            for (int i = 0; i < indexes.Length; i++)
                                indexes[i] = FileListIndex(deletedFiles[i]);
                            Array.Sort(indexes);
                            for (int i = indexes.Length - 1; i >= 0; i--)  // removing deleted files from the list
                                if (indexes[i] >= 0)
                                    fileList.RemoveAt(indexes[i]);
                        }
                        listChanged = true;
                    }
                    else if (!dirFiles && DeletedFile != null) 
                    {   // remove deleted file remove dir notification
                        List < ImageFileInfo > newlist= new List< ImageFileInfo >();
                        for (int i = 0; fileList.Count > 0; i++)
                        {
                            if (fileList[i].FSName != DeletedFile)
                                newlist.Add(fileList[i]);
                            else
                                listChanged = true;
                        }
                        if (listChanged)
                            fileList = newlist;
                    }
                    if (listChanged || isFirst)
                    {
                        AppendNewFiles();    // appending new files to the list
                        if (isFirst)         // do not show last when list form opened
                            lastAdded = null;
                        RebuildIndexesAndGroups(!isDownloadedDir);
                    }
                    if (listChanged || lastUpdated < DateTime.Now.AddSeconds(-mandatoryUpdate))
                    {
                        if (ImageCount == 0 && !isFirst)
                        {
                            notifyEmptyDir?.Invoke(); // sends empty dir notification
                            return false;
                        }
                        RebuildDisplayedList();
                        lastUpdated = DateTime.Now;
                    }
                    int loaded = 0;
                    DeletedFile = null;
                    foreach (ImageFileInfo ifi in displayed)
                    {
                        if (abortSynchronization)
                            break;
                        if (!ifi.priority || (ifi.IsEncrypted && !DataAccess.Private))
                            continue;
                        ifi.UpdateThumbnail();
                        loaded++;
                        //Debug.WriteLine("priority " + d.Name + " updated");
                    }
                    if (loaded == 0)
                        UpdateHiddenThumbnails(8);
                    return !abortSynchronization;
                }
            }
            catch (Exception)
            {
                return false;
            }
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
                if (ifi.NeedThumbnail && (!ifi.IsEncrypted || DataAccess.Private))
                {
                    //Debug.WriteLine("hidden " + d.Name + " updated " + max);
                    if (abortSynchronization)
                        break;
                    ifi.UpdateThumbnail();
                    if (max-- < 0)
                        return;
                }
                thumbnailUpdateIndex++;
            }
        }
        void AppendNewFiles()
        {
            DirectoryInfo[] directories;
            if (dirFiles) // so far only directories passes => all entries treated as directories
                directories = directory.GetDirectories();
            else
            {
                directories = new DirectoryInfo[srcList.Length];
                for (int i = 0; i < srcList.Length; i++)
                {
                    string dirFullName = Path.Combine(directory.FullName, srcList[i]);
                    directories[i] = Directory.Exists(dirFullName) ? new DirectoryInfo(dirFullName) : null;
                    if (DataAccess.Private && directories[i] == null)
                    {
                        dirFullName = Path.Combine(directory.FullName, Scramble.MangleFile(srcList[i]));
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
                        var ifi = new ImageFileInfo(fsf, true);
                        if (isDownloadedDir && !isFirst)
                            AddNewImageFileToFront(ifi, true);
                        else
                            AppendNewImageFile(ifi, true);
                    }
                }
                finally { }
            }
            FileInfo[] files;
            if (dirFiles)
                files = directory.GetFiles();
            else
                files = new FileInfo[0]; // all entries treated as directories
            foreach (var fi in files)
            {
                var ifi = new ImageFileInfo(fi);
                if (isDownloadedDir && !isFirst)
                    AddNewImageFileToFront(ifi);
                else
                    AppendNewImageFile(ifi);
            }
        }
        void AddNewImageFileToFront(ImageFileInfo ifi, bool header = false)
        {   // insert new item to the front of list and indexTable
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
        void AppendNewImageFile(ImageFileInfo ifi, bool header = false)
        {   // append new item to the list and indexTable
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
        void RebuildIndexesAndGroups(bool withSort = true)
        {   // applied when fileList changed
            int i = 0;
            try
            {
                lock (this)
                {
                    indexTable.Clear();
                    var newGL = new List<ImageGroup>();
                    if (withSort)
                        fileList.Sort(new RealNameComparer());
                    for (; i < ImageCount; i++) // complete rebuild of index list
                        indexTable.Add(fileList[i].Name, i);
                    if (GroupView)
                    {
                        for (i = 0; i < ImageCount; i++)
                        {
                            var ifi = fileList[i];
                            int n = newGL.Count;
                            if (ifi.IsDirInfo || ifi.IsInfoImage)
                            {
                                if (n > 0)
                                    newGL[n - 1].SetLast(i - 1);
                                continue;
                            }
                            if (n == 0 || !newGL[n - 1].NameMatches(ifi))
                            {
                                newGL.Add(new ImageGroup(fileList[i].RealName, i));
                                ifi.Group = newGL[newGL.Count - 1];
                                if (n > 0)
                                    newGL[n - 1].SetLast(i - 1);
                            }
                        }
                        if (newGL.Count > 0)
                            newGL[newGL.Count - 1].SetLast(i - 1);
                        if (newGL.Count == GroupCount)
                            for (i = 0; i < newGL.Count; i++)
                                foreach (var ig in groupList)
                                {
                                    if (newGL[i].Name == ig.Name)
                                    {
                                        newGL[i].Expanded = ig.Expanded;
                                        break;
                                    }
                                }
                    }
                    groupList = newGL;
                }
            }
            catch(Exception ex)
            {
                string s = fileList[i].FSPath + ex.Message;
            }
            //Debug.WriteLine(" *RebuildIndexesAndGroups list=" + ImageCount + " groups=" + GroupCount);
            //foreach (var gr in groupList)
            //    Debug.WriteLine(gr.ToString());
        }
        void RebuildDisplayedList()
        {
            lock (this)
            {   // complete rebuild of index list
                displayed.Clear();
                if (!GroupView || GroupCount < 2)
                {
                    for (int i = 0; i < ImageCount; i++)
                    {
                        var ifi = fileList[i];
                        ifi.DisplayListIndex = i;
                        displayed.Add(ifi);
                    }
                    //Debug.WriteLine("###*RebuildDisplayedList: all count=" + displayed.Count);
                }
                else
                {
                    int gInd = -1;
                    int ind = 0;
                    for (int i = 0; i < ImageCount; i++)
                    {
                        var ifi = fileList[i];
                        if (ifi.IsDirInfo || ifi.IsInfoImage)
                        {
                            ifi.DisplayListIndex = ind++;
                            displayed.Add(ifi);
                            continue;
                        }
                        if (gInd < 0 || !groupList[gInd].NameMatches(ifi))
                        {
                            gInd++;
                            Debug.Assert(gInd < GroupCount, "group index " + gInd + " exceeds GroupCount " + GroupCount);
                            ifi.DisplayListIndex = ind++; // always show first
                            displayed.Add(ifi);
                            continue;
                        }
                        if (groupList[gInd].NameMatches(ifi) && groupList[gInd].Expanded)
                        {
                            ifi.DisplayListIndex = ind++;
                            displayed.Add(ifi);
                        }
                    }
                    Debug.Assert(displayed.Count == ind);
                }
            }
        }
        #endregion
        public void SortFileListByRealName() { RebuildIndexesAndGroups(); }
        public int FileListIndex(string name) { if (indexTable.TryGetValue(name, out int ind)) return ind; return -1; }
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
                    if (ifi == null || ifi.IsDirInfo)
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
                            string dest = Path.Combine(toDirectory.FullName, Scramble.MangleFile(Path.GetFileName(ifi.FSPath)));
                            File.Move(ifi.FSPath, dest);
                        }
                        else if (DataAccess.Private && !ifi.IsEncrypted)
                        {   // when PrivateAccessAllowed move images with encription and name mangling
                            string name = ifi.IsMovie ? ifi.FSName + ".vid" : ifi.IsExact ? ifi.FSName + ".exa" : ifi.FSName + ".jpe";
                            byte[] src = File.ReadAllBytes(ifi.FSPath);
                            var warn = DataAccess.CreateFile(Path.Combine(toDirectory.FullName, Scramble.MangleFile(name)), src, true);
                            if (warn.Length != 0)
                                warnings += ifi.FSName + ": " + warn + Environment.NewLine;
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
            //Thread.Sleep(synchronizationDelay);
            return warnings;
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
                if (dest == Direction.Next || dest == Direction.Prev)
                    return ind < 0 ? displayed.Count - 1 : ind >= displayed.Count ? 0 : ind; ;
                for (int i = ind; i < displayed.Count && i >= 0; i += del)
                    if (displayed[i].IsGroupHead)
                        return i;
                return dest == Direction.NextGroup ? displayed.Count - 1 : 0;
            }
            return -1;
        }
        FileInfo GetInfoFile(DirectoryInfo di)
        {
            bool useMangled = DataAccess.Private;
            string nInfo = ImageFileName.InfoFileName(viewInfoType);
            string nMangled = Scramble.MangleFile(nInfo);
            FileInfo info = null;
            FileInfo img = null;
            foreach (var f in di.GetFiles())
            {
                if (useMangled && f.Name.StartsWith(nMangled))
                    return f;
                if (info == null && f.Name.StartsWith(nInfo))
                {
                    if (!useMangled)
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
