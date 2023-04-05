using System.Text;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobActionConvert : JobAction
    {
        public enum FunctionType
        {
            ByteToStringUtf8
        }

        public string ReferenceName { get; set; }
        public string ResultReferenceName { get; set; }
        public FunctionType Type { get; set; }

        public override Result Execute()
        {
            var result = new Result() { Succesfully = false };

            if (!ResultItems.ContainsKey(ReferenceName))
            {
                result.AddMessage($"Reference name <{ReferenceName}> is unknown");
            }

            if (!Active)
            {
                result.Succesfully = true;
                result.AddMessage("Convert action is deactivated");
                return result;
            }

            try
            {
                switch (Type)
                {
                    case FunctionType.ByteToStringUtf8:
                        var convertedContent = Encoding.UTF8.GetString((byte[])ResultItems[ReferenceName].Value);
                        AddValue(ResultReferenceName, JobActionResultItemType.String, convertedContent);
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Exceptions.Add(ex);
            }

            return result;
        }
    }
}
