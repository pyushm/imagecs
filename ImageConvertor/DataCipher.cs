using ImageProcessor;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Markup;

namespace ImageProcessor
{
    public class DataCipher
    {
        const int iterations = 1000;
        static string passwordFile = "password";
        //static SymmetricAlgorithm alg = Rijndael.Create();
        static SymmetricAlgorithm alg = Aes.Create();
        static int[] deltaKey = new int[alg.KeySize / 8];
        static int[] deltaIV = new int[alg.BlockSize / 8];
        static byte[] salt = new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };
        static DataCipher()
        {
            if (File.Exists(passwordFile))
            {
                byte[] src = File.ReadAllBytes(passwordFile);
                if (src.Length == sizeof(int) * (deltaKey.Length + deltaIV.Length))
                {
                    int[] ia = new int[deltaKey.Length + deltaIV.Length];
                    Buffer.BlockCopy(src, 0, ia, 0, src.Length);
                    for (int i = 0; i < deltaKey.Length; i++)
                        deltaKey[i] = ia[i];
                    for (int i = 0; i < deltaIV.Length; i++)
                        deltaIV[i] = ia[i + deltaKey.Length];
                }
            }
        }
        public static void ChangePassword(string oldp, string newp)
        {
            //Rfc2898DeriveBytes ndb = new Rfc2898DeriveBytes(newp, salt);
            //Rfc2898DeriveBytes odb = new Rfc2898DeriveBytes(oldp, salt);
            byte[] ndb = Rfc2898DeriveBytes.Pbkdf2(newp, salt, iterations, HashAlgorithmName.SHA1, deltaKey.Length + deltaIV.Length);
            byte[] odb = Rfc2898DeriveBytes.Pbkdf2(oldp, salt, iterations, HashAlgorithmName.SHA1, deltaKey.Length + deltaIV.Length);
            //byte[] nkey = ndb.GetBytes(alg.KeySize / 8);
            //byte[] niv = ndb.GetBytes(alg.BlockSize / 8);
            //byte[] okey = odb.GetBytes(alg.KeySize / 8);
            //byte[] oiv = odb.GetBytes(alg.BlockSize / 8);
            byte[] nkey = ndb[..deltaKey.Length];
            byte[] niv = ndb[deltaKey.Length..(deltaKey.Length+ deltaIV.Length)];
            byte[] okey = odb[..deltaKey.Length];
            byte[] oiv = odb[deltaKey.Length..(deltaKey.Length + deltaIV.Length)];
            for (int i = 0; i < deltaKey.Length; i++)
                deltaKey[i] += okey[i] - nkey[i];
            for (int i = 0; i < deltaIV.Length; i++)
                deltaIV[i] += oiv[i] - niv[i];
            byte[] ba = new byte[sizeof(int) * (deltaKey.Length + deltaIV.Length)];
            Buffer.BlockCopy(deltaKey, 0, ba, 0, sizeof(int) * deltaKey.Length);
            Buffer.BlockCopy(deltaIV, 0, ba, sizeof(int) * deltaKey.Length, sizeof(int) * deltaIV.Length);
            using (var fs = File.OpenWrite(passwordFile)) { fs.Write(ba, 0, ba.Length); }
        }
        public static DataCipher Create(string password)
        {
            if (password == null || password.Length < 4)
                return null;
            byte[] pdb = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA1, deltaKey.Length + deltaIV.Length);
            byte[] key = pdb[..deltaKey.Length];
            for (int i = 0; i < key.Length; i++)
                key[i] = (byte)(key[i] + deltaKey[i]);
            byte[] iv = pdb[deltaKey.Length..(deltaKey.Length + deltaIV.Length)];
            for (int i = 0; i < iv.Length; i++)
                iv[i] = (byte)(iv[i] + deltaIV[i]);
            DataCipher ds = new DataCipher(key, iv);
            try
            {
                byte[] verify = new byte[] { 72, 172, 189,  51, 209, 144,  39,  65, 106, 171,  99,  52,  63, 102,  77, 195,
                                            241, 108, 118,  33, 201,  89,  38, 153, 149, 175,  51,  64,  45, 118,  11, 188,
                                             21,  45, 164, 157, 188, 185, 132, 252,   4,  84, 216, 233,   7, 219,  55,  73 };
                byte[] test = ds.Decrypt(verify);
                if (test.Length != 43)
                    return null;
                byte[] bytes = new byte[] {  109, 112, 100,  32, 112, 101,  32, 109, 110, 116, 104, 107, 111, 114,  97 };
                for (int i = 0; i < bytes.Length; i++)
                    if (test[3 * i] != bytes[i])
                        return null;
            }
            catch { return null; }
            return ds;
        }
        DataCipher(byte[] key, byte[] iv)
        {
            alg.Key = key;
            alg.IV = iv;
        }
        public byte[] Encrypt(byte[] src)
        {   // encrypts original data
            byte[] res;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write))
                    cs.Write(src, 0, src.Length);
                res = ms.ToArray();
            }
            return res;
        }
        public byte[] Decrypt(byte[] src)
        {   // decrypt encrypted data into original
            byte[] res;
            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    using (CryptoStream cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write))
                        cs.Write(src, 0, src.Length);
                }
                finally { res = ms.ToArray(); }
            }
            return res;
        }
    }
    public static class DataAccess
    {
        static DataCipher cipher = null;
        static string lastImageFile = "lastImageFile";
        public static string NullCipher = "Can't access private data: encryption not set";
        public static string Warning { get; private set; }
        public static bool AllowPrivateAccess(string password) { cipher = DataCipher.Create(password); return Private; }
        public static bool Private => cipher != null; // enforces conversion to private state for any file change 
        public static byte[] ReadBytes(byte[] src, bool encrypted)
        {
            Warning = "";
            if (!encrypted)
                return src;
            try
            {
                if (cipher == null)
                    Warning = NullCipher;
                else
                    return cipher.Decrypt(src);
            }
            catch (Exception ex)
            {
                Warning = "Decryption failed: " + ex.Message;
            }
            return new byte[0];
        }
        public static byte[] ReadFile(string fullPath, bool encrypted)
        {
            byte[] src = File.ReadAllBytes(fullPath);
            return ReadBytes(src, encrypted);
        }
        public static byte[] WriteBytes(byte[] src, bool encrypt)
        {
            Warning = "";
            if (!encrypt)
                return src;
            if (cipher == null)
                Warning = NullCipher;
            else
            {
                try
                {
                    return cipher.Encrypt(src);
                }
                catch (Exception ex)
                {
                    Warning = "Encryption failed: " + ex.Message;
                }
            }
            return new byte[0];
        }
        static bool WriteFile(string fullPath, byte[] src, bool encrypt)
        {
            if (encrypt)
            {
                src = WriteBytes(src, encrypt);
                if (Warning.Length > 0)
                    return false;
            }
            using (var fs = File.OpenWrite(fullPath)) { fs.Write(src, 0, src.Length); }
            return true;
        }
        public static string CreateFile(string fullPath, byte[] src, bool encrypt)
        {
            if (fullPath == null)
                return "";
            FileInfo fi = new FileInfo(fullPath);
            if (fi.Exists)
                    return "File '\" + fullPath + \"' exists";
            if (WriteFile(fullPath, src, encrypt))
                return "";
            return "Faled to create '\" + fullPath + \"' ";
        }
        public static string UpdateFile(string fullPath, byte[] src, bool encrypt)
        {
            if (fullPath == null)
                return "";
            FileInfo fi = new FileInfo(fullPath);
            if (fi.Exists)
            {
                try
                {
                    if (File.Exists(lastImageFile))
                    {
                        File.SetAttributes(lastImageFile, FileAttributes.Normal);
                        File.Delete(lastImageFile);
                    }
                    fi.MoveTo(lastImageFile);
                    File.SetAttributes(lastImageFile, FileAttributes.Normal);
                }
                catch
                {
                    return "Image '\" + fullPath + \"' not saved to lastImageFile";
                }
            }
            if (WriteFile(fullPath, src, encrypt))
                return "";
            fi = new FileInfo(lastImageFile);
            if (!fi.Exists)
                return "Image '\" + fullPath + \"' lost";
            fi.MoveTo(fullPath);
            return "Image '\" + fullPath + \"' was not updated"; ;
        }
        public static bool DecryptToTemp(string srcPath, string tmpPath)
        {
            byte[] src = File.ReadAllBytes(srcPath);
            byte[] imageBytes = ReadBytes(src, true);
            if (imageBytes.Length > 0)
            {
                if (File.Exists(tmpPath))
                    File.Delete(tmpPath);
                return WriteFile(tmpPath, imageBytes, false);
            }
            return false;
        }
    }
}
