namespace BMapr.GDAL.WebApi.Models.Spatial
{
    public class Extent
    {
        public double Xmin { get; set; }
        public double Ymin { get; set; }
        public double Xmax { get; set; }
        public double Ymax { get; set; }
        public int Crs { get; set; } = 0;

        #region readonly properties

        public double AspectRatio
        {
            get
            {
                if (Height == 0 && Width == 0)
                {
                    return 1;
                }
                if (Height == 0)
                {
                    return double.MaxValue;
                }
                return Width / Height;
            }
        }

        public double Width
        {
            get
            {
                return Math.Abs(Xmax - Xmin);
            }
        }

        public double Height
        {
            get
            {
                return Math.Abs(Ymax - Ymin);
            }
        }

        public Point Center
        {
            get
            {
                return new Point()
                {
                    X = (Xmax + Xmin) / 2,
                    Y = (Ymax + Ymin) / 2
                };
            }
        }

        public Point UpperLeftCorner
        {
            get
            {
                return new Point()
                {
                    X = Xmin,
                    Y = Ymax
                };
            }
        }

        public double Area
        {
            get
            {
                return Width * Height;
            }
        }

        public string GeometryWkt
        {
            get
            {
                return String.Format("POLYGON(({0} {1},{0} {3},{2} {3}, {2} {1}, {0} {1}))",
                    Xmin,
                    Ymin,
                    Xmax,
                    Ymax
                    );
            }
        }

        public double[] Array
        {
            get
            {
                return new double[] { Xmin, Ymin, Xmax, Ymax };
            }
        }

        #endregion

        #region constructors

        public Extent()
        {
        }

        public Extent(double xmin, double ymin, double xmax, double ymax)
        {
            Xmin = Math.Min(xmin, xmax);
            Ymin = Math.Min(ymin, ymax);
            Xmax = Math.Max(xmin, xmax);
            Ymax = Math.Max(ymin, ymax);
        }

        public Extent(Point point, double width, double height)
        {
            var hWidth = Math.Abs(width) / 2;
            var hHeight = Math.Abs(height) / 2;

            Xmin = point.X - hWidth;
            Xmax = point.X + hWidth;
            Ymin = point.Y - hHeight;
            Ymax = point.Y + hHeight;
        }

        public Extent(double[] extent)
        {
            if (extent.Length != 4)
            {
                throw new FormatException("extent has to be an array with 4 value");
            }

            Xmin = extent[0];
            Ymin = extent[1];
            Xmax = extent[2];
            Ymax = extent[3];
        }
        #endregion

        /// <summary>
        /// Increases/decreases rectangle size additively in width and height
        /// </summary>
        /// <remarks>
        /// The center of the rectangle remains unchanged during the operation.<br />
        /// addWidth and addHeight may be negative. There is no check whether the
        /// resulting rectangle has negative width or height.
        /// </remarks>

        public void Blow(double addWidth, double addHeight)
        {
            double awd2 = addWidth / 2;
            Xmin -= awd2;
            Xmax += awd2;
            double ahd2 = addHeight / 2;
            Ymin -= ahd2;
            Ymax += ahd2;
        }

        /// <summary>
        /// Rotate the extent
        /// </summary>
        /// <param name="angle">angle in radians</param>
        /// <returns></returns>

        public Extent Rotate(double angle)
        {
            angle = Math.Abs(angle) % (Math.PI);

            if (angle > Math.PI / 2)
            {
                angle = angle - (Math.PI / 2);
            }

            var rotatedWidth = Width * Math.Cos(angle) + Height * Math.Sin(angle);
            var rotatedHeight = Height * Math.Cos(angle) + Width * Math.Sin(angle);
            return new Extent(Center, rotatedWidth, rotatedHeight);
        }

        public bool Intersection(Extent gaExtent)
        {
            if (gaExtent.Xmax < Xmin || gaExtent.Xmin > Xmax)
            {
                return false;
            }

            if (gaExtent.Ymax < Ymin || gaExtent.Ymin > Ymax)
            {
                return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            double tol = 0.001;
            if (obj.GetType().Equals(typeof(Extent)))
            {
                var ext = obj as Extent;
                return Math.Abs(ext.Xmax - this.Xmax) < tol && Math.Abs(ext.Xmin - this.Xmin) < tol && Math.Abs(ext.Ymax - this.Ymax) < tol && Math.Abs(ext.Ymin - this.Ymin) < tol;
            }
            return base.Equals(obj);
        }

        public override string ToString()
        {
            return string.Format("({0},{1}/{2},{3})", this.Xmin, this.Ymin, this.Xmax, this.Ymax);
        }

        public Extent Join(Extent extent)
        {
            return new Extent()
            {
                Xmin = Math.Min(this.Xmin, extent.Xmin),
                Ymin = Math.Min(this.Ymin, extent.Ymin),
                Xmax = Math.Max(this.Xmax, extent.Xmax),
                Ymax = Math.Max(this.Ymax, extent.Ymax)
            };
        }
    }
}
