using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ImageProcessor
{
    public class SoundLike
    {
        static Dictionary<char, char> charVals = new Dictionary<char, char>() { 
            { 'a', 'a' }, { 'b', 'b' }, { 'c', 'c' }, { 'd', 'd' }, { 'e', 'a' }, { 'f', 'f' },
            { 'g', 'g' }, { 'h', ' ' }, { 'i', 'i' }, { 'j', 'g' }, { 'k', 'c' }, { 'l', 'l' },
            { 'm', 'm' }, { 'n', 'n' }, { 'o', 'o' }, { 'p', 'b' }, { 'q', 'c' }, { 'r', 'r' },
            { 's', 's' }, { 't', 'd' }, { 'u', 'v' }, { 'v', 'v' }, { 'w', 'v' }, { 'x', 'x' },
            { 'y', 'i' }, { 'z', 's' }, };
        public readonly string Pattern;
        public int Length { get { return Pattern.Length; } }
        public SoundLike(string s)
        {
            int l = s.Length - 1;
            for (; l >= 0; l--)
                if (char.IsLetter(s[l]) && char.IsLower(s[l]))
                    break;
            if(l!= s.Length - 1)
                s = s.Substring(0, l + 1);
            StringBuilder sb = new StringBuilder(s.Length);
            s = s.ToLower();
            char last = ' ';
            for (int i = 0; i <= l; i++)
            {
                if (!char.IsLetter(s[i]))
                    continue;   // ignore non-letters
                if (s[i] == 'y' && i < l)
                {
                    char c = s[i + 1];
                    if (c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u')
                        continue;
                }
                char sv = charVals[s[i]];
                if (sv == ' ' || sv == last) // skip empty and duplicates
                    continue;
                sb.Append(sv);
                last = sv;
            }
            Pattern = sb.ToString();
        }
        public int Difference(string s)
        {
            string samplePattern = (new SoundLike(s)).Pattern;
            int pInd = 0;
            int sInd = 0;
            int penalty = 0;
            int pLeft = Pattern.Length;
            int sLeft = samplePattern.Length;
            while (pLeft > 0 && sLeft > 0)
            {
                if (Pattern[pInd] != samplePattern[sInd])
                {
                    penalty++;
                    if (pLeft > sLeft && Pattern[pInd + 1] == samplePattern[sInd])
                        pInd++;
                    if (sLeft > pLeft && Pattern[pInd] == samplePattern[sInd + 1])
                        sInd++;
                }
                pInd++;
                sInd++;
                pLeft = Pattern.Length - pInd;
                sLeft = samplePattern.Length - sInd;
            }
            return 300 * (2 * penalty + pLeft + sLeft) / (Pattern.Length + samplePattern.Length + 1);
        }
    }
    public delegate void NewDirectoryNode(DirectoryInfo fi, string relativePath);
    public delegate void NewImageSelection(string image);
    public enum Relation
    {
        Only1 = 0,
        Only2 = 1,
        Newer1 = 2,
        Newer2 = 3,
        DifferentLength = 4,
        Header = 5
    }
    public class DirDifference
    {
        public static char dirPrefix = '/';
        public readonly string Path1;
        public readonly string Path2;
        List<string>[] listArray = new List<string>[5];
        public int Count(Relation r) { return List(r).Count; }
        public int Count1 { get { return Count(Relation.Only1) + Count(Relation.Newer1) + Count(Relation.DifferentLength); } }
        public int Count2 { get { return Count(Relation.Only2) + Count(Relation.Newer2) + Count(Relation.DifferentLength); } }
        public List<string> List(Relation r) { if (listArray[(int)r] == null) listArray[(int)r] = new List<string>(); return listArray[(int)r]; }
        public bool Identical
        {
            get
            {
                if (CompareError != null)
                    return false;
                foreach (var l in listArray)
                    if (l != null && l.Count > 0)
                        return false;
                return true;
            }
        }
        public readonly string CompareError;
        public DirDifference() { } // info list differences
        public DirDifference(string p1, string p2, DirDifference dirDif, DirDifference fileDif, string error) // complete directory differences
        {
            Path1 = p1;
            Path2 = p2;
            CompareError = error;
            listArray[(int)Relation.Only1] = new List<string>();
            listArray[(int)Relation.Only2] = new List<string>();
            if (dirDif != null)
            {
                foreach (var d in dirDif.List(Relation.Only1))
                    listArray[(int)Relation.Only1].Add(dirPrefix + d);
                foreach (var d in dirDif.List(Relation.Only2))
                    listArray[(int)Relation.Only2].Add(dirPrefix + d);
            }
            if (fileDif != null)
            {
                listArray[(int)Relation.Only1].AddRange(fileDif.List(Relation.Only1));
                listArray[(int)Relation.Only2].AddRange(fileDif.List(Relation.Only2));
                listArray[(int)Relation.Newer1] = fileDif.List(Relation.Newer1);
                listArray[(int)Relation.Newer2] = fileDif.List(Relation.Newer2);
                listArray[(int)Relation.DifferentLength] = fileDif.List(Relation.DifferentLength);
            }
            else
            {
                listArray[(int)Relation.Newer1] = new List<string>();
                listArray[(int)Relation.Newer2] = new List<string>();
                listArray[(int)Relation.DifferentLength] = new List<string>();
            }
        }
    }
    public class SearchResult
    {
        const char separator = '\u25CF';
        public static bool DirOnly { get; set; }
        bool sorted = false;
        public class MatchingFile
        {
            public string Name { get; private set; }    // file name
            public double Dif { get; private set; }     // difference to pattren (0 - exact match)
            internal MatchingFile(string n, double d, MatchingDir md) { Name = n; Dif = d; MatchingDir = md; }
            public MatchingDir MatchingDir { get; private set; }
            public override string ToString() { return (Dif == 0 || Dif == int.MaxValue ? "   " : Dif.ToString("f1")) + separator + Name; }
        }
        static Comparison<MatchingFile> FileComparison = delegate (MatchingFile p1, MatchingFile p2)
        {
            double d = p1.Dif - p2.Dif;
            return d == 0 ? string.Compare(p1.Name, p2.Name) : Math.Sign(d);
        };
        public class MatchingDir
        {
            public string Name { get; private set; }    // directory name
            public double Dif { get; private set; }     // difference to pattren (0 - exact match)
            public List<MatchingFile> Files { get; private set; }
            public bool IsEmpty {  get { return Files == null || Files.Count == 0; } }
            internal void AddFile(string f, double d) { if(Files != null) Files.Add(new MatchingFile(f, d, this)); if (Dif > d) Dif = d; }
            internal MatchingDir(string n, double d) { Name = n; Dif = d; Files = null; }
            internal MatchingDir(string n) { Name = n; Dif = int.MaxValue; Files = new List<MatchingFile>(); }
            public override string ToString() { return (DirOnly ? Dif == 0 || Dif == int.MaxValue ? "   " : Dif.ToString("f1") + separator : "") + Name; }
        }
        static Comparison<MatchingDir> DirComparison = delegate (MatchingDir p1, MatchingDir p2)
        {
            double d = p1.Dif - p2.Dif;
            return d == 0 ? string.Compare(p1.Name, p2.Name) : Math.Sign(d);
        };
        public List<MatchingDir> GetMatchedDirs()
        {
            if(!sorted)
            {
                matchingDirs.Sort(DirComparison);
                if(!DirOnly)
                    foreach (var dir in matchingDirs)
                        dir.Files?.Sort(FileComparison);
                sorted = true;
            }
            return matchingDirs;
        }
        List<MatchingDir> matchingDirs;
        MatchingDir currentDir;
        internal SearchResult() { matchingDirs = new List<MatchingDir>(); }
        internal void Clear()   { matchingDirs.Clear(); sorted = false; }
        internal void AddDir(MatchingDir md) { matchingDirs.Add(md); }
        internal MatchingDir AddDir(string n) { currentDir = new MatchingDir(n); matchingDirs.Add(currentDir); return currentDir; }
        internal void AddDir(string n, double d) { matchingDirs.Add(new MatchingDir(n, d)); }
    }
    public enum DirName
	{
        Downloaded,
		NewArticles,
		AllDevicy,
		Work,
		Root       // parent - has to be last
	}
}
