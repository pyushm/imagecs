using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace NNTP
{
	public class NewArticleStorage
	{
		struct ArticlePart
		{
            //public class Comparer : IComparer
            //{
            //    int IComparer.Compare(object fi1, object fi2)
            //    {
            //        return ((ArticlePart)fi1).first - ((ArticlePart)fi2).first;
            //    }
            //}
            public class Comparer : IComparer<ArticlePart>
            {
                int IComparer<ArticlePart>.Compare(ArticlePart fi1, ArticlePart fi2)
                {
                    return fi1.first - fi2.first;
                }
            }
            public int first;
			public int last;
			public string name;
			public string fileName;
			public bool append;
			public bool deleteFile;
			public ArticlePart(string fileName_, int fpart, int lpart)
			{
				fileName=fileName_;
				first=fpart;
				last=lpart;
				name="part"+fpart+'-'+lpart+'.';
				append=false;
				deleteFile=false;
			}
			public static bool ParseFileName(string fileName, out int f, out int l)
			{
                f = 0;
				l=0;
				try
				{
					int pos0=fileName.IndexOf(".part")+5;
					if(pos0<0)
                        return false;
                    int pos1 = fileName.IndexOf('-', pos0);	// first '-' after "part"
                    if (pos1 < 0)
                        return false;
                    int pos2 = fileName.IndexOf('.', pos1);	// first '.' after '-'
                    if (pos2 < 0)
                        return false;
                    f = int.Parse(fileName.Substring(pos0, pos1 - pos0));
					l=int.Parse(fileName.Substring(pos1+1, pos2-pos1-1));
					return true;
				}
                catch
				{
					return false;
				}
			}
		}
        DirectoryInfo directory;
        public NewArticleStorage(DirectoryInfo directory_)
		{
            directory = directory_;
		}
        public string TempFileName(string prefix, string fileName)
		{
            if (Path.GetExtension(fileName).ToLower() == ".jpeg")
				fileName=Path.GetFileNameWithoutExtension(fileName)+".jpg";
            return Path.Combine(directory.FullName, prefix + fileName); 
		}
		internal FileInfo[] GetFiles(string pattern)
		{
			return directory.GetFiles(pattern);
		}
        public string Save(string prefix, string fileName, byte[] ba)
		{
			string fullName=TempFileName(prefix, fileName);
			SaveAttempt(10, fullName, ba);
			return fullName;
		}
		bool SaveAttempt(int attempt, string fullName, byte[] ba)
		{
			try
			{
				FileStream fs=new FileStream(fullName, FileMode.Create, FileAccess.Write, FileShare.Read);
				BinaryWriter w = new BinaryWriter(fs);
				w.Write(ba);
				w.Close();
				fs.Close();
				return true;
			}
			catch(Exception ex)
			{
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                if (attempt < 0)
					return false;
				Thread.Sleep(100);
				return SaveAttempt(attempt-1, fullName, ba);
			}
		}
        public string SavePart(string prefix, string fn, byte[] bData, int part, int numParts)
		{
			string dataFileName="";
			FileInfo[] files=directory.GetFiles('*'+fn);	// find all parts
			ArticlePart nfp=new ArticlePart(TempFileName(prefix, fn), part, part);	// newly received part
			ArticlePart[] parts=new ArticlePart[files.Length+1];
			int numFiles=0;
			for(int i=0; i<files.Length; i++)
			{
				int f;
				int l;
				if(ArticlePart.ParseFileName(files[i].Name, out f, out l))
					parts[numFiles++]=new ArticlePart(files[i].FullName, f, l);
			}
			for(int i=1; i<numFiles; i++)
			{											// validates if new data are not already loaded
				if(parts[i].first<=part && part<=parts[i].last)
					return dataFileName;
			}
			parts[numFiles++]=nfp;
			Array.Sort(parts, 0, numFiles, new ArticlePart.Comparer());
			int prevInd=0;
			for(int i=1; i<numFiles; i++)
			{											// mark contigeous parts for append
				ArticlePart cur=parts[i];
				if(parts[prevInd].last+1<cur.first)
					continue;
				if(parts[prevInd].last+1==cur.first)
					parts[prevInd].append=true;
				if(parts[prevInd].last>=cur.first)
					throw new Exception("Part sequence error");
				prevInd=i;
			}
			try
			{
				int first=0;							// first in contigeous sequence
				int last=0;
				FileInfo newFile=null;					// new multi-part file
				FileStream buffer=null;					// new multi-part stream
				byte[] tba;
				for(int i=0; i<numFiles; i++)
				{
                    string newName="";
					if(newFile==null)
					{
						if(parts[i].first==part)		// store newly received part
                            parts[i].fileName=newName=Save(prefix, nfp.name + fn, bData);
						if(parts[i].append)				// first in append sequence
                        {
                            first = parts[i].first;
                            newFile = new FileInfo(parts[i].fileName);
                            while (newName == parts[i].fileName && (!newFile.Exists || newFile.Length != bData.Length))
                            {
                                newFile = new FileInfo(parts[i].fileName);
                                Thread.Sleep(100);
                            }
                            bool notGotAccess=true;
                            string sReason = "";
                            int repeat = 10;
                            while (notGotAccess && repeat-->0)
                            {
                                try
                                {
                                    buffer = newFile.Open(FileMode.Append, FileAccess.Write, FileShare.None);
                                    notGotAccess = false;
                                }
                                catch (Exception ex)
                                {
                                    Thread.Sleep(200);
                                    sReason = ex.Message;
                                }
                            }
                            if (notGotAccess)
                            {
                                string s = sReason;
                            }
                        }
					}
					else
					{
                        bool isLast = parts[i].first == part;
                        if (isLast)		// just received part
							tba=bData;
						else							// previously storred part
						{
							tba=LoadAttempt(10, parts[i].fileName);
							parts[i].deleteFile=true;
						}
                        try
                        {
                            buffer.Write(tba, 0, tba.Length);
                        }
                        catch (Exception ex)
                        {
                            string s = ex.Message;
                        }
                        last = parts[i].last;
						if(!parts[i].append)			// last in append sequence
						{
                            try
                            {
							    buffer.Close();
                            }
                            catch (Exception ex)
                            {
                                string s = ex.Message;
                            }
                            ArticlePart np = new ArticlePart(null, first, last);
							if(np.first==1 && np.last==numParts)// all parts collected
							{
                                dataFileName = TempFileName(prefix, fn);
								newFile.MoveTo(dataFileName);
							}
							else
                                newFile.MoveTo(TempFileName(prefix, np.name + fn));
							first=0;
							last=0;
							buffer=null;
							newFile=null;
						}
					}
				}
				for(int i=0; i<numFiles; i++)
				{
					if(parts[i].deleteFile)
						File.Delete(parts[i].fileName);
				}
			}
			catch(Exception ex)
			{
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
			return dataFileName;
		}
        public byte[] Load(string prefix, out string fileName)
		{
            FileInfo[] files = directory.GetFiles(prefix + '*');
			if(files.Length>0)
			{
                fileName = Path.GetFileName(files[0].FullName);
                if(fileName.StartsWith(prefix))
                    fileName = fileName.Substring(prefix.Length);
                return LoadAttempt(10, files[0].FullName);
			}
			fileName="";
			return null;
		}
		static internal byte[] Load(string fileName)
		{
			return LoadAttempt(10, fileName);
		}
		static byte[] LoadAttempt(int attempt, string fileName)
		{
			try
			{
				FileStream fs=new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
				BinaryReader r = new BinaryReader(fs);
				byte[] ba=r.ReadBytes((int)fs.Length);
				r.Close();
				fs.Close();
				return ba;
			}
            catch (Exception ex)
			{
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                if (attempt < 0)
					return new byte[0];
				Thread.Sleep(100);
				return LoadAttempt(attempt-1, fileName);
			}
		}
	}
}
