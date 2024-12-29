using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Media;
using System.Diagnostics;

namespace ImageProcessor
{
    public delegate void NotifyMessage(string message);
    public delegate void NotifyMessages(List<string> messages);
    public enum RenameType
	{
        None,
        Directory,  // directory name only
		FileName,   // part of file name in the directory 
        AddPrefix,  // beginning of file name in the directory 
    }
    public enum Conversion
    {
        None = 0,
        Encode = 1,         // mangle name and encrypt file
        ToJPG = 3,          // compress to JPG format
        ReduceSize = 2000,  // resize large images to max size 
    }
    public class FileManager
	{
		bool stopFlag;
		Navigator navigator;
        Conversion covertion = Conversion.None;
        bool sync;
        public event NotifyMessage notifyStatus;
        public event NotifyMessage notifyResults;
        public event NotifyMessages notifyFinal;
        List<string> messages = new List<string>();
        public string TextToReplace         { get; set; }
        public string TextReplacement       { get; set; }
        public string NewDirName            { get; set; }
        public FileManager(Navigator n)     { navigator=n; }
		public void Stop()					{ stopFlag=true; }
        public void ApplyAdjustmentRecursively(DirectoryInfo start, Conversion operation_, bool sync_)
        {
            sync = sync_;
            covertion = operation_;
            navigator.ProcessDirecory = ConvertFiles;
            try { navigator.ApplyRecursively(start, ""); }
            catch (Exception ex) { ReportResults(ex.Message+Environment.NewLine+ex.StackTrace); }
            finally
            {
                navigator.ProcessDirecory = null;
                if (!sync)
                    notifyFinal?.Invoke(messages);
            }
        }
        public string ResizeImage(string fullPath, bool exact, int maxSize, bool encrypted)
        {
            var ba = BitmapAccess.LoadImage(fullPath, encrypted);
            double scale = ba.ScaleReducingImageTo(maxSize);
            if (scale >= 1)
                return ""; // image already smaller than maxSize
            var bs = new BitmapAccess(ba.Source, new ScaleTransform(scale, scale));
            return bs.SaveToFile(fullPath, exact, encrypted);
        }
        void ConvertFiles(DirectoryInfo directory, string relativePath)
        {   // called from recursive dierectory processing in Navigator
            if (stopFlag || covertion == Conversion.None)
                return;
            if (sync)
                ReportStatus(covertion.ToString() + " in " + directory.FullName);
            if (covertion == Conversion.Encode && relativePath.Length > 0 && !FileName.IsMangled(directory.Name))
            {   // mangle dir name
                string newDirName = FileName.Mangle(directory.Name);
                if(newDirName != directory.Name)
                    directory.MoveTo(Path.Combine(directory.Parent.FullName, newDirName));
            }
            FileInfo[] filesToProcess = directory.GetFiles();
            foreach (FileInfo file in filesToProcess)
            {
                try
                {
                    if (stopFlag)
                        break;
                    if (covertion == Conversion.ReduceSize)
                    {
                        ImageFileInfo ifi = new ImageFileInfo(file);
                        string ret = ResizeImage(file.FullName, ifi.IsExact, (int)Conversion.ReduceSize, ifi.IsEncrypted);
                        if (ret.Length > 0)
                            ReportResults(ret);
                        continue;
                    }
                    string name = file.Name;   // name with extension
                    if (covertion == Conversion.Encode)
                    {
                        ImageFileName ifi = new ImageFileName(name);
                        if (!ifi.IsInfoImage)
                            name = FileName.MangleFile(name);
                        bool mangled = name != file.Name;
                        bool needEncryption = !ifi.IsEncrypted;
                        string newFilePath = Path.Combine(file.DirectoryName, name);
                        try
                        {
                            if (needEncryption)
                            {
                                string suffix = ifi.IsMovie ? ".vid" : !ifi.IsImage ? ".drw" : ifi.IsExact ? ".exa" : ".jpe";
                                name = ifi.FSName + suffix;
                                byte[] src = File.ReadAllBytes(file.FullName);
                                if (!DataAccess.WriteFile(newFilePath, src, true))
                                    ReportResults(name + ": " + DataAccess.Warning);
                                else
                                {
                                    string warnings = ImageFileInfo.Delete(file.FullName);
                                    if (warnings.Length > 0)
                                        ReportResults(warnings);
                                }
                            }
                            else if (covertion == Conversion.Encode)
                                file.MoveTo(newFilePath);
                        }
                        catch (Exception ex)
                        {
#if DEBUG
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
#endif
                            ReportResults(name + ": " + ex.Message);
                        }
                    }
                    if (covertion == Conversion.ToJPG)
                    {
                        ImageFileName ifi = new ImageFileName(name);
                        if (ifi.IsInfoImage || !ifi.IsExact)
                            continue;
                        try
                        {
                            var ba = BitmapAccess.LoadImage(file.FullName, ifi.IsEncrypted);
                            string newFilePath = Path.GetFileNameWithoutExtension(name) + (ifi.IsEncrypted ? ".jpe" : ".jpg");
                            newFilePath = Path.Combine(file.DirectoryName, newFilePath);
                            var ret = ba.SaveToFile(newFilePath, false, ifi.IsEncrypted);
                            if (ret.Length > 0)
                                ReportResults(name + ": " + DataAccess.Warning);
                            else
                            {
                                string warnings = ImageFileInfo.Delete(file.FullName);
                                if (warnings.Length > 0)
                                    ReportResults(warnings);
                            }
                        }
                        catch (Exception ex)
                        {
#if DEBUG
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
#endif
                            ReportResults(name + ": " + ex.Message);
                        }
                    }
                }
                catch
                {
                    throw new Exception(file.ToString());
                }
            }
        }
        public void DirectoryOrFilesRename(DirectoryInfo directory, RenameType renameType)
        {
            if (renameType == RenameType.Directory)
            {
                string dn = NewDirName.Trim();
                if (dn.Length > 0)
                {
                    string ndn = Path.Combine(directory.Parent.FullName, FileName.Mangle(dn));
                    directory.MoveTo(ndn);
                }
                return;
            }
            FileInfo[] filesToProcess = directory.GetFiles();
            int lr = TextToReplace.Length;
            int la = TextReplacement.Length;
            int l = TextToReplace.Length;
            foreach (FileInfo file in filesToProcess)
            {
                string name = file.Name;   // name with extension
                //name = ImageFileName.NameWithoutTempPrefix(name);
                if (ImageFileName.InfoType(name) != null)
                    continue;
                name = FileName.UnMangleFile(name);
                try
                {
                    if(renameType == RenameType.AddPrefix)
                    {
                        name = TextReplacement + name;
                    }
                    else if (renameType == RenameType.FileName)
                    {
                        int ind = lr == 0 ? -1 : name.IndexOf(TextToReplace);
                        if (ind < 0)
                            continue;
                        name = name.Substring(0, ind) + TextReplacement + name.Substring(ind+l);
                    }
                    string newFileName = Path.Combine(file.DirectoryName, FileName.MangleFile(name));
                    if (newFileName != file.FullName)
                    {
                        try { file.MoveTo(newFileName); }
                        catch (Exception ex) { messages.Add(name + ": " + ex.Message); }
                    }
                }
                catch
                {
                    throw new Exception(file.ToString());
                }
            }
        }
        void ReportStatus(string message)	 { notifyStatus?.Invoke(message); }
		public void ReportResults(string message)
        {
            if(sync)
                notifyResults?.Invoke(message);
            else
                messages.Add(message);
        }
	}
}
