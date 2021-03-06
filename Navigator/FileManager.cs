using System;
using System.IO;
using System.Collections.Generic;
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
    public enum ImageAdjustmentType
    {
        None,
        Resize,
        Encrypt,        // encrypt single image file
        Mangle
    }
    public class FileManager
	{
		int maxImageSize;
		bool stopFlag;
		Navigator navigator;
        ImageAdjustmentType adjustmentType = ImageAdjustmentType.None;
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
        public bool SetResizeModifyers(string maxImageSizeString, int limitImageSize)
		{
            string message = "Image Size Below Limit";
            if(int.TryParse(maxImageSizeString, out maxImageSize))
            {
                if (maxImageSize > limitImageSize)
                    return true;
            }
            else
                message = "Image Size is NAN";
            ReportResults(message);
            return false;
		}
        public void ApplyAdjustmentRecursively(DirectoryInfo start, ImageAdjustmentType operation_, bool sync_)
        {
            sync = sync_;
            adjustmentType = operation_;
            navigator.ProcessDirecory = AdjustFiles;
            try { navigator.ApplyRecursively(start, ""); }
            catch (Exception ex) { ReportResults(ex.Message+Environment.NewLine+ex.StackTrace); }
            finally
            {
                navigator.ProcessDirecory = null;
                if (!sync)
                    notifyFinal?.Invoke(messages);
            }
        }
        public void AdjustFiles(DirectoryInfo directory, string relativePath)
        {
            if (stopFlag || adjustmentType == ImageAdjustmentType.None)
                return;
            if (sync)
                ReportStatus(adjustmentType.ToString() + " in " + directory.FullName);
            if (adjustmentType == ImageAdjustmentType.Mangle && relativePath.Length > 0 && !ImageFileName.IsMangled(directory.Name))
            {   // mangle dir name
                string newDirName = ImageFileName.MangleText(directory.Name);
                if(newDirName != directory.Name)
                    directory.MoveTo(Path.Combine(directory.Parent.FullName, newDirName));
            }
            FileInfo[] filesToProcess = directory.GetFiles();
            bool resezeError = false;
            foreach (FileInfo file in filesToProcess)
            {
                try
                {
                    if (stopFlag)
                        break;
                    if (adjustmentType == ImageAdjustmentType.Resize)
                    {
                        ImageFileInfo ifi = new ImageFileInfo(file);
                        if (ifi.IsEncryptedImage)
                            resezeError = true;
                        else
                        {
                            string ret = BitmapAccess.ResizeImage(file.FullName, ifi.IsExact, 2000);
                            if (ret.Length > 0)
                                ReportResults(ret);
                        }
                        continue;
                    }
                    string name = file.Name;   // name with extension
                    if (adjustmentType == ImageAdjustmentType.Mangle && ImageFileName.InfoMode(name) == null)
                        name = ImageFileName.FSMangle(name);
                    else if (adjustmentType == ImageAdjustmentType.Encrypt)
                    {
                        ImageFileName ifi = new ImageFileName(file.Name);
                        if (ifi.IsUnencryptedImage)
                            name = ifi.IsExact ? ifi.FSName + ".exa" : ifi.FSName + ".jpe";
                        if (ifi.IsUnencryptedVideo)
                            name = ifi.FSName + ".vid";
                    }
                    string newFileName = Path.Combine(file.DirectoryName, name);
                    if (newFileName != file.FullName)
                    {
                        try
                        {
                            if (adjustmentType == ImageAdjustmentType.Encrypt)
                            {
                                byte[] src = File.ReadAllBytes(file.FullName);
                                if (!DataAccess.WriteFile(newFileName, src, true))
                                    ReportResults(name + ": " + DataAccess.Warning);
                                else
                                {
                                    string warnings = ImageFileInfo.Delete(file.FullName);
                                    if (warnings.Length > 0)
                                        ReportResults(warnings);
                                }
                            }
                            else if (adjustmentType == ImageAdjustmentType.Mangle)
                                file.MoveTo(newFileName);
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
                if (resezeError)
                    ReportResults("Resizing encrypted images NOT IMPLEMENTED");
            }
        }
        public void Rename(DirectoryInfo directory, RenameType renameType)
        {
            if (renameType == RenameType.Directory)
            {
                string dn = NewDirName.Trim();
                if (dn.Length > 0)
                {
                    string ndn = Path.Combine(directory.Parent.FullName, ImageFileName.MangleText(dn));
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
                if (ImageFileName.InfoMode(name) != null)
                    continue;
                name = ImageFileName.FSUnMangle(name);
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
                    string newFileName = Path.Combine(file.DirectoryName, ImageFileName.FSMangle(name));
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
