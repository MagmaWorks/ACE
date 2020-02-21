using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using FireDesign;
using InteractionDiagram3D;
using MWGeometry;
using Newtonsoft.Json;
using Material = InteractionDiagram3D.Material;

namespace ColumnDesign
{
    public enum GeoShape { Rectangular, Circular, Polygonal, LShaped, TShaped }
    public enum FDesignMethod { Table, Isotherm_500, Zone_Method, Advanced }

    public class Column
    {
        public string Name { get; set; }
        // Geometry
        public GeoShape Shape { get; set; } = GeoShape.Rectangular;
        public bool IsRectangular { get => Shape == GeoShape.Rectangular; }
        public bool IsCircular { get => Shape == GeoShape.Circular; }
        public bool IsPolygonal { get => Shape == GeoShape.Polygonal; }
        public bool IsLShaped { get => Shape == GeoShape.LShaped; }
        public bool IsTShaped { get => Shape == GeoShape.TShaped; }
        public double Length { get; set; } = 3150;
        public double LX { get; set; } = 350;
        public double LY { get; set; } = 350;
        public double Angle { get; set; }
        public double Radius { get; set; } = 200;
        public double Diameter { get; set; } = 400;
        public int Edges { get; set; } = 5;
        public double HX { get; set; } = 600;
        public double hX { get; set; } = 300;
        public double HY { get; set; } = 400;
        public double hY { get; set; } = 200;
        public double sx1;
        public double sx2;
        public double sy1;
        public double sy2;
        public double Theta = 0;
        public int Nrebars;
        public MWPoint3D Point1 { get; set; }
        public MWPoint3D Point2 { get; set; }

        // Material
        public Concrete ConcreteGrade { get; set; }
        public Concrete CustomConcreteGrade { get; set; }
        public double MaxAggSize { get; set; } = 20;

        // Loads
        public Load SelectedLoad { get; set; }
        public Load FireLoad { get; set; }

        public List<Load> Loads { get; set; } = new List<Load>();
        public List<string> FireLoadNames { get => LoadNames.Where(l => l != "ALL LOADS").Append("0.7*[selected]").ToList(); }
        public List<string> LoadNames { get => Loads.Select(l => l.Name).ToList(); }
        public bool AllLoads { get => SelectedLoad.Name == "ALL LOADS"; }

        // Design
        public double EffectiveLength { get; set; } = 0.7;
        public double CoverToLinks { get; set; } = 40;
        public Steel SteelGrade { get; set; } = new Steel("500B", 500);
        public Steel CustomSteelGrade { get; set; }
        public int BarDiameter { get; set; } = 16;
        public int LinkDiameter { get; set; } = 10;
        public int NRebarX { get; set; } = 3;
        public int NRebarY { get; set; } = 3;
        public int NRebarCirc { get; set; } = 5;

        // Fire Design
        public int R { get; set; } = 120; // fire resistance in min
        public FireExposition SidesExposed { get; set; } = FireExposition.MoreThanOneSide;
        public FDesignMethod FireDesignMethod { get => (FDesignMethod) Enum.Parse(typeof(FDesignMethod), FDMStr); }
        //public FireDesignMethod FireDesignMethod { get; set; } = new FireDesignMethod(FDesignMethod.Table);
        public string FDMStr { get; set; } = "Table";
        public FCurve FireCurve { get => (FCurve)Enum.Parse(typeof(FCurve), FCStr); }
        public string FCStr { get; set; } = "Standard";
        [JsonIgnore]
        public TemperatureProfile TP;

        public int IDReduction { get; set; } = 100;
        [JsonIgnore]
        public List<MWPoint3D> diagramVertices = new List<MWPoint3D>();
        [JsonIgnore]
        public List<Tri3D> diagramFaces = new List<Tri3D>();

        [JsonIgnore]
        public List<MWPoint3D> fireDiagramVertices = new List<MWPoint3D>();
        [JsonIgnore]
        public List<Tri3D> fireDiagramFaces = new List<Tri3D>();

        // Checks
        public bool? CapacityCheck { get; set; } = false;
        public bool? FireCheck { get; set; } = false;
        public bool? SpacingCheck { get; set; } = false;
        public bool? MinMaxSteelCheck { get; set; } = false;
        public bool? MinRebarCheck { get; set; } = false;
        public bool GuidanceCheck { get; set; } = false;
        public string GuidanceMessage { get; set; } = "All good!";

        List<FireData> fireTable = new List<FireData>();
        List<ConcreteData> concreteData = new List<ConcreteData>();
        List<SteelData> steelData = new List<SteelData>();
        const double gs = 1.15;
        const double gc = 1.5;
        const double acc = 0.85;

        public Dictionary<double, double> CarbonData = new Dictionary<double, double>();
        public Dictionary<double, double[]> SteelCosts = new Dictionary<double, double[]>();
        public Dictionary<double, double[]> ConcreteCosts = new Dictionary<double, double[]>();

        public double UtilP { get; set; } = 0;
        public double UtilMx { get; set; } = 0;
        public double UtilMy { get; set; } = 0;

        const double concreteVolMass = 2.5e3;
        const double steelVolMass = 7.5e3;

        // Optimisation
        public double Cost { get; set; } = 0;

        // Interaction diagram settings
        public int DiagramDisc { get; set; } = 30;

        public Column()
        { }

        public Column(ETABSColumnDesign_Plugin.Column c0)
        {
            Name = c0.name;
            LX = c0.width;
            LY = c0.depth;
            Length = c0.length;
            CustomConcreteGrade = new Concrete("Custom", c0.fc, c0.E);
            ConcreteGrade = CustomConcreteGrade;
            CustomSteelGrade = new Steel("Custom", 400);
            /*M2Top = Math.Ceiling(c0.M2Top);
            M2Bot = Math.Ceiling(c0.M2Bot);
            M3Top = Math.Ceiling(c0.M3Top);
            M3Bot = Math.Ceiling(c0.M3Bot);
            P = Math.Ceiling(Math.Abs(c0.P));*/
            Point1 = MWPoint3D.point3DByCoordinates(c0.Point1.X, c0.Point1.Y, c0.Point1.Z);
            Point2 = MWPoint3D.point3DByCoordinates(c0.Point2.X, c0.Point2.Y, c0.Point2.Z);
            Angle = c0.Angle;
            Loads = c0.Loads.Select(l => new Load(l)).ToList();
            Load all = new Load() { Name = "ALL LOADS" };
            Loads.Insert(0, all);
            SelectedLoad = Loads[1];

            //NRebarX = (int)((LX - 2 * CoverToLinks) / 90);
            //NRebarY = (int)((LY - 2 * CoverToLinks) / 90);

            SetFireData();
            SetConcreteData();
            SetSteelData();
            InitializeCarbonData();
            InitializeConcreteCosts();
            InitializeSteelCosts();
        }

        public Column Clone()
        {
            Column col = new Column();
            col.Name = this.Name;
            col.Shape = this.Shape;
            col.LX = this.LX;
            col.LY = this.LY;
            col.Diameter = this.Diameter;
            col.Radius = this.Radius;
            col.Edges = this.Edges;
            col.Length = this.Length;
            col.Point1 = this.Point1;
            col.Point2 = this.Point2;
            col.Angle = this.Angle;

            col.CoverToLinks = this.CoverToLinks;
            col.EffectiveLength = this.EffectiveLength;
            col.fireTable = this.fireTable;
            col.LinkDiameter = this.LinkDiameter;
            col.MaxAggSize = this.MaxAggSize;
            col.R = this.R;
            col.FCStr = this.FCStr;
            col.FDMStr = this.FDMStr;

            col.ConcreteGrade = this.ConcreteGrade;
            col.CustomConcreteGrade = this.CustomConcreteGrade;
            col.SteelGrade = this.SteelGrade;
            col.CustomSteelGrade = this.CustomSteelGrade;
            col.SelectedLoad = this.SelectedLoad;
            col.Loads = this.Loads.Select(l => l.Clone()).ToList();
            col.FireLoad = this.FireLoad;

            col.NRebarX = this.NRebarX;
            col.NRebarY = this.NRebarY;
            col.BarDiameter = this.BarDiameter;
            
            col.diagramVertices = this.diagramVertices;
            col.diagramFaces = this.diagramFaces;

            col.CapacityCheck = this.CapacityCheck;
            col.FireCheck = this.FireCheck;
            col.SpacingCheck = this.SpacingCheck;
            col.MinMaxSteelCheck = this.MinMaxSteelCheck;
            col.MinRebarCheck = this.MinRebarCheck;

            col.Cost = this.Cost;

            col.DiagramDisc = this.DiagramDisc;

            return col;
        }

        public void GetInteractionDiagram()
        {
            List<Composite> composites = new List<Composite>();

            // Materials
            double fc_steel = Math.Min(0.00175 * SteelGrade.E * 1E3, SteelGrade.Fy / 1.15);
            Material concrete = new Material(ConcreteGrade.Name, MatYpe.Concrete, 0.85 * ConcreteGrade.Fc / 1.5, 0, ConcreteGrade.E, 0.00175, ConcreteGrade.Density);
            Material steel = new Material(SteelGrade.Name, MatYpe.Steel, SteelGrade.Fy / 1.15, SteelGrade.Fy / 1.15, SteelGrade.E);
            //Material steel = new Material(steelGrade.Name, MatYpe.Steel, fc_steel, fc_steel);

            if (Shape == GeoShape.Rectangular)
            {
                // Creation of the concrete section
                ConcreteSection cs = new ConcreteSection(new List<MWPoint2D>()
                                                    {
                                                        MWPoint2D.Point2DByCoordinates(0,0),
                                                        MWPoint2D.Point2DByCoordinates(LX,0),
                                                        MWPoint2D.Point2DByCoordinates(LX,LY),
                                                        MWPoint2D.Point2DByCoordinates(0,LY)
                                                    },
                                                    concrete);
                composites.Add(cs);

                // Creation of the rebars
                double xspace = (LX - 2 * (CoverToLinks + LinkDiameter + BarDiameter / 2.0)) / (NRebarX - 1);
                double yspace = (LY - 2 * (CoverToLinks + LinkDiameter + BarDiameter / 2.0)) / (NRebarY - 1);
                for (int i = 0; i < NRebarX; i++)
                {
                    var x = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + i * xspace;
                    for (int j = 0; j < NRebarY; j++)
                    {
                        if(i == 0 || i == NRebarX - 1 || j == 0  || j == NRebarY-1)
                        {
                            var y = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + j * yspace;
                            Rebar r = new Rebar(MWPoint2D.Point2DByCoordinates(x, y), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                            composites.Add(r);
                        }
                    }
                }
            }
            else if(Shape == GeoShape.Circular)
            {
                // Creation of the concrete section
                List<MWPoint2D> circlePts = new List<MWPoint2D>();
                double theta = 0;
                double N = 180;
                double inc = 2 * Math.PI / N;
                for(int i = 0; i < N; i++)
                {
                    theta = i * inc;
                    double X = Diameter / 2 * Math.Cos(theta);
                    double Y = Diameter / 2 * Math.Sin(theta);
                    circlePts.Add(new MWPoint2D(X, Y));
                }
                composites.Add(new ConcreteSection(circlePts, concrete));
                MWPoint2D bary = Points.GetBarycenter(circlePts);
                Console.WriteLine("conc : X = {0}, Y = {1}", bary.X, bary.Y);
                
                // Creation of the rebars
                List<MWPoint2D> steelPos = new List<MWPoint2D>();
                theta = 0;
                inc = 2 * Math.PI / NRebarCirc;
                for (int i = 0; i < NRebarCirc; i++)
                {
                    theta = i * inc;
                    double x = (Diameter / 2 - CoverToLinks - LinkDiameter - BarDiameter / 2.0) * Math.Cos(theta);
                    double y = (Diameter / 2 - CoverToLinks - LinkDiameter - BarDiameter / 2.0) * Math.Sin(theta);
                    Rebar r = new Rebar(new MWPoint2D(x, y), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                    steelPos.Add(new MWPoint2D(x, y));
                    composites.Add(r);
                }
                bary = Points.GetBarycenter(steelPos);
                Console.WriteLine("steel : X = {0}, Y = {1}", bary.X, bary.Y);
            }
            else if (Shape == GeoShape.Polygonal)
            {
                // Creation of the concrete section
                List<MWPoint2D> PolyPts = new List<MWPoint2D>();
                double theta = 0;
                double inc = 2 * Math.PI / Edges;
                for (int i = 0; i < Edges; i++)
                {
                    theta = i * inc;
                    double X = Radius * Math.Cos(theta);
                    double Y = Radius * Math.Sin(theta);
                    PolyPts.Add(new MWPoint2D(X, Y));
                }
                composites.Add(new ConcreteSection(PolyPts, concrete));

                MWPoint2D bary = Points.GetBarycenter(PolyPts);
                Console.WriteLine("conc : X = {0}, Y = {1}", bary.X, bary.Y);

                // Creation of the rebars
                theta = 0;
                List<MWPoint2D> steelPos = new List<MWPoint2D>();
                double dd = (CoverToLinks + LinkDiameter + BarDiameter / 2.0) / Math.Sin((Edges - 2.0) * Math.PI / (2.0 * Edges) );
                for (int i = 0; i < Edges; i++)
                {
                    theta = i * inc;
                    double x = (Radius - dd) * Math.Cos(theta);
                    double y = (Radius - dd) * Math.Sin(theta);
                    Rebar r = new Rebar(new MWPoint2D(x, y), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                    steelPos.Add(new MWPoint2D(x, y));
                    composites.Add(r);
                }

                bary = Points.GetBarycenter(steelPos);
                Console.WriteLine("steel : X = {0}, Y = {1}", bary.X, bary.Y);
            }
            else if (Shape == GeoShape.LShaped)
            {
                // creation of the concrete section
                List<Point> pts = GetLShapedContour();
                List<MWPoint2D> LShapedPts = pts.Select(p => new MWPoint2D(p.X, p.Y)).ToList();
                composites.Add(new ConcreteSection(LShapedPts, concrete));
                
                // creation of the rebars
                List<Point> rebars = GetLShapedRebars();
                for (int i = 0; i < rebars.Count; i++)
                {
                    Rebar r = new Rebar(new MWPoint2D(rebars[i].X, rebars[i].Y), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                    //steelPos.Add(new MWPoint2D(x, y));
                    composites.Add(r);
                }

            }
            else if (Shape == GeoShape.TShaped)
            {
                // creation of the concrete section
                List<Point> pts = GetTShapedContour();
                List<MWPoint2D> TShapedPts = pts.Select(p => new MWPoint2D(p.X, p.Y)).ToList();
                composites.Add(new ConcreteSection(TShapedPts, concrete));
                
                // creation of the rebars
                List<Point> rebars = GetTShapedRebars();
                for (int i = 0; i < rebars.Count; i++)
                {
                    Rebar r = new Rebar(new MWPoint2D(rebars[i].X, rebars[i].Y), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                    //steelPos.Add(new MWPoint2D(x, y));
                    composites.Add(r);
                }

            }

            Diagram d = new Diagram(composites,DiagramDisc);

            diagramVertices = d.vertices;
            diagramFaces = d.faces;

            if(IDReduction != 100)
            {
                double red = IDReduction / 100.0;
                for (int i = 0; i < diagramVertices.Count; i++)
                    diagramVertices[i] = new MWPoint3D(
                        red * diagramVertices[i].X, 
                        red * diagramVertices[i].Y, 
                        red * diagramVertices[i].Z
                        );
                for(int i = 0; i < diagramFaces.Count; i++)
                {
                    for(int j = 0; j < diagramFaces[i].Points.Count; j++)
                    {
                        diagramFaces[i].Points[j] = new MWPoint3D(
                            red* diagramFaces[i].Points[j].X, 
                            red* diagramFaces[i].Points[j].Y, 
                            red* diagramFaces[i].Points[j].Z
                            );
                    }
                }
            }
        }

        public List<Point> GetLShapedContour()
        {
            double angle = Theta * Math.PI / 180;
            double H1 = HX * Math.Abs(Math.Cos(angle)) + HY * Math.Abs(Math.Sin(angle));
            double h1 = hX * Math.Abs(Math.Cos(angle)) + hY * Math.Abs(Math.Sin(angle));
            double H2 = HX * Math.Abs(Math.Sin(angle)) + HY * Math.Abs(Math.Cos(angle));
            double h2 = hX * Math.Abs(Math.Sin(angle)) + hY * Math.Abs(Math.Cos(angle));

            List<Point> ContourPts = new List<Point>()
            {
                new Point(-H1 / 2, -H2 / 2),
                new Point(H1 / 2, -H2 / 2),
                new Point(H1 / 2, -H2 / 2 + h2),
                new Point(-H1 / 2 + h1, -H2 / 2 + h2),
                new Point(-H1 / 2 + h1, H2 / 2),
                new Point(-H1 / 2, H2 / 2)
            };


            return ContourPts.Select(p => new Point(Math.Cos(angle) * p.X - Math.Sin(angle) * p.Y,
                                                    Math.Sin(angle) * p.X + Math.Cos(angle) * p.Y)).ToList();
        }

        public List<Point> GetLShapedRebars()
        {
            double angle = Theta * Math.PI / 180;
            double H1 = HX * Math.Abs(Math.Cos(angle)) + HY * Math.Abs(Math.Sin(angle));
            double h1 = hX * Math.Abs(Math.Cos(angle)) + hY * Math.Abs(Math.Sin(angle));
            double H2 = HX * Math.Abs(Math.Sin(angle)) + HY * Math.Abs(Math.Cos(angle));
            double h2 = hX * Math.Abs(Math.Sin(angle)) + hY * Math.Abs(Math.Cos(angle));

            int NrotX = (Theta == 0 || Theta == 180) ? NRebarX : NRebarY;
            int NrotY = (Theta == 0 || Theta == 180) ? NRebarY : NRebarX;

            double d = CoverToLinks + LinkDiameter + BarDiameter / 2;
            List<Point> rebars = new List<Point>()
            {
                new Point(-H1/2 + d, -H2/2 + d),
                new Point(-H1/2 + h1 - d, -H2/2 + d),
                new Point(-H1/2 + d, -H2/2 + h2 - d),
                new Point(-H1/2 + h1 - d, -H2/2 + h2 - d),
            };
            if(HX - hX > 2 * BarDiameter)
            {
                rebars.Add(new Point(H1 / 2 - d, -H2 / 2 + d));
                rebars.Add(new Point(H1 / 2 - d, -H2 / 2 + h2 - d));
            }
            if(HY - hY > 2 * BarDiameter)
            {
                rebars.Add(new Point(-H1 / 2 + d, H2 / 2 - d));
                rebars.Add(new Point(-H1 / 2 + h1 - d, H2 / 2 - d));
            }

            double ly1 = H2 - h2;
            double ly2 = h2 - 2 * d;
            double lx1 = H1 - h1;
            double lx2 = h1 - 2 * d;
            int nX1 = 0;
            int nX2 = 0;
            int nY1 = 0;
            int nY2 = 0;
            List<Point> addX1 = new List<Point>();
            List<Point> addX2 = new List<Point>();
            List<Point> addY1 = new List<Point>();
            List<Point> addY2 = new List<Point>();
            for (int i = 4; i <= NrotX; i++)
            {
                if (lx1 >= lx2)
                {
                    addX1 = new List<Point>();
                    nX1++;
                    double dx = (H1 - h1) / (nX1 + 1);
                    for (int j = 1; j <= nX1; j++)
                    {
                        addX1.Add(new Point(-H1 / 2 + h1 + j * dx - d, -H2 / 2 + d));
                        addX1.Add(new Point(-H1 / 2 + h1 + j * dx - d, -H2 / 2 + h2 - d));
                    }
                    lx1 = (H1 - h1) / (nX1 + 1);
                }
                else
                {
                    addX2 = new List<Point>();
                    nX2++;
                    double dx = (h1 - 2 * d) / (nX2 + 1);
                    for (int j = 1; j <= nX2; j++)
                    {
                        addX2.Add(new Point(-H1 / 2 + d + j * dx, -H2 / 2 + d));
                        addX2.Add(new Point(-H1 / 2 + d + j * dx, H2 / 2 - d));
                    }
                    lx2 = (h1 - 2 * d) / (nX2 + 1);
                }
            }
            for (int i = 4; i <= NrotY; i++)
            {
                if (ly1 >= ly2)
                {
                    addY1 = new List<Point>();
                    nY1++;
                    double dy = (H2 - h2) / (nY1 + 1);
                    for (int j = 1; j <= nY1; j++)
                    {
                        addY1.Add(new Point(-H1 / 2 + d, -H2 / 2 + h2 + j * dy - d));
                        addY1.Add(new Point(-H1 / 2 + h1 - d, -H2 / 2 + h2 + j * dy - d));
                    }
                    ly1 = (H2 - h2) / (nY1 + 1);
                }
                else
                {
                    addY2 = new List<Point>();
                    nY2++;
                    double dy = (h2 - 2 * d) / (nY2 + 1);
                    for (int j = 1; j <= nY2; j++)
                    {
                        addY2.Add(new Point(-H1 / 2 + d, -H2 / 2 + d + j * dy));
                        addY2.Add(new Point(H1 / 2 - d, -H2 / 2 + d + j * dy));
                    }
                    ly2 = (h2 - 2 * d) / (nY2 + 1);
                }
            }
            sx1 = lx1;
            sx2 = lx2;
            sy1 = ly1;
            sy2 = ly2;

            rebars.AddRange(addX1);
            rebars.AddRange(addX2);
            rebars.AddRange(addY1);
            rebars.AddRange(addY2);

            rebars = rebars.Select(p => new Point(Math.Cos(angle) * p.X - Math.Sin(angle) * p.Y,
                                                  Math.Sin(angle) * p.X + Math.Cos(angle) * p.Y)).ToList();

            Nrebars = rebars.Count;

            return rebars;
        }

        public List<Point> GetTShapedContour()
        {
            List<Point> ContourPts = new List<Point>()
            {
                new Point(-hX/2, -HY/2),
                new Point(hX/2, -HY/2),
                new Point(hX/2, HY/2-hY),
                new Point(HX/2, HY/2-hY),
                new Point(HX/2, HY/2),
                new Point(-HX/2,HY/2),
                new Point(-HX/2,HY/2-hY),
                new Point(-hX/2,HY/2-hY)
            };

            return ContourPts;
        }

        public List<Point> GetTShapedRebars()
        {
            double d = CoverToLinks + LinkDiameter + BarDiameter / 2;
            List<Point> rebars = new List<Point>()
            {
                
                new Point(hX/2 - d, HY/2 - hY + d),
                new Point(hX/2 - d, HY/2 - d),
                new Point(-hX/2 + d, HY/2 -d),
                new Point(-hX/2 + d, HY/2 - hY + d)
            };
            if (HX - hX > 4 * BarDiameter)
            {
                rebars.Add(new Point(HX / 2 - d, HY / 2 - hY + d));
                rebars.Add(new Point(HX / 2 - d, HY / 2 - d));
                rebars.Add(new Point(-HX / 2 + d, HY / 2 - d));
                rebars.Add(new Point(-HX / 2 + d, HY / 2 - hY + d));
            }
            if (HY - hY > 2 * BarDiameter)
            {
                rebars.Add(new Point(-hX / 2 + d, -HY / 2 + d));
                rebars.Add(new Point(hX / 2 - d, -HY / 2 + d));
            }

            double ly1 = HY - hY;
            double ly2 = hY - 2 * d;
            double lx1 = (HX - hX)/2;
            double lx2 = hX - 2 * d;
            int nX1 = 0;
            int nX2 = 0;
            int nY1 = 0;
            int nY2 = 0;
            List<Point> addX1 = new List<Point>();
            List<Point> addX2 = new List<Point>();
            List<Point> addY1 = new List<Point>();
            List<Point> addY2 = new List<Point>();
            for (int i = 3; i <= NRebarX; i++)
            {
                if (lx1 >= lx2)
                {
                    addX1 = new List<Point>();
                    nX1++;
                    double dx = (HX - hX) / 2 / (nX1 + 1);
                    for (int j = 1; j <= nX1; j++)
                    {
                        addX1.Add(new Point(-HX / 2 + d + j * dx, HY / 2 - d));
                        addX1.Add(new Point(-HX / 2 + d + j * dx, HY / 2 - hY + d));
                        addX1.Add(new Point(HX / 2 - d - j * dx, HY / 2 - d));
                        addX1.Add(new Point(HX / 2 - d - j * dx, HY / 2 - hY + d));
                    }
                    lx1 = (HX - hX) / 2 / (nX1 + 1);
                }
                else
                {
                    addX2 = new List<Point>();
                    nX2++;
                    double dx = (hX - 2 * d) / (nX2 + 1);
                    for (int j = 1; j <= nX2; j++)
                    {
                        addX2.Add(new Point(-hX / 2 + d + j * dx, HY / 2 - d));
                        addX2.Add(new Point(-hX / 2 + d + j * dx, -HY / 2 + d));
                    }
                    lx2 = (hX - 2 * d) / (nX2 + 1);
                }
            }
            for (int i = 4; i <= NRebarY; i++)
            {
                if (ly1 >= ly2)
                {
                    addY1 = new List<Point>();
                    nY1++;
                    double dy = (HY - hY) / (nY1 + 1);
                    for (int j = 1; j <= nY1; j++)
                    {
                        addY1.Add(new Point(-hX / 2 + d, -HY / 2 + d + j * dy));
                        addY1.Add(new Point(hX / 2 - d, -HY / 2 + d + j * dy));
                    }
                    ly1 = (HY - hY) / (nY1 + 1);
                }
                else
                {
                    addY2 = new List<Point>();
                    nY2++;
                    double dy = (hY - 2 * d) / (nY2 + 1);
                    for (int j = 1; j <= nY2; j++)
                    {
                        addY2.Add(new Point(-HX / 2 + d, HY / 2 - hY + d + j * dy));
                        addY2.Add(new Point(HX / 2 - d, HY / 2 - hY + d + j * dy));
                    }
                    ly2 = (hY - 2 * d) / (nY2 + 1);
                }
            }
            sx1 = lx1;
            sx2 = lx2;
            sy1 = ly1;
            sy2 = ly2;

            rebars.AddRange(addX1);
            rebars.AddRange(addX2);
            rebars.AddRange(addY1);
            rebars.AddRange(addY2);

            Nrebars = rebars.Count;

            return rebars;
        }

        public bool isInsideCapacity(bool allLoads = false)
        {
            return isInsideInteractionDiagram(this.diagramFaces, this.diagramVertices, allLoads);
        }

        public bool isInsideInteractionDiagram(List<Tri3D> faces, List<MWPoint3D> vertices, bool allLoads = false, bool firecheck = false)
        {
            bool all = (SelectedLoad.Name == "ALL LOADS" || allLoads) ? true : false;
            GetDesignMoments();
            MWPoint3D p0 = MWPoint3D.point3DByCoordinates
            (
                2 * vertices.Min(x => x.X),
                0,
                0
            );

            List<MWPoint3D> points = new List<MWPoint3D>();

            if (firecheck)
                points.Add(new MWPoint3D(FireLoad.Mxd, FireLoad.Myd, -FireLoad.P));
            else if(all)
            {
                for(int i = 1; i < Loads.Count; i++)
                    points.Add(new MWPoint3D(Loads[i].Mxd, Loads[i].Myd, -Loads[i].P));
            }
            else
                points.Add(new MWPoint3D(SelectedLoad.Mxd, SelectedLoad.Myd, -SelectedLoad.P));
            
            for(int k = 0; k < points.Count; k++)
            {
                int compt = 0;
                MWPoint3D p = points[k];
                for (int i = 0; i < faces.Count; i++)
                {
                    MWPoint3D pInter0 = Polygon3D.PlaneLineIntersection(new MWPoint3D[] { p0, p }, faces[i].Points);
                    if (pInter0.X != double.NaN)
                    {
                        MWPoint3D pInter = pInter0;
                        MWVector3D v = Vectors3D.VectorialProduct(MWVector3D.vector3DByCoordinates(p0.X - pInter.X, p0.Y - pInter.Y, p0.Z - pInter.Z),
                                                                    MWVector3D.vector3DByCoordinates(p.X - pInter.X, p.Y - pInter.Y, p.Z - pInter.Z));

                        List<MWPoint3D> pts = faces[i].Points;
                        double a1 = Vectors3D.TriangleArea(MWVector3D.vector3DByCoordinates(pts[0].X - pInter.X, pts[0].Y - pInter.Y, pts[0].Z - pInter.Z),
                                                                MWVector3D.vector3DByCoordinates(pts[1].X - pInter.X, pts[1].Y - pInter.Y, pts[1].Z - pInter.Z));
                        double a2 = Vectors3D.TriangleArea(MWVector3D.vector3DByCoordinates(pts[1].X - pInter.X, pts[1].Y - pInter.Y, pts[1].Z - pInter.Z),
                                                                MWVector3D.vector3DByCoordinates(pts[2].X - pInter.X, pts[2].Y - pInter.Y, pts[2].Z - pInter.Z));
                        double a3 = Vectors3D.TriangleArea(MWVector3D.vector3DByCoordinates(pts[2].X - pInter.X, pts[2].Y - pInter.Y, pts[2].Z - pInter.Z),
                                                                MWVector3D.vector3DByCoordinates(pts[0].X - pInter.X, pts[0].Y - pInter.Y, pts[0].Z - pInter.Z));
                        double a0 = Vectors3D.TriangleArea(MWVector3D.vector3DByCoordinates(pts[1].X - pts[0].X, pts[1].Y - pts[0].Y, pts[1].Z - pts[0].Z),
                                                                MWVector3D.vector3DByCoordinates(pts[2].X - pts[0].X, pts[2].Y - pts[0].Y, pts[2].Z - pts[0].Z));
                        if (Math.Abs(a1 + a2 + a3 - a0) < 1E-3)
                            compt++;
                        if (compt == 2) break;
                    }
                }

                if (compt % 2 == 0)
                    return false;
            }

            return true;
            
        }

        public bool CheckSpacing()
        {
            List<double> sizes = new List<double>() { BarDiameter, MaxAggSize + 5, 20 };
            double smin = sizes.Max();
            if (Shape == GeoShape.Rectangular)
            {
                double sx = (LX - 2 * (CoverToLinks + LinkDiameter) - BarDiameter) / (NRebarX - 1);
                double sy = (LY - 2 * (CoverToLinks + LinkDiameter) - BarDiameter) / (NRebarY - 1);
                if (sx >= smin && sy >= smin)
                    return true;
                else
                    return false;
            }
            else if (Shape == GeoShape.Circular)
            {
                double x0 = Diameter / 2.0 - CoverToLinks - LinkDiameter - BarDiameter / 2.0;
                double x1 = x0 * Math.Cos(2 * Math.PI / NRebarCirc);
                double y1 = x0 * Math.Sin(2 * Math.PI / NRebarCirc);
                double s = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1, 2));
                if (s >= smin)
                    return true;
                else
                    return false;
            }
            else if (Shape == GeoShape.Polygonal)
            {
                double x0 = Diameter / 2 - CoverToLinks - LinkDiameter - BarDiameter / 2.0;
                double x1 = x0 * Math.Cos(2 * Math.PI / Edges);
                double y1 = x0 * Math.Sin(2 * Math.PI / Edges);
                double s = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1, 2));
                if (s >= smin)
                    return true;
                else
                    return false;
            }
            else if (Shape == GeoShape.LShaped)
            {
                if (sx1 < smin || sx2 < smin || sy1 < smin || sy2 < smin)
                    return false;
                else
                    return true;
            }
            return false;
        }

        public bool CheckFire()
        {
            //double Nrd = 0;
            double Nrd = (GetSteelArea() * SteelGrade.Fy / gs + GetConcreteArea() * acc * ConcreteGrade.Fc / gc) / 1E3;
            //double mufi = 0.7 * SelectedLoad.P / Nrd;
            double mufi = FireLoad.P / Nrd;
            
            // Eurocode Table 5.2.1a
            double afi = 0;
            mufi = (mufi <= 0.35) ? 0.2 : ((mufi <= 0.6) ? 0.5 : (mufi <= 0.7 ? 0.7 : 2));
            if (mufi == 2)
                return false;
            if (fireTable.Count == 0) SetFireData();
            List<FireData> fdata = fireTable.Where(x => x.mu == mufi && x.R == R && x.sidesExposed == SidesExposed).ToList();
            fdata = fdata.OrderByDescending(x => x.minDimension).ToList();
            switch (Shape)
            {
                case (GeoShape.Rectangular):
                    for (int i = 0; i < fdata.Count; i++)
                    {
                        if (LX >= fdata[i].minDimension && LY >= fdata[i].minDimension)
                        {
                            afi = fdata[i].axisDistance;
                            break;
                        }
                    }
                    break;
                case (GeoShape.Circular):
                    for (int i = 0; i < fdata.Count; i++)
                    {
                        if (Diameter >= fdata[i].minDimension)
                        {
                            afi = fdata[i].axisDistance;
                            break;
                        }
                    }
                    break;
                case (GeoShape.Polygonal):
                    for (int i = 0; i < fdata.Count; i++)
                    {
                        if (2 * Radius >= fdata[i].minDimension)
                        {
                            afi = fdata[i].axisDistance;
                            break;
                        }
                    }
                    break;
                case (GeoShape.LShaped):
                    for (int i = 0; i < fdata.Count; i++)
                    {
                        if (HX - hX >= fdata[i].minDimension && HY - hY >= fdata[i].minDimension)
                        {
                            afi = fdata[i].axisDistance;
                            break;
                        }
                    }
                    break;
                case (GeoShape.TShaped):
                    for (int i = 0; i < fdata.Count; i++)
                    {
                        if (HX - hX >= fdata[i].minDimension && HY - hY >= fdata[i].minDimension)
                        {
                            afi = fdata[i].axisDistance;
                            break;
                        }
                    }
                    break;
            }
            double cminb = Math.Max(this.LinkDiameter, this.BarDiameter - this.LinkDiameter);
            double cnommin = Math.Max(afi - this.BarDiameter / 2.0 - this.LinkDiameter, cminb + 10);

            if (afi == 0)
            {
                return false;
            }
            else
            {
                if (this.CoverToLinks >= cnommin)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public (bool, FormulaeVM) CheckFireZoneMethod(bool newdesign = false)
        {
            if (newdesign)
            {
                if (Shape == GeoShape.Rectangular)
                    TP = new TemperatureProfile(LX / 1e3, LY / 1e3, R * 60, FireCurve);
                else if (Shape == GeoShape.LShaped)
                    TP = new TemperatureProfile(HX / 1e3, HY / 1e3, hX / 1e3, hY / 1e3, Theta, R * 60, FireCurve);
                else if (Shape == GeoShape.TShaped)
                    TP = new TemperatureProfile(HX / 1e3, HY / 1e3, hX / 1e3, hY / 1e3, R * 60, FireCurve);
            }
            else if (!TP?.TempMap.Keys.Contains(R) ?? true)
            {
                if (Shape == GeoShape.Rectangular)
                    TP = new TemperatureProfile(LX / 1e3, LY / 1e3, R * 60, FireCurve);
                else if (Shape == GeoShape.LShaped)
                    TP = new TemperatureProfile(HX / 1e3, HY / 1e3, hX / 1e3, hY / 1e3, Theta, R * 60, FireCurve);
                else if (Shape == GeoShape.TShaped)
                    TP = new TemperatureProfile(HX / 1e3, HY / 1e3, hX / 1e3, hY / 1e3, R * 60, FireCurve);
            }

            TP.GetContours(R, Shape.ToString());

            if (steelData.Count == 0) SetSteelData();
            if (concreteData.Count == 0) SetConcreteData();

            int nDiv = 5;
            double w = (LX <= LY) ? LX / 2 : LY / 2 ;
            double e = w / nDiv;
            
            double sumK = 0;

            for(int i = 0; i < nDiv; i++)
            {
                double x = (LX <= LY)? -LX / 2 + (i + 0.5) * e : 0;
                double y = (LY < LX)? -LY / 2 + (i + 0.5) * e : 0;

                double temp = getTemp(new MWPoint2D(x, y));
                sumK += concreteData.First(c => c.Temp == temp).k;
            }
            double kcm = (1 - 0.2 / nDiv) / nDiv * sumK;
            double kctm = concreteData.First(c => c.Temp == getTemp(new MWPoint2D(0, 0))).k;

            double az = w * (1 - Math.Pow((kcm / kctm), 1.3));

            double NRfi = (LX - 2 * az) * (LY - 2 * az) * 0.85 * ConcreteGrade.Fc / 1.5 / 1e3;

            double xspace = (LX - 2 * (CoverToLinks + LinkDiameter + BarDiameter / 2.0)) / (NRebarX - 1);
            double yspace = (LY - 2 * (CoverToLinks + LinkDiameter + BarDiameter / 2.0)) / (NRebarY - 1);
            double area = Math.PI * Math.Pow(BarDiameter / 2, 2) / 1e6;
            for (int i = 0; i < NRebarX; i++)
            {
                var x = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + i * xspace - LX / 2;
                for (int j = 0; j < NRebarY; j++)
                {
                    var y = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + j * yspace - LY / 2;
                    if (i == 0 || i == NRebarX - 1 || j == 0 || j == NRebarY - 1)
                    {

                        double temp = getTemp(new MWPoint2D(x, y));
                        SteelData sd = steelData.First(s => s.Temp == temp);
                        NRfi += area * SteelGrade.Fy / 1.15 * 1e3 * sd.kf;
                    }
                }
            }

            // check moments
            double Mx = 0;
            double My = 0;

            double As = Math.Pow(BarDiameter / 2, 2) * Math.PI / 1e6;

            double As1Fsd = 0;
            var yy = CoverToLinks + LinkDiameter + BarDiameter / 2.0 - LY / 2;
            for (int i = 0; i < NRebarX; i++)
            {
                var x = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + i * xspace - LX / 2;
                double temp = getTemp(new MWPoint2D(x, yy));
                As1Fsd += As * SteelGrade.Fy / gs * steelData.First(s => s.Temp == temp).kf;
            }
            double X = As1Fsd / (LX - 2 * az) / (acc * ConcreteGrade.Fc / gc );

            Mx = As1Fsd * Math.Abs(yy - (LY / 2 - X / 2)) + As1Fsd * (LY - 2 * yy);

            As1Fsd = 0;
            var xx = CoverToLinks + LinkDiameter + BarDiameter / 2.0  - LX / 2;
            for (int i = 0; i < NRebarY; i++)
            {
                var y = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + i * yspace - LY / 2;
                double temp = getTemp(new MWPoint2D(xx, y));
                As1Fsd += As * SteelGrade.Fy / gs * steelData.First(s => s.Temp == temp).kf;
            }
            X = As1Fsd / (LY - 2 * az) / (acc * ConcreteGrade.Fc / gc );

            My = As1Fsd * Math.Abs(xx - (LX / 2 - X / 2)) + As1Fsd * (LX - 2 * xx);

            // formulae
            FormulaeVM f = new FormulaeVM();
            f.Narrative = "Nominal cover for fire and bond requirements";
            f.Expression = new List<string>();
            f.Ref = "EN1992-1-2 ANNEX B.1";
            f.Expression.Add(@"ZoneMethod");
            f.Expression.Add(@"a_z = " + Math.Round(az) + " mm");
            f.Expression.Add(@"N_{R,fi} = (L_x - 2 a_z)(L_y - 2 a_z)f_{cd} + A_s\times f_{yd,fi}= " + Math.Round(NRfi) + " kN");
            f.Expression.Add(@"N_{Ed,fi} = " + Math.Round(FireLoad.P) + " kN");
            f.Expression.Add(@"M_{xR,fi} = " + Math.Round(Mx) + " kN.m");
            f.Expression.Add(@"M_{xd,fi} = " + Math.Round(FireLoad.Mxd) + "kN.m");
            f.Expression.Add(@"M_{yR,fi} = " + Math.Round(My) + " kN.m");
            f.Expression.Add(@"M_{yd,fi} = " + Math.Round(FireLoad.Myd) + "kN.m");

            bool res = (NRfi > FireLoad.P && Mx > FireLoad.Mxd && My > FireLoad.Myd);

            f.Status = res ? CalcStatus.PASS : CalcStatus.FAIL;
            f.Conclusion = res ? "PASS" : "FAIL";

            return (res, f);
        }

        public (bool, FormulaeVM) CheckFireIsotherm500(bool newdesign = false)
        {
            if (Shape == GeoShape.Rectangular)
                return CheckFireIsotherm500_Rectangular();
            //else if (Shape == GeoShape.LShaped)
            //    return CheckFireIsotherm500_LShaped();
            return (false, null);
        }

        // Isotherm 500 method for rectangular columns
        public (bool, FormulaeVM) CheckFireIsotherm500_Rectangular(bool newdesign = false)
        {
            if (newdesign)
                TP = new TemperatureProfile(LX / 1e3, LY / 1e3, R * 60, FireCurve);
            else if (!TP?.TempMap.Keys.Contains(R) ?? true)
                TP = new TemperatureProfile(LX / 1e3, LY / 1e3, R * 60, FireCurve);

            TP.GetContours(R, GeoShape.Rectangular.ToString());

            if (steelData.Count == 0) SetSteelData();

            FireDesign.Contour iso500 = TP.ContourPts.First(c => c.Level == 500);
            double maxX = iso500.Points.Max(x => x.X);
            double minX = iso500.Points.Min(x => x.X);
            double maxY = iso500.Points.Max(x => x.Y);
            double minY = iso500.Points.Min(x => x.Y);

            double dX = Math.Min(Math.Abs(minX - LX / 2), Math.Abs(maxX - LX / 2));
            double dY = Math.Min(Math.Abs(minY - LY / 2), Math.Abs(maxY - LY / 2));

            double NRfi = (LX - 2 * dX) * (LY - 2 * dY) * 0.85 * ConcreteGrade.Fc / 1.5 / 1e3;

            double xspace = (LX - 2 * (CoverToLinks + LinkDiameter + BarDiameter / 2.0)) / (NRebarX - 1);
            double yspace = (LY - 2 * (CoverToLinks + LinkDiameter + BarDiameter / 2.0)) / (NRebarY - 1);
            double area = Math.PI * Math.Pow(BarDiameter / 2, 2) / 1e6;
            for (int i = 0; i < NRebarX; i++)
            {
                var x = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + i * xspace - LX / 2;
                for (int j = 0; j < NRebarY; j++)
                {
                    var y = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + j * yspace - LY / 2;
                    if (i == 0 || i == NRebarX - 1 || j == 0 || j == NRebarY - 1)
                    {

                        double temp = getTemp(new MWPoint2D(x, y));
                        SteelData sd = steelData.First(s => s.Temp == temp);
                        NRfi += area * SteelGrade.Fy / 1.15 * 1e3 * sd.kf;
                    }
                }
            }

            // check moments
            double Mx = 0;
            double My = 0;

            double As = Math.Pow(BarDiameter / 2, 2) * Math.PI / 1e6;

            double As1Fsd = 0;
            var yy = CoverToLinks + LinkDiameter + BarDiameter / 2.0 - LY / 2;
            for (int i = 0; i < NRebarX; i++)
            {
                var x = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + i * xspace - LX / 2;
                double temp = getTemp(new MWPoint2D(x, yy));
                As1Fsd += As * SteelGrade.Fy * steelData.First(s => s.Temp == temp).kf;
            }
            double X = As1Fsd / (LX - 2 * dX) / ConcreteGrade.Fc;

            Mx = As1Fsd * Math.Abs(yy - (LY / 2 - X / 2)) + As1Fsd * (LY - 2 * yy);

            As1Fsd = 0;
            var xx = CoverToLinks + LinkDiameter + BarDiameter / 2.0 - LX / 2;
            for (int i = 0; i < NRebarY; i++)
            {
                var y = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + i * yspace - LY / 2;
                double temp = getTemp(new MWPoint2D(xx, y));
                As1Fsd += As * SteelGrade.Fy * steelData.First(s => s.Temp == temp).kf;
            }
            X = As1Fsd / (LY - 2 * dY) / ConcreteGrade.Fc;

            My = As1Fsd * Math.Abs(xx - (LX / 2 - X / 2)) + As1Fsd * (LX - 2 * xx);

            // formulae
            FormulaeVM f = new FormulaeVM();
            f.Narrative = "Nominal cover for fire and bond requirements";
            f.Expression = new List<string>();
            f.Ref = "EN1992-1-2 ANNEX B.2";
            f.Expression.Add(@"Isotherm 500");
            f.Expression.Add(@"a_x = " + Math.Round(dX) + " mm");
            f.Expression.Add(@"a_y = " + Math.Round(dY) + " mm");
            f.Expression.Add(@"N_{R,fi} = (L_x - 2 a_x)(L_y - 2 a_y)f_{cd} + A_s\times f_{yd,fi}= " + Math.Round(NRfi) + " kN");
            f.Expression.Add(@"N_{Ed,fi} = " + Math.Round(FireLoad.P) + " kN");
            f.Expression.Add(@"M_{xR,fi} = " + Math.Round(Mx) + " kN.m");
            f.Expression.Add(@"M_{xd,fi} = " + Math.Round(FireLoad.Mxd) + "kN.m");
            f.Expression.Add(@"M_{yR,fi} = " + Math.Round(My) + " kN.m");
            f.Expression.Add(@"M_{yd,fi} = " + Math.Round(FireLoad.Myd) + "kN.m");

            bool res = (NRfi > FireLoad.P && Mx > FireLoad.Mxd && My > FireLoad.Myd);

            f.Status = res ? CalcStatus.PASS : CalcStatus.FAIL;
            f.Conclusion = res ? "PASS" : "FAIL";

            return (res, f);
        }

        // Isotherm 500 method for L Shaped columns
        //public (bool, FormulaeVM) CheckFireIsotherm500_LShaped(bool newdesign = false)
        //{
        //    if (newdesign)
        //            TP = new TemperatureProfile(HX / 1e3, HY / 1e3, hX / 1e3, hY / 1e3, R * 60, FireCurve);
        //    else if (!TP?.TempMap.Keys.Contains(R) ?? true)
        //            TP = new TemperatureProfile(HX / 1e3, HY / 1e3, hX / 1e3, hY / 1e3, R * 60, FireCurve);

        //    TP.GetContours(R, false);

        //    if (steelData.Count == 0) SetSteelData();

        //    FireDesign.Contour iso500 = TP.ContourPts.First(c => c.Level == 500);
        //    double maxX = iso500.Points.Max(x => x.X);
        //    double minX = iso500.Points.Min(x => x.X);
        //    double maxY = iso500.Points.Max(x => x.Y);
        //    double minY = iso500.Points.Min(x => x.Y);

        //    double dX = Math.Min(Math.Abs(minX - HX / 2), Math.Abs(maxX - HX / 2));
        //    double dY = Math.Min(Math.Abs(minY - HY / 2), Math.Abs(maxY - HY / 2));

        //    double NRfi = (HX - 2 * dX) * (HY - 2 * dY) * 0.85 * ConcreteGrade.Fc / 1.5 / 1e3;

        //    List<Point> rebars = GetLShapedRebars();

        //    double area = Math.PI * Math.Pow(BarDiameter / 2, 2) / 1e6;
        //    for (int i = 0; i < rebars.Count; i++)
        //    {
        //        double temp = getTemp(new MWPoint2D(rebars[i].X, rebars[i].Y));
        //        SteelData sd = steelData.First(s => s.Temp == temp);
        //        NRfi += area * SteelGrade.Fy / 1.15 * 1e3 * sd.kf;
        //    }

        //    // check moments
        //    double Mx = 0;
        //    double My = 0;

        //    Point COG = GetLShapeCOG();
            
        //    double As1Fsd = 0;
        //    for(int i = 0; i < rebars.Count; i++)
        //    {
        //        double temp = getTemp(new MWPoint2D(rebars[i].X, rebars[i].Y));
        //        As1Fsd += area * SteelGrade.Fy * steelData.First(s => s.Temp == temp).kf;
        //    }
        //    double X = As1Fsd / (HX - 2 * dX) / ConcreteGrade.Fc;


        //    var yy = CoverToLinks + LinkDiameter + BarDiameter / 2.0 - HY / 2;
            
        //    Mx = As1Fsd * Math.Abs(yy - (HY / 2 - X / 2)) + As1Fsd * (HY - 2 * yy);

        //    As1Fsd = 0;
        //    var xx = CoverToLinks + LinkDiameter + BarDiameter / 2.0 - LX / 2;
        //    for (int i = 0; i < NRebarY; i++)
        //    {
        //        var y = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + i * yspace - LY / 2;
        //        double temp = getTemp(new MWPoint2D(xx, y));
        //        As1Fsd += area * SteelGrade.Fy * steelData.First(s => s.Temp == temp).kf;
        //    }
        //    X = As1Fsd / (LY - 2 * dY) / ConcreteGrade.Fc;

        //    My = As1Fsd * Math.Abs(xx - (LX / 2 - X / 2)) + As1Fsd * (LX - 2 * xx);

        //    // formulae
        //    FormulaeVM f = new FormulaeVM();
        //    f.Narrative = "Nominal cover for fire and bond requirements";
        //    f.Expression = new List<string>();
        //    f.Ref = "EN1992-1-2 ANNEX B.2";
        //    f.Expression.Add(@"Isotherm 500");
        //    f.Expression.Add(@"a_x = " + Math.Round(dX) + " mm");
        //    f.Expression.Add(@"a_y = " + Math.Round(dY) + " mm");
        //    f.Expression.Add(@"N_{R,fi} = (L_x - 2 a_x)(L_y - 2 a_y)f_{cd} + A_s\times f_{yd,fi}= " + Math.Round(NRfi) + " kN");
        //    f.Expression.Add(@"N_{Ed,fi} = " + Math.Round(FireLoad.P) + " kN");
        //    f.Expression.Add(@"M_{xR,fi} = " + Math.Round(Mx) + " kN.m");
        //    f.Expression.Add(@"M_{xd,fi} = " + Math.Round(FireLoad.Mxd) + "kN.m");
        //    f.Expression.Add(@"M_{yR,fi} = " + Math.Round(My) + " kN.m");
        //    f.Expression.Add(@"M_{yd,fi} = " + Math.Round(FireLoad.Myd) + "kN.m");

        //    bool res = (NRfi > FireLoad.P && Mx > FireLoad.Mxd && My > FireLoad.Myd);

        //    f.Status = res ? CalcStatus.PASS : CalcStatus.FAIL;
        //    f.Conclusion = res ? "PASS" : "FAIL";

        //    return (res, f);
        //}

        public Point GetLShapeCOG()
        {
            double x = -(HX - hX) * (HY - hY) * hX / 2;
            x /= (HX * HY - (HX - hX) * (HY - hY));
            double y = -(HX - hX) * (HY - hY) * hY / 2;
            y /= (HX * HY - (HX - hX) * (HY - hY));
            return new Point(x, y);
        }

        public Point GetTShapeCOG()
        {
            double y = 0.5 * hY * (HX - hX) * (HY - hY) / (HX * hY + hX * (HY - hY));
            return new Point(0, y);
        }

        public void UpdateTP(bool newdesign = true)
        {
            if (newdesign)
            {
                if (Shape == GeoShape.Rectangular)
                    TP = new TemperatureProfile(LX / 1e3, LY / 1e3, R * 60, FireCurve);
                else if (Shape == GeoShape.LShaped)
                    TP = new TemperatureProfile(HX / 1e3, HY / 1e3, hX / 1e3, hY / 1e3, Theta, R * 60, FireCurve);
                else if (Shape == GeoShape.TShaped)
                    TP = new TemperatureProfile(HX / 1e3, HY / 1e3, hX / 1e3, hY / 1e3, R * 60, FireCurve);
            }
            else if (!TP?.TempMap.Keys.Contains(R) ?? true)
            {
                if (Shape == GeoShape.Rectangular)
                    TP = new TemperatureProfile(LX / 1e3, LY / 1e3, R * 60, FireCurve);
                else if (Shape == GeoShape.LShaped)
                    TP = new TemperatureProfile(HX / 1e3, HY / 1e3, hX / 1e3, hY / 1e3, Theta, R * 60, FireCurve);
                else if (Shape == GeoShape.TShaped)
                    TP = new TemperatureProfile(HX / 1e3, HY / 1e3, hX / 1e3, hY / 1e3, R * 60, FireCurve);
            }

            TP.GetContours(R, Shape.ToString());
        }

        public void UpdateFireID(bool newdesign = false)
        {
            if (fireTable.Count == 0) SetFireData();
            if (concreteData.Count == 0) SetConcreteData();
            if (steelData.Count == 0) SetSteelData();

            UpdateTP(newdesign);

            List<Composite> composites = new List<Composite>();

            // Materials
            //Material concrete = new Material(ConcreteGrade.Name, MatYpe.Concrete, 0.85 * ConcreteGrade.Fc / 1.5, 0, ConcreteGrade.E);

            if (Shape == GeoShape.Rectangular)
            {
                // Creation of the concrete sections
                for(int i = 1; i < TP.ContourPts.Count-1; i++)
                {
                    ConcreteData cd = concreteData.First(x => x.Temp == TP.ContourPts[i].Level);
                    Material concrete = new Material(ConcreteGrade.Name+"_"+ TP.ContourPts[i].Level, MatYpe.Concrete, cd.k * 0.85 * ConcreteGrade.Fc / 1.5, 0, ConcreteGrade.E, cd.Ec1, cd.density);
                    //composites.Add(new ConcreteSection(TP.ContourPts[i].Points.Concat(TP.ContourPts[i-1].Points.Reverse<MWPoint2D>()).ToList(),concrete));
                    composites.Add(new ConcreteSection(TP.ContourPts[i].Points.Where((p, index) => index % 5 == 0).ToList(),concrete));
                }
                int n_last = TP.ContourPts.Count - 1;
                ConcreteData cd_last = concreteData.First(x => x.Temp == TP.ContourPts[n_last].Level);
                Material concrete_last = new Material(ConcreteGrade.Name + "_" + TP.ContourPts[n_last].Level, MatYpe.Concrete, cd_last.k * 0.85 * ConcreteGrade.Fc / 1.5, 0, ConcreteGrade.E, cd_last.Ec1, cd_last.density);
                composites.Add(new ConcreteSection(TP.ContourPts[n_last].Points.Distinct().ToList(), concrete_last));


                // Creation of the rebars
                double xspace = (LX - 2 * (CoverToLinks + LinkDiameter + BarDiameter / 2.0)) / (NRebarX - 1);
                double yspace = (LY - 2 * (CoverToLinks + LinkDiameter + BarDiameter / 2.0)) / (NRebarY - 1);
                for (int i = 0; i < NRebarX; i++)
                {
                    var x = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + i * xspace - LX / 2;
                    for (int j = 0; j < NRebarY; j++)
                    {
                        if (i == 0 || i == NRebarX - 1 || j == 0 || j == NRebarY - 1)
                        {
                            var y = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + j * yspace - LY / 2;
                            double temp = GetRebarTemp(new MWPoint2D(x, y));
                            SteelData sd = steelData.First(s => s.Temp == temp);
                            Material steel = new Material(SteelGrade.Name, MatYpe.Steel, sd.kf * SteelGrade.Fy / 1.15, sd.kf * SteelGrade.Fy / 1.15, sd.kE * SteelGrade.E);
                            Rebar r = new Rebar(MWPoint2D.Point2DByCoordinates(x, y), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                            composites.Add(r);
                        }
                    }
                }
            }
            else if (Shape == GeoShape.LShaped)
            {
                // Creation of the concrete sections
                for (int i = 1; i < TP.ContourPts.Count - 1; i++)
                {
                    ConcreteData cd = concreteData.First(x => x.Temp == TP.ContourPts[i].Level);
                    Material concrete = new Material(ConcreteGrade.Name + "_" + TP.ContourPts[i].Level, MatYpe.Concrete, cd.k * 0.85 * ConcreteGrade.Fc / 1.5, 0, ConcreteGrade.E, cd.Ec1, cd.density);
                    composites.Add(new ConcreteSection(TP.ContourPts[i].Points.Where((p, index) => index % 5 == 0).ToList(), concrete)); // reduces the number of points by 5
                }
                int n_last = TP.ContourPts.Count - 1;
                ConcreteData cd_last = concreteData.First(x => x.Temp == TP.ContourPts[n_last].Level);
                Material concrete_last = new Material(ConcreteGrade.Name + "_" + TP.ContourPts[n_last].Level, MatYpe.Concrete, cd_last.k * 0.85 * ConcreteGrade.Fc / 1.5, 0, ConcreteGrade.E, cd_last.Ec1, cd_last.density);
                composites.Add(new ConcreteSection(TP.ContourPts[n_last].Points.Distinct().ToList(), concrete_last));

                // Creation of the rebars
                List<Point> rebars = GetLShapedRebars();
                for (int i = 0; i < rebars.Count; i++)
                {
                    double temp = GetRebarTemp(new MWPoint2D(rebars[i].X, rebars[i].Y));
                    SteelData sd = steelData.First(s => s.Temp == temp);
                    Material steel = new Material(SteelGrade.Name, MatYpe.Steel, sd.kf * SteelGrade.Fy / 1.15, sd.kf * SteelGrade.Fy / 1.15, sd.kE * SteelGrade.E);
                    Rebar r = new Rebar(MWPoint2D.Point2DByCoordinates(rebars[i].X, rebars[i].Y), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                    composites.Add(r);
                }

            }
            else if (Shape == GeoShape.TShaped)
            {
                // Creation of the concrete sections
                for (int i = 1; i < TP.ContourPts.Count - 1; i++)
                {
                    ConcreteData cd = concreteData.First(x => x.Temp == TP.ContourPts[i].Level);
                    Material concrete = new Material(ConcreteGrade.Name + "_" + TP.ContourPts[i].Level, MatYpe.Concrete, cd.k * 0.85 * ConcreteGrade.Fc / 1.5, 0, ConcreteGrade.E, cd.Ec1, cd.density);
                    composites.Add(new ConcreteSection(TP.ContourPts[i].Points.Where((p, index) => index % 5 == 0).ToList(), concrete)); // reduces the number of points by 5
                }
                int n_last = TP.ContourPts.Count - 1;
                ConcreteData cd_last = concreteData.First(x => x.Temp == TP.ContourPts[n_last].Level);
                Material concrete_last = new Material(ConcreteGrade.Name + "_" + TP.ContourPts[n_last].Level, MatYpe.Concrete, cd_last.k * 0.85 * ConcreteGrade.Fc / 1.5, 0, ConcreteGrade.E, cd_last.Ec1, cd_last.density);
                composites.Add(new ConcreteSection(TP.ContourPts[n_last].Points.Distinct().ToList(), concrete_last));

                // Creation of the rebars
                List<Point> rebars = GetTShapedRebars();
                for (int i = 0; i < rebars.Count; i++)
                {
                    double temp = GetRebarTemp(new MWPoint2D(rebars[i].X, rebars[i].Y));
                    SteelData sd = steelData.First(s => s.Temp == temp);
                    Material steel = new Material(SteelGrade.Name, MatYpe.Steel, sd.kf * SteelGrade.Fy / 1.15, sd.kf * SteelGrade.Fy / 1.15, sd.kE * SteelGrade.E);
                    Rebar r = new Rebar(MWPoint2D.Point2DByCoordinates(rebars[i].X, rebars[i].Y), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                    composites.Add(r);
                }

            }

            Diagram d = new Diagram(composites, DiagramDisc);
            fireDiagramFaces = d.faces;
            fireDiagramVertices = d.vertices;
            
        }

        public double getTemp(MWPoint2D pt)
        {
            for (int j = TP.ContourPts.Count - 1; j > 0; j--)
            {
                bool inner = Polygons.isInside(TP.ContourPts[j-1].Points, pt);
                bool outer = Polygons.isInside(TP.ContourPts[j].Points, pt);
                if (outer && !inner)
                {
                    return TP.ContourPts[j].Level;
                }
            }
            if (Polygons.isInside(TP.ContourPts[0].Points, pt)) return TP.ContourPts[0].Level;
            Console.WriteLine("Point didn't match with any contours ...");
            return 0;
        }

        public bool CheckIsInsideFireID()
        {
            if (fireDiagramFaces.Count > 0)
                return isInsideInteractionDiagram(fireDiagramFaces, fireDiagramVertices, firecheck: true);
            else
                return false;
        }

        private double GetRebarTemp(MWPoint2D pt)
        {
            if (Polygons.isInside(TP.ContourPts[0].Points, pt)) return 100;
            double level = 200;
            for(int i = 1; i < TP.ContourPts.Count; i++)
            {
                if (Polygons.isInside(TP.ContourPts[i].Points.Concat(TP.ContourPts[i - 1].Points.Reverse<MWPoint2D>()).ToList(), pt))
                    return level;
                else
                    level += 100;
            }
            return level;
        }

        public bool CheckSteelQtty()
        {
            double Ac = LX * LY;
            double Asmin = Math.Max(0.1 * SelectedLoad.P / (SteelGrade.Fy * 1e-3 / gs), 0.002 * Ac);
            double Asmax = 0.04 * Ac;
            double As = 0;
            if (Shape == GeoShape.Rectangular)
                As = 2 * (NRebarX + NRebarY - 2) * Math.PI * Math.Pow(BarDiameter / 2.0, 2);
            else if (Shape == GeoShape.Circular)
                As = NRebarCirc * Math.PI * Math.Pow(BarDiameter / 2.0, 2);
            else if (Shape == GeoShape.Polygonal)
                As = Edges * Math.PI * Math.Pow(BarDiameter / 2.0, 2);
            else if (Shape == GeoShape.LShaped)
                As = 8 + (NRebarX - 3) * 2 + (NRebarY - 3) * 2;
            else if (Shape == GeoShape.TShaped)
                As = 10;
            if (As > Asmax)
                return false;
            else if (As < Asmin)
                return false;

            return true;
            
        }

        public void GetDesignMoments()
        {
            List<Load> loadsToCheck;
            loadsToCheck = (AllLoads) ? Loads: new List<Load>() { SelectedLoad };

            for(int i = 0; i < loadsToCheck.Count; i++)
            {
                List<double> sizes = new List<double>() { this.BarDiameter, this.MaxAggSize + 5, 20 };
                double smin = sizes.Max();
                double sx = (this.LX - 2 * (this.CoverToLinks + this.LinkDiameter) - this.BarDiameter) / (this.NRebarX - 1);
                double sy = (this.LY - 2 * (this.CoverToLinks + this.LinkDiameter) - this.BarDiameter) / (this.NRebarY - 1);

                double abar = Math.PI * Math.Pow(this.BarDiameter / 2.0, 2);
                double As = (this.NRebarX * this.NRebarY - (this.NRebarX - 2) * (this.NRebarY - 2)) * Math.PI * Math.Pow(this.BarDiameter / 2.0, 2);
                double[] dxs = new double[this.NRebarY];
                dxs[0] = this.LY - this.CoverToLinks - this.LinkDiameter - this.BarDiameter / 2.0;

                double[] effDepths = GetEffectiveDepths();
                double dx = effDepths[0];
                double dy = effDepths[1];

                double l0 = this.EffectiveLength * this.Length;

                double[] Inertias = GetSecondMomentInertia();
                double Ix = Inertias[0];
                double Iy = Inertias[1];
                double Ac = GetConcreteArea();
                double A = GetColumnArea();

                double ix = Math.Sqrt(Ix / A);
                double iy = Math.Sqrt(Iy / A);

                double lambdax = l0 / ix;

                double lambday = l0 / iy;

                double ei = l0 / 400;
                int kx = (loadsToCheck[i].MxTop * loadsToCheck[i].MxBot >= 0) ? 1 : -1;
                double M01x = kx * Math.Min(Math.Abs(loadsToCheck[i].MxTop), Math.Abs(loadsToCheck[i].MxBot)) + ei * loadsToCheck[i].P / 1E3;
                double M02x = Math.Max(Math.Abs(loadsToCheck[i].MxTop), Math.Abs(loadsToCheck[i].MxBot)) + ei * loadsToCheck[i].P / 1E3;

                double omega = As * this.SteelGrade.Fy / gs /
                    (Ac * acc * this.ConcreteGrade.Fc / gc);
                double B = Math.Sqrt(1 + 2 * omega);
                double rmx = M01x / M02x;
                double C = 1.7 - rmx;
                double n = loadsToCheck[i].P / (Ac * acc * this.ConcreteGrade.Fc / gc) * 1E3;
                double lambdaxlim = 20 * 0.7 * B * C / Math.Sqrt(n);
                bool secondorderx = false;
                if (lambdax < lambdaxlim)
                {
                }
                else
                {
                    secondorderx = true;
                }

                int ky = (loadsToCheck[i].MyTop * loadsToCheck[i].MyBot >= 0) ? 1 : -1;
                double M01y = ky * Math.Min(Math.Abs(loadsToCheck[i].MyTop), Math.Abs(loadsToCheck[i].MyBot)) + ei * loadsToCheck[i].P / 1E3;
                double M02y = Math.Max(Math.Abs(loadsToCheck[i].MyTop), Math.Abs(loadsToCheck[i].MyBot)) + ei * loadsToCheck[i].P / 1E3;

                double rmy = M01y / M02y;
                C = 1.7 - rmy;
                double lambdaylim = 20 * 0.7 * B * C / Math.Sqrt(n);
                bool secondordery = false;
                if (lambday < lambdaylim)
                {
                }
                else
                {
                    secondordery = true;
                }

                FormulaeVM f8 = new FormulaeVM();
                double[] axialDist = GetAxialLength();
                double peri = GetPerimeter();
                if (!secondorderx)
                {
                    double Medx = Math.Max(M02x, loadsToCheck[i].P * Math.Max(axialDist[1] * 1E-3 / 30, 20 * 1E-3));
                    loadsToCheck[i].Mxd = Math.Round(Medx, 1);
                }
                else
                {
                    double u = peri;
                    double nu = 1 + omega;
                    double Kr = Math.Min(1, (nu - n) / (nu - 0.4));
                    double eyd = this.SteelGrade.Fy / gs / this.SteelGrade.E / 1E3;
                    double r0 = 0.45 * dx / eyd;
                    double h0 = 2 * Ac / u;
                    double alpha1 = Math.Pow(35 / (this.ConcreteGrade.Fc + 8), 0.7);
                    double alpha2 = Math.Pow(35 / (this.ConcreteGrade.Fc + 8), 0.2);
                    double phiRH = (1 + (0.5 / (0.1 * Math.Pow(h0, 1.0 / 3))) * alpha1) * alpha2;
                    double bfcm = 16.8 / Math.Sqrt(this.ConcreteGrade.Fc + 8);
                    double bt0 = 1 / (0.1 + Math.Pow(28, 0.2));
                    double phi0 = phiRH * bfcm * bt0;
                    double phiInf = phi0;
                    double phiefy = phiInf * 0.8;
                    double betay = 0.35 + this.ConcreteGrade.Fc / 200 - lambdax / 150;
                    double kphiy = Math.Max(1, 1 + betay * phiefy);
                    double r = r0 / (Kr * kphiy);
                    double e2x = Math.Pow(l0, 2) / (r * 10);
                    double m2x = loadsToCheck[i].P * e2x * 1E-3;
                    double M0e = 0.6 * M02x + 0.4 * M01x;
                    List<double> Ms = new List<double>() { M02x, M0e + m2x, M01x + 0.5 * m2x, Math.Max(axialDist[1] * 1E-3 / 30, 20 * 1E-3) * loadsToCheck[i].P };
                    double Medx = Ms.Max();
                    loadsToCheck[i].Mxd = Math.Round(Medx, 1);

                }

                if (!secondordery)
                {
                    double Medy = Math.Max(M02y, loadsToCheck[i].P * Math.Max(axialDist[0] * 1E-3 / 30, 20 * 1E-3));
                    loadsToCheck[i].Myd = Math.Round(Medy, 1);
                }
                else
                {
                    double u = peri;
                    double nu = 1 + omega;
                    double Kr = Math.Min(1, (nu - n) / (nu - 0.4));
                    double eyd = SteelGrade.Fy / gs / this.SteelGrade.E / 1E3;
                    double r0 = 0.45 * dy / eyd;
                    double h0 = 2 * Ac / u;
                    double alpha1 = Math.Pow(35 / (this.ConcreteGrade.Fc + 8), 0.7);
                    double alpha2 = Math.Pow(35 / (this.ConcreteGrade.Fc + 8), 0.2);
                    double phiRH = (1 + (0.5 / (0.1 * Math.Pow(h0, 1.0 / 3))) * alpha1) * alpha2;
                    double bfcm = 16.8 / Math.Sqrt(this.ConcreteGrade.Fc + 8);
                    double bt0 = 1 / (0.1 + Math.Pow(28, 0.2));
                    double phi0 = phiRH * bfcm * bt0;
                    double phiInf = phi0;
                    double phiefy = phiInf * 0.8;
                    double betay = 0.35 + this.ConcreteGrade.Fc / 200 - lambday / 150;
                    double kphiy = Math.Max(1, 1 + betay * phiefy);
                    double r = r0 / (Kr * kphiy);
                    double e2y = Math.Pow(l0, 2) / (r * 10);
                    double m2y = loadsToCheck[i].P * e2y * 1E-3;
                    double M0e = 0.6 * M02y + 0.4 * M01y;
                    List<double> Ms = new List<double>() { M02y, M0e + m2y, M01y + 0.5 * m2y, Math.Max(axialDist[0] * 1E-3 / 30, 20 * 1E-3) * loadsToCheck[i].P };
                    double Medy = Ms.Max();
                    loadsToCheck[i].Myd = Math.Round(Medy, 1);

                }
            }

        }

        public double[] GetEffectiveDepths()
        {
            List<MWPoint2D> rebars = new List<MWPoint2D>();
            double minX = 0;
            double maxX = 0;
            double minY = 0;
            double maxY = 0;
            MWPoint2D bary = new MWPoint2D();
            if (Shape == GeoShape.Rectangular)
            {
                bary = new MWPoint2D(LX / 2, LY / 2);
                // Creation of the rebar positions
                double xspace = (LX - 2 * (CoverToLinks + LinkDiameter + BarDiameter / 2.0)) / (NRebarX - 1);
                double yspace = (LY - 2 * (CoverToLinks + LinkDiameter + BarDiameter / 2.0)) / (NRebarY - 1);
                for (int i = 0; i < NRebarX; i++)
                {
                    var x = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + i * xspace;
                    for (int j = 0; j < NRebarY; j++)
                    {
                        if(i == 0 || i == NRebarX-1 || j == 0 || j == NRebarY-1)
                        {
                            var y = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + j * yspace;
                            rebars.Add(new MWPoint2D(x, y));
                        }
                    }
                }
                minX = -LX / 2;
                maxX = LX / 2;
                minY = -LY / 2;
                maxY = LY / 2;
            }
            else if (Shape == GeoShape.Circular)
            {
                bary = new MWPoint2D(Diameter / 2, Diameter / 2);
                // Creation of the rebar positions
                double inc = 2 * Math.PI / NRebarCirc;
                double theta = 0;
                for (int i = 0; i < NRebarCirc; i++)
                {
                    theta = i * inc;
                    double x = (Diameter / 2 - CoverToLinks - LinkDiameter - BarDiameter / 2.0) * Math.Cos(theta);
                    double y = (Diameter / 2 - CoverToLinks - LinkDiameter - BarDiameter / 2.0) * Math.Sin(theta);
                    rebars.Add(new MWPoint2D(x, y));
                }
                minX = -Diameter / 2;
                maxX = Diameter / 2;
                minY = -Diameter / 2;
                maxY = Diameter / 2;
            }
            else if (Shape == GeoShape.Polygonal || Shape == GeoShape.LShaped || Shape == GeoShape.TShaped)
            {
                List<MWPoint2D> concPoints = new List<MWPoint2D>();
                // Creation of the rebar positions
                double inc = 2 * Math.PI / Edges;
                double theta = 0;
                double dd = (CoverToLinks + LinkDiameter + BarDiameter / 2.0) / Math.Sin((Edges - 2.0) * Math.PI / (2.0 * Edges));
                for (int i = 0; i < Edges; i++)
                {
                    theta = i * inc;
                    double x = (Radius - dd) * Math.Cos(theta);
                    double y = (Radius - dd) * Math.Sin(theta);
                    rebars.Add(new MWPoint2D(x, y));
                    double xx = Radius * Math.Cos(theta);
                    double yy = Radius * Math.Sin(theta);
                    concPoints.Add(new MWPoint2D(xx, yy));
                }
                bary = Points.GetBarycenter(concPoints);
                minX = concPoints.Min(p => p.X);
                maxX = concPoints.Max(p => p.X);
                minY = concPoints.Min(p => p.Y);
                maxY = concPoints.Max(p => p.Y);
            }

            double area = Math.PI * Math.Pow(BarDiameter / 2.0, 2);

            // calculation of moments of inertia
            double inertiaX = 0;
            double inertiaY = 0;
            for (int i = 0; i < rebars.Count; i++)
            {
                MWPoint2D pt = rebars[i];
                // according to X
                inertiaX += area * Math.Pow(pt.Y - bary.Y, 2);
                // according to Y
                inertiaY += area * Math.Pow(pt.X - bary.X, 2);
            }
            double totArea = rebars.Count * Math.PI * Math.Pow(BarDiameter / 2.0, 2);

            // Radius of gyration
            double gyradiusX = Math.Sqrt(inertiaX / totArea);
            double gyradiusY = Math.Sqrt(inertiaY / totArea);

            // Effective depths
            double dx = Math.Min(Math.Abs(minY), maxY) + gyradiusX; // it's conservative to take the min
            double dy = Math.Min(Math.Abs(minX), maxX) + gyradiusY;

            return new double[] { dx, dy };
        }

        public double[] GetSecondMomentInertia()
        {
            if (Shape == GeoShape.Rectangular)
            {
                double Ix = (LX * Math.Pow(LY, 3)) / 12;
                double Iy = (LY * Math.Pow(LX, 3)) / 12;
                return new double[] { Ix, Iy, LX * LY };
            }
            else if (Shape == GeoShape.Circular)
            {
                double I = Math.PI * Math.Pow(Diameter, 4) / 64;
                return new double[] { I, I, Math.PI * Math.Pow(Diameter / 2, 2) };
            }
            else if (Shape == GeoShape.Polygonal || Shape == GeoShape.LShaped || Shape == GeoShape.TShaped)
            {
                double Ix = 0;
                double Iy = 0;
                double area = 0;
                List<MWPoint2D> points = new List<MWPoint2D>();
                double theta = 0;
                double inc = 2 * Math.PI / Edges;
                for (int i = 0; i < Edges; i++)
                {
                    theta = i * inc;
                    double x = Radius * Math.Cos(theta);
                    double y = Radius * Math.Sin(theta);
                    points.Add(new MWPoint2D(x, y));
                }
                points.Add(points[0]);
                for (int i = 0; i < Edges; i++)
                {
                    Ix += (points[i].X * points[i + 1].Y - points[i + 1].X * points[i].Y) * (Math.Pow(points[i].X, 2) + points[i].X * points[i + 1].X + Math.Pow(points[i + 1].X, 2)) / 12;
                    Iy += (points[i].X * points[i + 1].Y - points[i + 1].X * points[i].Y) * (Math.Pow(points[i].Y, 2) + points[i].Y * points[i + 1].Y + Math.Pow(points[i + 1].Y, 2)) / 12;
                    area += 0.5 * Math.Abs(((points[i + 1].X - 0) * (points[i].Y - 0) - (points[i + 1].Y - 0) * (points[i].Y - 0)));
                }

                return new double[] { Ix, Iy, area };
            }
            return null;
        }

        public double GetColumnArea()
        {
            if (Shape == GeoShape.Rectangular)
                return LX * LY;
            else if (Shape == GeoShape.Circular)
                return Math.PI * Math.Pow(Diameter / 2, 2);
            else if (Shape == GeoShape.Polygonal)
                return Edges * Math.Pow(Radius, 2) * Math.Cos(Math.PI / Edges) * Math.Sin(Math.PI / Edges);
            else if (Shape == GeoShape.LShaped)
                return HX * HY - (HX - hX) * (HY - hY);
            else if (Shape == GeoShape.TShaped)
                return HX * HY - (HX - hX) * (HY - hY);
            return 0;
        }

        public double GetSteelArea()
        {
            int n = 0;
            if (Shape == GeoShape.Rectangular)
                n = 2 * (NRebarX + NRebarY - 2);
            else if (Shape == GeoShape.Circular)
                n = NRebarCirc;
            else if (Shape == GeoShape.Polygonal)
                n = Edges;
            else if (Shape == GeoShape.LShaped)
                n = 8 + (NRebarX - 3) * 2 + (NRebarY - 3) * 2;
            else if (Shape == GeoShape.TShaped)
                n = 10;
            return n * Math.PI * Math.Pow(BarDiameter / 2.0, 2);
        }

        public double GetConcreteArea()
        {
            return GetColumnArea() - GetSteelArea();
        }

        public double[] GetAxialLength()
        {
            if (Shape == GeoShape.Rectangular)
                return new double[] { LX, LY };
            else if (Shape == GeoShape.Circular)
                return new double[] { Diameter, Diameter };
            else if (Shape == GeoShape.Polygonal)
            {
                List<MWPoint2D> points = new List<MWPoint2D>();
                double theta = 0;
                double inc = 2 * Math.PI / Edges;
                for (int i = 0; i < Edges; i++)
                {
                    theta = i * inc;
                    double x = Radius * Math.Cos(theta);
                    double y = Radius * Math.Sin(theta);
                    points.Add(new MWPoint2D(x, y));
                }
                return new double[] { Math.Abs(points.Min(p => p.X) - points.Max(p => p.X)), Math.Abs(points.Min(p => p.Y) - points.Max(p => p.Y)) };
            }
            else if (Shape == GeoShape.LShaped)
                return new double[] { HX, HY };
            else if (Shape == GeoShape.TShaped)
                return new double[] { HX, HY };
            return null;
        }

        public double GetPerimeter()
        {
            if (Shape == GeoShape.Rectangular)
                return 2 * (LX + LY);
            else if (Shape == GeoShape.Circular)
                return 2 * Math.PI * Diameter / 2;
            else if (Shape == GeoShape.Polygonal)
            {
                double n = Edges;
                return n * Diameter * Math.Sin(Math.PI / n);
            }
            else if (Shape == GeoShape.LShaped)
                return 2 * (HX + HY);
            else if (Shape == GeoShape.TShaped)
                return 2 * (HX + HY);
            return 0;
        }

        public double SteelVol()
        {
            return GetSteelArea() * Length;
        }

        public double ConcreteVol()
        {
            return GetConcreteArea() * Length;
        }

        public double[] GetEmbodiedCarbon()
        {
            if (CarbonData.Count == 0) InitializeCarbonData();
            double Fc = ConcreteGrade.Fc;
            
            double rebarCarbon = Math.Round(1.46 * SteelVol() / 1E9 * steelVolMass, 1);

            // the concrte embodied carbon is taken from the table or interpolated if necessary
            double concreteRatio = 0;
            if (CarbonData.Keys.Contains(ConcreteGrade.Fc))
                concreteRatio = CarbonData[Fc];
            else if(Fc < CarbonData.Min(x => x.Key))
            {
                double y0 = CarbonData.ElementAt(0).Value;
                double x0 = CarbonData.ElementAt(0).Key;
                double y1 = CarbonData.ElementAt(1).Value;
                double x1 = CarbonData.ElementAt(1).Key;
                concreteRatio = y0 - (y1 - y0) / (x1 - x0) * (x0 - Fc);
            }
            else if(Fc > CarbonData.Max(x => x.Key))
            {
                double y0 = CarbonData.ElementAt(CarbonData.Count - 2).Value;
                double x0 = CarbonData.ElementAt(CarbonData.Count - 2).Key;
                double y1 = CarbonData.ElementAt(CarbonData.Count - 1).Value;
                double x1 = CarbonData.ElementAt(CarbonData.Count - 1).Key;
                concreteRatio = y0 + (y1 - y0) / (x1 - x0) * (Fc - x0);
            }
            else
            {
                var xsup = CarbonData.First(x => x.Key > Fc).Key;
                var xinf = CarbonData.Reverse().First(x => x.Key < Fc).Key;
                var ysup = CarbonData.First(x => x.Key > Fc).Value;
                var yinf = CarbonData.Reverse().First(x => x.Key < Fc).Value;
                concreteRatio = yinf + (ysup - yinf) / (xsup - xinf) * (Fc - xinf);
            }
            double concreteCarbon = Math.Round(concreteRatio * ConcreteVol() / 1E9 * concreteVolMass, 1);

            return new double[] { concreteCarbon, rebarCarbon, concreteCarbon + rebarCarbon };

        }

        public double[] GetCost()
        {
            if (SteelCosts.Count == 0) InitializeSteelCosts();
            if (ConcreteCosts.Count == 0) InitializeConcreteCosts();
            double steel = SteelVol() / 1e9 * steelVolMass / 1e3 * SteelCosts.FirstOrDefault(x => x.Key == BarDiameter).Value[0];
            double concrete = ConcreteVol() / 1e9 * ConcreteCosts.FirstOrDefault(x => x.Key == Math.Round(ConcreteGrade.Fc)).Value[0];
            double formwork = GetPerimeter() * Length * 45 / 1e6;

            return new double[] { concrete, steel, formwork, steel + concrete + formwork };
        }

        public void GetUtilisation()
        {
            double bigN = 1E6;
            MWPoint3D p0 = new MWPoint3D(SelectedLoad.Mxd, SelectedLoad.Myd,-SelectedLoad.P);

            MWPoint3D pMX_pos = new MWPoint3D(SelectedLoad.Mxd+10e5, SelectedLoad.Myd,-SelectedLoad.P);
            MWPoint3D pMX_neg = new MWPoint3D(SelectedLoad.Mxd -10e5, SelectedLoad.Myd,-SelectedLoad.P);
            MWPoint3D pMY_pos = new MWPoint3D(SelectedLoad.Mxd, SelectedLoad.Myd +10e5,-SelectedLoad.P);
            MWPoint3D pMY_neg = new MWPoint3D(SelectedLoad.Mxd, SelectedLoad.Myd -10e5,-SelectedLoad.P);
            MWPoint3D pP_pos = new MWPoint3D(SelectedLoad.Mxd, SelectedLoad.Myd,-SelectedLoad.P -10e5);
            MWPoint3D pP_neg = new MWPoint3D(SelectedLoad.Mxd, SelectedLoad.Myd,-SelectedLoad.P +10e5);
            
            MWPoint3D[] points = new MWPoint3D[] { pMX_pos, pMX_neg, pMY_pos, pMY_neg, pP_pos, pP_neg };
            double[] Util = new double[] { bigN, bigN, bigN, bigN, bigN, bigN };
            double[] interDist = new double[] { bigN, bigN, bigN, bigN, bigN, bigN };

            for (int i = 0; i < this.diagramFaces.Count; i++)
            {
                for(int j = 0; j < points.Length; j++)
                {
                    MWPoint3D pInter0 = Polygon3D.PlaneLineIntersection(new MWPoint3D[] { p0, points[j] }, this.diagramFaces[i].Points);
                    if (pInter0.X != double.NaN)
                    {
                        MWPoint3D pInter = pInter0;
                        MWVector3D v = Vectors3D.VectorialProduct(MWVector3D.vector3DByCoordinates(p0.X - pInter.X, p0.Y - pInter.Y, p0.Z - pInter.Z),
                                                                    MWVector3D.vector3DByCoordinates(points[j].X - pInter.X, points[j].Y - pInter.Y, points[j].Z - pInter.Z));

                        List<MWPoint3D> pts = this.diagramFaces[i].Points;
                        double a1 = Vectors3D.TriangleArea(MWVector3D.vector3DByCoordinates(pts[0].X - pInter.X, pts[0].Y - pInter.Y, pts[0].Z - pInter.Z),
                                                                MWVector3D.vector3DByCoordinates(pts[1].X - pInter.X, pts[1].Y - pInter.Y, pts[1].Z - pInter.Z));
                        double a2 = Vectors3D.TriangleArea(MWVector3D.vector3DByCoordinates(pts[1].X - pInter.X, pts[1].Y - pInter.Y, pts[1].Z - pInter.Z),
                                                                MWVector3D.vector3DByCoordinates(pts[2].X - pInter.X, pts[2].Y - pInter.Y, pts[2].Z - pInter.Z));
                        double a3 = Vectors3D.TriangleArea(MWVector3D.vector3DByCoordinates(pts[2].X - pInter.X, pts[2].Y - pInter.Y, pts[2].Z - pInter.Z),
                                                                MWVector3D.vector3DByCoordinates(pts[0].X - pInter.X, pts[0].Y - pInter.Y, pts[0].Z - pInter.Z));
                        double a0 = Vectors3D.TriangleArea(MWVector3D.vector3DByCoordinates(pts[1].X - pts[0].X, pts[1].Y - pts[0].Y, pts[1].Z - pts[0].Z),
                                                                MWVector3D.vector3DByCoordinates(pts[2].X - pts[0].X, pts[2].Y - pts[0].Y, pts[2].Z - pts[0].Z));
                        if (Math.Abs(a1 + a2 + a3 - a0) < 1E-3)
                        {
                            double inter = Points.Distance3D(pInter, p0);
                            if (inter < interDist[j])
                            {
                                interDist[j] = inter;
                                if(j == 0 || j == 1)
                                    Util[j] = Math.Abs(p0.X / pInter.X) * 100;
                                else if (j == 2 || j == 3)
                                    Util[j] = Math.Abs(p0.Y / pInter.Y) * 100;
                                else if (j == 4 || j == 5)
                                    Util[j] = Math.Abs(p0.Z / pInter.Z) * 100;

                            }
                        }
                    }
                }
            }

            UtilMx = (Util[0] != bigN || Util[1] != bigN)? Math.Round(Math.Min(Util[0],Util[1]),1) : -1;
            UtilMy = (Util[2] != bigN || Util[3] != bigN) ? Math.Round(Math.Min(Util[2], Util[3]),1) : -1;
            UtilP = (Util[4] != bigN || Util[5] != bigN) ?  Math.Round(Math.Min(Util[4], Util[5]),1) : -1;
        }

        public bool CheckMinRebarNo()
        {
            if (R < 120)
            {
                if(Shape == GeoShape.Circular)
                    return NRebarCirc >= 6 ? true : false;
                else
                    return true;
            }
            else
            {
                if (Shape == GeoShape.Rectangular)
                    return (2 * (NRebarX + NRebarY - 2) >= 8) ? true : false;
                else if (Shape == GeoShape.Circular)
                    return (NRebarCirc >= 8) ? true : false;
                else if (Shape == GeoShape.Polygonal)
                    return (Edges >= 8) ? true : false;
                else if (Shape == GeoShape.LShaped)
                    return (8 + (NRebarX - 3) * 2 + (NRebarY - 3) * 2 >= 8) ? true : false;
                else if (Shape == GeoShape.TShaped)
                    return (Nrebars >= 8) ? true : false;
            }
            return false;
        }

        public void CheckGuidances()
        {
            // bar spacing
            StringBuilder message = new StringBuilder();
            if(Shape == GeoShape.Rectangular)
            {
                double d = 2 * (CoverToLinks + LinkDiameter) + BarDiameter;
                double sx = (LX - d) / (NRebarX - 1);
                double sy = (LY - d) / (NRebarY - 1);
                if(sx < 75 || sy < 75)
                    message.AppendLine("Preferred minimum spacing is 75mm");
                if (sx > 175 || sy > 175)
                    message.AppendLine("Preferred max spacing is 300mm for compression bars and 175mm for tension bars");
            }
            else if (Shape == GeoShape.Circular)
            {
                double x0 = Diameter / 2.0 - CoverToLinks - LinkDiameter - BarDiameter / 2.0;
                double x1 = x0 * Math.Cos(2 * Math.PI / NRebarCirc);
                double y1 = x0 * Math.Sin(2 * Math.PI / NRebarCirc);
                double s = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1, 2));
                if (s < 75)
                    message.AppendLine("Preferred minimum spacing is 75mm");
                if (s > 175)
                    message.AppendLine("Preferred max spacing is 300mm for compression bars and 175mm for tension bars");
            }
            else if (Shape == GeoShape.Polygonal)
            {
                double x0 = Diameter / 2 - CoverToLinks - LinkDiameter - BarDiameter / 2.0;
                double x1 = x0 * Math.Cos(2 * Math.PI / Edges);
                double y1 = x0 * Math.Sin(2 * Math.PI / Edges);
                double s = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1, 2));
                if (s < 75)
                    message.AppendLine("Preferred minimum spacing is 75mm");
                if (s > 175)
                    message.AppendLine("Preferred max spacing is 300mm for compression bars and 175mm for tension bars");
            }
            else if (Shape == GeoShape.LShaped)
            {
                if (sx1 < 75 || sx2 < 75 || sy1 < 75 || sy2 < 75)
                    message.AppendLine("Preferred minimum spacing is 75mm");
                if (sx1 > 175 || sx2 > 175 || sy1 > 175 || sy2 > 175)
                    message.AppendLine("Preferred max spacing is 300mm for compression bars and 175mm for tension bars");
            }
            else if (Shape == GeoShape.TShaped)
            {
                if (sx1 < 75 || sx2 < 75 || sy1 < 75 || sy2 < 75)
                    message.AppendLine("Preferred minimum spacing is 75mm");
                if (sx1 > 175 || sx2 > 175 || sy1 > 175 || sy2 > 175)
                    message.AppendLine("Preferred max spacing is 300mm for compression bars and 175mm for tension bars");
            }
            if (message.Length > 0)
            {
                GuidanceCheck = false;
                GuidanceMessage = message.ToString();
            }
            else
            {
                GuidanceCheck = true;
                GuidanceMessage = "All good!";
            }
        }
        
        private void InitializeCarbonData()
        {
            CarbonData.Add(32, 0.163);
            CarbonData.Add(40, 0.188);
            CarbonData.Add(50, 0.205);
            CarbonData.Add(60, 0.23);
        }

        private void InitializeSteelCosts()
        {
            SteelCosts.Add(10, new double[] { 1300, 1450 });
            SteelCosts.Add(12, new double[] { 1225, 1372 });
            SteelCosts.Add(16, new double[] { 1100, 1220 });
            SteelCosts.Add(20, new double[] { 998, 1100 });
            SteelCosts.Add(25, new double[] { 950, 1050 });
            SteelCosts.Add(32, new double[] { 910, 1001 });
            SteelCosts.Add(40, new double[] { 870, 960 });
        }

        private void InitializeConcreteCosts()
        {
            ConcreteCosts.Add(30, new double[] { 96.4, 96.4 });
            ConcreteCosts.Add(32, new double[] { 98.5, 98.5 });
            ConcreteCosts.Add(35, new double[] { 101.17, 101.17 });
            ConcreteCosts.Add(40, new double[] { 102.13, 102.13 });
            ConcreteCosts.Add(50, new double[] { 104.03, 104.03 });
        }

        public void SetFireData()
        {
            fireTable.Add(new FireData(30, 0.2, 200, 25));
            fireTable.Add(new FireData(30, 0.5, 200, 25));
            fireTable.Add(new FireData(30, 0.7, 200, 32));
            fireTable.Add(new FireData(30, 0.7, 300, 27));
            fireTable.Add(new FireData(60, 0.2, 200, 25));
            fireTable.Add(new FireData(60, 0.5, 200, 36));
            fireTable.Add(new FireData(60, 0.5, 300, 31));
            fireTable.Add(new FireData(60, 0.7, 250, 46));
            fireTable.Add(new FireData(60, 0.7, 350, 40));
            fireTable.Add(new FireData(90, 0.2, 200, 31));
            fireTable.Add(new FireData(90, 0.2, 300, 25));
            fireTable.Add(new FireData(90, 0.5, 300, 45));
            fireTable.Add(new FireData(90, 0.5, 400, 38));
            fireTable.Add(new FireData(90, 0.7, 350, 53));
            fireTable.Add(new FireData(90, 0.7, 450, 40));
            fireTable.Add(new FireData(120, 0.2, 250, 40));
            fireTable.Add(new FireData(120, 0.2, 350, 35));
            fireTable.Add(new FireData(120, 0.5, 350, 45));
            fireTable.Add(new FireData(120, 0.5, 450, 40));
            fireTable.Add(new FireData(120, 0.7, 350, 57));
            fireTable.Add(new FireData(120, 0.7, 450, 51));
            fireTable.Add(new FireData(180, 0.2, 350, 45));
            fireTable.Add(new FireData(180, 0.5, 350, 63));
            fireTable.Add(new FireData(180, 0.7, 450, 70));
            fireTable.Add(new FireData(240, 0.2, 350, 61));
            fireTable.Add(new FireData(240, 0.5, 350, 75));
            fireTable.Add(new FireData(30, 0.7, 155, 25, FireExposition.OneSide));
            fireTable.Add(new FireData(60, 0.7, 155, 25, FireExposition.OneSide));
            fireTable.Add(new FireData(90, 0.7, 155, 25, FireExposition.OneSide));
            fireTable.Add(new FireData(120, 0.7, 175, 35, FireExposition.OneSide));
            fireTable.Add(new FireData(180, 0.7, 230, 55, FireExposition.OneSide));
            fireTable.Add(new FireData(240, 0.7, 295, 70, FireExposition.OneSide));
        }

        public void SetConcreteData()
        {
            double d = ConcreteGrade.Density;
            concreteData.Add(new ConcreteData(25, 1, 0.0025, 0.020, AggregateType.Siliceous, d));
            concreteData.Add(new ConcreteData(100, 1, 0.0040, 0.0225, AggregateType.Siliceous, d));
            concreteData.Add(new ConcreteData(200, 0.95, 0.0055, 0.025, AggregateType.Siliceous, d * 0.98));
            concreteData.Add(new ConcreteData(300, 0.85, 0.0070, 0.0275, AggregateType.Siliceous, d * 0.965));
            concreteData.Add(new ConcreteData(400, 0.75, 0.0100, 0.0300, AggregateType.Siliceous, d * 0.95));
            concreteData.Add(new ConcreteData(500, 0.60, 0.0150, 0.0325, AggregateType.Siliceous, d * 0.94125));
            concreteData.Add(new ConcreteData(600, 0.45, 0.0250, 0.0350, AggregateType.Siliceous, d * 0.9325));
            concreteData.Add(new ConcreteData(700, 0.30, 0.0250, 0.0375, AggregateType.Siliceous, d * 0.92375));
            concreteData.Add(new ConcreteData(800, 0.15, 0.0250, 0.0400, AggregateType.Siliceous, d * 0.915));
            concreteData.Add(new ConcreteData(900, 0.08, 0.0250, 0.0425, AggregateType.Siliceous, d * 0.90625));
            concreteData.Add(new ConcreteData(1000, 0.04, 0.0250, 0.045, AggregateType.Siliceous, d * 0.8975));
            concreteData.Add(new ConcreteData(1100, 0.01, 0.0250, 0.0475, AggregateType.Siliceous, d * 0.88875));
            concreteData.Add(new ConcreteData(1200, 0.00, 0.0250, 0.050, AggregateType.Siliceous, d * 0.88));
        }

        public void SetSteelData()
        {
            steelData.Add(new SteelData(25, 1.0, 1.0));
            steelData.Add(new SteelData(100, 1.0, 1.0));
            steelData.Add(new SteelData(200, 1.0, 0.9));
            steelData.Add(new SteelData(300, 1.0, 0.8));
            steelData.Add(new SteelData(400, 1.0, 0.7));
            steelData.Add(new SteelData(500, 0.78, 0.6));
            steelData.Add(new SteelData(600, 0.47, 0.31));
            steelData.Add(new SteelData(700, 0.23, 0.13));
            steelData.Add(new SteelData(800, 0.11, 0.09));
            steelData.Add(new SteelData(900, 0.06, 0.07));
            steelData.Add(new SteelData(1000, 0.04, 0.04));
            steelData.Add(new SteelData(1100, 0.02, 0.02));
            steelData.Add(new SteelData(1200, 0.0, 0.0));
        }
    }

    public class Load
    {
        public string Name { get; set; }
        public double MxTop { get; set; } = 0;
        public double MxBot { get; set; } = 0;
        public double MyTop { get; set; } = 0;
        public double MyBot { get; set; } = 0;
        public double P { get; set; } = 0;

        public double Mxd { get; set; } = 0;
        public double Myd { get; set; } = 0;

        public Load()
        {

        }

        public Load(ETABSColumnDesign_Plugin.Load l)
        {
            Name = l.Name;
            MxTop = Math.Round(l.MxTop);
            MxBot = Math.Round(l.MxBot);
            MyTop = Math.Round(l.MyTop);
            MyBot = Math.Round(l.MyBot);
            P = -Math.Round(l.P);
        }

        public Load Clone()
        {
            return new Load()
            {
                Name = this.Name,
                MxTop = this.MxTop,
                MxBot = this.MxBot,
                MyTop = this.MyTop,
                MyBot = this.MyBot,
                P = this.P
            };
        }

    }

    //public class FireDesignMethod
    //{
    //    public FDesignMethod Method { get; set; }
    //    public bool IsSelectable { get; set; }

    //    public FireDesignMethod(FDesignMethod method, bool selectable = true)
    //    {
    //        Method = method;
    //        IsSelectable = selectable;
    //    }
    //}

}