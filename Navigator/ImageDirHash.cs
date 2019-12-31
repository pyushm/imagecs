using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
namespace ImageProcessor
{
    [Serializable]
    public class ImageDirHash
    {   // ImageHash's of all image files in drectory
        public const string DirInfoFileName = "Ꮇ@unfu.dat"; //"@hash.dat";
        DirectoryInfo dir;  // full directory path
        public bool Updated { get { return ImageInfos != null && ImageInfos.Count > 0 && !ImageInfos.FirstOrDefault().Value.IsEmpty && infoFile.LastWriteTime >= dir.LastWriteTime; } }
        FileInfo infoFile;  // stored info File associated with dir info
        public Dictionary<string, ImageHash> ImageInfos { get; private set; }// file name with extension -> ImageInfo
         //var res = from kvp in oldInfoList where kvp.Value.SatHash == 0 select kvp; // linq
        public ImageDirHash(DirectoryInfo dirInfo)
        {   // reading stored imageInfos
            dir = dirInfo;
            infoFile = new FileInfo(Path.Combine(dir.FullName, DirInfoFileName));
            ImageInfos = Load();
        }
        public void Update()
        {
            if (Updated)
                return;
            ImageInfos = Create();
            // Load(); // test
        }
        Dictionary<string, ImageHash> Create()
        {
            Dictionary<string, ImageHash> newInfoList = new Dictionary<string, ImageHash>();
            FileInfo[] files = dir.GetFiles();
            foreach (var file in files)
            {
                ImageFileInfo fInfo = new ImageFileInfo(file);
                if (!fInfo.IsRegularImage)
                    continue;
                ImageHash info;
                try
                {
                    if (ImageInfos == null || !ImageInfos.TryGetValue(file.Name, out info) || info.CreateTime < file.LastWriteTime)
                        info = new ImageHash(fInfo);
                    newInfoList.Add(file.Name, info);
                }
                catch(Exception ex) { Debug.WriteLine("Creation of ImageInfo from " + infoFile.FullName + " failed: " + ex.Message); }
            }
            FileStream fs = null;
            try
            {
                fs = new FileStream(infoFile.FullName, FileMode.Create, FileAccess.Write);
                try
                {
                    (new BinaryFormatter()).Serialize(fs, newInfoList);
                    infoFile = new FileInfo(infoFile.FullName);
                }
                catch (Exception ex) { Debug.WriteLine("Serialization of " + infoFile.FullName + " failed: " + ex.Message); }
            }
            catch (Exception ex) { Debug.WriteLine("Saving " + infoFile.FullName + " failed: " + ex.Message); }
            if (fs != null)
                fs.Close();
            return newInfoList;
        }
        Dictionary<string, ImageHash> Load()
        {
            if (!infoFile.Exists)
                return null;
            FileStream fs = null;
            Dictionary<string, ImageHash> iid = null;
            try { fs = new FileStream(infoFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); }
            catch(Exception ex) { Debug.WriteLine("Loading " + infoFile.FullName + " failed: " + ex.Message); }
            try
            {
                BinaryFormatter f = new BinaryFormatter();
                object o = f.Deserialize(fs);
                iid = (Dictionary<string, ImageHash>)o;
            }
            catch (Exception ex) { Debug.WriteLine("Deserialization of " + infoFile.FullName + " failed: " + ex.Message); }
            if (fs != null)
                fs.Close();
            return iid;
        }
    }
}
