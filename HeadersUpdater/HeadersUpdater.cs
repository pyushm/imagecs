using System;

namespace NNTP
{
	public class HeadersUpdater : GroupManager
	{
		public HeadersUpdater(string server) : base("HeadersUpdateLog.txt") { }
		[STAThread]
		static void Main() 
		{
			HeadersUpdater updater=new HeadersUpdater("news.west.cox.net");
			updater.UpdateAllHeaders();
		}
        void UpdateAllHeaders()
        {
            foreach (Group group in SubscribedGroups)
            {
                if (SetCurrentGroup(group))
                {
                    ConnectGroup();
                    LoadNewHeaders();
                }
            }
            SendMessage("All groups updated " + DateTime.Now.ToString(), Common.MessageType.Info);
        }

	}
}
