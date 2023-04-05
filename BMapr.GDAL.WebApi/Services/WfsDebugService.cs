namespace BMapr.GDAL.WebApi.Services
{
    public class WfsDebugService
    {
        public static void WriteTransactioonRequest(string dataProjectPath, string transactionContent)
        {
            var wfstDebugFolder = IniFolders(dataProjectPath);

            var now = DateTime.Now;
            var contentFilenameTransaction = Path.Combine(wfstDebugFolder, $"{now.Ticks}_{now:yyyyMMdd-HHmmss}_transaction_request.xml");

            File.WriteAllText(contentFilenameTransaction, transactionContent);
        }

        public static void WriteTransactionResponse(string dataProjectPath, string transactionResponseContent)
        {
            var wfstDebugFolder = IniFolders(dataProjectPath);

            var now = DateTime.Now;
            var contentFilenameTransaction = Path.Combine(wfstDebugFolder, $"{now.Ticks}_{now:yyyyMMdd-HHmmss}_transaction_response.xml");

            File.WriteAllText(contentFilenameTransaction, transactionResponseContent);
        }

        public static void WriteSqlTransaction(string dataProjectPath, string transactionSqlContent)
        {
            var wfstDebugFolder = IniFolders(dataProjectPath);

            var now = DateTime.Now;
            var contentFilenameTransaction = Path.Combine(wfstDebugFolder, $"{now.Ticks}_{now:yyyyMMdd-HHmmss}_transaction_sql.sql");

            File.WriteAllText(contentFilenameTransaction, transactionSqlContent);
        }

        private static string IniFolders(string dataProjectPath)
        {
            var debugFolder = Path.Combine(dataProjectPath, "_debug");

            if (!Directory.Exists(debugFolder))
            {
                Directory.CreateDirectory(debugFolder);
            }

            var wfstDebugFolder = Path.Combine(debugFolder, "wfs-t");

            if (!Directory.Exists(wfstDebugFolder))
            {
                Directory.CreateDirectory(wfstDebugFolder);
            }

            return wfstDebugFolder;
        }
    }
}
