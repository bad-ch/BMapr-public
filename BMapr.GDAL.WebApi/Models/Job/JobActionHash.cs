using System.Security.Cryptography;
using System.Text;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobActionHash : JobAction
    {
        public enum HashType
        {
            Md5,
            Sha256,
            Sha384,
            Sha512,
        }

        public string ReferenceName { get; set; }
        public HashType Type { get; set; }
        public string ResultReferenceName { get; set; }


        public override Result Execute()
        {
            var result = new Result() { Succesfully = false };

            if (!ResultItems.ContainsKey(ReferenceName))
            {
                result.AddMessage($"Unkown reference name {ReferenceName}");
                return result;
            }

            var sourceValue = ResultItems[ReferenceName];

            if (!(sourceValue.Value is string || sourceValue.Value is byte[]))
            {
                result.AddMessage($"Reference name type  is not supported {sourceValue.GetType()}");
                return result;
            }

            string hash;

            switch (Type)
            {
                case HashType.Md5:
                    hash = sourceValue.Value is string ? ComputeMd5((string)sourceValue.Value) : ComputeMd5((byte[])sourceValue.Value);
                    break;
                case HashType.Sha256:
                    hash = sourceValue.Value is string ? ComputeSha256((string)sourceValue.Value) : ComputeSha256((byte[])sourceValue.Value);
                    break;
                case HashType.Sha384:
                    hash = sourceValue.Value is string ? ComputeSha384((string)sourceValue.Value) : ComputeSha384((byte[])sourceValue.Value);
                    break;
                case HashType.Sha512:
                    hash = sourceValue.Value is string ? ComputeSha512((string)sourceValue.Value) : ComputeSha512((byte[])sourceValue.Value);
                    break;
                default:
                    result.AddMessage($"Unkown type {Type}");
                    return result;
            }

            result.Succesfully = true;
            AddValue(ResultReferenceName, JobActionResultItemType.Hash, hash);
            return result;
        }

        private string ComputeSha256(string value)
        {
            return ComputeSha256(Encoding.UTF8.GetBytes(value));
        }

        private string ComputeSha256(byte[] value)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashValue = sha256.ComputeHash(value);
                return Convert.ToHexString(hashValue);
            }
        }

        private string ComputeSha384(string value)
        {
            return ComputeSha384(Encoding.UTF8.GetBytes(value));
        }

        private string ComputeSha384(byte[] value)
        {
            using (SHA384 sha384 = SHA384.Create())
            {
                byte[] hashValue = sha384.ComputeHash(value);
                return Convert.ToHexString(hashValue);
            }
        }

        private string ComputeSha512(string value)
        {
            return ComputeSha512(Encoding.UTF8.GetBytes(value));
        }

        private string ComputeSha512(byte[] value)
        {
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] hashValue = sha512.ComputeHash(value);
                return Convert.ToHexString(hashValue);
            }
        }

        private string ComputeMd5(string value)
        {
            return ComputeMd5(Encoding.UTF8.GetBytes(value));
        }

        private string ComputeMd5(byte[] value)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashValue = md5.ComputeHash(value);
                return Convert.ToHexString(hashValue);
            }
        }
    }
}
