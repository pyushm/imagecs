using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace NNTP
{
	public enum State
	{
		Invalid,
		Disconnected,
		Connecting,
		Connected
	}
    public delegate void OnMessage(string message, Common.MessageType level);
	public class NNTPClient : TcpClient
	{   // initializes and connects (do not need to call Connect)
		public NNTPClient(string host, int port) : base(host, port) { }
	}
	public class Reader
	{
      //1xx - Informative message
      //2xx - Command ok
      //3xx - Command ok so far, send the rest of it.
      //4xx - Command was correct, but couldn't be performed for
      //      some reason.
      //5xx - Command unimplemented, or incorrect, or a serious
      //      program error occurred.
 
      //x0x - Connection, setup, and miscellaneous messages
      //x1x - Newsgroup selection
      //x2x - Article selection
      //x3x - Distribution functions
      //x4x - Posting
      //x8x - Nonstandard (private implementation) extensions
      //x9x - Debugging output

      //100 help text
      //190
      //  through
      //199 debug output

      //200 server ready - posting allowed
      //201 server ready - no posting allowed

      //400 service discontinued

      //500 command not recognized
      //501 command syntax error
      //502 access restriction or permission denied
      //503 program fault - command not performed

        public delegate int OnNewLine(string line);
        static int connectionPort = 119;			// NNTP connection port
		bool postingAllowed;
        bool stop;
		NNTPClient connection;					// network connection
        NetworkStream networkStream = null;     // stream of current connection
        State hostState;						// host connection status
		string hostName;						// server location
        string user;
        string password;
		int Timeout=30000;						// connection timeout (default to 30 second)
		string lastMessage="";					// last message (only different message sent)
		string groupName="";
		string displayGroupName="";
		int lastStorredArticleId=0;
		int firstArticleId =0;					// current group first article id on the server 
		int lastArticleId =0;					// current group last article id on the server 
		int lastArticleIdLoaded;				// last loaded article id to the current group
		public event OnMessage onMessageEvent;
		public int FirstArticleId				{ get { return firstArticleId; } }
		public int LastArticleId				{ get { return lastArticleId; } }
		public bool Valid						{ get { return hostState!=State.Invalid; } }
        public bool Stop                        { get { return stop; } set { stop = value; } }
        public bool IsPostingAllowed            { get { return postingAllowed; } }
		public State HostState					{ get { return hostState; } }
		public string HostName					{ get { return hostName; } }
        public bool IsConnected                 { get { return hostState == State.Connected && connection!=null && connection.Connected; } }

        public Reader(string hostName_, string user_, string password_, OnMessage onMessage)
		{
            hostName = hostName_;
            user = user_;
            password = password_;
            onMessageEvent += onMessage;
			hostState = State.Disconnected;
		}
        NetworkStream GetNetworkStream()			
		{
            if (connection != null && connection.Connected && networkStream != null)
                return networkStream;
            networkStream = null;
			if(connection==null)
				return null;
			try
			{
                networkStream = connection.GetStream();
			}
			catch(Exception err)
			{
                SendMessage("Stream error: " + err.Message, Common.MessageType.Warning);
			}
            return networkStream;
		}
        void SendMessage(string msg, Common.MessageType level)
		{
			if(msg.Length==0 || msg==lastMessage)
				return;
			lastMessage=msg;
			if(onMessageEvent!=null)
				onMessageEvent(msg, level);
		}
        public bool ConnectHost(int maxConnectionAttempts)		        
		{	// connects to host on current thread
            if (IsConnected)
                return true;
            stop = false;
			int attempts=maxConnectionAttempts;
			hostState = State.Connecting;
			try
			{
                while (!IsConnected && --attempts >= 0)
				{
                    if (stop)
                        return false;
					ConnectHostAttempt();
                    if (!IsConnected)
					{
                        Thread.Sleep(5000);
						int na=maxConnectionAttempts-attempts;
                        SendMessage("Connecting " + hostName + ": " + na + " attempts", Common.MessageType.Info);
					}
				}
                if (IsConnected)
					return true;
				hostState=State.Disconnected;
                if (attempts <= 0)
                {
                    SendMessage("Unable to connect to " + hostName + ": attempts exeeded " + maxConnectionAttempts, Common.MessageType.Error);
                    return false;
                }
                SendMessage("Unable to connect to " + hostName + ": " + lastMessage, Common.MessageType.Error);
                return false;
            }
			catch(Exception err)
			{
				hostState=State.Disconnected;
				SendMessage("Not connected: "+err.Message, Common.MessageType.Error);
                return false;
            }
		}
		void ConnectHostAttempt()				
		{	// connects to host attempt
            hostState = State.Connecting;
            try { connection = new NNTPClient(hostName, connectionPort); }
			catch(Exception ex)
            {
                SendMessage(hostName + " not responding: " + ex.Message, Common.MessageType.Warning);
                return;
            }
			connection.ReceiveTimeout = Timeout;
			// connect the stream
			NetworkStream s = connection.GetStream();
			StreamReader r = new StreamReader( s );
			// read the welcome message
			string welcome = r.ReadLine();
            if (welcome == null)
			{
				SendMessage("Didn't get server response." , Common.MessageType.Warning);
			}
            else if (!welcome.StartsWith("2"))
            {
                SendMessage("Unexpected server response.  " + welcome, Common.MessageType.Warning);
                Disconnect();
            }
            else
            {
                if (welcome.StartsWith("201"))
                    postingAllowed = false;
                else if (welcome.StartsWith("200"))
                    postingAllowed = true;
                hostState = State.Connected;
            }
		}
        public void Disconnect()			    
		{
            if (IsConnected)
            {
                //NetworkStream ns = null;
                //try
                //{
                //    ns = connection.GetStream();
                //    StreamWriter w = new StreamWriter(ns);
                //    if (IsConnected)
                //    {
                //        w.AutoFlush = true;
                //        w.WriteLine("QUIT");
                //    }
                //}
                //finally
                //{
                //    try
                //    {
                //        StreamReader r = new StreamReader(ns);
                //        r.ReadToEnd();
                //    }
                //    catch { }
                //    try { connection.Close(); }
                //    catch { }
                //}
                try
                {
                    connection.Client.Shutdown(SocketShutdown.Both);
                    connection.Client.Disconnect(false);
                    if(networkStream != null)
                        networkStream.Close();
                    connection.Close();
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
            }
            connection = null;
            networkStream = null;
			hostState=State.Disconnected;
		}
		public bool AuthenticateUser(StreamReader sr, StreamWriter sw)
		{
            sr = new StreamReader(GetNetworkStream());
            sw = new StreamWriter(GetNetworkStream());
            sw.AutoFlush = true;
			string response;
			sw.WriteLine( "AUTHINFO USER " + user );
            response = sr.ReadLine();
			if(!response.StartsWith( "381" ) )
			{
                SendMessage("Unknown user.  " + response, Common.MessageType.Warning);
				return false;
			}
			sw.WriteLine( "AUTHINFO PASS " + password );
            response = sr.ReadLine();
            if (!response.StartsWith("281"))
			{
                SendMessage("Password check failed for user " + user + ": " + response, Common.MessageType.Warning);
				return false;
			}
			SendMessage("User authenticated", Common.MessageType.Info);
			return true;	// success
		}
		public State ReconnectGroup()			
		{
            Disconnect();
            return ConnectServerGroup(groupName, displayGroupName, lastStorredArticleId, -1, 100);
        }
        public string LoadAllGroups(OnNewLine OnNewGroupCallback)
        {
            if (!ConnectHost(1))
            {
                SendMessage("Unable to connect to host", Common.MessageType.Warning);
                return "Unable to connect";
            }
            NetworkStream ns = GetNetworkStream();
            if (ns == null)
                return "";
            string response = "";
            StreamReader r = new StreamReader(ns);
            StreamWriter w = new StreamWriter(ns);
            w.AutoFlush = true;
            int count = 0;
            int totalCount = 0;
            //int count = 0;
            try
            {
                w.WriteLine("LIST");
                response = r.ReadLine();
                if (response.StartsWith("480"))
                {
                    if (AuthenticateUser(r, w))
                    {
                        w.WriteLine("LIST");
                        response = r.ReadLine();
                    }
                    else
                        return "Unable to authenticate user";
                }
                if (!response.StartsWith("215"))				// unexpected responce
                {
                    SendMessage("Unexpected responce: " + response, Common.MessageType.Warning);
                    try { r.ReadToEnd(); }
                    catch { }
                    return "Unexpected responce";
                }
                while (true)							// command valid, group list to follow
                {
                    response = r.ReadLine();				// read header
                    if (response == null || response == "." || response == "")	// check if finished
                    {
                        SendMessage("Downloaded " + count + " groups of " + totalCount+" total.", Common.MessageType.Info);
                        response = "okay";
                        break;
                    }
                    totalCount++;
                    count += OnNewGroupCallback(response);	// sending line back to manager
                }
            }
            catch (Exception err)
            {
                response = "Interrupted";
                if (!err.Message.StartsWith("Object reference"))
                    SendMessage("Downloading headers interupted: " + err.Message, Common.MessageType.Warning);
            }
            return response;
        }
		public State ConnectServerGroup(string name, string display, int lastDownloaded, int nStorred, int maxConnectionAttempts)
		{	// connects to group on current thread
			lastStorredArticleId=lastDownloaded;
			groupName=name;
			displayGroupName=display;
			State state=State.Disconnected;
			SendMessage("Connecting to "+displayGroupName, Common.MessageType.Info);
			int attempts=maxConnectionAttempts;
			try
			{
				do
				{
					int na=maxConnectionAttempts-attempts;
					SendMessage("Connecting to "+displayGroupName+": "+na+" attempts", Common.MessageType.Info);
                    if (!IsConnected)
                        ConnectHost(maxConnectionAttempts);
                    if (IsConnected)
						state=ConnectGroupAttempt();
                    if (state == State.Invalid)
                        return State.Invalid;
                    if (!IsConnected)
                        Thread.Sleep(5000);
				} while(state!=State.Connected && --attempts>=0);
				if(state==State.Connected) 
				{
					if(lastStorredArticleId<firstArticleId)
						lastStorredArticleId=firstArticleId;
					int n=lastArticleId-lastStorredArticleId;
					if(nStorred>=0)
						SendMessage("Connected to " + displayGroupName + ": "+nStorred+" messages, "+n+" new available", Common.MessageType.Info);
					return State.Connected;
				}
				if(attempts<=0)
					SendMessage("Number of attempts exeeded "+maxConnectionAttempts+": "+lastMessage, Common.MessageType.Warning);
				else
					SendMessage("Not connected: "+lastMessage, Common.MessageType.Warning);
				return State.Disconnected;
			}
			catch(Exception err)
			{
				SendMessage("Not connected: "+err.Message, Common.MessageType.Error);
				return State.Disconnected;
			}
		}
		State ConnectGroupAttempt()				
		{
            NetworkStream ns = GetNetworkStream();
			if(ns==null)
				return State.Disconnected;
			try
			{
				StreamReader r = new StreamReader( ns );
                StreamWriter w = null;
                try
                {
                    w = new StreamWriter(ns);
                    w.AutoFlush = true;
                    w.WriteLine("GROUP " + groupName);
                }
                catch
                {
                    Disconnect();
                }
				string strResponse = r.ReadLine();
                // group select success
				if( strResponse.StartsWith( "480" ) )
				{
                    if (AuthenticateUser(r, w))
                    {
                        w.WriteLine("GROUP " + groupName);
                        strResponse = r.ReadLine();
                    }
                    else
                    {
                        Disconnect();
                        return State.Disconnected;
                    }
                }
				if( strResponse.StartsWith( "211" ) )
				{
					string[] MessageNumbers = strResponse.Split( ' ' );
//					estimatedCount = Convert.ToInt32( MessageNumbers[1] );
					firstArticleId = Convert.ToInt32( MessageNumbers[2] );
					lastArticleId = Convert.ToInt32( MessageNumbers[3] );
					return State.Connected;
				}
				else
				{
					// group does not exist
					if( strResponse.StartsWith( "411" ) )
					{
						SendMessage("Server does not have group "+groupName, Common.MessageType.Warning);
						return State.Invalid;
					}
					else
					{
                        Disconnect();
						SendMessage("Unexpected response: " + strResponse, Common.MessageType.Warning);
						return State.Invalid;
					}
				}
			}
			catch(Exception err)
			{
				if(!err.Message.StartsWith("Object reference"))
					SendMessage("Failed connecting to group: " + err.Message, Common.MessageType.Warning);
				return State.Invalid;
			}
		}
        public string DownloadHeaders(int minArticleId, int maxArticleId, OnNewLine OnNewLineCallback)
		{	// loads new group headers
			string l="Not loaded";						// end reading status ("." - all read, "c" -canceled)
			lastArticleIdLoaded=minArticleId-1;
			int attempts=10;
			while(l.Length!=1 && lastArticleIdLoaded<maxArticleId)
			{
                if (!IsConnected)
				{
					if(attempts--<0)
					{
						l="Unable to connect";
						break;
					}
					SendMessage("Group "+displayGroupName+" disconnected", Common.MessageType.Warning);
					if(ReconnectGroup()!=State.Connected)
						continue;
				}
                l = DownloadHeadersAttempt(lastArticleIdLoaded + 1, maxArticleId, OnNewLineCallback);
			}
            return l;
		}
        string DownloadHeadersAttempt(int firstID, int lastID, OnNewLine OnNewLineCallback)
		{
			if(firstID<lastID)
				SendMessage(displayGroupName+": downloading headers.", Common.MessageType.Info);
			string XOVERCommand = "XOVER " + firstID + "-" + lastID;
            NetworkStream ns = GetNetworkStream();
			if(ns==null)
				return "";
			string lRet="";
			StreamReader r = new StreamReader( ns );
			StreamWriter w = new StreamWriter( ns );
			w.AutoFlush = true;
			try
			{
				w.WriteLine( XOVERCommand );			// send XOVER command
				lRet=r.ReadLine();
				if(!lRet.StartsWith( "224" ) )				// unexpected responce
				{
					SendMessage("Unexpected responce: "+lRet, Common.MessageType.Warning);
					try { r.ReadToEnd(); }
					catch { }
					return "Unexpected responce";
				}
				while (true)							// command valid, group list to follow
				{	
					lRet = r.ReadLine();				// read header
					if( lRet == "." )					// check if finished
						break;
                    int ret = OnNewLineCallback(lRet);	// sending line back to manager
                    if (ret >= 0)						// normal reading loop
						lastArticleIdLoaded=ret;	
					else								// handling cancel loading signal
					{
						lRet = "c";
						break;
					}
				}
				if(firstID>=lastID)						// to stop the cycle for one header
					lastArticleIdLoaded++;
			}
			catch(Exception err)
			{
				lRet="Interrupted";
				if(!err.Message.StartsWith("Object reference"))
					SendMessage("Downloading headers interupted: "+err.Message, Common.MessageType.Warning);
			}
			return lRet;
		}
		public string DownloadArticle(ArticleHeader.Item header)
		{
			return RetrieveArticle("BODY ", "222", header);
//			return RetrieveArticle("ARTICLE ", "220", header);
//			return RetrieveArticle("HEAD ", "221", header);
		}
		string RetrieveArticle(string cmd, string expectedResponse, ArticleHeader.Item header)
		{	// loads article
			string l="";
			while(true)
			{
                if (stop)
                    return "";
                if (!IsConnected)
				{
					SendMessage(displayGroupName+" disconnected", Common.MessageType.Warning);
					if(ReconnectGroup()!=State.Connected)
						continue;
				}
				l=RetrieveArticleAttempt(cmd, expectedResponse, header);
				if(l.Length>1)
				{
					SendMessage("Downloaded article "+header.ArticleId+": "+header.Subject, Common.MessageType.Info);
					break;
				}
				else if(l=="N")
				{
					SendMessage("Message "+header.ArticleId+" is not available on the server.", Common.MessageType.Warning);
					break;
				}
				else if(l=="T")
				{
                    SendMessage(displayGroupName + " lost connection", Common.MessageType.Warning);
                    if (ReconnectGroup() != State.Connected)
                        continue;
				}
				else
				{
					Thread.Sleep(2000);
                    if (IsConnected)
						break;
				}
			}
			return l;
		}
		string RetrieveArticleAttempt(string command, string expectedResponse, ArticleHeader.Item header)
		{
            NetworkStream ns = GetNetworkStream();
			if(ns==null)
				return "";
			int totalLineCount=header.LineCount;
			if(totalLineCount<20)
				totalLineCount=Math.Max(header.ByteCount/45, 100);
			int increment=totalLineCount/10;
            StreamReader r;
            StreamWriter w; 
            try
            {
                r = new StreamReader(ns, System.Text.Encoding.GetEncoding("iso-8859-1"));
                w = new StreamWriter(ns);
                w.AutoFlush = true;
            }
            catch (Exception err)
            {
                SendMessage("Lost connection: " + err.Message, Common.MessageType.Warning); 
                return "";
            }
			try
			{
				string l;
				try 
				{ 
					w.WriteLine(command+header.ArticleId);
					l= r.ReadLine();
					if(l==null)
						return "T";
				}
				catch 
				{ 
					return "T"; 
				}
				if(!l.StartsWith(expectedResponse))	// unexpected responce
				{
                    if (l.StartsWith("423") || l.StartsWith("430"))
						return "N";
					else if(l.StartsWith("400"))
						return "T";
                    //try { r.ReadToEnd(); }
                    //catch { }
                    SendMessage("Unexpected responce: "+l, Common.MessageType.Warning);
                    Disconnect();
					return "";
				}
				else
				{	// command valid, group list to follow
					bool Done = false;
					int lineCount = 0;
					StringBuilder messageBody = new StringBuilder(100000);
					do
					{
						if(lineCount%increment==0)
						{
							int pc=10*(10*(lineCount+1)/totalLineCount);
							SendMessage("Downloading article "+header.ArticleId+": "+header.Subject+" ["+pc+"% done]", Common.MessageType.Info);
						}
						l = r.ReadLine();	// read header
						if( l == "." )		// check if finished
							Done = true;
						else
						{
							// remove padded '.'
							if( l.StartsWith("..") )
								l = l.Remove(0,1);
							// line retrieved, append to body
							lineCount++;
							messageBody.Append( l );
							messageBody.Append( "\r\n" );
							//if(onNewArticleLine != null)
							//	onNewArticleLine(l);
						}
					} while (!Done);
					return messageBody.ToString();
				}
			}
			catch(Exception err)
			{
				if(!err.Message.StartsWith("Object reference") && !err.Message.StartsWith("Unable to read"))
					SendMessage("Downloading message interupted: "+err.Message, Common.MessageType.Warning);
				return "";
			}
		}
	}
}
