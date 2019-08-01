using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace XAsset.Plugins.XAsset.Custom
{
    public class HashUtil
    {
        public static string GetHashOfFile(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                return GetHash(stream);
            }
        }
        public static string GetHash(Stream fs)
        {
            HashAlgorithm ha = HashAlgorithm.Create();
            byte[] bytes = ha.ComputeHash(fs);
            fs.Close();
            return ToHexString(bytes);
        }

        public static string GetHash(string s)
        {
            return GetHash(Encoding.UTF8.GetBytes(s));
        }

        public static string GetHash(byte[] data)
        {
            HashAlgorithm ha = HashAlgorithm.Create();
            byte[] bytes = ha.ComputeHash(data);
            return ToHexString(bytes);
        }

        public static string ToHexString(byte[] bytes)
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    strB.Append(bytes[i].ToString("X2"));
                }

                hexString = strB.ToString().ToLower();
            }

            return hexString;
        }

//        public static string GetMD5OfFile(string filePath)
//        {
//            var readAllBytes = File.ReadAllBytes(filePath);
//            return GetMD5(readAllBytes);
//        }
//
//        public static string GetMD5(byte[] bytes)
//        {
//            byte[] hash = Crypto.ComputeMD5Hash(bytes);
//            return ToHexString(hash);
//        }
    }
}