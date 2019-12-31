using System;
using System.Collections.Generic;

namespace NNTP
{
    [Serializable]
    public class ArticleHeader              // minimal header data (copy of server header content), used to permanently store header
    {
        internal int LineCount;
        internal int ByteCount;
        internal DateTime Date;
        internal string Subject;
        public int ArticleId;
        public int TrueID                   { get { return Math.Abs(ArticleId); } }
        internal ArticleHeader(ArticleHeader ah)
        {
            LineCount = ah.LineCount;
            ByteCount = ah.ByteCount;
            Date = ah.Date;
            ArticleId = ah.ArticleId;
            Subject = ah.Subject;
        }
        internal ArticleHeader(string headerLine)
        {
            LineCount = 0;
            ByteCount = 0;
            Date = new DateTime();
            ArticleId = 0;
            Subject = "";
            headerLine.Replace("\t\t", "\t<empty>\t");// find empty  fields
            string[] fields = headerLine.Split('\t');	// parse out header line
            int nFields = fields.Length;
            if (nFields < 2)
                return;
            try { ArticleId = Convert.ToInt32(fields[0]); }
            catch { ArticleId = 0; }
            Subject = fields[1];
            if (nFields < 4)
                return;
            //h.From = fields[2];
            try
            {
                int cutm = fields[3].IndexOf('-');
                int cutp = fields[3].IndexOf('+');
                if (cutm > 0)
                {
                    Date = DateTime.Parse(fields[3].Substring(0, cutm));
                    int off = int.Parse(fields[3].Substring(cutm + 1, 4));
                    Date = Date.AddHours(off / 100.0);
                }
                else if (cutp > 0)
                {
                    Date = DateTime.Parse(fields[3].Substring(0, cutp));
                    int off = int.Parse(fields[3].Substring(cutp + 1, 4));
                    Date = Date.AddHours(-off / 100.0);
                }
                else
                    Date = DateTime.Parse(fields[3]);
            }
            catch { Date = new DateTime(); }
            //h.MessageId = Convert.ToInt32(fields[4]);
            //h.References = fields[5];
            if (nFields < 7)
                return;
            try { ByteCount = Convert.ToInt32(fields[6]); }
            catch { ByteCount = -1000; }
            if (nFields < 8)
                return;
            try { LineCount = Convert.ToInt32(fields[7]); }
            catch { LineCount = -1; }
        }
        //public class DateComparer : System.Collections.IComparer
        //{
        //    int System.Collections.IComparer.Compare(object l1, object l2)
        //    {
        //        int ret = DateTime.Compare(((ArticleHeader)l1).Date, ((ArticleHeader)l2).Date);
        //        if (ret == 0)
        //            ret = String.Compare(((ArticleHeader)l1).Subject, ((ArticleHeader)l2).Subject);
        //        return ret;
        //    }
        //}
        public class SubjectComparer : IComparer<ArticleHeader>
        {
            int IComparer<ArticleHeader>.Compare(ArticleHeader l1, ArticleHeader l2)
            {
                return string.Compare(l1.Subject, l2.Subject);
            }
        }
        public class IDComparer : IComparer<ArticleHeader>
        {
            int IComparer<ArticleHeader>.Compare(ArticleHeader l1, ArticleHeader l2)
            {
                return l1.TrueID - l2.TrueID;
            }
        }
        public class NameComparer : IComparer<ArticleHeader>
        {
            int IComparer<ArticleHeader>.Compare(ArticleHeader l1, ArticleHeader l2)
            {
                int ret = string.Compare(l1.Subject, l2.Subject);
                if (ret == 0)
                    return l1.TrueID - l2.TrueID;
                return ret;
            }
        }
        public class Item : ArticleHeader   // one-to-one corresponds to headerListView.Item
        {
            static int numFields = 5;
            string articleFileName = "";	// name of the data file in the temporary storage
            bool downloadedElsewhere = false; // true if article downloaded elsewhere
            bool markedForDownloading = false; // true if article downloaded elsewhere
            bool udated = false;
            string group;					// display name of the group to which message belongs
            int numParts = 1;
            int partNum = 1;
            public bool DownloadingFailed = false;
            public bool EarlierPartMissing = false;
            string subjectBase = "";
            public string ArticleFileName { get { return articleFileName; } set { articleFileName = value; } }
            public new int ByteCount { get { return base.ByteCount; } }
            public new int LineCount { get { return base.LineCount; } }
            public new string Subject { get { return base.Subject; } }
            public new DateTime Date { get { return base.Date; } }
            public bool DownloadedElsewhere { get { return downloadedElsewhere; } }
            public bool MarkedForDownloading { get { return markedForDownloading; } set { markedForDownloading = value; udated = true; } }
            public bool NotLoaded { get { return ArticleId > 0; } }
            public bool Downloaded { get { return ArticleId < 0; } }
            public bool Updated { get { if (!udated) return false; udated = false; return true; } set { udated = value; } }
            public string SubjectBase { get { return subjectBase; } }
            public bool SingleMessage { get { return numParts <= 1; } }
            public static int NumFields { get { return numFields; } }
            public int NumParts { get { return numParts; } }
            public int PartNum { get { return partNum; } }
            public bool LastInSequence { get { return partNum == numParts; } }
            internal Item(ArticleHeader ah, string group_) : base(ah) { group = group_; ParseSubject(); }
            public Item(string headerLine, string group_) : base(headerLine) { group = group_; ParseSubject(); }
            public void SetSingleMessage()
            {
                numParts = 1;
                partNum = 1;
                subjectBase = Subject;
            }
            public void SetDownloadedElsewhere() { downloadedElsewhere = true; udated = true; }
            public bool PartOf(ArticleHeader.Item ahi) { return numParts == ahi.numParts && partNum <= ahi.partNum; }
            public void ResetPartNumber(int pn) { partNum = pn; }
            public string[] Fields          // GUI representation of the item
            {
                get
                {
                    string[] fields = new string[numFields];
                    fields[0] = Date.ToShortDateString() + ' ' + Date.ToShortTimeString();
                    fields[1] = (ByteCount / 1000).ToString() + "Kb";
                    fields[2] = Subject;
                    fields[3] = TrueID.ToString();
                    fields[4] = group;
                    return fields;
                }
            }
            void ParseSubject()	// finds last ...(part/of)... or [part/of] in the subject
            {
                partNum = 1;
                numParts = 1;
                int[] pna, npa, pos;
                int len = ParseSubjectPart(out pna, out npa, out pos, new char[] { '(', ')' });
                if(len == 0)
                    len = ParseSubjectPart(out pna, out npa, out pos, new char[] { '[', ']' });
                for (int i = 0; i < len; i++)
                {
                    if (pna[i] == 0 || npa[i] == 0 || pna[i] > npa[i])
                        continue;
                    partNum = pna[i];
                    numParts = npa[i];
                    if (numParts == 1)
                        break;
                    subjectBase = pos[i] < 0 ? "" : Subject.Substring(0, pos[i]);
                }
                if (numParts == 1)
                    subjectBase = Subject;
            }
            int ParseSubjectPart(out int[] pna, out int[] npa, out int[] pos, char[] braces)
            {
                string[] ss = Subject.Split(new char[] { braces[0] });
                pna = new int[ss.Length - 1];
                npa = new int[ss.Length - 1];
                pos = new int[ss.Length - 1];
                int ind = 0;
                int p = ss[0].Length;
                for (int i = 0; i < ss.Length - 1; i++)
                {
                    if (ParseSubjectPart(ss[i + 1], out pna[ind], out npa[ind], braces[1]))
                    {
                        pos[ind] = p;
                        ind++;
                    }
                    p += ss[i + 1].Length+1;
                }
                return ind;
            }
            bool ParseSubjectPart(string sp, out int partNum, out int numParts, char close)
            {
                partNum = 0;
                numParts = 0;
                try
                {
                    int pos1 = sp.IndexOf('/');	// first '/' after '('
                    if (pos1 < 0)
                        return true;
                    int pos2 = sp.IndexOfAny(new char[] { close }, pos1);	// first ')' after '/'
                    if (pos2 < 0)
                        return true;
                    partNum = int.Parse(sp.Substring(0, pos1));
                    numParts = int.Parse(sp.Substring(pos1 + 1, pos2 - pos1 - 1));
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            public class Collection : List<ArticleHeader>
            {
                int maxId = 0;                  // max article id of the collection
                public int MaxId { get { return maxId; } }
                public new Item this[int i]
                {
                    get
                    {
                        try { return (Item)base[i]; }
                        catch { return new Item("", ""); }
                    }
                    set { base[i] = value; }
                }
                public Collection() : base() { }
                public Collection(int len) : base(len) { }
                public Collection(ArticleHeader[] aha, string group_)
                    : base(aha.Length)
                {
                    foreach (ArticleHeader ah in aha)
                        Add(new ArticleHeader.Item(ah, group_));
                }
                public Collection(Collection ahic)
                    : base()
                {
                    AddRange(ahic);
                    maxId = Math.Max(maxId, ahic.maxId);
                }
                public void Add(Item ahi)
                {
                    if (maxId < ahi.TrueID)
                        maxId = ahi.TrueID;
                    base.Add(ahi);
                }
                public bool AddUnique(Item ahi)
                {
                    int id = ahi.TrueID;
                    ArticleHeader found = Find(ahi, new ArticleHeader.IDComparer());
                    if (found == null)
                    {
                        ahi.Updated = true;
                        Add(ahi);
                        return true;
                    }
                    if (ahi.ArticleId < 0)
                        found.ArticleId = ahi.ArticleId;
                    return false;
                }
                public ArticleHeader[] GetRawHeaders()
                {
                    ArticleHeader[] aha = new ArticleHeader[Count];
                    for (int i = 0; i < Count; i++)
                        aha[i] = new ArticleHeader(this[i]);
                    return aha;
                }
                public List<ArticleHeader> Intersect(Collection list, IComparer<ArticleHeader> comparer)
                {
                    return Common.ListComparer<ArticleHeader>.FirstInSecondOnly(this, list, comparer);
                }
                public ArticleHeader Find(ArticleHeader ah, IComparer<ArticleHeader> comparer)
                {
                    foreach (ArticleHeader ahl in this)
                        if (comparer.Compare(ahl, ah) == 0)
                            return ahl;
                    return null;
                }
            }
        }
    }
}
