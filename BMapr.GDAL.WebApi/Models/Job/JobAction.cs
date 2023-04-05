using System.Reflection.Metadata;
using System.Text;
using BMapr.GDAL.WebApi.Services;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public abstract class JobAction
    {
        [JsonIgnore]
        public ILogger<JobGod> Logger { get; set; }

        [JsonIgnore]
        public Config Config;

        [JsonIgnore]
        public string Project;

        [JsonIgnore]
        public int ProcessId;

        public bool Active { get; set; } = true;

        [JsonIgnore] public Dictionary<string, JobActionResultItem> ResultItems { get; set; } = new Dictionary<string, JobActionResultItem>();

        public abstract Result Execute();

        public string Replace(string value)
        {
            value = value.Replace("#project#", Project);
            value = value.Replace("#processId#", ProcessId.ToString());
            value = value.Replace("#now#", DateTime.Now.ToString("O"));
            value = value.Replace("#guid#", Guid.NewGuid().ToString());

            foreach (var resultItem in ResultItems)
            {
                // todo better error handling for byte arrays and so on
                var pattern = $"#val({resultItem.Key})#";
                value = value.Replace(pattern, resultItem.Value.Value.ToString());
            }

            return value;
        }

        public void AddValue(string OutputName, JobActionResultItemType resultType,  object value)
        {
            var result = new JobActionResultItem() {Type = resultType, Value = value};

            if (ResultItems.ContainsKey(OutputName))
            {
                ResultItems[OutputName] = result;
            }
            else
            {
                ResultItems.Add(OutputName, result);
            }
        }
    }
}
