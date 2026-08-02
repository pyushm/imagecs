using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static ImageProcessor.Navigator;

namespace ImageProcessor
{
    public enum SpecName
    {
        Downloaded,
        NewArticles,
        AllDevicy,
        Work,
        Root       // parent - has to be last
    }
    public interface INavigator
    {
        string RootName { get; }
        void SetActiveDir(DirectoryInfo di);
        void SetActiveImageName(string n);
        void RunVideoFile(ImageFileInfo videoFile);
    }
    public class Navigator : INavigator
	{
        int searchLevel;
        static int MatchRange = 100;  // maximum match difference percentage included into search results
        static ImageHash.Comparer imageInfoComparer = new ImageHash.Comparer(56);
        static DirectoryInfo[] specialDirectories = null;
        public delegate void NewDirectoryNode(DirectoryInfo fi, string relativePath);
        public delegate void NewDirSelecton(DirectoryInfo fi);
        public delegate void NewImageSelection(string image);
        public static DirectoryInfo SpecDir(SpecName sd) { return specialDirectories == null ? null : specialDirectories[(int)sd]; }
        public static DirectoryInfo Root        { get { return specialDirectories == null ? null : specialDirectories[(int)SpecName.Root]; } }
        public static bool IsSpecDir(DirectoryInfo testdi)
        {
            if (specialDirectories != null)
                foreach (DirectoryInfo di in specialDirectories)
                    if (di.FullName == testdi.FullName)
                        return true;
            return false;
        }
        public static bool IsSpecDir(DirectoryInfo testdi, SpecName sd) { return specialDirectories == null ? false : specialDirectories[(int)sd].FullName == testdi.FullName; }
        string[] textPatterns;   // 1 item - search for pattern in string; >1 - serch exact string for each item (1 extra char at the end allowed)
        SoundLike soundPattern;
        int viewedDayAgo = int.MaxValue;
        int changedDayAgo = int.MaxValue;
        SearchResult searchResult;
        List<DirDifference> dirDifferences = new List<DirDifference>();
        bool stopSearch = false;
        public enum SearchMode
        {
            Names,
            Sound,
            File,
            //Image
        }
        public enum FileCompare
        {
            Same,
            Newer,
            Older,
            SourceOnly,
            CompareOnly
        }
        SearchMode searchMode;
        public NewDirectoryNode ProcessDirecory;
        public NewImageSelection onNewImageSelection = null;
        public NewDirSelecton onNewDirSelection = null;
        NewDirectoryNode RunSearchInDirecory = null;
        string activeImageName = null;
        const string MediaExe = "C:/Program Files (x86)/VideoLAN/VLC/vlc.exe";
        public string RootName { get; private set; } = "../stuff/";
        public void RunVideoFile(ImageFileInfo videoFile)
        {
            string tempVideoFile = "_._";
            if (videoFile.IsEncrypted)
                DataAccess.DecryptToTemp(videoFile.FSPath, tempVideoFile);
            string name = videoFile.IsEncrypted ? tempVideoFile : '\"' + videoFile.FSPath + '\"';
            Process p = Process.Start(MediaExe, name);
            p.WaitForExit();
            p.Dispose();
            if (videoFile.IsEncrypted)
                File.Delete(tempVideoFile);
        }
        public void SetActiveImageName(string n) { activeImageName = n; onNewImageSelection?.Invoke(activeImageName); }
        public void SetActiveDir(DirectoryInfo di) { activeImageName = new ImageDirInfo(di).RealPath; onNewDirSelection?.Invoke(di); }
        public bool StopSearch          { get { return stopSearch; } set { stopSearch = value; } }
        public string[] GetMatchedDirNames()
        {
            List<string> ret = new List<string>();
            foreach (var dir in searchResult.GetMatchedDirs())
                ret.Add(dir.Name);
            return ret.ToArray<string>();
        }
        public Navigator()
        {
            //Common.XMLStore settings = new Common.XMLStore(Path.Combine(Directory.GetCurrentDirectory(), "Customization.xml"));
            //RootName = settings.GetString("location.root", "../stuff/");
            //MediaExe = settings.GetString("path.media");
            searchResult = new SearchResult();
            string[] dirNames = Enum.GetNames(typeof(SpecName));
            specialDirectories = new DirectoryInfo[dirNames.Length];
            for (int i = 0; i < dirNames.Length; i++)
            {
                string dirName;
                if (dirNames[i] == "Root")
                    dirName = RootName;
                else
                    dirName = Path.Combine(RootName, dirNames[i]);
                specialDirectories[i] = new DirectoryInfo(dirName);
                if (!specialDirectories[i].Exists)
                    throw new Exception("Special directory " + specialDirectories[i].FullName + " does not exist.");
            }
        }
		public DirectoryInfo[] GetDirectories(DirectoryInfo di)
		{
            try
            {
                if (di == null || !di.Exists)
                    return new DirectoryInfo[0];
                if(!IsSpecDir(di, SpecName.Root))
                    return di.GetDirectories();
                List<DirectoryInfo> rootList = new List<DirectoryInfo>();
                foreach (var sd in specialDirectories)
                    if (!IsSpecDir(sd, SpecName.Root))
                        rootList.Add(sd);
                return rootList.ToArray();
            }
            catch
            {
                return new DirectoryInfo[0];
            }
		}
        public DirectoryInfo GetSearchRoot(string name)
        {
            if (name == null || name.Length == 0 || !Directory.Exists(name))
                return Root;
            DirectoryInfo di = new DirectoryInfo(name);
            if (IsSpecDir(di.Parent, SpecName.AllDevicy) || IsSpecDir(di))
                return di;
            return Root;
        }
        public SearchResult GenerateSearchList(SearchMode mode, DirectoryInfo start, string name, string daysOld, bool viewed)
        {
            searchMode = mode;
            soundPattern = mode == SearchMode.Sound ? new SoundLike(name) : null;
            if (mode == SearchMode.File || mode == SearchMode.Names)
            {
                if (name == null)
                    textPatterns = null;
                else
                {
                    string textPattern = name.ToLower();
                    var tps = textPattern?.Split(new char[] { ',', ' ', '.', '-' }, StringSplitOptions.RemoveEmptyEntries);
                    List<string> tpsl = new List<string>();
                    foreach (string tp in tps)
                        if (tp.Length > 1)
                            tpsl.Add(tp);
                    textPatterns = tpsl.ToArray();
                }
            }
            //if (mode == SearchMode.Image)
            //{
            //    if (activeImageName == null)
            //        return null;
            //    imageInfoComparer.SetPattern(activeImageName);
            //    //Debug.WriteLine("************************** image pattern " + activeImageName + "***************************");
            //    //Debug.WriteLine(imageInfoComparer.Pattern.ToString());
            //    //Debug.Write(imageInfoComparer.Pattern.ToBWMString());
            //}
            int days = int.MaxValue;
            try { days = int.Parse(daysOld); }
            catch { days = int.MaxValue; }
            if(viewed)
                viewedDayAgo = days;
            else
                changedDayAgo = days;
            NewDirectoryNode callback = mode == SearchMode.File ? MatchFileName :
                //mode == SearchMode.Image ? MatchImage :
                mode is SearchMode.Names or SearchMode.Sound ? MatchDirectory : (NewDirectoryNode)null;
            if (callback == null)
                return null;
            //Debug.WriteLine("###DaysOld=" + searchDaysOld+" patterns="+ textPatterns==null ? 0 : textPatterns.Length);
            StopSearch = false;
            searchResult.Clear();
            RunSearchInDirecory = callback;
            searchLevel = 0;
            try { SearchRecursively(start, ""); }
            finally { RunSearchInDirecory = null; }
            return searchResult;
            //return Search(start, callback);
        }
        public void SearchRecursively(DirectoryInfo dirNode, string relativePath)
        {
            if (StopSearch)
                return;
            relativePath = Scramble.UnMangleFile(relativePath);
            RunSearchInDirecory?.Invoke(dirNode, relativePath);
            DirectoryInfo[] subdirs = dirNode.GetDirectories();
            //Debug.WriteLine(searchLevel.ToString() + '\t' + Scramble.UnMangle(dirNode.Name.ToLower()) + '\t' + relativePath);
            searchLevel++;
            foreach (DirectoryInfo subdir in subdirs)
            {
                string mn = Scramble.UnMangle(subdir.Name);
                string newRelativePath = Path.Combine(relativePath, mn);
                SearchRecursively(subdir, newRelativePath);
            }
            searchLevel--;
        }
        public List<DirDifference> CompareDirectoryTree(DirectoryInfo d1, DirectoryInfo d2)
        {
            dirDifferences.Clear();
            if (d1 != null && d2 != null)
                CompareRecursively(d1, d2);
            return dirDifferences;
        }
        public void ApplyRecursively(DirectoryInfo dirNode, string relativePath)
        {
            ProcessDirecory?.Invoke(dirNode, relativePath);
            DirectoryInfo[] subdirs = dirNode.GetDirectories();
            foreach (DirectoryInfo subdir in subdirs)
            {
                string newRelativePath = Path.Combine(relativePath, subdir.Name);
                ApplyRecursively(subdir, newRelativePath);
            }
        }
        //public void CreateImageHashes(DirectoryInfo dirNode)
        //{
        //    if (!IsSpecDir(dirNode) && !IsSpecDir(dirNode.Parent))
        //    {
        //        ImageDirHash dii = new ImageDirHash(dirNode);
        //        dii.Update();
        //    }
        //    DirectoryInfo[] subdirs = dirNode.GetDirectories();
        //    Parallel.ForEach(subdirs, (subdir) =>
        //    { CreateImageHashesRecursively(subdir); });
        //}
        //public void CreateImageHashesRecursively(DirectoryInfo dirNode)
        //{
        //    if (StopSearch)
        //        return;
        //    if (!IsSpecDir(dirNode) && !IsSpecDir(dirNode.Parent))
        //    {
        //        ImageDirHash dii = new ImageDirHash(dirNode);
        //        dii.Update();
        //    }
        //    DirectoryInfo[] subdirs = dirNode.GetDirectories();
        //    foreach (DirectoryInfo subdir in subdirs)
        //        CreateImageHashesRecursively(subdir);
        //}
        #region Private Methods
        DirDifference CompareLists(FileSystemInfo[] l1, FileSystemInfo[] l2, bool subDirs)
        {
            var comparer = StringComparer.OrdinalIgnoreCase; // Ordinal
            DirDifference diff = new DirDifference();
            var sortedL1 = l1.Where(file => file.Extension != ".dat").OrderBy(file => file.Name, comparer);
            var sortedL2 = l2.Where(file => file.Extension != ".dat").OrderBy(file => file.Name, comparer);
            var enl1 = sortedL1.GetEnumerator();
            var enl2 = sortedL2.GetEnumerator();
            bool l1Active = enl1.MoveNext();
            bool l2Active = enl2.MoveNext();
                    //while (l1Active || l2Active)
                    //{ // prints lists side-by-side
                    //    if (l1Active) { Debug.Write(" <1 " + enl1.Current.Name); l1Active = enl1.MoveNext(); }
                    //    if (l2Active) { Debug.Write(" 2> \t" + enl2.Current.Name); l2Active = enl2.MoveNext(); }
                    //    Debug.WriteLine(' ');
                    //}
                    //enl1 = sortedL1.GetEnumerator();
                    //enl2 = sortedL2.GetEnumerator();
                    //l1Active = enl1.MoveNext();
                    //l2Active = enl2.MoveNext();
            while (l1Active || l2Active)
            {
                int res = !l1Active ? 1 : !l2Active ? -1 : string.Compare(enl1.Current.Name, enl2.Current.Name, StringComparison.OrdinalIgnoreCase);// OrdinalIgnoreCase);
                        //if (l1Active) { Debug.Write(" <- " + enl1.Current.Name); }
                        //if (l2Active) { Debug.Write(" -> \t" + enl2.Current.Name); }
                if (res < 0)
                {
                    diff.List(Relation.Only1).Add(enl1.Current.Name);
        //Debug.WriteLine("\t " + res+ " \tOnly1 <- "+enl1.Current.Name);
                    l1Active = enl1.MoveNext();
                    continue;
                }
                else if (res > 0)
                {
                    diff.List(Relation.Only2).Add(enl2.Current.Name);
        //Debug.WriteLine("\t " + res + " \tOnly2 <- "+enl2.Current.Name);
                    l2Active = enl2.MoveNext();
                    continue;
                }
        //else 
        //    Debug.WriteLine("\t " + res);
                if ((subDirs || ((FileInfo)enl1.Current).Length != ((FileInfo)enl2.Current).Length))
                {
                    TimeSpan ts = enl1.Current.LastWriteTime - enl2.Current.LastWriteTime;
                    if (ts > TimeSpan.Zero)
                        diff.List(Relation.Newer1).Add(enl1.Current.Name);
                    else if (ts < TimeSpan.Zero)
                        diff.List(Relation.Newer2).Add(enl1.Current.Name);
                    else
                        diff.List(Relation.DifferentLength).Add(enl1.Current.Name);
                }
                l1Active = enl1.MoveNext();
                l2Active = enl2.MoveNext();
            }
            return diff;
        }
        void CompareRecursively(DirectoryInfo d1, DirectoryInfo d2)
        {
            DirDifference fileDif = null;
            DirDifference dirDif = null;
            string error = null;
            try
            {
                fileDif = CompareLists(d1.GetFiles(), d2.GetFiles(), false);
                dirDif = CompareLists(d1.GetDirectories(), d2.GetDirectories(), true);
            }
            catch(Exception ex)
            {
                error = d1.FullName + "<->" + d2.FullName + ": " + ex.Message;
                Debug.WriteLine(error);
            }
            DirDifference totDif = new DirDifference(d1.FullName, d2.FullName, dirDif, fileDif, error);
            if (!totDif.Identical)
                dirDifferences.Add(totDif);
            if (dirDif == null || dirDif.Identical)
                return;
            foreach (string subdir in dirDif.List(Relation.Newer1))
            {
                DirectoryInfo newSource = new DirectoryInfo(Path.Combine(d1.FullName, subdir));
                DirectoryInfo newCompare = new DirectoryInfo(Path.Combine(d2.FullName, subdir));
                CompareRecursively(newSource, newCompare);
            }
            foreach (string subdir in dirDif.List(Relation.Newer2))
            {
                DirectoryInfo newSource = new DirectoryInfo(Path.Combine(d1.FullName, subdir));
                DirectoryInfo newCompare = new DirectoryInfo(Path.Combine(d2.FullName, subdir));
                CompareRecursively(newSource, newCompare);
            }
        }
        //SearchResult Search(DirectoryInfo start, NewDirectoryNode callback)
        //{
        //    StopSearch = false;
        //    searchResult.Clear();
        //    RunSearchInDirecory = callback;
        //    try { SearchRecursively(start, ""); }
        //    finally { RunSearchInDirecory = null; }
        //    return searchResult;
        //}
        void MatchDirectory(DirectoryInfo dirNode, string relativePath)
        {   // matches directory by name, sound, date
            if (relativePath.Length == 0)
                return;
            double totalDif = 0;
            if (textPatterns != null && textPatterns.Length>0) // by name
            {
                string item = Scramble.UnMangle(dirNode.Name.ToLower());
                string[] fields = item.Split(new char[] { ImageFileName.multiNameChar, ImageFileName.synonymChar });
                double dif = int.MaxValue;
                if (textPatterns.Length == 1)
                {
                    string textPattern = textPatterns[0];
                    foreach (string field in fields)
                    {
                        int ind = field.IndexOf(textPattern);
                        if (ind < 0)
                            continue;
                        double letterDif = 100 / (field.Length + textPattern.Length);
                        dif = Math.Min(dif, letterDif * (ind + Math.Abs(field.Length - textPattern.Length)));
                    }
                    if (dif == int.MaxValue)
                        return;
                    totalDif += dif;
                }
                else
                {
                    if (dirNode.Name.Contains('+'))
                        return;
                    foreach(string textPattern in textPatterns)
                    {
                        foreach (string field in fields)
                        {
                            if (field.IndexOf(textPattern) != 0)
                                continue;
                            if (field.Length - textPattern.Length < 2)
                            {
                                dif = 0;
                                break;
                            }
                        }
                        if (dif == 0)
                            break;
                    }
                    if (dif == int.MaxValue)
                        return;
                }
            }
            if (soundPattern != null && soundPattern.Pattern != null)     // by sound
            {
                int dif = int.MaxValue;
                string item = Scramble.UnMangle(dirNode.Name.ToLower());
                string[] fields = item.Split(new char[] { ImageFileName.multiNameChar, ImageFileName.synonymChar });
                foreach (string field in fields)
                {
                    int i = field.Length - 1;
                    for (; i >= 0; i--)
                        if (char.IsLower(field[i]))
                            break;
                    if (i <= 0)
                        return;
                    string n = field.Substring(0, i+1);
                    dif = Math.Min(dif, soundPattern.Difference(n));
                    Debug.Assert(dif >= 0);
                }
                if (dif == int.MaxValue)
                    return;
                totalDif += dif;
            }
            if (viewedDayAgo != int.MaxValue) // by date
            {
                var lastTime = dirNode.LastAccessTime;
                var difDays = (DateTime.Today - lastTime).TotalDays;
                if (difDays > viewedDayAgo)
                    return;
                totalDif += 100 * difDays / (viewedDayAgo + 1);
            }
            if (changedDayAgo != int.MaxValue) // by date
            {
                var lastTime = dirNode.LastWriteTime;
                var difDays = (DateTime.Today - lastTime).TotalDays;
                if (difDays > changedDayAgo)
                    return;
                totalDif += 100 * difDays / (changedDayAgo + 1);
            }
            if (totalDif < MatchRange)
                searchResult.AddDir(relativePath, totalDif);
        }
        //void MatchImage(DirectoryInfo dirNode, string relativePath)
        //{
        //    ImageDirHash dii = new ImageDirHash(dirNode);
        //    if (dii.ImageInfos == null)
        //        return;
        //    SearchResult.MatchingDir matchingDir = new SearchResult.MatchingDir(relativePath);
        //    foreach (var item in dii.ImageInfos)
        //    {
        //        int dif = imageInfoComparer.HashDifference(item.Value);
        //        if (dif < imageInfoComparer.MaxDifference)
        //        {
        //            matchingDir.AddFile(Path.Combine(relativePath, item.Key), dif);
        //            //Debug.WriteLine(imageInfoComparer.Pattern.ToDifString(item.Value) + '\t' + dif + '\t' + item.Key);
        //        }
        //    }
        //    if(!matchingDir.IsEmpty)
        //        searchResult.AddDir(matchingDir);
        //}
        void MatchFileName(DirectoryInfo dirNode, string relativePath)
        {
            FileInfo[] files;
            files = dirNode.GetFiles();
            if (files.Length == 0)
                return;
            SearchResult.MatchingDir matchingDir = null;
            bool matchingDirNotAdded = true;
            foreach (FileInfo file in files)
            {
                double difference = 0;
                if (viewedDayAgo != int.MaxValue) // by date
                {
                    var difDays = (DateTime.Today - file.LastWriteTime).TotalDays;
                    if (difDays > viewedDayAgo)    // exact day limit
                        continue;
                    else
                        difference += difDays;
                }
                string fn = Scramble.UnMangleFile(file.Name).ToLower();
                //string fnne = Path.GetFileNameWithoutExtension(fn);
                if (textPatterns != null && textPatterns.Length > 0) // multiple name patterns
                {
                    int dif = int.MaxValue;
                    foreach (string textPattern in textPatterns)
                    {
                        if (fn.Contains(textPattern))
                            dif = Math.Min(dif, fn.Length - textPattern.Length);
                    }
                    if (textPatterns.Length > 1 && dif > 1) // in multiple search only 1 letter difference allowed
                        continue;
                    if (dif == int.MaxValue)
                        continue;
                    difference += dif;
                }
                if (matchingDirNotAdded)
                {
                    matchingDir = searchResult.AddDir(relativePath);
                    matchingDirNotAdded = false;
                }
                //matchingDir?.AddFile((relativePath.Length == 0 ? fn : relativePath + '/' + fn), relevance);
                matchingDir?.AddFile(fn, difference);
            }
        }
        #endregion
    }
}
