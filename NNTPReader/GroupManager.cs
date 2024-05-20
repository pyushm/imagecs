using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.Serialization.Formatters.Soap;
using System.Runtime.Serialization.Formatters.Binary;
using ImageProcessor;

namespace NNTP
{
    public enum ProcessState
    {
        Disconnected,
        Connecting,
        Connected,
        FailedConnect,
        RetrievingGroups,
        GotGroups,
        FailedGetGroups
    }
    public class HostManager
    {
        //static int numAttempts = 10;
		protected Reader reader;
        protected Thread connectingThread = null;// thread for asynchronous processing of requests
        protected ProcessState processState;
        protected List<Group> hostGroups;       // all groups on server
        protected bool idle;
        protected string hostName;
        protected int totalGroups;              // number of server groups 
        protected int totalHeaders;             // sum of all hostGroups' number of headers 
        Common.LogQueue messages;
        public bool Idle                        { get { return idle; } }
        public ProcessState ProcessState        { get { return processState; } }
        public Reader NNTPReader                { get { return reader; } }
        public Common.LogQueue Messages         { get { return messages; } }
        public Group[] HostGroups               { get { return hostGroups == null ? new Group[0] : hostGroups.ToArray(); } }
        public string[] Fields                  
        {
            get
            {
                string[] fields = new string[3];
                fields[0]=hostName;
                if (ProcessState == ProcessState.GotGroups)
                {
                    fields[1] = hostGroups.Count.ToString() + " of " + totalGroups;
                    fields[2] = totalHeaders.ToString();
                }
                else
                    fields[1] = ProcessState.ToString();
                return fields;
            }
        }
        public HostManager(string host)         
        {
            hostName = host;
            messages = new Common.LogQueue();
            reader = host != null && host.Length > 0 ? new Reader(host, "", "", SendMessage) : null;
        }
        public void StartConnectHost()          { StartOperation(new ThreadStart(ConnectHost)); }
        protected void ConnectHost()            
        {
            processState = ProcessState.Connecting;
            processState = reader.ConnectHost(1) ? ProcessState.Connected : ProcessState.FailedConnect;
        }
        public void StartGetServerGroups()      { StartOperation(new ThreadStart(GetServerGroups)); }
        protected void GetServerGroups()        // loads available groups
        {
            if (processState != ProcessState.Connected)
                return;
            processState = ProcessState.RetrievingGroups;
            hostGroups = new List<Group>();
            totalHeaders = 0;
            totalGroups = 0;
            string result = reader.LoadAllGroups(new Reader.OnNewLine(GotNewGroupLine));
            processState = result == "okay" ? ProcessState.GotGroups : ProcessState.FailedGetGroups;
            if (result == "okay")
                hostGroups.Sort(new Group.Comparer());
            else
                SendMessage(result, Common.MessageType.Warning);
        }
        protected int GotNewGroupLine(string l)	// callback from LoadAllGroups
        {
            totalGroups++;
            GroupInfo gi = GroupInfo.FromLine(l);
            if (gi == null)
                return 0;
            hostGroups.Add(gi);
            totalHeaders += gi.NumHeaders;
            return 1;
        }
        protected bool StartOperation(ThreadStart operation)
        {
            try
            {
                //if (connectingThread != null && connectingThread.IsAlive && numAttempts-- > 0)
                if (connectingThread != null && connectingThread.IsAlive)
                    Thread.Sleep(300);
                //if (numAttempts <= 0)
                //{
                //    SendMessage(connectingThread.Name + " is running", Common.MessageType.Warning);
                //    return false;
                //}
                idle = false;
                connectingThread = new Thread(operation);
                connectingThread.Name = operation.Method.Name;
                connectingThread.Start();
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                SendMessage(operation.Method.Name + " failed to start: " + ex.Message, Common.MessageType.Error);
                idle = true;
                return false;
            }
        }
        public void Stop()                      
        {
            reader.Stop = true;
            if (connectingThread != null && connectingThread.IsAlive)
                connectingThread.Abort();
        }
        protected void SendMessage(string msg, Common.MessageType level)
        {
            messages.Add(msg, level);
        }
    }
    public class GroupManager : HostManager, IAssociatedPath
	{
		public delegate string OnNeedName(string group, int id);
        string paintExe;
        string editorExe;
        string mediaExe;
        string archiveExe;
        string dataDirName;
        string DataFileName(string fileName)    
        {
            return Path.Combine(dataDirName, fileName);
        }
        Navigator navigator;
        NewArticleStorage storage;
		DateTime firstDateToKeep;				// the earlist date of the message to keep
		string busyDataFileName;				// file containing groups in use
        string subscribedGroupFileName;		    // file storing subscribed groups
        string serverGroupFileName;		        // file storing server groups
        string blockedHeadersFileName = "blockedHeaders.txt";			
		string loadedArticlesFileName;
		string groupDataStoreDir;
		string hostDisplayName;
		HostGroup currentGroup;					// group associated with server
        Group[] subscribedGroups;		        // locally storred group base data (array used in serialization)
        bool serverGroupsChanged=false;         // server group list differs from old if true
		int numHeadersToLoad;					// expected number of headers to load
        ArticleHeader.Item.Collection newArticlesToDownload;// newly selected articles for download
        ArticleHeader.Item.Collection articlesToDownload;// headers to load
        ArticleHeader.Item.Collection downloadedArticles;// array of ArticleHeaderData loaded in all groups
		List<string> blockedHeaders;			// array of header keywords to skip at loading
		bool downloadingCanceled=false;
        int nLoadedHeaders;                     // number of loaded headers
        int nFilteredHeaders;                   // number of blocked headers
        int lastDownloadedArticleId;
		byte[] lastDownloadedImageData;
		string lastDownloadedImageFileName="";
		string oldSubjectBase="";
		int numDaysToKeepHeaders;               // max num days to keep headers in group and downloaded store
		int maxHeadersToLoad;                   // max num headers to load in one batch
        int minImageFileLength;                 // min size of image file to load (ignore smaller)
        int nHeadersToSkip;                     // number of headers to skip at start loading
        public event OnNeedName onNeedName;
        public string RootName                  { get { return navigator.RootName; } }
        public string PaintExe                  { get { return navigator.PaintExe; } }
        public string EditorExe                 { get { return navigator.EditorExe; } }
        public string MediaExe                  { get { return navigator.MediaExe; } }
        public string MediaTmpLocation          { get { return navigator.MediaTmpLocation; } }
        public string ActiveImageName           { set { navigator.ActiveImageName = value; } }
        public string ArchiveExe                { get { return archiveExe; } }
        public Navigator Navigator              { get { return navigator; } }
		public HostGroup CurrentGroup			{ get { return currentGroup; } }
        public Group[] SubscribedGroups         { get { return subscribedGroups; } }
		public bool CancelingDownload			{ get { return downloadingCanceled; } }
        public ArticleHeader.Item.Collection Headers { get { if (currentGroup == null) return null; return currentGroup.Headers; } }
        public GroupManager(string logName) : base(null)
		{
            try
            {
                string d=Directory.GetCurrentDirectory();
                Common.XMLStore settings = new Common.XMLStore("Customization.xml");
                paintExe = settings.GetString("path.paint");
                editorExe = settings.GetString("path.editor");
                mediaExe = settings.GetString("path.media");
                archiveExe = settings.GetString("path.archive");
                numDaysToKeepHeaders = settings.GetInt("limit.daysToKeepHeaders", 30);
                maxHeadersToLoad = settings.GetInt("limit.headersToLoad", 50000);
                minImageFileLength = settings.GetInt("limit.imageFileLength", 20000);
                string server = settings.GetString("location.server", "news.west.cox.net");
                string root = settings.GetString("location.root", "../stuff/");
                string user = settings.GetString("location.user", "");
                string password = settings.GetString("location.password", "");
                settings.SaveToFile();
                if (server == null || server.Length == 0)
                    throw new Exception("NNTP server name is not specified");
                navigator = new Navigator();
                dataDirName =Navigator.Root.FullName + "groupData";
                hostDisplayName = server.Replace('.', '_');
                subscribedGroupFileName = DataFileName(hostDisplayName + "_groups.txt");
                serverGroupFileName = DataFileName(hostDisplayName + "_server_groups.txt");
                busyDataFileName = DataFileName(hostDisplayName + "_busy.txt");
                groupDataStoreDir = DataFileName(hostDisplayName);
                loadedArticlesFileName = Path.Combine(groupDataStoreDir, "downloaded");
                currentGroup = null;
                hostName = server;
                reader = new Reader(server, user, password, SendMessage);
                LoadSubscribedGroups();
                storage = new NewArticleStorage(Navigator.SpecDir(SpecName.NewArticles));
                articlesToDownload = new ArticleHeader.Item.Collection();
                newArticlesToDownload = new ArticleHeader.Item.Collection();
                downloadedArticles = new ArticleHeader.Item.Collection();
                LoadDownloadedHeaders(2);
                LoadBlockedHeaders();
                idle = true;
                processState = ProcessState.Disconnected;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
		}
		~GroupManager()							{ Cleanup(); }
        public void Cleanup()                   
        {
            Stop();
            //StoreBusyGroups(null, CurrentGroup);
            UpdateDownloadedHeadersStore(1);
        }
        void AddToDownloaded(ArticleHeader.Item ahi) 
        {
            ahi.MarkedForDownloading = false;
            downloadedArticles.Add(ahi);
            AppendDownloadedHeader(1, ahi);
        }
		string FileNameFromUserInput(string subjectBase, int id) 
		{
            //Console.WriteLine("FileNameFromUserInput called");
            string[] files = Directory.GetFiles(Navigator.SpecDir(SpecName.NewArticles).FullName);
            List<string> matches = new List<string>();
            foreach (string fp in files)
            {
                string fn = Path.GetFileName(fp);
                string[] nameParts = fn.Split(new char[] { '.' }, 3);
                if (nameParts.Length > 2 && nameParts[1].StartsWith("part") && subjectBase.Contains(nameParts[2]))
                    matches.Add(nameParts[2]);
            }
            string suggestion="";
            if (matches.Count > 0)
            {
                foreach (string fn in matches)
                {
                    if (suggestion.Length < fn.Length)
                        suggestion = fn;
                }
            }
            if (matches.Count == 1)
                return suggestion;
            if (suggestion.Length == 0)
                suggestion = subjectBase;
            //Console.WriteLine("suggestion='" + suggestion + "' subjectBase='" + subjectBase+"'");
            if (onNeedName != null)
                return onNeedName(suggestion, id);
			return "";
		}
		// hadling blocked subject
		void LoadBlockedHeaders()				
		{
            blockedHeaders = new List<string>();
			FileStream fs=new FileStream(DataFileName(blockedHeadersFileName), FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
			TextReader inStream=new StreamReader(fs);
			string l;
			while((l=inStream.ReadLine())!=null)
				blockedHeaders.Add(l);
			fs.Close();
		}
		bool IsBlockedHeader(ArticleHeader.Item h)   
		{
            if (h.SingleMessage && h.ByteCount < minImageFileLength)
				return true;
            string[] si = h.Subject.Split(' ');
            bool isPrevName = false;
            foreach(string s in si)
            {
                bool isName = s.Length >= 3;
                foreach (char c in s)
                    if (!char.IsUpper(c))
                    {
                        isName = false;
                        break;
                    }
                if (isName && isPrevName)
                    return true;        // 2 uppercase words in a row
                isPrevName = isName;
            }
			foreach(string headerKeywords in blockedHeaders)
			{
				if(h.Subject.ToLower().IndexOf(headerKeywords)>=0)
					return true;
			}
			return false;
		}
		// group list management
        protected void LoadSubscribedGroups()   
        {
            try
            {
                FileStream fs = new FileStream(subscribedGroupFileName,
                    FileMode.Open, FileAccess.Read, FileShare.Read);
                SoapFormatter sf = new SoapFormatter();
                subscribedGroups = (Group[])sf.Deserialize(fs);
                fs.Close();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                subscribedGroups = new Group[0];
            }
        }
        bool StoreSubscribedGroups(int attempt) 
        {
            try
            {
                FileStream fs = new FileStream(subscribedGroupFileName, FileMode.Create);
                SoapFormatter sf = new SoapFormatter();
                sf.Serialize(fs, subscribedGroups);
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
                return StoreSubscribedGroups(attempt - 1);
            }
        }
        List<Group> LoadOldServerGroups()          
        {
            string[] sgna=Common.TextFile.Read(serverGroupFileName);
            List<Group> sga = new List<Group>(sgna.Length);
            for (int i = 0; i < sgna.Length; i++)
                sga.Add(new Group(sgna[i], 0));
            return sga;
        }
        public void StoreServerGroups(GroupInfo[] groups)
        {
            if (groups == null || groups.Length == 0 || !serverGroupsChanged)
                return;
            StreamWriter sw= Common.TextFile.OpenNew(serverGroupFileName);
            foreach (GroupInfo group in groups)
                if(group.State>=0)
                    sw.WriteLine((string)group);
            sw.Close();
            serverGroupsChanged = false;
        }
        public GroupInfo[] CheckServerGroups(out List<Group> deletedSubscribedGroups)   // loads available groups
        {
            hostGroups = new List<Group>();
            reader.LoadAllGroups(new Reader.OnNewLine(GotNewGroupLine));
            List<Group> oldServerGroups = LoadOldServerGroups();
            Common.ListComparer<Group> lc = new Common.ListComparer<Group>(hostGroups, oldServerGroups, new Group.Comparer());
            List<Group> newGroups = lc.FirstOnly;
            List<Group> oldGroups = lc.FirstInSecond;
            List<Group> deletedGroups = lc.SecondOnly;
            if (newGroups.Count > 0 || deletedGroups.Count > 0)
                serverGroupsChanged = true;
            lc = new Common.ListComparer<Group>(oldGroups, new List<Group>(subscribedGroups), new Group.Comparer());
            deletedSubscribedGroups = lc.SecondOnly;
            GroupInfo[] gia = new GroupInfo[newGroups.Count + oldGroups.Count + deletedGroups.Count];
            int i = 0;
            foreach (string name in deletedGroups)
                gia[i++] = new GroupInfo(name);
            foreach (GroupInfo gi in newGroups)
            {
                gi.State = 1;
                gia[i++] = gi;
            }
            foreach (GroupInfo gi in oldGroups)
                gia[i++] = gi;
            return gia;
        }
        public void DeleteSubscribedGroups(string[] groupsToDelete)
        {
            int i = 0;
            List<Group> ga = new List<Group>();
            foreach (Group g in subscribedGroups)
            {
                bool deleted = false;
                foreach (string name in groupsToDelete)
                    if (string.Compare(g.Name, name, true) == 0)
                    {
                        HostGroup hg = new HostGroup(g, groupDataStoreDir);
                        File.Delete(hg.StoreFileName);
                        i++;
                        deleted = true;
                        if (CurrentGroup != null && CurrentGroup.Name != name)
                            CurrentGroup.MarkDeleted();
                        break;
                    }
                if (!deleted)
                    ga.Add(g);
            }
            ga.Sort(new Group.Comparer());
            subscribedGroups = ga.ToArray();
            if (StoreSubscribedGroups(4))
                SendMessage(i.ToString() + " groups deleted", Common.MessageType.Info);
            else
                SendMessage("Selected groups were NOT deleted", Common.MessageType.Warning);
        }
        public void ResetGroups(string[] groupsToReset)
        {
            foreach (Group g in subscribedGroups)
            {
               foreach (string name in groupsToReset)
                    if (string.Compare(g.Name, name, true) == 0)
                    {
                        g.Reset();
                        break;
                    }
            }
            StoreSubscribedGroups(4);
        }
        public void AddSubscribedDroups(Group[] groupsToAdd)
        {
            List<Group> ga = new List<Group>(subscribedGroups);
            ga.AddRange(groupsToAdd);
            ga.Sort(new Group.Comparer());
            subscribedGroups = ga.ToArray();
            if (StoreSubscribedGroups(4))
                SendMessage(groupsToAdd.Length.ToString() + " groups added", Common.MessageType.Info);
            else
                SendMessage("Selected groups were NOT added", Common.MessageType.Warning);
        }
        List<string> GetBusyGroups()				// loads busy groups
		{
            List<string> busyGroups = new List<string>();
			FileStream fs=new FileStream(busyDataFileName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
			TextReader inStream=new StreamReader(fs);
			string l;
			while((l=inStream.ReadLine())!=null)
				busyGroups.Add(l);
			fs.Close();
			return busyGroups;
		}
        //bool GroupBusy(Group gb)				// returns true if group is busy
        //{
        //    string newGroup = gb.ShortName;
        //    ArrayList busyGroups=GetBusyGroups();
        //    foreach(string group in busyGroups)
        //        if(newGroup==group)
        //            return true;
        //    return false;
        //}
        //void StoreBusyGroups(Group busyGB, Group freeGB)
        //{
        //    string busy="";
        //    if(busyGB!=null)
        //        busy = busyGB.ShortName;
        //    string free="";
        //    if(freeGB!=null)
        //        free = freeGB.ShortName;
        //    ArrayList busyGroups=GetBusyGroups();
        //    ArrayList newBusy=new ArrayList(busyGroups.Count+1);
        //    bool alreadyBusy=false;
        //    foreach(string group in busyGroups)
        //    {
        //        if(free!=group)
        //            newBusy.Add(group);
        //        if(busy==group)
        //            alreadyBusy=true;
        //    }
        //    if(!alreadyBusy && busyGB!=null)
        //        newBusy.Add(busy);
        //    FileStream fs=new FileStream(busyDataFileName, FileMode.Create, FileAccess.Write, FileShare.None);
        //    TextWriter outStream=new StreamWriter(fs);
        //    foreach(string group in newBusy)
        //        outStream.WriteLine(group);
        //    outStream.Flush();
        //    fs.Close();
        //}
        public bool SetCurrentGroup(Group group)// sets current group to new value and updates busy groups
		{
            //if (GroupBusy(group))
            //{
            //    SendMessage("Group " + group.ShortName + " is busy", Common.MessageType.Warning);
            //    return false;
            //}
            //StoreBusyGroups(group, CurrentGroup);
            if (CurrentGroup != null && CurrentGroup.Name != group.Name)
				StoreGroupData();
            if (CurrentGroup == null || CurrentGroup.Name != group.Name)
			{
				lastDownloadedArticleId=0;
                currentGroup = new HostGroup(group, groupDataStoreDir);
			}
			return true;
		}
        // connecting to group
        public void StartConnectGroup()         { StartOperation(new ThreadStart(ConnectGroup)); }
        protected void ConnectGroup()	        // connects to current group
		{
            string error = "";
			int n=0;
			try
			{
                n = currentGroup.GetStoredHeaders(downloadedArticles);
			    if(n<=0)
                    SendMessage("Error reading " + CurrentGroup.ShortName + " headers: " + error, Common.MessageType.Warning);
                if (reader.ConnectServerGroup(currentGroup.FullName, currentGroup.ShortName, currentGroup.MaxArticleID, n, 100) == State.Connected)
                {
                    currentGroup.LastArticleIdOnServer = reader.LastArticleId;
                    currentGroup.FirstArticleIdOnServer = reader.FirstArticleId;
                }
			}
			catch(Exception ex)
			{
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                error = ex.Message;
			}
			idle=true;
		}
		public void Reconnect()					
		{
            if (ProcessState == ProcessState.Connecting)
                return;
            idle = false;
			if(reader.ReconnectGroup()!=State.Connected)
                SendMessage("Failed to reconnect group " + CurrentGroup.ShortName, Common.MessageType.Warning);
			idle=true;
		}
		// updating headers
        public void StartLoadingNewHeaders(bool filter, int skip) // asynchronously starts LoadNewHeaders
		{
            nLoadedHeaders = 0;
            nHeadersToSkip = maxHeadersToLoad * skip;
            nFilteredHeaders = filter ? 0 : -1;
            StartOperation(new ThreadStart(LoadNewHeaders));
		}
        protected void LoadNewHeaders()	        // updates group headers
		{
			downloadingCanceled=false;
			try
			{
                ConnectGroup();
			    firstDateToKeep=DateTime.Now.AddDays(-numDaysToKeepHeaders);
			    int firstToLoad=Math.Max(currentGroup.MaxArticleID+1, currentGroup.FirstArticleIdOnServer);
                firstToLoad += nHeadersToSkip;
                numHeadersToLoad = Math.Min(currentGroup.LastArticleIdOnServer - firstToLoad + 1, maxHeadersToLoad);
                if (numHeadersToLoad <= 0)
                {
                    SendMessage("Nothing to load: last server articleId [" + currentGroup.LastArticleIdOnServer +
                        "] < first to load [" + firstToLoad+']', Common.MessageType.Warning);
                    return;
                }
                string l = reader.DownloadHeaders(firstToLoad, firstToLoad + numHeadersToLoad, new Reader.OnNewLine(GotNewHeaderLine));
                string loadInfo = currentGroup.ShortName + ": downloaded " + nLoadedHeaders + ", filtered " + nFilteredHeaders;
                if (l == ".")
                    SendMessage(loadInfo + " (all requested)", Common.MessageType.Info);
                else if (l == "c")
                    SendMessage(loadInfo + " (download canceled)", Common.MessageType.Info);
                else
                    SendMessage(loadInfo + " (ptoblem: " + l + " )", Common.MessageType.Error);
                currentGroup.RemoveOldHeaders(firstDateToKeep);
                CurrentGroup.StorredMaxArticleId = CurrentGroup.MaxArticleID;
				StoreGroupData();
			}
			catch(Exception ex)
			{
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                SendMessage("Error storing group update: " + ex.Message, Common.MessageType.Error);
			}
			idle=true;
		}
        int GotNewHeaderLine(string l)			// callback from UpdateHeaders
		{
            ArticleHeader.Item lastHeader = new ArticleHeader.Item(l, currentGroup.ShortName);
            bool blocked = nFilteredHeaders >= 0 && IsBlockedHeader(lastHeader);
            if (lastHeader.NotLoaded && !blocked)// valid header not blocked header
            {
                nLoadedHeaders++;
				currentGroup.AddNewHeader(lastHeader);
                if (nLoadedHeaders / 1000 * 1000 == nLoadedHeaders)
                    SendMessage(CurrentGroup.ShortName + ": downloaded " + nLoadedHeaders + ", filtered " + nFilteredHeaders
                        + " of " + numHeadersToLoad + " headers downloaded", Common.MessageType.Info);
            }
            else
                nFilteredHeaders++;
			if(downloadingCanceled==true)
				return -1;
			return lastHeader.ArticleId;
		}
		// article downloading
        public void MarkUnread(ArticleHeader.Item ahi)
        {
            if (ahi.SingleMessage)
            {
                ahi.ArticleId = ahi.TrueID;
                ahi.Updated = true;
                return;
            }
            ArticleHeader.Item.Collection filteredHeaders = currentGroup.FilterHeaders(ahi.SubjectBase);
            filteredHeaders.Sort(new ArticleHeader.IDComparer());
            if (filteredHeaders.Count < 2)
            {	// single part article
                ahi.SetSingleMessage();
                ahi.ArticleId = ahi.TrueID;
                ahi.Updated = true;
                return;
            }
            foreach (ArticleHeader.Item ahin in filteredHeaders)
            {
                if (ahin.PartOf(ahi))
                {
                    ahin.ArticleId = ahin.TrueID;
                    ahin.Updated = true;
                }
            }
        }
		public void AddToDownloadList(ArticleHeader.Item ahi)
		{
            lock (newArticlesToDownload)
            {
                if (ahi.SingleMessage)
                {	// single part article
                    ahi.MarkedForDownloading = true;
                    newArticlesToDownload.AddUnique(ahi);
                    return;
                }
                ArticleHeader.Item.Collection filteredHeaders = currentGroup.FilterHeaders(ahi.SubjectBase);
                filteredHeaders.Sort(new ArticleHeader.SubjectComparer());
                if (filteredHeaders.Count < 2)
                {	// single part article
                    ahi.SetSingleMessage();
                    ahi.MarkedForDownloading = true;
                    newArticlesToDownload.AddUnique(ahi);
                    return;
                }
                ArticleHeader.Item.Collection parts = new ArticleHeader.Item.Collection();
                foreach (ArticleHeader.Item ahii in filteredHeaders)
                {
                    if (ahii.PartOf(ahi))
                        parts.Add(ahii);
                }
                if (parts.Count < ahi.PartNum)
                {
                    SendMessage("Previous parts of " + ahi.Subject + " are missing", Common.MessageType.Warning);
                    return;
                }
                foreach (ArticleHeader.Item ahii in parts)
                    if (ahii.NotLoaded)		// add all not loaded parts
                    {
                        ahii.MarkedForDownloading = true;
                        newArticlesToDownload.AddUnique(ahii);
                    }
            }
		}
		public void CancelDownloading()			
		{
			downloadingCanceled=true;
            foreach (ArticleHeader.Item ahi in articlesToDownload)
                ahi.MarkedForDownloading = false;
            lock (newArticlesToDownload)
            {
                foreach (ArticleHeader.Item ahi in newArticlesToDownload)
                    ahi.MarkedForDownloading = false;
			    newArticlesToDownload.Clear();
            }
		}
        public void StartDownloading()		    // asynchronously starts DownloadArticles
		{
            if (idle && (articlesToDownload.Count > 0 || newArticlesToDownload.Count > 0))
			{
                //ArticleHeader.Item.Collection articlesNotLoaded = new ArticleHeader.Item.Collection();
                //foreach (ArticleHeader.Item ahi in articlesToDownload)
                //{	// add first previously missed articles
                //    if (ahi.ArticleId > 0 && ahi.MarkedForDownloading)
                //        articlesNotLoaded.AddUnique(ahi);
                //}
                //lock (newArticlesToDownload)
                //{
                //    foreach (ArticleHeader.Item ahi in newArticlesToDownload)
                //        articlesNotLoaded.AddUnique(ahi);
                //    newArticlesToDownload.Clear();
                //    if (articlesNotLoaded.Count > 0)
                //    {
                //        articlesToDownload = new ArticleHeader.Item.Collection(articlesNotLoaded);
                //        StartOperation(new ThreadStart(DownloadArticles));
                //    }
                //}
                articlesToDownload.Clear();
                foreach (ArticleHeader.Item ahi in articlesToDownload)
                {	// add first previously missed articles
                    if (ahi.MarkedForDownloading && ahi.NotLoaded)
                        articlesToDownload.AddUnique(ahi);
                }
                lock (newArticlesToDownload)
                {
                    foreach (ArticleHeader.Item ahi in newArticlesToDownload)
                        articlesToDownload.AddUnique(ahi);
                    newArticlesToDownload.Clear();
                    if (articlesToDownload.Count > 0)
                        StartOperation(new ThreadStart(DownloadArticles));
                }
            }
		}
        void DownloadArticles()			        // downloads selected articles
		{
			downloadingCanceled=false;
            int failedPartNumber = -1;
			foreach(ArticleHeader.Item ahi in articlesToDownload)
			{
                string subjectBase = ahi.SubjectBase;
                if (failedPartNumber>=0)
                {
                    if (oldSubjectBase != subjectBase)
                        failedPartNumber = -1;
                    else if (failedPartNumber < ahi.PartNum)
                    {
                        ahi.EarlierPartMissing = true;
                        ahi.MarkedForDownloading = false;
                        continue;
                    }
                }
				if(downloadingCanceled)
					break;
				int articleId=ahi.ArticleId;
				if(articleId==0)
					continue;
				if(articleId<0)
				{
					string fileName;
                    byte[] data = storage.Load(TempPrefix(-articleId), out fileName);
					if(data!=null && fileName!="")
                        AddToDownloaded(ahi);
					continue;
				}
				int attempts=3;
				string article;
				while((article=reader.DownloadArticle(ahi)).Length==0 && attempts-->0);
                if (article.Length > 1)
                {
                    MessageDecoder me = new MessageDecoder();
                    byte[] ba = me.GetDecodedData(article, ahi.PartNum);
                    if (ba != null && ba.Length > 0)
                    {
                        try
                        {
                            if (me.DataName.Length > 0)
                                lastDownloadedImageFileName = me.DataName;
                            else if (oldSubjectBase != subjectBase)
                                lastDownloadedImageFileName = "";
                            if (lastDownloadedImageFileName == "")
                            {
                                lastDownloadedImageFileName = FileNameFromUserInput(subjectBase, articleId);
                            }
                            if (lastDownloadedImageFileName == "")
                                throw new Exception("No name stecified");
                            lastDownloadedImageData = ba;
                            ahi.ResetPartNumber(me.PartNumber);		// part number from data
                            if (lastDownloadedImageFileName == "")
                            {
                                lastDownloadedImageFileName = "part" + ahi.PartNum + "of" + ahi.NumParts + ".dat";
                                ahi.SetSingleMessage();				// to store as a single file
                            }
                            if (ahi.SingleMessage)
                                ahi.ArticleFileName = storage.Save(TempPrefix(articleId),
                                    lastDownloadedImageFileName, lastDownloadedImageData);
                            else
                            {
                                ahi.ArticleFileName = storage.SavePart(TempPrefix(articleId),
                                    lastDownloadedImageFileName, lastDownloadedImageData, ahi.PartNum, ahi.NumParts);
                                if (ahi.LastInSequence)
                                    lastDownloadedImageData = storage.Load(TempPrefix(articleId), out lastDownloadedImageFileName);
                                else
                                    lastDownloadedImageData = null;
                            }
                        }
                        catch (Exception ex)
                        {
#if DEBUG
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
#endif
                            SendMessage(ex.Message, Common.MessageType.Warning);
                        }
                    }
                    else
                    {
                        lastDownloadedImageData = null;
                        lastDownloadedImageFileName = "";
                        SendMessage("No image data found in " + articleId + ": message saved", Common.MessageType.Warning);
                    }
                    ahi.ArticleId = -articleId;
                    ahi.Updated = true;
                    lastDownloadedArticleId = articleId;
                    AddToDownloaded(ahi);
                }
                else if (article.Length == 1 && article[0] == 'N')
                {
                    failedPartNumber = ahi.PartNum;   // prevent loading unusable parts
                    ahi.DownloadingFailed = true;     // temporary or permanently not available
                    ahi.MarkedForDownloading = false;
                }
                oldSubjectBase = subjectBase;
			}
			idle=true;
            if (newArticlesToDownload.Count > 0)
                StartDownloading();
		}
        // group management
		public void RemoveDeletedHeaders()		// removes headers marked as unavailable (ArticleId=0)
		{
			int n=currentGroup.RemoveDeletedHeaders();
            SendMessage(currentGroup.ShortName + ": " + CurrentGroup.Count + " headers (" + n + " removed).", Common.MessageType.Info);
		}
        public void RemoveOldHeaders(int cutID) 
        {
            int n = currentGroup.RemoveOldHeaders(cutID);
            SendMessage(currentGroup.ShortName + ": " + CurrentGroup.Count + " headers (" + n + " removed).", Common.MessageType.Info);
        }
		void StoreGroupData()	            	
		{
			if(!currentGroup.StoreHeaders(5))
                SendMessage("Group " + currentGroup.ShortName + " failed to store headers", Common.MessageType.Warning);
            foreach (Group group in subscribedGroups)
            {
                if (currentGroup.Name == group.Name)
                {
                    group.StorredMaxArticleId = currentGroup.StorredMaxArticleId;
                    //group.StorredMaxArticleId = currentGroup.MaxArticleID;
                    break;
                }
            }
            if (!StoreSubscribedGroups(10))
                SendMessage("Group " + currentGroup.ShortName + " failed to store group base", Common.MessageType.Warning);
		}
		void LoadDownloadedHeaders(int attempt)	
		{
            FileStream fs = null;
			try
			{
				fs=new FileStream(loadedArticlesFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
				BinaryFormatter f=new BinaryFormatter();
				while(fs.Position < fs.Length)
					downloadedArticles.Add((ArticleHeader)f.Deserialize(fs));
				fs.Close();
			}
            catch (Exception ex)
			{
                if(fs!=null)
                    fs.Close();
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                if (attempt < 0)
					return;
				Thread.Sleep(100);
				LoadDownloadedHeaders(attempt-1);
			}
		}
		bool AppendDownloadedHeader(int attempt, ArticleHeader.Item ahd)
		{
            FileStream fs = null;
			try
			{
                fs = new FileStream(loadedArticlesFileName, FileMode.Append, FileAccess.Write);
				BinaryFormatter f=new BinaryFormatter();
                ArticleHeader ah = new ArticleHeader(ahd);  // required for serialization
                f.Serialize(fs, ah);
				fs.Close();
				return true;
			}
            catch (Exception ex)
			{
                if (fs != null)
                    fs.Close();
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                if (attempt < 0)
					return false;
				Thread.Sleep(100);
				return AppendDownloadedHeader(attempt-1, ahd);
			}
		}	
		public bool UpdateDownloadedHeadersStore(int attempt)
		{
			DateTime firstDate=DateTime.Today.AddDays(-numDaysToKeepHeaders);
            ArticleHeader.Item.Collection newaha = new ArticleHeader.Item.Collection();
			foreach(ArticleHeader ahd in downloadedArticles)
				if(ahd.Date>=firstDate)
					newaha.Add(ahd);
			downloadedArticles=newaha;
            FileStream fs = null;
            try
			{
				fs=new FileStream(loadedArticlesFileName, FileMode.Create, FileAccess.Write);
				BinaryFormatter f=new BinaryFormatter();
                foreach (ArticleHeader ahd in downloadedArticles)
                {
                    ArticleHeader ah = new ArticleHeader(ahd);
                    f.Serialize(fs, ah);
                }
				fs.Close();
				return true;
			}
            catch (Exception ex)
			{
                if (fs != null)
                    fs.Close();
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                if (attempt < 0)
					return false;
				Thread.Sleep(100);
				return UpdateDownloadedHeadersStore(attempt-1);
			}
		}
        public void ResetHeaders(string pattern, HostGroup group)
		{
            group.Headers=group.FilterHeaders(pattern);
            group.Headers.Sort(new ArticleHeader.IDComparer());
            SendMessage(group.Count.ToString() + " messages containing '" + pattern + "' in group " + group.ShortName, Common.MessageType.Info);
		}
        string TempPrefix(int id)               { return currentGroup.ShortName + id+'.'; }
        public string TempFileName(int id, string name) { return storage.TempFileName(TempPrefix(id), name); }
	}
}
