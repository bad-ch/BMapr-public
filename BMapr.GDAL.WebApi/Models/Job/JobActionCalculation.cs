using Jint;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobActionCalculation : JobAction
    {
        public string Expression { get; set; }

        public Dictionary<string, object> Items { get; set; }
        public string ResultReferenceName { get; set; }

        public override Result Execute()
        {
            var result = new Result() { Succesfully = false };

            if (string.IsNullOrEmpty(Expression))
            {
                result.Messages.Add("Expression has to be a valid JS");
                return result;
            }

            if (!Active)
            {
                result.Succesfully = true;
                result.AddMessage("Calculation action is deactivated");
                return result;
            }

            var currentValues = this.ResultItems.ToDictionary(key => key.Key, value => value.Value.Value);

            foreach (var item in Items)
            {
                if (currentValues.ContainsKey(item.Key))
                {
                    Logger.LogError($"JobActionCalculation Item {item.Key} not used because already part of the result items");
                    continue;
                }

                currentValues.Add(item.Key, item.Value);
            }

            var engine = new Engine();

            foreach (var currentItem in currentValues)
            {
                var value = currentItem.Value;

                switch (value)
                {
                    case bool:
                    case string:
                    case int:
                    case long:
                    case double:
                    case decimal:
                        engine.SetValue(currentItem.Key, value);
                        break;
                    default:
                        result.Messages.Add($"Jint ignore value {currentItem.Key}, wrong type {value.GetType()}");
                        break;
                }
            }

            object evaluatedValue;

            try
            {
                evaluatedValue = engine.Evaluate(Expression).ToObject();
                result.Succesfully = true;
            }
            catch (Exception ex)
            {
                result.Messages.Add($"Evaluating JS fail {ex.Message}");
                return result;
            }

            AddValue(ResultReferenceName, JobActionResultItemType.ResultJint,evaluatedValue);

            return result;
        }
    }
}
