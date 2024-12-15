using BMapr.GDAL.WebApi.Services;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobActionFtp : JobAction
    {
        public enum MethodType
        {
            Upload,
            Download,
        }

        public string Host { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public MethodType Method { get; set; } = MethodType.Download;
        public string RemoteFile { get; set; } = string.Empty;
        public string ReferenceName { get; set; } = string.Empty;

        public override Result Execute()
        {
            var result = new Result() { Succesfully = false };

            if (!Active)
            {
                result.Succesfully = true;
                result.AddMessage("FTP action is deactivated");
                return result;
            }

            try
            {
                var ftpService = new FtpService(Host, User, Password);

                if (Method == MethodType.Download)
                {
                    var content = ftpService.Download(RemoteFile);
                    AddValue(ReferenceName, JobActionResultItemType.File, content);
                    result.Succesfully = true;
                }
                else
                {
                    if (ResultItems.ContainsKey(ReferenceName) && ResultItems[ReferenceName].Value.GetType() == typeof(byte[]) )
                    {
                        ftpService.Upload(RemoteFile, (byte[])ResultItems[ReferenceName].Value);
                        result.Succesfully = true;
                    }
                    else
                    {
                        result.Messages.Add("Reference name not found or wrong type");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Exceptions.Add(ex);
                result.Messages.Add(ex.Message);
            }

            return result;
        }
    }
}
