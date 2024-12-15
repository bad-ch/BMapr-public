using System.Data.SQLite;
using BMapr.GDAL.WebApi.Models;

namespace BMapr.GDAL.WebApi.Services
{
    public class JobDbService
    {
        private string DbPath { get; set; }
        private SQLiteConnection Connection { get; set; }

        public JobDbService(string dbPath)
        {
            DbPath = dbPath;
            Connection = new SQLiteConnection($"URI=file:{DbPath}");
        }

        public Result AddJob(string reference, string key, string content)
        {
            var result = new Result(){Succesfully = false};

            try
            {
                Connection?.Open();
                using var cmd = new SQLiteCommand(Connection);
                cmd.CommandText = $"INSERT INTO runningJobs(ref, key, created) VALUES('{reference}','{key}','{DateTime.Now:O}')";
                //cmd.CommandText = $"INSERT INTO runningJobs(ref, key, content, created) VALUES('{reference}','{key}','{content}','{DateTime.Now:O}')";
                cmd.ExecuteNonQuery();
                Connection?.Close();
                result.Succesfully = true;
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("UNIQUE constraint failed"))
                {
                    result.Exceptions.Add(ex);
                }
            }

            return result;
        }

    }
}
