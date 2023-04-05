using Jint;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobActionSwitch : JobAction
    {
        public string Expression { get; set; }
        public List<JobAction> StackTrue { get; set; } = new List<JobAction>();
        public List<JobAction> StackFalse { get; set; } = new List<JobAction>();

        public override Result Execute()
        {
            var result = new Result() { Succesfully = false };

            if (string.IsNullOrEmpty(Expression))
            {
                result.Messages.Add("Expression has to be a valid JS if criteria with a boolean result");
                return result;
            }

            if (!Active)
            {
                result.Succesfully = true;
                result.AddMessage("Switch action is deactivated");
                return result;
            }

            var engine = new Engine();

            foreach (var resultItem in ResultItems)
            {
                var value = resultItem.Value.Value;

                switch (value)
                {
                    case string:
                    case int:
                    case long:
                    case double:
                    case decimal:
                        engine.SetValue(resultItem.Key, value);
                        break;
                    default:
                        result.Messages.Add($"Jint ignore value {resultItem.Key}, wrong type {value.GetType()}");
                        break;
                }
            }

            object evaluatedValue;

            try
            {
                evaluatedValue = engine.Evaluate(Expression).ToObject();
            }
            catch (Exception ex)
            {
                result.Messages.Add($"Evaluating JS fail {ex.Message}");
                return result;
            }

            if (!(evaluatedValue is bool))
            {
                result.Messages.Add("Result of the JS expression has to be a boolean");
                return result;
            }

            List<JobAction> actionList;

            if ((bool)evaluatedValue)
            {
                result.Messages.Add("Take actions from true stack");
                actionList = StackTrue;
            }
            else
            {
                result.Messages.Add("Take actions from false stack");
                actionList = StackFalse;
            }

            foreach (var action in actionList)
            {
                action.Logger = Logger;
                action.Project = Project;
                action.Config = Config;
                action.ProcessId = ProcessId;
                action.ResultItems = ResultItems;

                var innerResult = action.Execute();

                if (!innerResult.Succesfully)
                {
                    Logger.LogError(string.Join(",", innerResult.Messages));
                }

                if (innerResult.Succesfully && innerResult.Messages.Any())
                {
                    Logger.LogInformation(string.Join(",", innerResult.Messages));
                }

                ResultItems = action.ResultItems;
            }

            result.Succesfully = true;
            return result;
        }
    }
}
