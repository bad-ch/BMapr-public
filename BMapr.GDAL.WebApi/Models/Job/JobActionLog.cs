namespace BMapr.GDAL.WebApi.Models.Job
{
    public enum LogLevel
    {
        Error,
        Warning,
        Information,
    }

    public class JobActionLog : JobAction
    {
        public string Message { get; set; }
        public LogLevel Level { get; set; }

        public override Result Execute()
        {
            if (!Active)
            {
                var result = new Result();
                result.Succesfully = true;
                result.AddMessage("Log action is deactivated");
                return result;
            }

            switch (Level)
            {
                case LogLevel.Error:
                    Logger.LogError(Message);
                    break;
                case LogLevel.Warning:
                    Logger.LogWarning(Message);
                    break;
                case LogLevel.Information:
                    Logger.LogInformation(Message);
                    break;
            }

            return new Result(){Succesfully = true};
        }
    }
}
