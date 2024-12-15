using Serilog;
using System.Net;

namespace BMapr.GDAL.WebApi.Services
{
    public class FtpService
    {
        private readonly string host;
        private readonly string user;
        private readonly string password;

        private FtpWebRequest ftpRequest;
        private FtpWebResponse ftpResponse;
        private Stream ftpStream;
        private const int BufferSize = 2048;

        public FtpService(string hostIp, string userName, string password)
        {
            host = hostIp;
            user = userName;
            this.password = password;
        }

        public byte[] Download(string remoteFile, bool usePassive = true)
        {
            try
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + remoteFile);
                ftpRequest.Credentials = new NetworkCredential(user, password);
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = usePassive;
                ftpRequest.KeepAlive = true;
                //ftpRequest.EnableSsl = true; // enables FTPS TLS ove FTP

                ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                ftpStream = ftpResponse.GetResponseStream();

                MemoryStream ms = (MemoryStream)ftpStream;
                byte[] content = ms.ToArray();
                ms.Dispose();

                ftpStream.Close();
                ftpResponse.Close();

                return content;

            }
            catch (Exception ex)
            {
                Log.Error("Exception caught in process:", ex);
            }

            return null;
        }

        public void Download(string remoteFile, string localFile)
        {
            try
            {
                File.WriteAllBytes(localFile, Download(remoteFile));
            }
            catch (Exception ex)
            {
                Log.Error("Exception caught in process:", ex);
            }
        }

        public bool Upload(string remoteFile, string localFile)
        {
            return Upload(remoteFile, File.ReadAllBytes(localFile));
        }

        public bool Upload(string remoteFile, byte[] content)
        {
            try
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + remoteFile);
                ftpRequest.Credentials = new NetworkCredential(user, password);
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;
                ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
                ftpStream = ftpRequest.GetRequestStream();

                var memoryStream = new MemoryStream(content);
                var byteBuffer = new byte[BufferSize];
                int bytesSent = memoryStream.Read(byteBuffer, 0, BufferSize);
                try
                {
                    while (bytesSent != 0)
                    {
                        ftpStream.Write(byteBuffer, 0, bytesSent);
                        bytesSent = memoryStream.Read(byteBuffer, 0, BufferSize);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Exception caught in process:", ex);
                }

                memoryStream.Close();
                ftpStream.Close();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Exception caught in process:", ex);
                return false;
            }
        }

        public void Delete(string deleteFile)
        {
            try
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + deleteFile);
                ftpRequest.Credentials = new NetworkCredential(user, password);
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;

                ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                ftpResponse.Close();
            }
            catch (Exception ex)
            {
                Log.Error("Exception caught in process:", ex);
            }
        }

        public void Rename(string currentFileNameAndPath, string newFileName)
        {
            try
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + currentFileNameAndPath);
                ftpRequest.Credentials = new NetworkCredential(user, password);
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;

                ftpRequest.Method = WebRequestMethods.Ftp.Rename;
                ftpRequest.RenameTo = newFileName;
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

                ftpResponse.Close();
            }
            catch (Exception ex)
            {
                Log.Error("Exception caught in process:", ex);
            }
        }

        public void CreateDirectory(string newDirectory)
        {
            try
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + newDirectory);
                ftpRequest.Credentials = new NetworkCredential(user, password);
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;

                ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

                ftpResponse.Close();
            }
            catch (Exception ex)
            {
                Log.Error("Exception caught in process:", ex);
            }
        }

        public string GetFileCreatedDateTime(string fileName)
        {
            try
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + fileName);
                ftpRequest.Credentials = new NetworkCredential(user, password);
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;

                ftpRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                ftpStream = ftpResponse.GetResponseStream();

                var ftpReader = new StreamReader(ftpStream);
                var fileInfo = string.Empty;

                try
                {
                    fileInfo = ftpReader.ReadToEnd();
                }
                catch (Exception ex)
                {
                    Log.Error("Exception caught in process:", ex);
                }

                ftpReader.Close();
                ftpStream.Close();
                ftpResponse.Close();

                return fileInfo;
            }
            catch (Exception ex)
            {
                Log.Error("Exception caught in process:", ex);
            }

            return "";
        }

        public string GetFileSize(string fileName)
        {
            try
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + fileName);
                ftpRequest.Credentials = new NetworkCredential(user, password);
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;

                ftpRequest.Method = WebRequestMethods.Ftp.GetFileSize;
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                ftpStream = ftpResponse.GetResponseStream();

                var ftpReader = new StreamReader(ftpStream);
                var fileInfo = string.Empty;

                try
                {
                    while (ftpReader.Peek() != -1)
                    {
                        fileInfo = ftpReader.ReadToEnd();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Exception caught in process:", ex);
                }

                ftpReader.Close();
                ftpStream.Close();
                ftpResponse.Close();

                return fileInfo;
            }
            catch (Exception ex)
            {
                Log.Error("Exception caught in process:", ex);
            }

            return string.Empty;
        }

        public string[] DirectoryListSimple(string directory)
        {
            try
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + directory);
                ftpRequest.Credentials = new NetworkCredential(user, password);
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;

                ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                ftpStream = ftpResponse.GetResponseStream();

                var ftpReader = new StreamReader(ftpStream);
                var directoryRaw = string.Empty;

                try
                {
                    while (ftpReader.Peek() != -1)
                    {
                        directoryRaw += ftpReader.ReadLine() + "|";
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Exception caught in process:", ex);
                }

                ftpReader.Close();
                ftpStream.Close();
                ftpResponse.Close();

                try
                {
                    if (!string.IsNullOrEmpty(directoryRaw))
                    {
                        var directoryList = directoryRaw.Split("|".ToCharArray());
                        return directoryList;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Exception caught in process:", ex);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception caught in process:", ex);
            }

            return new[] { string.Empty };
        }

        public string[] DirectoryListDetailed(string directory)
        {
            try
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + directory);
                ftpRequest.Credentials = new NetworkCredential(user, password);
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;

                ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                ftpStream = ftpResponse.GetResponseStream();

                var ftpReader = new StreamReader(ftpStream);
                var directoryRaw = string.Empty;

                try
                {
                    while (ftpReader.Peek() != -1)
                    {
                        directoryRaw += ftpReader.ReadLine() + "|";
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Exception caught in process:", ex);
                }

                ftpReader.Close();
                ftpStream.Close();
                ftpResponse.Close();

                try
                {
                    if (!string.IsNullOrEmpty(directoryRaw))
                    {
                        string[] directoryList = directoryRaw.Split("|".ToCharArray());
                        return directoryList;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Exception caught in process:", ex);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception caught in process:", ex);
            }

            return new[] { string.Empty };
        }
    }
}
