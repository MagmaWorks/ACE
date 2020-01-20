using FireDesign;
using MWGeometry;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ColumnDesign
{
    public class LayoutView : ViewModelBase
    {
        GeoShape shape;
        public GeoShape Shape
        {
            get { return shape; }
            set { shape = value; RaisePropertyChanged(nameof(Shape)); }
        }

        PointCollection contourPoints;
        public PointCollection ContourPoints
        {
            get { return contourPoints; }
            set { contourPoints = value; RaisePropertyChanged(nameof(ContourPoints)); }
        }

        List<RebarObj> rebars = new List<RebarObj>();
        public List<RebarObj> Rebars
        {
            get { return rebars; }
            set { rebars = value; RaisePropertyChanged(nameof(Rebars)); }
        }

        double radius;
        public double Radius
        {
            get { return radius; }
            set { radius = value; RaisePropertyChanged(nameof(Radius)); }
        }

        double diameter;
        public double Diameter
        {
            get { return diameter; }
            set { diameter = value; RaisePropertyChanged(nameof(Diameter)); }
        }

        Point circCenter;
        public Point CircCenter
        {
            get { return circCenter; }
            set { circCenter = value; RaisePropertyChanged(nameof(CircCenter)); }
        }

        // Points for the X and Y axis
        Point xAxis1;
        public Point XAxis1
        {
            get { return xAxis1; }
            set { xAxis1 = value; RaisePropertyChanged(nameof(XAxis1)); }
        }

        Point xAxis2;
        public Point XAxis2
        {
            get { return xAxis2; }
            set { xAxis2 = value; RaisePropertyChanged(nameof(XAxis2)); }
        }

        Point yAxis1;
        public Point YAxis1
        {
            get { return yAxis1; }
            set { yAxis1 = value; RaisePropertyChanged(nameof(YAxis1)); }
        }

        Point yAxis2;
        public Point YAxis2
        {
            get { return yAxis2; }
            set { yAxis2 = value; RaisePropertyChanged(nameof(YAxis2)); }
        }

        // Additional points for the arrows
        PointCollection xArrow;
        public PointCollection XArrow
        {
            get { return xArrow; }
            set { xArrow = value; RaisePropertyChanged(nameof(XArrow)); }
        }

        PointCollection yArrow;
        public PointCollection YArrow
        {
            get { return yArrow; }
            set { yArrow = value; RaisePropertyChanged(nameof(YArrow)); }
        }

        public double sf = 1e-1;

        public Column column;

        // user control dimensions
        double winWidth;
        double winHeight;

        // Temperature profiles
        //TemperatureProfile tp;
        //public TemperatureProfile TP
        //{
        //    get { return tp; }
        //    set { tp = value; RaisePropertyChanged(nameof(TP)); }
        //}

        public List<double> TempLevels = new List<double>();

        ObservableCollection<Contour> tempContours;
        public ObservableCollection<Contour> TempContours
        {
            get { return tempContours; }
            set { tempContours = value; RaisePropertyChanged(nameof(TempContours)); }
        }

        bool displayFire = true;
        public bool DisplayFire
        {
            get { return displayFire; }
            set { displayFire = value; RaisePropertyChanged(nameof(DisplayFire)); }
        }

        Point fireButtonPos;
        public Point FireButtonPos
        {
            get { return fireButtonPos; }
            set { fireButtonPos = value; RaisePropertyChanged(nameof(FireButtonPos)); }
        }
        
        public LayoutView()
        {

        }

        public void UpdateLayout(Column col = null)
        {
            column = col ?? column;

            Shape = column.Shape;

            if(shape == GeoShape.Rectangular)
            {
                ContourPoints = new PointCollection(new List<Point>()
                {
                    new Point(-column.LX/2*sf,-column.LY/2*sf),
                    new Point(column.LX/2*sf,-column.LY/2*sf),
                    new Point(column.LX/2*sf,column.LY/2*sf),
                    new Point(-column.LX/2*sf,column.LY/2*sf)
                });
            }
            else if (shape == GeoShape.Circular)
            {
                Diameter = column.Diameter * sf;
                CircCenter = new Point(-column.Diameter / 2.0 * sf, -column.Diameter / 2.0 * sf);
            }
            else if(shape == GeoShape.Polygonal)
            {
                ContourPoints = new PointCollection();
                int n = column.Edges;
                double R = column.Radius;
                for (int i = 0; i < n; i++)
                {
                    double theta = i * 2 * Math.PI / n;
                    double X = R * Math.Cos(theta) * sf;
                    double Y = R * Math.Sin(theta) * sf;
                    ContourPoints.Add(new Point(X, Y));
                }
            }
            else if (shape == GeoShape.LShaped)
            {
                ContourPoints = new PointCollection()
                {
                    new Point(-column.HX / 2, -column.HY / 2),
                    new Point(column.HX / 2, -column.HY / 2),
                    new Point(column.HX / 2, -column.HY / 2 + column.hY),
                    new Point(-column.HX / 2 + column.hX, -column.HY / 2 + column.hY),
                    new Point(-column.HX / 2 + column.hX, column.HY / 2),
                    new Point(-column.HX / 2, column.HY / 2)
                };
                ContourPoints = new PointCollection(ContourPoints.Select(p => new Point(p.X * sf, p.Y * sf)));
            }

            rebars = new List<RebarObj>();
            if(shape == GeoShape.Rectangular)
            {
                for (int i = 0; i < column.NRebarX; i++)
                    for (int j = 0; j < column.NRebarY; j++)
                    {
                        // Bar is only added if we are not "in the middle"
                        if (!((j != 0 && j != column.NRebarY - 1) && (i != 0 && i != column.NRebarX - 1)))
                        {
                            double d = column.CoverToLinks + column.LinkDiameter + column.BarDiameter / 2;
                            double lx = column.LX - 2 * d;
                            double ly = column.LY - 2 * d;
                            Point pt = new Point()
                            {
                                X = (d + i * lx / (column.NRebarX - 1) - column.LX / 2 - column.BarDiameter / 2) * sf,
                                Y = (d + j * ly / (column.NRebarY - 1) - column.LY / 2 - column.BarDiameter / 2) * sf
                            };
                            rebars.Add(new RebarObj(pt, column.BarDiameter * sf));
                        }

                    }
            }
            else if(shape == GeoShape.Circular)
            {
                rebars = new List<RebarObj>();
                int n = column.NRebarCirc;
                double R = column.Diameter / 2;
                for (int i = 0; i < n; i++)
                {
                    double theta = i * 2 * Math.PI / n;
                    double X = ((R - column.CoverToLinks - column.LinkDiameter) * Math.Cos(theta) - column.BarDiameter / 2) * sf;
                    double Y = ((R - column.CoverToLinks - column.LinkDiameter) * Math.Sin(theta) - column.BarDiameter / 2) * sf;
                    rebars.Add(new RebarObj(new Point(X, Y), column.BarDiameter * sf));
                }
            }
            else if(shape == GeoShape.Polygonal)
            {
                rebars = new List<RebarObj>();
                int n = column.Edges;
                double R = column.Radius;
                double d = (column.CoverToLinks + column.LinkDiameter + column.BarDiameter / 2) / Math.Sin((column.Edges - 2.0) * Math.PI / (2.0 * column.Edges) );
                for (int i = 0; i < n; i++)
                {
                    double theta = i * 2 * Math.PI / n;
                    double X = ((R - d) * Math.Cos(theta) - column.BarDiameter / 2 ) * sf;
                    double Y = ((R - d) * Math.Sin(theta) - column.BarDiameter / 2 ) * sf;
                    rebars.Add(new RebarObj(new Point(X, Y), column.BarDiameter * sf));
                }
            }
            else if (shape == GeoShape.LShaped)
            {
                rebars = new List<RebarObj>();
                List<Point> pts = column.GetLShapedRebars();
                for (int i = 0; i < pts.Count; i++)
                {
                    rebars.Add(new RebarObj(new Point((pts[i].X - column.BarDiameter / 2) * sf, (pts[i].Y - column.BarDiameter / 2) * sf), column.BarDiameter * sf));
                }
            }

            Radius = column.BarDiameter / 2.0;

            XAxis1 = new Point(-(column.LX/2 + 100) * sf, 0);
            XAxis2 = new Point((column.LX/2 + 100) * sf, 0);
            YAxis1 = new Point(0,-(column.LY/2 + 100) * sf);
            YAxis2 = new Point(0,(column.LY/2 + 100) * sf);

            XArrow = new PointCollection()
            {
                xAxis2,
                new Point(xAxis2.X - 5, xAxis2.Y + 2.5),
                new Point(xAxis2.X - 5, xAxis2.Y - 2.5)
            };

            YArrow = new PointCollection()
            {
                yAxis1,
                new Point(yAxis1.X - 2.5, yAxis1.Y + 5),
                new Point(yAxis1.X + 2.5, yAxis1.Y + 5)
            };

            fireButtonPos = new Point(winWidth / 2 - 60, winHeight / 2 - 60);

            if (tempContours?.Count > 0)
                GetContourPolygons();

            RaisePropertyChanged(nameof(Rebars));
            RaisePropertyChanged(nameof(Shape));
            RaisePropertyChanged(nameof(TempContours));
        }

        public void LoadGraph(Column c)
        {
            //if(c.TP.TempMap)
            //TP = new TemperatureProfile(c.LX / 1000, c.LY / 1000, c.R * 60);
            //TP.GetContours();
            //c.TP = TP;
            TempContours = new ObservableCollection<Contour>(c.TP.ContourPts.Select(x => new Contour(x)));
            TempLevels = c.TP.Levels;
            
            GetContourPolygons();
        }
        
        private void GetContourPolygons()
        {
            double h = column.LY * sf / tempContours.Count;
            tempContours.ToList().ForEach(d => d.DisplayPoints = new PointCollection(d.Points.Select(x => new Point(x.X * sf, x.Y * sf))));
            //tempContours.ToList().ForEach(d =>
            //    d.DisplayPoints = new PointCollection(
            //        d.Points.Select(p => new Point((column.LX / 2 - p.X * 1e3) * sf, (column.LY / 2 - p.Y * 1e3) * sf))
            //        .Concat(d.Points.Select(p => new Point((column.LX / 2 - p.X * 1e3) * sf, -(column.LY / 2 - p.Y * 1e3) * sf)).Reverse())
            //        .Concat(d.Points.Select(p => new Point(-(column.LX / 2 - p.X * 1e3) * sf, -(column.LY / 2 - p.Y * 1e3) * sf)))
            //        .Concat(d.Points.Select(p => new Point(-(column.LX / 2 - p.X * 1e3) * sf, (column.LY / 2 - p.Y * 1e3) * sf)).Reverse())
            //        ));
            tempContours[0].ContourPolygons = tempContours[0].DisplayPoints;
            tempContours[0].Color = new SolidColorBrush(Rainbow(0.67));
            tempContours[0].ScaleRectangles = new PointCollection(new List<Point>()
                {
                    new Point(winWidth / 2 - 100,column.LY/2*sf),
                    new Point(winWidth / 2 - 80,column.LY/2*sf),
                    new Point(winWidth / 2 - 80,column.LY/2*sf - h),
                    new Point(winWidth / 2 - 100,column.LY/2*sf - h)
                });
            tempContours[0].KeyPos = new Point(winWidth / 2 - 70, column.LY / 2 * sf - h - 5);
            tempContours[0].Level = TempLevels[0];
            for (int i = 1; i < tempContours.Count; i++)
            {
                tempContours[i].ContourPolygons = new PointCollection(
                    tempContours[i].DisplayPoints.Concat(tempContours[i - 1].DisplayPoints.Reverse())
                    );
                tempContours[i].Color = new SolidColorBrush(Rainbow( ( 1.0 - i * 1.0 / (tempContours.Count - 1))/ 1.5));
                tempContours[i].ScaleRectangles = new PointCollection(new List<Point>()
                    {
                        new Point(winWidth / 2 - 100, column.LY/2*sf - i * h),
                        new Point(winWidth / 2 - 80, column.LY/2*sf - i * h),
                        new Point(winWidth / 2 - 80, column.LY/2*sf - (i+1) * h),
                        new Point(winWidth / 2 - 100, column.LY/2*sf - (i+1) * h)
                    });
                tempContours[i].KeyPos = new Point(winWidth / 2 - 70, column.LY / 2 * sf - (i + 1) * h - 5);
                tempContours[i].Level = TempLevels[i];
            }
            
            RaisePropertyChanged(nameof(TempContours));

        }

        public static Color Rainbow(double progress)
        {
            double div = (Math.Abs(progress % 1) * 6);
            int ascending = (int)((div % 1) * 255);
            int descending = 255 - ascending;

            switch ((int)div)
            {
                case 0:
                    return Color.FromArgb(150, 255, Convert.ToByte(ascending), 0);
                case 1:
                    return Color.FromArgb(150, Convert.ToByte(descending), 255, 0);
                case 2:
                    return Color.FromArgb(150, 0, 255, Convert.ToByte(ascending));
                case 3:
                    return Color.FromArgb(150, 0, Convert.ToByte(descending), 255);
                case 4:
                    return Color.FromArgb(150, Convert.ToByte(ascending), 0, 255);
                default: // case 5:
                    return Color.FromArgb(150, 255, 0, Convert.ToByte(descending));
            }
        }

        public void LayoutSizeChanged(double h, double w)
        {
            try
            {
                double height = column.LY;
                double width = column.LX;
                if(column.Shape == GeoShape.Circular)
                {
                    height = column.Diameter;
                    width = column.Diameter;
                }
                else if(column.Shape == GeoShape.Polygonal)
                {
                    height = 2 * column.Radius;
                    width = 2 * column.Radius;
                }
                else if (column.Shape == GeoShape.LShaped)
                {
                    height = column.HY;
                    width = column.HX;
                }
                if (column != null)
                {
                    winHeight = h;
                    winWidth = w;

                    double sf0 = 2.0 / 3 * winHeight / height;
                    double sf1 = 2.0 / 3 * winWidth / width;

                    sf = Math.Min(sf0, sf1);

                    UpdateLayout();
                }
            }
            catch { }
        }

    }
    
    public class RebarObj
    {
        public Point Position { get; set; } = new Point(0, 0);
        public double Radius { get; set; } = 0;

        public RebarObj(Point pt, double r)
        {
            Position = pt;
            Radius = r;
        }
        
    }

    public class Contour : ViewModelBase
    {
        public List<MWPoint2D> Points { get; set; }
        public PointCollection DisplayPoints { get; set; }
        PointCollection contourPolygons;
        public PointCollection ContourPolygons
        {
            get { return contourPolygons; }
            set { contourPolygons = value; RaisePropertyChanged(nameof(ContourPolygons)); }
        }

        PointCollection scaleRectangles;
        public PointCollection ScaleRectangles
        {
            get { return scaleRectangles; }
            set { scaleRectangles = value; RaisePropertyChanged(nameof(ScaleRectangles)); }
        }

        public Brush Color { get; set; }
        public double Level { get; set; }
        Point keyPos;
        public Point KeyPos
        {
            get { return keyPos; }
            set { keyPos = value; RaisePropertyChanged(nameof(KeyPos)); }
        }

        public Contour()
        {

        }

        public Contour(FireDesign.Contour c)
        {
            Points = c.Points;
        }
    }
}
