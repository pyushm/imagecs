using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media.Media3D;

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
        Encode = 1,             // mangle name and encrypt file
        MangleChar = 2,         // replace manglr char with lowercase
        ToJPG = 3,              // compress to JPG format
        LimitSize1 = 1100,      // image dimension limit: xxxxyyyy or ssss for both
        LimitSize2 = 1560,      // image dimension limit: xxxxyyyy or ssss for both
        LimitSize3 = 2200,      // image dimension limit: xxxxyyyy or ssss for both
        LimitSize4 = 33002200,  // image dimension limit: xxxxyyyy or ssss for both
    }
    public class FileManager
	{
		bool stopFlag;
		Navigator navigator;
        Conversion covertion = Conversion.None;
        //bool sync;
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
            //sync = sync_;
            covertion = operation_;
            navigator.ProcessDirecory = ConvertFiles;
            try { navigator.ApplyRecursively(start, ""); }
            catch (Exception ex) { ReportResults("***********" + ex.Message+Environment.NewLine+ex.StackTrace); }
            finally
            {
                navigator.ProcessDirecory = null;
                //if (!sync)
                //    notifyFinal?.Invoke(messages);    causes cross-thread error
            }
        }
        public string ResizeImage(string fullPath, bool exact, int sizeLimit, bool encrypted)
        {
            var ba = BitmapAccess.LoadImage(fullPath, encrypted);
            double scale = ba.ScaleReducingImageTo(sizeLimit);
            if (scale >= 1)
                return ""; // image already smaller than sizeLimit
            IntSize saveSize = Scaler.GetSize(ba.Width, ba.Height, sizeLimit);
            Rect rect = new Rect(0, 0, saveSize.Width, saveSize.Height);
            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            RenderTargetBitmap rtb = new RenderTargetBitmap(saveSize.Width, saveSize.Height, 96, 96, PixelFormats.Default);
            group.Children.Add(new ImageDrawing(ba.Source, rect));
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawDrawing(group);
            rtb.Render(drawingVisual);
            var bs = new BitmapAccess(rtb);
            return bs.SaveToExistingFile(fullPath, exact, encrypted);
        }
        void ConvertFiles(DirectoryInfo directory, string relativePath)
        {   // called from recursive dierectory processing in Navigator
            if (stopFlag || covertion == Conversion.None)
                return;
            ReportStatus(covertion.ToString() + " in " + directory.FullName);
            if (covertion == Conversion.Encode && relativePath.Length > 0 && !Scramble.IsMangled(directory.Name))
            {   // mangle dir name
                string newDirName = Scramble.ManglePrivate(directory.Name);
                if (newDirName != directory.Name)
                    directory.MoveTo(Path.Combine(directory.Parent.FullName, newDirName));
            }
            //if (covertion == Conversion.MangleChar && relativePath.Length > 0 && directory.Name[0] == '\u13B7')
            //{   // mangle dir name
            //    var old = directory.Name;
            //    string newDirName = '\uAB87' + directory.Name.Substring(1);
            //    if (newDirName != directory.Name)
            //    {
            //        var np = Path.Combine(directory.Parent.FullName, newDirName);
            //        directory.MoveTo(np);
            //        ReportResults(Scramble.UnMangle(newDirName) + ": " + old + " -> " + np);
            //    }
            //}
            FileInfo[] filesToProcess = directory.GetFiles();
            foreach (FileInfo file in filesToProcess)
            {
                try
                {
                    if (stopFlag)
                        break;
                    if ((int)covertion >= (int)Conversion.LimitSize1)
                    {
                        ImageFileInfo ifi = new ImageFileInfo(file);
                        string ret = ResizeImage(file.FullName, ifi.IsExact, (int)covertion, ifi.IsEncrypted);
                        if (ret.Length > 0)
                            ReportResults(ret);
                        continue;
                    }
                    string name = file.Name;   // name with extension
                    if (covertion == Conversion.Encode)
                    {
                        ImageFileName ifi = new ImageFileName(name);
                        if (!ifi.IsInfoImage)
                            name = Scramble.MangleFile(name);
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
                                var warn = DataAccess.CreateFile(newFilePath, src, true);
                                if (warn.Length > 0)
                                    ReportResults(name + ": " + warn + Environment.NewLine + file.FullName + "was not removed");
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
                            ReportResults(name + ": " + ex.Message);
                            Debug.WriteLine(ex.StackTrace);
                        }
                    }
                    //if (covertion == Conversion.MangleChar && (file.Name[0] == '@' || file.Name[1] == '@'))
                    //{
                    //    string newName = null;
                    //    var sn = file.Name;
                    //    newName = sn.Replace("exa.exa", "exa");
                    //    if(newName==sn)
                    //        newName = sn.Replace("jpe.jpe", "jpe");
                    //    // Path.GetFileNameWithoutExtension(file.Name);
                    //    //if (sn.Contains("Preview.exa")) newName = "@Preview.exa";
                    //    //else if (sn.Contains("Preview.jpe")) newName = "@Preview.jpe";
                    //    //else if (sn.Contains("Sys.exa")) newName = "@Sys.exa";
                    //    //else if (sn.Contains("Vag.exa")) newName = "@Vag.exa"; // Int
                    //    //else if (sn.Contains("Sys.jpe")) newName = "@Sys.jpe";
                    //    //else if (sn.Contains("Vag.jpe")) newName = "@Vag.jpe";
                    //    //else if (sn.Contains("Detail.exa")) newName = "@Detail.exa";//Qrgnvy
                    //    //else if (sn.Contains("Detail.jpe")) newName = "@Detail.jpe";
                    //    if (!string.IsNullOrEmpty(newName) && newName != sn)
                    //    {
                    //        var nf = Path.Combine(directory.FullName, newName);// + file.Extension);
                    //        file.MoveTo(nf);
                    //        ReportResults('\t' + sn + " -> " + newName);
                    //    }
                    //}
                    if (covertion == Conversion.MangleChar && file.Name[0] == '\u13B7')
                    {
                        var old = file.FullName;
                        string newFileName;
                        newFileName = Scramble.mangleChar + file.Name.Substring(1);
                        if (newFileName != file.Name)
                        {
                            var nf = Path.Combine(directory.FullName, newFileName);
                            file.MoveTo(nf);
                            ReportResults('\t' + old + " -> " + nf);
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
                            var ret = ba.SaveToNewFile(newFilePath, false, ifi.IsEncrypted);
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
                            ReportResults(name + ": " + ex.Message);
                            Debug.WriteLine(ex.StackTrace);
                        }
                    }
                }
                catch(Exception ex)
                {
                    ReportResults(file.FullName + ": " + ex.Message);
                }
            }
        }
        public string DirectoryOrFilesRename(DirectoryInfo directory, RenameType renameType)
        {
            var oldName = directory.FullName;
            if (renameType == RenameType.Directory)
            {
                string dn = NewDirName.Trim();
                if (dn.Length > 0)
                {
                    string ndn = Path.Combine(directory.Parent.FullName, Scramble.ManglePrivate(dn));
                    directory.MoveTo(ndn);
                    return oldName;

                }
                return null;
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
                name = Scramble.UnMangleFile(name);
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
                    string newFileName = Path.Combine(file.DirectoryName, Scramble.MangleFile(name));
                    if (newFileName != file.FullName)
                    {
                        try { file.MoveTo(newFileName); }
                        catch (Exception ex) { messages.Add(name + ": " + ex.Message); }
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(file.FullName + ": " + ex.Message);
                    throw new Exception(file.ToString());
                }
            }
            return null;
        }
        void ReportStatus(string message)	 
        {
            Debug.WriteLine(message);
            //if (sync) 
            //    notifyStatus?.Invoke(message); 
        }
        public void ReportResults(string message)
        {
            Debug.WriteLine(message);
            //if (sync)
            //    notifyResults?.Invoke(message);
            //else
            //    messages.Add(message);
        }
    }
}
