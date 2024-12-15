using BMapr.GDAL.WebApi.Models;
using OSGeo.GDAL;

namespace BMapr.GDAL.WebApi.Services
{
    public static class RasterDatasetService
    {
        public static Result ExtractBand(byte[] content, string format, string copyPath)
        {
            var result = new Result(){Succesfully = true};

            try
            {
                //GdalConfiguration.ConfigureGdal();

                var memoryFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{format}");

                File.WriteAllBytes(memoryFilePath, content);

                var ds = Gdal.Open(memoryFilePath, Access.GA_Update);
                var latestIndex = ds.RasterCount;

                var dsOneBand = GetDatasetWithExtractBand(ds, latestIndex);

                var saveFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_save.{format}");
                string[] options = null;

                var datasetOutput = Gdal.GetDriverByName("netCDF").CreateCopy(saveFilePath, dsOneBand, 0, options, null, null);

                if (!string.IsNullOrEmpty(copyPath))
                {
                    File.Copy(saveFilePath, copyPath, true);
                }

                datasetOutput.Dispose();
                dsOneBand.Dispose();
                ds.Dispose();
            }
            catch (Exception ex)
            {
                result.Succesfully = false;
                result.Exceptions.Add(ex);
                result.Messages.Add(ex.Message);
            }

            return result;
        }

        private static Dataset GetDatasetWithExtractBand(Dataset dataset, int useBand)
        {
            var driver = Gdal.GetDriverByName("MEM");
            var options = new string[] { };

            Dataset datasetExtract = driver.Create("", dataset.RasterXSize, dataset.RasterYSize, 1, dataset.GetRasterBand(1).DataType, options);

            var geoTransform = new double[6];
            dataset.GetGeoTransform(geoTransform);

            var georefDataset = new double[]
            {
                geoTransform[0], // xmin
                geoTransform[1], // pixel width
                geoTransform[2],
                geoTransform[3], // ymax
                geoTransform[4],
                geoTransform[5], // pixel height
            };

            datasetExtract.SetGeoTransform(georefDataset);
            datasetExtract.SetProjection(dataset.GetProjection());

            int size = datasetExtract.RasterXSize * datasetExtract.RasterYSize * (Gdal.GetDataTypeSize(dataset.GetRasterBand(1).DataType) / 8);

            for (int i = 0; i < dataset.RasterCount; i++)
            {
                if (i + 1 != useBand)
                {
                    continue;
                }

                var sourceBand = dataset.GetRasterBand(i + 1);
                var destBand = datasetExtract.GetRasterBand(1);

                var buffer = new double[size];
                sourceBand.ReadRaster(0, 0, sourceBand.XSize, sourceBand.YSize, buffer, sourceBand.XSize, sourceBand.YSize, 0, 0);
                destBand.WriteRaster(0, 0, destBand.XSize, destBand.YSize, buffer, destBand.XSize, destBand.YSize, 0, 0);
            }

            return datasetExtract;
        }
    }
}
