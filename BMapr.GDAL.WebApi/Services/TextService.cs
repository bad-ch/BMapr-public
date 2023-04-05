using System.Text.RegularExpressions;

namespace BMapr.GDAL.WebApi.Services
{
    public class TextService
    {
        public static List<string> GetContentBetween(string value, string startTag, string endTag)
        {
            MatchCollection matches = Regex.Matches(value, @$"{startTag}([A-Za-z0-9]*){endTag}");

            var contentMatch = new List<string>();

            foreach (Match match in matches)
            {
                contentMatch.Add(match.Groups[1].Value);
            }

            return contentMatch;
        }

        public static string? GetContentBetweenSimple(string value, string startTag, string endTag)
        {
            var start = value.IndexOf(startTag, StringComparison.InvariantCulture);
            var end = value.IndexOf(endTag, StringComparison.InvariantCulture);
            var content = string.Empty;

            if (start >= 0 && end > 0 && !string.IsNullOrEmpty(value))
            {
                content = new string(value.Skip(start+startTag.Length).Take(end-start-startTag.Length).ToArray());
            }

            return content;
        }

    }
}
