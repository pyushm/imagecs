using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ImageProcessor
{
    public interface IAssociatedPath
    {
        string RootName         { get; }
        string PaintExe         { get; }
        string EditorExe        { get; }
        string MediaExe         { get; }
        string MediaTmpLocation { get; }
        string ActiveImageName  { set; }
    }
    public class Navigator : IAssociatedPath
	{
        #region Private Members
        static public int MatchRange = 100;  // maximum match difference percentage included into search results
        static ImageHash.Comparer imageInfoComparer = new ImageHash.Comparer(56);
        string[] textPatterns;   // 1 item - search for pattern in string; >1 - serch exact string for each item (1 extra char at the end allowed)
        static DirectoryInfo[] specialDirectories;
        SoundLike soundPattern;
        int searchDaysOld = int.MaxValue;
        SearchResult searchResult;
        List<DirDifference> dirDifferences = new List<DirDifference>();
        bool stopSearch = false;
        #endregion
        public enum SearchMode
        {
            Name,
            Sound,
            File,
            Image
        }
        public enum FileCompare
        {
            Same,
            Newer,
            Older,
            SourceOnly,
            CompareOnly
        }
        public NewDirectoryNode ProcessDirecory;
        public NewImageSelection onNewImageSelection;
        NewDirectoryNode searchMatch = null;
        string activeImageName = null;
        public string RootName          { get; private set; }
        public string PaintExe          { get; private set; }
        public string EditorExe         { get; private set; }
        public string MediaExe          { get; private set; }
        public string MediaTmpLocation  { get { return "_._"; } }
        public string ActiveImageName   { get { return activeImageName; } set { activeImageName = value; onNewImageSelection?.Invoke(activeImageName); } }
        public bool StopSearch          { get { return stopSearch; } set { stopSearch = value; } }
        public DirectoryInfo DirInfo(int i) { return specialDirectories[i]; }
        public DirectoryInfo DirInfo(DirName sd) { return DirInfo((int)sd); }
        public DirectoryInfo Root       { get { return specialDirectories[(int)DirName.Root]; } }
        public DirectoryInfo AllDevicy  { get { return specialDirectories[(int)DirName.AllDevicy]; } }
        public string[] GetMatchingDirNames()
        {
            List<string> ret = new List<string>();
            foreach (var dir in searchResult.GetMatchedDirs())
                ret.Add(dir.Name);
            return ret.ToArray<string>();
        }
        public Navigator()
        {
            Common.XMLStore settings = new Common.XMLStore(Path.Combine(Directory.GetCurrentDirectory(), "Customization.xml"));
            RootName = settings.GetString("location.root", "../stuff/");
            PaintExe = settings.GetString("path.paint");
            EditorExe = settings.GetString("path.editor");
            MediaExe = settings.GetString("path.media");
            searchResult = new SearchResult();
            string[] dirNames = Enum.GetNames(typeof(DirName));
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
                if (di.FullName != Root.FullName)
                    return di.GetDirectories();
                int nChildren = (int)DirName.Root;
                DirectoryInfo[] children = new DirectoryInfo[nChildren];
                for (int i = 0; i < nChildren; i++)
                    children[i] = DirInfo(i);
                return children;
            }
            catch
            {
                return new DirectoryInfo[0];
            }
		}
        public static bool IsSpecialDirectory(DirectoryInfo testdi)
        {
            foreach (DirectoryInfo di in specialDirectories)
                if (di.FullName == testdi.FullName)
                    return true;
            return false;
        }
        public bool IsAllDevicy(DirectoryInfo testdi) { return AllDevicy.FullName == testdi.FullName; }
        public DirectoryInfo GetSearchRoot(string name)
        {
            if (name == null || name.Length == 0 || !Directory.Exists(name))
                return Root;
            DirectoryInfo di = new DirectoryInfo(name);
            if (IsAllDevicy(di) || IsAllDevicy(di.Parent) || IsSpecialDirectory(di))
                return di;
            return Root;
        }
        public SearchResult GenerateSearchList(SearchMode mode, DirectoryInfo start, string name, string daysOld)
        {
            soundPattern = mode == SearchMode.Sound ? new SoundLike(name) : null;
            string textPattern = mode == SearchMode.File || mode == SearchMode.Name ? name.ToLower() : null;
            if (mode == SearchMode.Name)
            {
                var tps = textPattern?.Split(new char[] { ',', ' ', '.', '-' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> tpsl = new List<string>();
                foreach (string tp in tps)
                    if (tp.Length > 1)
                        tpsl.Add(tp);
                textPatterns = tpsl.ToArray();
            }
            else if (textPattern != null)
                textPatterns = new string[] { textPattern };
            else
                textPatterns = null;
            if (mode == SearchMode.Image)
            {
                if (activeImageName == null)
                    return null;
                imageInfoComparer.SetPattern(activeImageName);
                //Debug.WriteLine("************************** image pattern " + activeImageName + "***************************");
                //Debug.WriteLine(imageInfoComparer.Pattern.ToString());
                //Debug.Write(imageInfoComparer.Pattern.ToBWMString());
            }
            try { searchDaysOld = int.Parse(daysOld); }
            catch { searchDaysOld = int.MaxValue; }
            NewDirectoryNode callback = mode == SearchMode.File ? MatchFileName :
                mode == SearchMode.Image ? MatchImage :
                mode == SearchMode.Name ? MatchDirectory :
                mode == SearchMode.Sound ? MatchDirectory : (NewDirectoryNode)null;
            if (callback == null)
                return null;
            return Search(start, callback);
        }
        public void CreateImageHashes(DirectoryInfo dirNode)
        {
            if (!IsSpecialDirectory(dirNode) && !IsSpecialDirectory(dirNode.Parent))
            {
                ImageDirHash dii = new ImageDirHash(dirNode);
                dii.Update();
            }
            DirectoryInfo[] subdirs = dirNode.GetDirectories();
            Parallel.ForEach(subdirs, (subdir) =>
            { CreateImageHashesRecursively(subdir); });
        }
        public void CreateImageHashesRecursively(DirectoryInfo dirNode)
        {
            if (StopSearch)
                return;
            if (!IsSpecialDirectory(dirNode) && !IsSpecialDirectory(dirNode.Parent))
            {
                ImageDirHash dii = new ImageDirHash(dirNode);
                dii.Update();
            }
            DirectoryInfo[] subdirs = dirNode.GetDirectories();
            foreach (DirectoryInfo subdir in subdirs)
                CreateImageHashesRecursively(subdir);
        }
        public List<DirDifference> CompareDirectoryTree(DirectoryInfo d1, DirectoryInfo d2)
        {
            dirDifferences.Clear();
            if (d1 != null && d2 != null)
                CompareRecursively(d1, d2);
            return dirDifferences;
        }
        public void SearchRecursively(DirectoryInfo dirNode, string relativePath)
        {
            if (StopSearch)
                return;
            relativePath = FileName.UnMangleFile(relativePath);
            searchMatch?.Invoke(dirNode, relativePath);
            DirectoryInfo[] subdirs = dirNode.GetDirectories();
            foreach (DirectoryInfo subdir in subdirs)
            {
                string mn = FileName.UnMangle(subdir.Name);
                string newRelativePath = Path.Combine(relativePath, mn);
                SearchRecursively(subdir, newRelativePath);
            }
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
        #region Private Methods
        DirDifference CompareFiles(DirectoryInfo d1, DirectoryInfo d2)
        {
            return CompareFileLists(d1.GetFiles(), d2.GetFiles(), false);
        }
        DirDifference CompareSubDirectories(DirectoryInfo d1, DirectoryInfo d2)
        {
            return CompareFileLists(d1.GetDirectories(), d2.GetDirectories(), true);
        }
        DirDifference CompareFileLists(FileSystemInfo[] l1, FileSystemInfo[] l2, bool subDirs)
        {
            DirDifference diff = new DirDifference();
            IOrderedEnumerable<FileSystemInfo> sortedL1 = l1.OrderBy(file => file.Name);
            IOrderedEnumerable<FileSystemInfo> sortedL2 = l2.OrderBy(file => file.Name);
            IEnumerator<FileSystemInfo> enl1 = sortedL1.GetEnumerator();
            IEnumerator<FileSystemInfo> enl2 = sortedL2.GetEnumerator();
            bool srcActive = enl1.MoveNext();
            bool cmpActive = enl2.MoveNext();
            while (srcActive || cmpActive)
            {
                int res = !srcActive ? 1 : !cmpActive ? -1 : string.Compare(enl1.Current.Name, enl2.Current.Name, true);
                if (res < 0)
                {
                    if(ImageDirHash.DirInfoFileName != enl1.Current.Name)
                        diff.List(Relation.Only1).Add(enl1.Current.Name);
                    srcActive = enl1.MoveNext();
                    continue;
                }
                else if (res > 0)
                {
                    if (ImageDirHash.DirInfoFileName != enl2.Current.Name)
                        diff.List(Relation.Only2).Add(enl2.Current.Name);
                    cmpActive = enl2.MoveNext();
                    continue;
                }
                if ((subDirs || ((FileInfo)enl1.Current).Length != ((FileInfo)enl2.Current).Length) && ImageDirHash.DirInfoFileName != enl1.Current.Name)
                {
                    TimeSpan ts = enl1.Current.LastWriteTime - enl2.Current.LastWriteTime;
                    if (ts > TimeSpan.Zero)
                        diff.List(Relation.Newer1).Add(enl1.Current.Name);
                    else if (ts < TimeSpan.Zero)
                        diff.List(Relation.Newer2).Add(enl1.Current.Name);
                    else
                        diff.List(Relation.DifferentLength).Add(enl1.Current.Name);
                }
                srcActive = enl1.MoveNext();
                cmpActive = enl2.MoveNext();
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
                fileDif = CompareFiles(d1, d2);
                dirDif = CompareSubDirectories(d1, d2);
            }
            catch(Exception ex)
            {
                error = d1.FullName + "<->" + d2.FullName + ": " + ex.Message;
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
        SearchResult Search(DirectoryInfo start, NewDirectoryNode callback)
        {
            StopSearch = false;
            searchResult.Clear();
            searchMatch = callback;
            try { SearchRecursively(start, ""); }
            finally { searchMatch = null; }
            return searchResult;
        }
        void MatchDirectory(DirectoryInfo dirNode, string relativePath)
        {   // matches directory by name, sound, date
            if (relativePath.Length == 0)
                return;
            double totalDif = 0;
            if (textPatterns != null && textPatterns.Length>0) // by name
            {
                string item = FileName.UnMangle(dirNode.Name.ToLower());
                string[] fields = item.Split(new char[] { ImageFileInfo.multiNameChar, ImageFileInfo.synonymChar });
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
                string item = FileName.UnMangle(dirNode.Name.ToLower());
                string[] fields = item.Split(new char[] { ImageFileInfo.multiNameChar, ImageFileInfo.synonymChar });
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
            if (searchDaysOld != int.MaxValue) // by date
            {
                var difDays = (DateTime.Today - dirNode.LastWriteTime).TotalDays;
                if (difDays > searchDaysOld)
                    return;
                totalDif += 100 * difDays / (searchDaysOld+1);
            }
            if(totalDif < MatchRange)
                searchResult.AddDir(relativePath, totalDif);
        }
        void MatchImage(DirectoryInfo dirNode, string relativePath)
        {
            ImageDirHash dii = new ImageDirHash(dirNode);
            if (dii.ImageInfos == null)
                return;
            SearchResult.MatchingDir matchingDir = new SearchResult.MatchingDir(relativePath);
            foreach (var item in dii.ImageInfos)
            {
                int dif = imageInfoComparer.HashDifference(item.Value);
                if (dif < imageInfoComparer.MaxDifference)
                {
                    matchingDir.AddFile(Path.Combine(relativePath, item.Key), dif);
                    Debug.WriteLine(imageInfoComparer.Pattern.ToDifString(item.Value) + '\t' + dif + '\t' + item.Key);
                }
            }
            if(!matchingDir.IsEmpty)
                searchResult.AddDir(matchingDir);
        }
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
                if (searchDaysOld != int.MaxValue) // by date
                {
                    var difDays = (DateTime.Today - file.LastWriteTime).TotalDays;
                    if (difDays > searchDaysOld)    // exact day limit
                        continue;
                    else
                        difference += difDays;
                }
                string fn = FileName.UnMangleFile(file.Name).ToLower();
                string fnne = Path.GetFileNameWithoutExtension(fn);
                if (textPatterns != null && textPatterns.Length > 0) // multiple name patterns
                {
                    int dif = int.MaxValue;
                    foreach (string textPattern in textPatterns)
                    {
                        if (fnne.Contains(textPattern))
                            dif = Math.Min(dif, fnne.Length - textPattern.Length);
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
