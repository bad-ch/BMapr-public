using BMapr.GDAL.WebApi.Models.Job;
using Newtonsoft.Json;
using System.Data.SQLite;

namespace BMapr.GDAL.WebApi.Test
{
    [TestClass]
    public class JobUnitTests
    {
        [TestMethod]
        public void CreateJobContent()
        {
            var job = new Job() {Key = "123", CronExpression = "cron1234"};

            job.Actions.Add(new JobActionLog() { Message = "Test 1", Level = LogLevel.Error});
            job.Actions.Add(new JobActionLog() { Message = "Test 2", Level = LogLevel.Warning });
            job.Actions.Add(new JobActionLog() { Message = "Test 3", Level = LogLevel.Information });

            var content = JsonConvert.SerializeObject(job, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
        }

        [TestMethod]
        public void CreateJobUrlAction()
        {
            var job = new Job() { Key = "123", CronExpression = "cron1234" };

            job.Actions.Add(new JobActionWebRequest
            {
                Active = true,
                Url = "https://www.sbb.ch",
                Method = "GET",
                Headers = new Dictionary<string, string>(),
                Accept = "*",
                TimeOut = 30000,
                AllowAutoRedirect = true,
                ReferenceName = "Test",
            });

            var content = JsonConvert.SerializeObject(job, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
        }

        [TestMethod]
        public void CreateJobUrlActionQuery()
        {
            var job = new Job() { Key = "123", CronExpression = "cron1234" };

            job.Actions.Add(new JobActionWebRequestQuery
            {
                Active = true,
                Url = "https://www.sbb.ch",
                Method = "GET",
                Headers = new Dictionary<string, string>(),
                Accept = "*",
                TimeOut = 30000,
                AllowAutoRedirect = true,
                ReferenceName = "Test123",
                ReferenceNameQuery = "Test123Query",
                Id = "user_logger"
            });

            var content = JsonConvert.SerializeObject(job, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
        }

        [TestMethod]
        public void JobActionCalculation()
        {
            var jobAction = new JobActionCalculation
            {
                Active = true,
                Expression = "a * 3.141592654 * b",
                Items = new Dictionary<string, object>(){{"a",2},{"b",0.25}},
                ResultReferenceName = "Result",
            };

            jobAction.Execute();

            var content = JsonConvert.SerializeObject(jobAction, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
        }

        [TestMethod]
        public void JobActionCalculationJson()
        {
            var jobAction = new JobActionCalculation
            {
                Active = true,
                Expression = "var json = JSON.parse('{\"test\": {\"subnode\" : 5} }'); return json.test.subnode;",
                Items = new Dictionary<string, object>() { { "a", 2 }, { "b", 0.25 } },
                ResultReferenceName = "Result",
            };

            jobAction.Execute();

            var content = JsonConvert.SerializeObject(jobAction, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
        }

        [TestMethod]
        public void CreateJobDb()
        {
            var job = new Job() { Key = "123", CronExpression = "cron1234" };

            job.Actions.Add(new JobActionLog() { Message = "Test 1" });
            job.Actions.Add(new JobActionLog() { Message = "Test 2" });
            job.Actions.Add(new JobActionLog() { Message = "Test 3" });

            var content = JsonConvert.SerializeObject(job, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });

            string cs = @"URI=file:C:\...\BMapr.GDAL.WebApi\Data\jobs\jobs.db";

            using var con = new SQLiteConnection(cs);
            con.Open();

            using var cmd = new SQLiteCommand(con);

            //cmd.CommandText = "DROP TABLE IF EXISTS runningJobs";
            //cmd.ExecuteNonQuery();

            cmd.CommandText = @"CREATE TABLE runningJobs(ref Text PRIMARY KEY,key TEXT, content TEXT, created DATETIME)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = $"INSERT INTO runningJobs(ref, key, content, created) VALUES('{job.Key}','{job.Key}','{content}','{DateTime.Now:O}')";
            cmd.ExecuteNonQuery();
        }
    }
}
