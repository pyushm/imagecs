using System;
using System.IO;
using System.Collections;
using System.Threading;

namespace ShaderEffects
{
    public class TextFile
    {
        public static string[] Read(string fileName)
        {
            string warning;
            return Read(new FileInfo(fileName), out warning, 1);
        }

        public static string[] Read(string fileName, out string warning, int maxAttempts)
        {
            warning = "";
            if (fileName == null || fileName.Length == 0)
            {
                return new string[0];
            }

            return Read(new FileInfo(fileName), out warning, maxAttempts);
        }

        public static string[] Read(FileInfo fi, out string warning, int maxAttempts)
        {
            if (fi == null)
            {
                warning = "Null file specified for reading";
                return new string[0];
            }

            if (!fi.Exists)
            {
                warning = "File " + fi.Name + " does not exist";
                return new string[0];
            }

            int num = 0;
            warning = "";
            while (++num < maxAttempts)
            {
                fi = new FileInfo(fi.FullName);
                if (fi.Length > 0)
                {
                    break;
                }

                Thread.Sleep(1000);
            }

            if (fi.Length == 0)
            {
                if (maxAttempts > 1)
                {
                    warning = "File " + fi.Name + " does not have data for " + num + " seconds";
                }

                return new string[0];
            }

            ArrayList arrayList = new ArrayList();
            num = 0;
            do
            {
                TextReader textReader = null;
                try
                {
                    textReader = new StreamReader(fi.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                    string text;
                    while ((text = textReader.ReadLine()) != null)
                    {
                        if (text.Length > 0)
                        {
                            arrayList.Add(text);
                        }
                    }

                    textReader.Close();
                    num = maxAttempts;
                }
                catch (Exception ex)
                {
                    if (num >= maxAttempts)
                    {
                        warning = "Reading file " + fi.Name + " failed: " + ex.Message;
                        textReader?.Close();
                        return new string[0];
                    }

                    Thread.Sleep(1000);
                }
            }
            while (num++ < maxAttempts);
            return (string[])arrayList.ToArray(typeof(string));
        }

        public static bool HasData(FileInfo file)
        {
            return file != null && file.Exists && file.Length > 0;
        }

        public static StreamWriter OpenNew(string name)
        {
            return OpenForWrite(name, append: false);
        }

        public static StreamWriter OpenForAppend(string name)
        {
            return OpenForWrite(name, append: true);
        }

        private static StreamWriter OpenForWrite(string name, bool append)
        {
            int num = 3;
            while (num-- > 0)
            {
                try
                {
                    return new StreamWriter(name, append);
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }

            return null;
        }
    }

}
