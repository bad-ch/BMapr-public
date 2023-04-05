using Serilog;
using System.Drawing.Imaging;
using System.Drawing;
using System.Net;
using System.Runtime.Versioning;

namespace BMapr.GDAL.WebApi.Services
{
    [SupportedOSPlatform("windows")]
    public class ImageService
    {
        public static Bitmap MergeTwoImages(Image firstImage, Image secondImage)
        {
            if (firstImage == null && secondImage == null)
            {
                return null;
            }

            if (firstImage == null)
            {
                return secondImage as Bitmap;
            }

            if (secondImage == null)
            {
                return firstImage as Bitmap;
            }

            if (firstImage.Width != secondImage.Width || firstImage.Height != secondImage.Height)
            {
                secondImage = ResizeImage(secondImage, new Size(firstImage.Width, firstImage.Height));
            }

            var outputImage = firstImage as Bitmap;

            if (outputImage == null)
            {
                return null;
            }

            using (Graphics graphics = Graphics.FromImage(outputImage))
            {
                graphics.DrawImage(secondImage, new Rectangle(new Point(), secondImage.Size), new Rectangle(new Point(), secondImage.Size), GraphicsUnit.Pixel);
            }

            return outputImage;
        }

        public static Image ResizeImage(Image image, Size size)
        {
            return new Bitmap(image, size); // TODO catch or document exception
        }

        public static Image GetImageFromBase64String(string base64String)
        {
            var patter = ";base64,";
            var index = base64String.IndexOf(patter, StringComparison.Ordinal);

            if (index >= 0)
            {
                base64String = base64String.Substring(index + patter.Length);
            }

            byte[] data = Convert.FromBase64String(base64String);
            Image image = null;

            using (var stream = new MemoryStream(data, 0, data.Length))
            {
                image = Image.FromStream(stream);
            }

            return image;
        }

        public static string GetBase64StringFromImage(Image image, System.Drawing.Imaging.ImageFormat format, string output = "html")
        {
            string base64String = String.Empty;
            string mimeType = String.Empty;

            string template = output == "html" ? "data:{0};base64,{1}" : "{1}";

            if (format == ImageFormat.Png)
            {
                mimeType = "image/png";
            }
            else if (format == ImageFormat.Gif)
            {
                mimeType = "image/gif";
            }
            else if (format == ImageFormat.Jpeg)
            {
                mimeType = "image/jpg";
            }
            else
            {
                format = ImageFormat.Png;
            }

            using (var memoryStream = new MemoryStream())
            {
                image.Save(memoryStream, format);
                byte[] imageBytes = memoryStream.ToArray();
                base64String = String.Format(template, mimeType, Convert.ToBase64String(imageBytes));
            }

            return base64String;
        }

        public static string GetHtmlImageFromBase64(string base64Image, System.Drawing.Imaging.ImageFormat format)
        {
            string mimeType = String.Empty;

            if (format == ImageFormat.Png)
            {
                mimeType = "image/png";
            }
            else if (format == ImageFormat.Gif)
            {
                mimeType = "image/gif";
            }
            else if (format == ImageFormat.Jpeg)
            {
                mimeType = "image/jpg";
            }
            else
            {
                return String.Empty;
            }

            return String.Format("data:{0};base64,{1}", mimeType, base64Image);
        }

        public static string GetBase64StringFromImage(string filePath)
        {
            Image image = null;

            if (!File.Exists(filePath))
            {
                return String.Empty;
            }

            try
            {
                image = Image.FromFile(filePath);
            }
            catch (Exception ex)
            {
                Log.Error("Exception occured", ex);
            }

            if (image == null)
            {
                return String.Empty;
            }

            var file = new FileInfo(filePath);
            ImageFormat imageFormat;

            switch (file.Extension.ToLower())
            {
                case ".png":

                    imageFormat = ImageFormat.Png;
                    break;

                case ".gif":

                    imageFormat = ImageFormat.Gif;
                    break;

                case ".jpg":
                case ".jpeg":

                    imageFormat = ImageFormat.Jpeg;
                    break;

                default:
                    return String.Empty;
            }

            return GetBase64StringFromImage(image, imageFormat);
        }

        public static byte[] ImageToByteArray(System.Drawing.Image image, System.Drawing.Imaging.ImageFormat format)
        {
            var ms = new MemoryStream();
            image.Save(ms, format);
            return ms.ToArray();
        }

        public static byte[] ImageToByteArray(System.Drawing.Image image, string mimeType)
        {
            ImageFormat format;

            switch (mimeType.ToLower())
            {
                case "image/png":

                    format = ImageFormat.Png;
                    break;

                case "image/jpg":
                case "image/jpeg":

                    format = ImageFormat.Jpeg;
                    break;

                default:

                    format = ImageFormat.Png;
                    break;
            }

            return ImageToByteArray(image, format);
        }

        public static Image GetImageFromUrl(Uri uri)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(uri);

            using (HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                using (Stream stream = httpWebReponse.GetResponseStream())
                {
                    return Image.FromStream(stream);
                }
            }
        }

        /// <summary>
        /// rotate image
        /// </summary>
        /// <param name="image"></param>
        /// <param name="angle">angle in radians, clockwise positiv, counterclockwise negativ</param>
        /// <param name="bkColor">backgroundcolor</param>
        /// <returns></returns>

        public static Image RotateImage(Image image, double angle, Color bkColor)
        {
            angle = angle / Math.PI * 180;

            angle = angle % 360;

            if (angle < 0)
            {
                angle += 360;
            }

            if (angle > 180)
            {
                angle -= 360;
            }

            var bmp = (Bitmap)image;

            var pixelformat = default(System.Drawing.Imaging.PixelFormat);
            pixelformat = bkColor == Color.Transparent ? PixelFormat.Format32bppArgb : bmp.PixelFormat;

            float sin = (float)Math.Abs(Math.Sin(angle * Math.PI / 180.0)); // this function takes radians
            float cos = (float)Math.Abs(Math.Cos(angle * Math.PI / 180.0)); // this one too
            float newImgWidth = sin * bmp.Height + cos * bmp.Width;
            float newImgHeight = sin * bmp.Width + cos * bmp.Height;
            float originX = 0f;
            float originY = 0f;

            if (angle > 0)
            {
                if (angle <= 90)
                {
                    originX = sin * bmp.Height;
                }
                else
                {
                    originX = newImgWidth;
                    originY = newImgHeight - sin * bmp.Width;
                }
            }
            else
            {
                if (angle >= -90)
                {
                    originY = sin * bmp.Width;
                }
                else
                {
                    originX = newImgWidth - sin * bmp.Height;
                    originY = newImgHeight;
                }
            }

            Bitmap newImg = new Bitmap((int)newImgWidth, (int)newImgHeight, pixelformat);
            Graphics g = Graphics.FromImage(newImg);

            g.Clear(bkColor);
            g.TranslateTransform(originX, originY); // offset the origin to our calculated values
            g.RotateTransform((float)angle); // set up rotate
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            g.DrawImageUnscaled(bmp, 0, 0); // draw the image at 0, 0
            g.Dispose();

            return (Image)newImg;
        }

        public static Image CropImage(Image image, int posx, int posy, int width, int height)
        {
            Rectangle cropRect = new Rectangle(posx, posy, width, height);

            Bitmap bitmapSource = (Bitmap)image;
            Bitmap bitmapTarget = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(bitmapTarget))
            {
                g.DrawImage(bitmapSource, new Rectangle(0, 0, bitmapTarget.Width, bitmapTarget.Height),
                                 cropRect,
                                 GraphicsUnit.Pixel);
            }

            return (Image)bitmapTarget;
        }

        public static bool CreateThumbnail(string filename, string filenameThumbnail, int width, int height)
        {
            var bitmap = CreateThumbnail(filename, width, height);

            if (bitmap == null)
            {
                return false;
            }

            var image = (Image)bitmap;

            image.Save(filenameThumbnail, ImageFormat.Jpeg);
            return true;
        }

        public static Bitmap CreateThumbnail(string filename, int width, int height)
        {
            System.Drawing.Bitmap bmpOut = null;
            try
            {
                Bitmap loBMP = new Bitmap(filename);
                ImageFormat loFormat = loBMP.RawFormat;

                decimal lnRatio;
                int lnNewWidth = 0;
                int lnNewHeight = 0;

                //*** If the image is smaller than a thumbnail just return it
                if (loBMP.Width < width && loBMP.Height < height)
                    return loBMP;

                if (loBMP.Width > loBMP.Height)
                {
                    lnRatio = (decimal)width / loBMP.Width;
                    lnNewWidth = width;
                    decimal lnTemp = loBMP.Height * lnRatio;
                    lnNewHeight = (int)lnTemp;
                }
                else
                {
                    lnRatio = (decimal)height / loBMP.Height;
                    lnNewHeight = height;
                    decimal lnTemp = loBMP.Width * lnRatio;
                    lnNewWidth = (int)lnTemp;
                }
                bmpOut = new Bitmap(lnNewWidth, lnNewHeight);
                Graphics g = Graphics.FromImage(bmpOut);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.FillRectangle(Brushes.White, 0, 0, lnNewWidth, lnNewHeight);
                g.DrawImage(loBMP, 0, 0, lnNewWidth, lnNewHeight);

                loBMP.Dispose();
            }
            catch
            {
                return null;
            }

            return bmpOut;
        }
    }
}
