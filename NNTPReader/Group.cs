using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace NNTP
{
	[Serializable]
	public class Group                          // group base info used in permanent storage
	{
		protected static string groupNamePrefix="alt.binaries.";
        public static explicit operator string(Group gi) { return gi.Name; }
        public static string GroupNamePrefix    { get { return groupNamePrefix; } }
        string name;							// group name
        int maxArticleId;                       // max loaded article ID
		public string Name				    	{ get { return name; } }
		public string FullName					{ get { return groupNamePrefix+name; } }
        public string ShortName                 
        { 
            get 
            {
                string[] fields = name.Split(new char[] { '.', '-' });
                char[] cs = new char[fields.Length + 1];
                for (int j = 0; j < fields.Length; j++)
                    cs[j] = fields[j][0];
                cs[fields.Length] = name[name.Length - 1];
                return (new string(cs)).ToUpper();
            } 
        }
        public void Reset()                     { maxArticleId = 1; }
        public int StorredMaxArticleId { get { return maxArticleId; } set { if (maxArticleId < value) maxArticleId = value; } }
        //public int StorredMaxArticleId          { get { return maxArticleId; } set { maxArticleId = value; } }
        public Group(string name_, int maxId)   
        {
            name = name_;
            maxArticleId = maxId;
        }
        protected Group(Group gb)				
		{
			name=gb.name;
            maxArticleId = gb.maxArticleId;
		}
        public string[] Fields                  
        {
            get
            {
                string[] sa = new string[3];
                sa[0] = Name;
                sa[1] = maxArticleId.ToString();
                sa[2] = "";
                return sa;
            }
        }
        public class Comparer : IComparer<Group>
        {
            int IComparer<Group>.Compare(Group l1, Group l2)
            {
                return String.Compare((string)l1, (string)l2);
            }
        }
    }
    public class GroupInfo : Group
    {
        int state;                              // <0 - deleted, 0 - regular, >0 - new
        int minArticleId;                       // min article ID
        public int MinArticleId { get { return minArticleId; } }
        public int NumHeaders { get { return Math.Max(StorredMaxArticleId - minArticleId, 0); } }
        GroupInfo(string name_, int maxId, int minId) : base(name_, maxId) { minArticleId = minId; state = 0; }
        public GroupInfo(string name_) : base(name_, 0) { minArticleId = 0; state = -1; }
        public static GroupInfo FromLine(string groupLine)
        {
            int maxId = 0;
            int minId = 0;
            //headerLine.Replace("\t\t", "\t<empty>\t");// find empty  fields
            string[] fields = groupLine.Split(' ');	// parse out header line
            int nFields = fields.Length;
            if (nFields < 1)
                return null;
            if (!fields[0].StartsWith(groupNamePrefix))
                return null;
            if (nFields > 1)
            {
                try { maxId = int.Parse(fields[1]); }
                catch { maxId = 0; }
            }
            if (nFields > 2)
            {
                try { minId = int.Parse(fields[2]); }
                catch { minId = 0; }
            }
            return new GroupInfo(fields[0].Substring(groupNamePrefix.Length), maxId, minId);
        }
        public int State                        { get { return state; } set { state = value; } }
        public new string[] Fields              
        {
            get
            {
                string[] sa = base.Fields;
                int na = NumHeaders;
                sa[2] = na.ToString();
                return sa;
            }
        }
    }
    public class HostGroup : Group              // group with permanently storred headerCollection and connection to the server
    {
        static DateTime IgnoreDate = DateTime.Now.AddYears(-1);
        string storeFileName;				    // headerCollection storage file name
		int lastArticleIdOnServer;
        int firstArticleIdOnServer;
        bool markedDeleted=false;               // indicates that headers should not be storred
        ArticleHeader.Item.Collection headerCollection;// header collection of the group
        ArticleHeader.Item.Collection displayHeaders;// filterred header collection for display
        ArticleHeader.Item.Collection downloadedHeaders;// headers of previously downloadewd articles
        internal ArticleHeader.Item.Collection Headers { get { return displayHeaders; } set { displayHeaders = value; } }
        public int Count                        { get { return displayHeaders.Count; } }
        public int MaxArticleID                 { get { return Math.Max(headerCollection.MaxId, StorredMaxArticleId); } }
        public int FirstArticleIdOnServer       { get { return firstArticleIdOnServer; } set { firstArticleIdOnServer = value; } }
        public int LastArticleIdOnServer        { get { return lastArticleIdOnServer; } set { lastArticleIdOnServer = value; } }
        public string StoreFileName 			{ get { return storeFileName; } }
        public void MarkDeleted()               { markedDeleted=true; }
        public HostGroup(Group gb, string serializationDir) : base(gb)
        {
            storeFileName = Path.Combine(serializationDir, ShortName);
            headerCollection = new ArticleHeader.Item.Collection();
            lastArticleIdOnServer = 0;
        }
        ~HostGroup()                            // final update of header permanent storage
        {
            StoreHeaders(5);
        }
        public ArticleHeader.Item.Collection FilterHeaders(string subjectFilter)
        {
            string sf = subjectFilter.ToLower();
            ArticleHeader.Item.Collection al = new ArticleHeader.Item.Collection();
            foreach (ArticleHeader.Item ah in headerCollection)
            {
                if (sf.Length==0 || ah.Subject.ToLower().IndexOf(sf) >= 0)
                    al.Add(ah);
            }
            return al;
        }
        public int RemoveDeletedHeaders()       // removes headers marked for delete from headerCollection
        {
            ArticleHeader.Item.Collection newaha = new ArticleHeader.Item.Collection();
            foreach (ArticleHeader.Item ahi in headerCollection)
                if (ahi.ArticleId != 0)
                    newaha.Add(ahi);
            int ndel = headerCollection.Count - newaha.Count;
            displayHeaders=headerCollection = newaha;
            StorredMaxArticleId = headerCollection.MaxId;
            StoreHeaders(5);
            return ndel;
        }
        public int RemoveOldHeaders(DateTime firstDate)// removes old headers from headerCollection
        {
            ArticleHeader.Item.Collection newaha = new ArticleHeader.Item.Collection();
            StorredMaxArticleId = headerCollection.MaxId;
            foreach (ArticleHeader.Item ahi in headerCollection)
                if (ahi.Date >= firstDate || ahi.Date < IgnoreDate)
                    newaha.Add(ahi);
            int ndel = headerCollection.Count - newaha.Count;
            displayHeaders = headerCollection = newaha;
            StoreHeaders(5);
            return ndel;
        }
        public int RemoveOldHeaders(int cutID)// removes old headers from headerCollection
        {
            ArticleHeader.Item.Collection newaha = new ArticleHeader.Item.Collection();
            StorredMaxArticleId = headerCollection.MaxId;
            foreach (ArticleHeader.Item ahi in headerCollection)
                if (ahi.TrueID > cutID)
                    newaha.Add(ahi);
            int ndel = headerCollection.Count - newaha.Count;
            displayHeaders = headerCollection = newaha;
            StoreHeaders(5);
            return ndel;
        }
        public int GetStoredHeaders(ArticleHeader.Item.Collection downloadedHeaders_)
        {
            downloadedHeaders = downloadedHeaders_;
            LoadStoredHeaders(3);
            IComparer<ArticleHeader> comp = new ArticleHeader.IDComparer();
            headerCollection.Sort(comp);
            ArticleHeader.Item prev = new ArticleHeader.Item("", ShortName);
            ArticleHeader.Item.Collection newHeaders = new ArticleHeader.Item.Collection();
            foreach (ArticleHeader.Item ah in headerCollection)
            {
                if (comp.Compare(ah, prev) != 0)
                {
                    newHeaders.Add(ah);
                    prev = ah;
                }
                else if (ah.ArticleId < 0)
                    prev.ArticleId = ah.ArticleId;
            }
            displayHeaders = headerCollection = newHeaders;
            List<ArticleHeader> alreadyLoaded = headerCollection.Intersect(downloadedHeaders, new ArticleHeader.SubjectComparer());
            foreach (ArticleHeader.Item ahd in alreadyLoaded)
                ahd.SetDownloadedElsewhere();
            headerCollection.Sort(new ArticleHeader.IDComparer());
            return headerCollection.Count;
        }
        void LoadStoredHeaders(int attempt)     
        {
            ArticleHeader[] aha;
            try
            {
                FileStream fs = new FileStream(storeFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                BinaryFormatter f = new BinaryFormatter();
                aha = (ArticleHeader[])f.Deserialize(fs);
                fs.Close();
                headerCollection = new ArticleHeader.Item.Collection(aha, ShortName);
            }
            catch (System.IO.FileNotFoundException)
            {
                headerCollection = new ArticleHeader.Item.Collection();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                headerCollection = new ArticleHeader.Item.Collection();
                if (attempt < 0)
                    throw new Exception(ex.Message);
                Thread.Sleep(200);
                LoadStoredHeaders(attempt - 1);
            }
        }
        public bool StoreHeaders(int attempt)   
        {
            if (markedDeleted)
                return true;
            try
            {
                ArticleHeader[] aha = headerCollection.GetRawHeaders();
                FileStream fs = new FileStream(storeFileName, FileMode.Create);
                BinaryFormatter f = new BinaryFormatter();
                f.Serialize(fs, aha);
                fs.Close();
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                if (attempt < 0)
                    return false;
                Thread.Sleep(100);
                return StoreHeaders(attempt - 1);
            }
        }
		public void AddNewHeader(ArticleHeader.Item ahi)// new header from the server
		{
            if (downloadedHeaders.Find(ahi, new ArticleHeader.SubjectComparer())!=null)
                ahi.SetDownloadedElsewhere();
            headerCollection.Add(ahi);
		}
	}
}
