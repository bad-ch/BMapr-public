using System.Security.Cryptography;

namespace BMapr.GDAL.WebApi.Services
{
    public class HashService
    {
        public static string CreateFromFileMd5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-","").ToLower();
                }
            }
        }

        public static string CreateFromStringMd5(string text, string salt = "")
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            using (var md5 = MD5.Create())
            {
                byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(text + salt);
                byte[] hashBytes = md5.ComputeHash(textBytes);
                string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return hash;
            }
        }
    }
}
