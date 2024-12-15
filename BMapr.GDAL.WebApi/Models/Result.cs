using System.Net;

namespace BMapr.GDAL.WebApi.Models
{
    public class Result
    {
        public enum LogLevel
        {
            None,
            Debug,
            Error,
            Fatal,
            Warn,
            Info
        }

        public bool Succesfully { get; set; }
        public bool Warning { get; set; }
        public List<string> Messages { get; set; }
        public List<Exception> Exceptions { get; set; }

        public Result()
        {
            Messages = new List<string>();
            Exceptions = new List<Exception>();
            Succesfully = false;
            Warning = false;
        }
        public void AddMessage(string message)
        {
            AddMessage(message, null, LogLevel.None);
        }

        public void AddMessage(string message, Exception exception)
        {
            AddMessage(message, exception, LogLevel.None);
        }

        public void AddMessage(string message, Exception exception, LogLevel logLevel)
        {
            Messages.Add(message);

            if (exception != null)
            {
                Exceptions.Add(exception);
            }

            if (logLevel != LogLevel.None)
            {
                AddLogItem(logLevel, message, exception);
            }
        }

        public void TakeoverEvents(Result result)
        {
            Messages.AddRange(result.Messages);
            Exceptions.AddRange(result.Exceptions);
        }

        private void AddLogItem(LogLevel logLevel, string message = "", Exception exception = null)
        {
            // todo log4net

            //var logMessage = String.Format("{0}, {1}, {2}", message, exception != null ? exception.Message : String.Empty, exception != null ? exception.Source : String.Empty);

            //switch (logLevel)
            //{
            //    case LogLevel.Debug:
            //        log.Debug(logMessage);
            //        break;
            //    case LogLevel.Error:
            //        log.Error(logMessage);
            //        break;
            //    case LogLevel.Warn:
            //        log.Warn(logMessage);
            //        break;
            //    case LogLevel.Fatal:
            //        log.Fatal(logMessage);
            //        break;
            //    case LogLevel.Info:
            //        log.Info(logMessage);
            //        break;
            //}
        }
    }

    public class Result<T> : Result
    {
        protected Type _type { get; set; }
        protected T _value { get; set; }

        public T Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                if (value != null)
                {
                    _type = value.GetType();
                }
            }
        }
    }

    public class WebResult<T> : Result
    {
        public HttpStatusCode Status { get; set; }
        public string ContentType { get; set; }
        public string Encoding { get; set; }

        protected Type _type { get; set; }
        protected T _value { get; set; }

        public T Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                if (value != null)
                {
                    _type = value.GetType();
                }
            }
        }
    }
}
