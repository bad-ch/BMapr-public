using System.Globalization;
using BMapr.GDAL.WebApi.Models.Spatial.Vector;
using BMapr.GDAL.WebApi.Models.Tracking;
using LiteDB;

namespace BMapr.GDAL.WebApi.Services
{
    public class TrackingService
    {
        [Obsolete]
        public static List<LocationSkeleton> GetCurrentPosition(string projectPath, List<string> tidsExclude)
        {
            var positions = new List<LocationSkeleton>();
            var cards = GetCards(projectPath, tidsExclude);

            foreach (var card in cards)
            {
                var dbPath = Path.Combine(projectPath, $"{card.Tid}.db");

                if (!File.Exists(dbPath))
                {
                    continue;
                }

                using (var dbLocation = new LiteDatabase($"Filename={dbPath};connection=shared"))
                {
                    var colLocation = dbLocation.GetCollection<Location>("locations");

                    var location = colLocation.Query().OrderByDescending(x => x.TimestampSecondsLocationFix).FirstOrDefault();
                    positions.Add(new LocationSkeleton()
                    {
                        Type = "Location",
                        Tid = location.Tid,
                        Longitude = location.Longitude,
                        Latitude = location.Latitude,
                        TimestampSecondsLocationFix = location.TimestampSecondsLocationFix
                    });
                }
            }

            return positions;
        }

        public static List<Card> GetCards(string projectPath, List<string> tidsExclude)
        {
            var dbName = $"cards.db";
            var dbPath = Path.Combine(projectPath, dbName);
            var cards = new List<Card>();

            using (var dbCards = new LiteDatabase($"Filename={dbPath};connection=shared"))
            {
                var col = dbCards.GetCollection<Card>("cards");

                cards = col.Query().Where(x => !tidsExclude.Contains(x.Tid)).ToList();
            }

            return cards;
        }

        public static Dictionary<Card, List<Location>> GetLatestPosition(string projectPath, List<string> tidsExclude)
        {
            var positions = new Dictionary<Card, List<Location>>();
            var cards = GetCards(projectPath, tidsExclude);

            foreach (var card in cards)
            {
                var dbPath = Path.Combine(projectPath, $"{card.Tid}.db");

                if (!File.Exists(dbPath))
                {
                    continue;
                }

                using (var dbLocation = new LiteDatabase($"Filename={dbPath};connection=shared"))
                {
                    var colLocation = dbLocation.GetCollection<Location>("locations");
                    var locations = new List<Location>(){ colLocation.Query().OrderByDescending(x => x.TimestampSecondsLocationFix).FirstOrDefault() };
                    positions.Add(card, locations);
                }
            }

            return positions;
        }

        public static Dictionary<Card,List<Location>> GetPositions(string projectPath, List<string> tidsExclude, DateTime start, DateTime end)
        {
            var positions = new Dictionary<Card, List<Location>>();
            var cards = GetCards(projectPath, tidsExclude);

            var unixTsStart =((DateTimeOffset)start).ToUnixTimeSeconds();
            var unixTsEnd = ((DateTimeOffset)end).ToUnixTimeSeconds();

            foreach (var card in cards)
            {
                var dbPath = Path.Combine(projectPath, $"{card.Tid}.db");

                if (!File.Exists(dbPath))
                {
                    continue;
                }

                using (var dbLocation = new LiteDatabase($"Filename={dbPath};connection=shared"))
                {
                    var colLocation = dbLocation.GetCollection<Location>("locations");
                    var locations = colLocation.Query().Where(x=> x.TimestampSecondsLocationFix >= unixTsStart && x.TimestampSecondsLocationFix <= unixTsEnd).OrderByDescending(x=>x.TimestampSecondsLocationFix).ToList();
                    positions.Add(card, locations);
                }
            }

            return positions;
        }

        public static FeatureCollection GetGeojson(Dictionary<Card, List<Location>> trackData)
        {
            var featureCollection = new FeatureCollection(){ Type= "FeatureCollection" };

            foreach (var card in trackData)
            {
                var feature = new Feature() {Type = "Feature"};

                if (card.Value.Count == 0)
                {
                    continue;
                }

                feature.Geometry = new Geometry(){ Type = card.Value.Count == 1 ? "Point" : "LineString" };

                if (card.Value.Count == 1)
                {
                    feature.Geometry.AddPoint(card.Value[0].Longitude, card.Value[0].Latitude);  
                }
                else
                {
                    feature.Geometry.AddLineString(card.Value.Select(x=> new double[]{x.Longitude, x.Latitude}).ToList());
                }

                var latestTimeStamp = card.Value[0].TimestampSecondsLocationFix;
                var latestDateTime = TimeService.UnixSecondsToDateTime(latestTimeStamp, true);

                feature.Properties.Add("tid", card.Key.Tid);
                feature.Properties.Add("name", card.Key.Name);
                feature.Properties.Add("face", card.Key.Face);
                feature.Properties.Add("latestTimeStamp", latestTimeStamp);
                feature.Properties.Add("latestDateTime", latestDateTime.ToString("o", CultureInfo.InvariantCulture));

                featureCollection.Features.Add(feature);
            }

            return featureCollection;
        }
    }
}
