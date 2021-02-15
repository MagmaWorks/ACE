using InteractionDiagram3D;
using MWGeometry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Material = InteractionDiagram3D.Material;
using CalcCore;
using System.Reflection;
using System.IO;

namespace ColumnDesignCalc
{
    public enum GeoShape { Rectangular, Circular, Polygonal, LShaped, TShaped, CustomShape }
    public enum FDesignMethod { Table, Isotherm_500, Zone_Method, Advanced }
    public enum FireExposition { OneSide, MoreThanOneSide };
    public enum AggregateType { Siliceous, Calcareous };

    public class Column
    {
        public string Name { get; set; } = "Col 350x350";
        // Geometry
        public GeoShape Shape { get; set; } = GeoShape.Rectangular;
        public bool IsRectangular { get => Shape == GeoShape.Rectangular; }
        public bool IsCircular { get => Shape == GeoShape.Circular; }
        public bool IsPolygonal { get => Shape == GeoShape.Polygonal; }
        public bool IsLShaped { get => Shape == GeoShape.LShaped; }
        public bool IsTShaped { get => Shape == GeoShape.TShaped; }
        public bool IsCustomShape { get => Shape == GeoShape.CustomShape; }
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
        public double customLX { get; set; } = 225;
        public double customLY { get; set; } = 900;
        public double d1x { get; set; } = 0;
        public double d1y { get; set; } = 0;
        public double d2x { get; set; } = 15;
        public double d2y { get; set; } = 315;
        public double d3x { get; set; } = 0;
        public double d3y { get; set; } = 0;
        public double d4x { get; set; } = 15;
        public double d4y { get; set; } = 315;
        public double Theta = 0;
        public int Nrebars { get => RebarsPos.Count; }
        public MWPoint3D Point1 { get; set; }
        public MWPoint3D Point2 { get; set; }

        public List<MWPoint2D> ContourPoints { get => GetContourPoints(); } // centered in LX/2 LY/2
        public bool IsAdvancedRebar { get; set; } = false;
        public string AdvancedRebarFile { get; set; }
        public List<MWPoint2D> RebarsPos { get => GetRebars(); } // centered in LX/2 LY/2
        public List<MWPoint2D> AdvancedRebarsPos { get; set; } = new List<MWPoint2D>() { new MWPoint2D(0, 0) }; // centered in LX/2 LY/2

        // Material
        public Concrete ConcreteGrade { get; set; }
        public double MaxAggSize { get; set; } = 20;
        public Steel SteelGrade { get; set; }
        //public Steel CustomSteelGrade { get; set; }

        // Loads
        public Load SelectedLoad { get; set; }
        public Load FireLoad { get; set; }

        public List<Load> Loads { get; set; } //= new List<Load>();
        public List<string> FireLoadNames { get => LoadNames.Append("0.7*[selected]").ToList(); }
        public List<string> LoadNames { get => Loads.Select(l => l.Name).ToList(); }
        public bool AllLoads { get; set; } = false;

        // Design
        public double EffectiveLength { get; set; } = 0.7;
        public double CoverToLinks { get; set; } = 40;
        public int BarDiameter { get; set; } = 16;
        public int LinkDiameter { get; set; } = 10;
        public double LinkSpacing { get; set; }
        public int NRebarX { get; set; } = 3;
        public int NRebarY { get; set; } = 3;
        public int NRebarCirc { get; set; } = 5;

        // Fire Design
        public int R { get; set; } = 120; // fire resistance in min
        public FireExposition SidesExposed { get; set; } = FireExposition.MoreThanOneSide;
        public FDesignMethod FireDesignMethod { get => (FDesignMethod)Enum.Parse(typeof(FDesignMethod), FDMStr); }
        //public FireDesignMethod FireDesignMethod { get; set; } = new FireDesignMethod(FDesignMethod.Table);
        public string FDMStr { get; set; } = "Table";
        public FCurve FireCurve { get => (FCurve)Enum.Parse(typeof(FCurve), FCStr); }
        public string FCStr { get; set; } = "Standard";
        //[JsonIgnore]
        public TemperatureProfile TP;

        // 3D interaction diagram
        public int IDReduction { get; set; } = 100;
        [JsonIgnore]
        public List<MWPoint3D> diagramVertices = new List<MWPoint3D>();
        [JsonIgnore]
        public List<Tri3D> diagramFaces = new List<Tri3D>();

        [JsonIgnore]
        public List<MWPoint3D> fireDiagramVertices = new List<MWPoint3D>();
        [JsonIgnore]
        public List<Tri3D> fireDiagramFaces = new List<Tri3D>();

        // 2D interaction diagrams
        [JsonIgnore]
        public List<MWPoint2D> MxMyPts { get; set; } = new List<MWPoint2D>();
        [JsonIgnore]
        public List<MWPoint2D> MxNPts { get; set; } = new List<MWPoint2D>();
        [JsonIgnore]
        public List<MWPoint2D> MyNPts { get; set; } = new List<MWPoint2D>();
        [JsonIgnore]
        public List<MWPoint2D> fireMxMyPts { get; set; } = new List<MWPoint2D>();
        [JsonIgnore]
        public List<MWPoint2D> fireMxNPts { get; set; } = new List<MWPoint2D>();
        [JsonIgnore]
        public List<MWPoint2D> fireMyNPts { get; set; } = new List<MWPoint2D>();
        // Checks
        public bool? CapacityCheck { get; set; } = false;
        public bool? FireCheck { get; set; } = false;
        public bool? SpacingCheck { get; set; } = false;
        public bool? MinMaxSteelCheck { get; set; } = false;
        public bool? MinRebarCheck { get; set; } = false;
        public bool GuidanceCheck { get; set; } = false;
        public string GuidanceMessage { get; set; } = "All good!";

        const double gs = 1.15;
        const double gc = 1.5;
        const double acc = 0.85;


        public double UtilP { get; set; } = 0;
        public double UtilMx { get; set; } = 0;
        public double UtilMy { get; set; } = 0;
        public double Util3D { get; set; } = 0;

        const double concreteVolMass = 2.5e3;
        const double steelVolMass = 7.5e3;

        // Optimisation
        public double Cost { get; set; } = 0;

        // Interaction diagram settings
        public int DiagramDisc { get; set; } = 30;

        public Column()
        {
            
        }

        public Column(ETABSv17_To_ACE.Column c0)
        {
            Name = c0.name;
            LX = c0.width;
            LY = c0.depth;
            Length = c0.length;
            ConcreteGrade = new Concrete("Custom", c0.fc, c0.E);
            SteelGrade = new Steel("500B", 500);
            Point1 = new MWPoint3D(c0.Point1.X, c0.Point1.Y, c0.Point1.Z);
            Point2 = new MWPoint3D(c0.Point2.X, c0.Point2.Y, c0.Point2.Z);
            Angle = c0.Angle;
            Loads = c0.Loads.Select(l => new Load(l)).ToList();
            SelectedLoad = Loads[0];
            FireLoad = Loads[0];

            GetContourPoints();
            GetRebars();
        }

        public Column(ETABSv18_To_ACE.Column c0)
        {
            Name = c0.name;
            LX = c0.width;
            LY = c0.depth;
            Length = c0.length;
            ConcreteGrade = new Concrete("Custom", c0.fc, c0.E);
            SteelGrade = new Steel("500B", 500);
            Point1 = new MWPoint3D(c0.Point1.X, c0.Point1.Y, c0.Point1.Z);
            Point2 = new MWPoint3D(c0.Point2.X, c0.Point2.Y, c0.Point2.Z);
            Angle = c0.Angle;
            Loads = c0.Loads.Select(l => new Load(l)).ToList();
            SelectedLoad = Loads[0];
            FireLoad = Loads[0];

            GetContourPoints();
            GetRebars();
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
            //col.fireTable = this.fireTable;
            col.LinkDiameter = this.LinkDiameter;
            col.MaxAggSize = this.MaxAggSize;
            col.R = this.R;
            col.FCStr = this.FCStr;
            col.FDMStr = this.FDMStr;

            col.ConcreteGrade = this.ConcreteGrade;
            col.SteelGrade = this.SteelGrade;
            //col.CustomSteelGrade = this.CustomSteelGrade;
            col.SelectedLoad = this.SelectedLoad;
            col.Loads = this.Loads.Select(l => l.Clone()).ToList();
            col.FireLoad = this.FireLoad;
            col.AllLoads = this.AllLoads;

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

        public List<MWPoint2D> GetContourPoints()
        {
            switch (Shape)
            {
                case (GeoShape.Rectangular):
                    return new List<MWPoint2D>()
                    {
                        new MWPoint2D(-LX/2,-LY/2),
                        new MWPoint2D(-LX/2,LY/2),
                        new MWPoint2D(LX/2,LY/2),
                        new MWPoint2D(LX/2,-LY/2),
                    };
                case (GeoShape.Circular):
                    var pts1 = new List<MWPoint2D>();
                    double N = 180;
                    double inc = 2 * Math.PI / N;
                    for (int i = 0; i < N; i++)
                    {
                        double theta = i * inc;
                        double X = Diameter / 2 * Math.Cos(theta);
                        double Y = Diameter / 2 * Math.Sin(theta);
                        pts1.Add(new MWPoint2D(X, Y));
                    }
                    return pts1;
                case (GeoShape.Polygonal):
                    var pts2 = new List<MWPoint2D>();
                    double incP = 2 * Math.PI / Edges;
                    for (int i = 0; i < Edges; i++)
                    {
                        double theta = i * incP;
                        double X = Radius * Math.Cos(theta);
                        double Y = Radius * Math.Sin(theta);
                        pts2.Add(new MWPoint2D(X, Y));
                    }
                    return pts2;
                case (GeoShape.LShaped):
                    double angle = Theta * Math.PI / 180;
                    double H1 = HX * Math.Abs(Math.Cos(angle)) + HY * Math.Abs(Math.Sin(angle));
                    double h1 = hX * Math.Abs(Math.Cos(angle)) + hY * Math.Abs(Math.Sin(angle));
                    double H2 = HX * Math.Abs(Math.Sin(angle)) + HY * Math.Abs(Math.Cos(angle));
                    double h2 = hX * Math.Abs(Math.Sin(angle)) + hY * Math.Abs(Math.Cos(angle));

                    List<MWPoint2D> pts = new List<MWPoint2D>()
                    {
                        new MWPoint2D(-H1 / 2, -H2 / 2),
                        new MWPoint2D(H1 / 2, -H2 / 2),
                        new MWPoint2D(H1 / 2, -H2 / 2 + h2),
                        new MWPoint2D(-H1 / 2 + h1, -H2 / 2 + h2),
                        new MWPoint2D(-H1 / 2 + h1, H2 / 2),
                        new MWPoint2D(-H1 / 2, H2 / 2)
                    };
                    return pts.Select(p => new MWPoint2D(Math.Cos(angle) * p.X - Math.Sin(angle) * p.Y, Math.Sin(angle) * p.X + Math.Cos(angle) * p.Y)).ToList();
                    
                case (GeoShape.TShaped):
                    return new List<MWPoint2D>()
                    {
                        new MWPoint2D(-hX/2, -HY/2),
                        new MWPoint2D(hX/2, -HY/2),
                        new MWPoint2D(hX/2, HY/2-hY),
                        new MWPoint2D(HX/2, HY/2-hY),
                        new MWPoint2D(HX/2, HY/2),
                        new MWPoint2D(-HX/2,HY/2),
                        new MWPoint2D(-HX/2,HY/2-hY),
                        new MWPoint2D(-hX/2,HY/2-hY)
                    };
                case (GeoShape.CustomShape):
                    return GetCustomShapeContour();
            }
            return null;
        }

        public List<MWPoint2D> GetRebars()
        {
            if(IsAdvancedRebar)
            {
                List<MWPoint2D> adv = new List<MWPoint2D>();
                foreach(var r in AdvancedRebarsPos)
                {
                    if (Polygons.isInside(ContourPoints, r))
                        adv.Add(r);
                }
                return adv;
            }
            else
            {
                
                switch (Shape)
                {
                    case (GeoShape.Rectangular):
                        double xspace = (LX - 2 * (CoverToLinks + LinkDiameter + BarDiameter / 2.0)) / (NRebarX - 1);
                        double yspace = (LY - 2 * (CoverToLinks + LinkDiameter + BarDiameter / 2.0)) / (NRebarY - 1);

                        List<MWPoint2D> posR = new List<MWPoint2D>();
                        for (int i = 0; i < NRebarX; i++)
                        {
                            var x = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + i * xspace;
                            for (int j = 0; j < NRebarY; j++)
                            {
                                if (i == 0 || i == NRebarX - 1 || j == 0 || j == NRebarY - 1)
                                {
                                    var y = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + j * yspace;
                                    posR.Add(new MWPoint2D(x - LX/2, y - LY/2));
                                }
                            }
                        }
                        return posR;
                    case (GeoShape.Circular):
                        double inc = 2 * Math.PI / NRebarCirc;
                        List<MWPoint2D> posC = new List<MWPoint2D>();
                        for (int i = 0; i < NRebarCirc; i++)
                        {
                            double theta = i * inc;
                            double x = (Diameter / 2 - CoverToLinks - LinkDiameter - BarDiameter / 2.0) * Math.Cos(theta);
                            double y = (Diameter / 2 - CoverToLinks - LinkDiameter - BarDiameter / 2.0) * Math.Sin(theta);
                            posC.Add(new MWPoint2D(x, y));
                        }
                        return posC;
                    case (GeoShape.Polygonal):
                        double incP = 2 * Math.PI / Edges;
                        double dd = (CoverToLinks + LinkDiameter + BarDiameter / 2.0) / Math.Sin((Edges - 2.0) * Math.PI / (2.0 * Edges));
                        List<MWPoint2D> posP = new List<MWPoint2D>();
                        for (int i = 0; i < Edges; i++)
                        {
                            double theta = i * incP;
                            double x = (Radius - dd) * Math.Cos(theta);
                            double y = (Radius - dd) * Math.Sin(theta);
                            posP.Add(new MWPoint2D(x, y));
                        }
                        return posP;
                    case (GeoShape.LShaped):
                        return GetLShapedRebars();
                    case (GeoShape.TShaped):
                        return GetTShapedRebars();
                    case (GeoShape.CustomShape):
                        List<MWPoint2D> posCust = new List<MWPoint2D>();
                        foreach (var r in AdvancedRebarsPos)
                        {
                            if (Polygons.isInside(ContourPoints, r))
                                posCust.Add(r);
                        }
                        return posCust;
                }
                return null;
            }
        }
        public void GetInteractionDiagram()
        {
            List<Composite> composites = new List<Composite>();
            // Materials
            double fc_steel = Math.Min(0.00175 * SteelGrade.E * 1E3, SteelGrade.Fy / 1.15);
            Material concrete = new Material(ConcreteGrade.Name, MatYpe.Concrete, 0.85 * ConcreteGrade.Fc / 1.5, 0, ConcreteGrade.E, epsMax: 0.0035, epsMax2: 0.00175, dens: ConcreteGrade.Density);
            Material steel = new Material(SteelGrade.Name, MatYpe.Steel, SteelGrade.Fy / 1.15, SteelGrade.Fy / 1.15, SteelGrade.E);
            //Material steel = new Material(steelGrade.Name, MatYpe.Steel, fc_steel, fc_steel);

            // Creation of the concrete section
            ConcreteSection cs = new ConcreteSection(ContourPoints.Select(p => new MWPoint2D(p.X + LX / 2, p.Y + LY / 2)).ToList(), concrete);
            composites.Add(cs);

            // Creation of the rebars
            foreach (var rebar in RebarsPos)
            {
                Rebar r = new Rebar(MWPoint2D.Point2DByCoordinates(rebar.X + LX / 2, rebar.Y + LY / 2), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                composites.Add(r);
            }

            if (Shape == GeoShape.Rectangular)
            {
                //// Creation of the concrete section
                //ConcreteSection cs = new ConcreteSection(ContourPoints.Select(p => new MWPoint2D(p.X + LX/2, p.Y + LY/2)).ToList(), concrete);
                //composites.Add(cs);

                //// Creation of the rebars
                //if (IsAdvancedRebar)
                //{
                //    foreach (var rebar in AdvancedRebarsPos)
                //    {
                //        Rebar r = new Rebar(MWPoint2D.Point2DByCoordinates(rebar.X + LX / 2, rebar.Y + LY / 2), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                //        composites.Add(r);
                //    }
                //}
                //else
                //{
                //    double xspace = (LX - 2 * (CoverToLinks + LinkDiameter + BarDiameter / 2.0)) / (NRebarX - 1);
                //    double yspace = (LY - 2 * (CoverToLinks + LinkDiameter + BarDiameter / 2.0)) / (NRebarY - 1);
                //    for (int i = 0; i < NRebarX; i++)
                //    {
                //        var x = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + i * xspace;
                //        for (int j = 0; j < NRebarY; j++)
                //        {
                //            if (i == 0 || i == NRebarX - 1 || j == 0 || j == NRebarY - 1)
                //            {
                //                var y = CoverToLinks + LinkDiameter + BarDiameter / 2.0 + j * yspace;
                //                Rebar r = new Rebar(MWPoint2D.Point2DByCoordinates(x, y), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                //                composites.Add(r);
                //            }
                //        }
                //    }
                //}

            }
            else if (Shape == GeoShape.Circular)
            {
                // Creation of the concrete section
                //List<MWPoint2D> circlePts = new List<MWPoint2D>();
                //double theta = 0;
                //double N = 180;
                //double inc = 2 * Math.PI / N;
                //for (int i = 0; i < N; i++)
                //{
                //    theta = i * inc;
                //    double X = Diameter / 2 * Math.Cos(theta);
                //    double Y = Diameter / 2 * Math.Sin(theta);
                //    circlePts.Add(new MWPoint2D(X, Y));
                //}
                //composites.Add(new ConcreteSection(circlePts, concrete));
                //MWPoint2D bary = Points.GetBarycenter(circlePts);
                //Console.WriteLine("conc : X = {0}, Y = {1}", bary.X, bary.Y);

                //// Creation of the rebars
                //List<MWPoint2D> steelPos = new List<MWPoint2D>();
                //theta = 0;
                //inc = 2 * Math.PI / NRebarCirc;
                //for (int i = 0; i < NRebarCirc; i++)
                //{
                //    theta = i * inc;
                //    double x = (Diameter / 2 - CoverToLinks - LinkDiameter - BarDiameter / 2.0) * Math.Cos(theta);
                //    double y = (Diameter / 2 - CoverToLinks - LinkDiameter - BarDiameter / 2.0) * Math.Sin(theta);
                //    Rebar r = new Rebar(new MWPoint2D(x, y), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                //    steelPos.Add(new MWPoint2D(x, y));
                //    composites.Add(r);
                //}
                //bary = Points.GetBarycenter(steelPos);
                //Console.WriteLine("steel : X = {0}, Y = {1}", bary.X, bary.Y);
            }
            else if (Shape == GeoShape.Polygonal)
            {
                //// Creation of the concrete section
                //List<MWPoint2D> PolyPts = new List<MWPoint2D>();
                //double theta = 0;
                //double inc = 2 * Math.PI / Edges;
                //for (int i = 0; i < Edges; i++)
                //{
                //    theta = i * inc;
                //    double X = Radius * Math.Cos(theta);
                //    double Y = Radius * Math.Sin(theta);
                //    PolyPts.Add(new MWPoint2D(X, Y));
                //}
                //composites.Add(new ConcreteSection(PolyPts, concrete));

                //MWPoint2D bary = Points.GetBarycenter(PolyPts);
                //Console.WriteLine("conc : X = {0}, Y = {1}", bary.X, bary.Y);

                //// Creation of the rebars
                //theta = 0;
                //List<MWPoint2D> steelPos = new List<MWPoint2D>();
                //double dd = (CoverToLinks + LinkDiameter + BarDiameter / 2.0) / Math.Sin((Edges - 2.0) * Math.PI / (2.0 * Edges));
                //for (int i = 0; i < Edges; i++)
                //{
                //    theta = i * inc;
                //    double x = (Radius - dd) * Math.Cos(theta);
                //    double y = (Radius - dd) * Math.Sin(theta);
                //    Rebar r = new Rebar(new MWPoint2D(x, y), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                //    steelPos.Add(new MWPoint2D(x, y));
                //    composites.Add(r);
                //}

                //bary = Points.GetBarycenter(steelPos);
                //Console.WriteLine("steel : X = {0}, Y = {1}", bary.X, bary.Y);
            }
            else if (Shape == GeoShape.LShaped)
            {
                //// creation of the concrete section
                //List<MWPoint2D> pts = GetLShapedContour();
                //List<MWPoint2D> LShapedPts = pts.Select(p => new MWPoint2D(p.X, p.Y)).ToList();
                //composites.Add(new ConcreteSection(LShapedPts, concrete));

                //// creation of the rebars
                //List<MWPoint2D> rebars = GetLShapedRebars();
                //for (int i = 0; i < rebars.Count; i++)
                //{
                //    Rebar r = new Rebar(new MWPoint2D(rebars[i].X, rebars[i].Y), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                //    //steelPos.Add(new MWPoint2D(x, y));
                //    composites.Add(r);
                //}
            }
            else if (Shape == GeoShape.TShaped)
            {
                //// creation of the concrete section
                //List<MWPoint2D> pts = GetTShapedContour();
                //List<MWPoint2D> TShapedPts = pts.Select(p => new MWPoint2D(p.X, p.Y)).ToList();
                //composites.Add(new ConcreteSection(TShapedPts, concrete));

                //// creation of the rebars
                //List<MWPoint2D> rebars = GetTShapedRebars();
                //for (int i = 0; i < rebars.Count; i++)
                //{
                //    Rebar r = new Rebar(new MWPoint2D(rebars[i].X, rebars[i].Y), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                //    //steelPos.Add(new MWPoint2D(x, y));
                //    composites.Add(r);
                //}
            }
            else if(Shape == GeoShape.CustomShape)
            {
                //// creation of the concrete section
                //List<MWPoint2D> pts = GetCustomShapeContour();
                //List<MWPoint2D> CustomShapePts = pts.Select(p => new MWPoint2D(p.X, p.Y)).ToList();
                //composites.Add(new ConcreteSection(CustomShapePts, concrete));

                //// creation of the rebars
                //List<MWPoint2D> rebars = GetCustomShapeRebars();
                //for (int i = 0; i < rebars.Count; i++)
                //{
                //    Rebar r = new Rebar(new MWPoint2D(rebars[i].X, rebars[i].Y), Math.PI * Math.Pow(BarDiameter / 2.0, 2), steel);
                //    composites.Add(r);
                //}
            }

            Diagram d = new Diagram(composites, DiagramDisc);

            diagramVertices = d.vertices;
            diagramFaces = d.faces;

            if (IDReduction != 100)
            {
                double red = IDReduction / 100.0;
                for (int i = 0; i < diagramVertices.Count; i++)
                    diagramVertices[i] = new MWPoint3D(
                        red * diagramVertices[i].X,
                        red * diagramVertices[i].Y,
                        red * diagramVertices[i].Z
                        );
                for (int i = 0; i < diagramFaces.Count; i++)
                {
                    for (int j = 0; j < diagramFaces[i].Points.Count; j++)
                    {
                        diagramFaces[i].Points[j] = new MWPoint3D(
                            red * diagramFaces[i].Points[j].X,
                            red * diagramFaces[i].Points[j].Y,
                            red * diagramFaces[i].Points[j].Z
                            );
                    }
                }
            }

        }

        //public List<MWPoint2D> GetLShapedContour()
        //{
        //    double angle = Theta * Math.PI / 180;
        //    double H1 = HX * Math.Abs(Math.Cos(angle)) + HY * Math.Abs(Math.Sin(angle));
        //    double h1 = hX * Math.Abs(Math.Cos(angle)) + hY * Math.Abs(Math.Sin(angle));
        //    double H2 = HX * Math.Abs(Math.Sin(angle)) + HY * Math.Abs(Math.Cos(angle));
        //    double h2 = hX * Math.Abs(Math.Sin(angle)) + hY * Math.Abs(Math.Cos(angle));

        //    List<MWPoint2D> ContourPts = new List<MWPoint2D>()
        //    {
        //        new MWPoint2D(-H1 / 2, -H2 / 2),
        //        new MWPoint2D(H1 / 2, -H2 / 2),
        //        new MWPoint2D(H1 / 2, -H2 / 2 + h2),
        //        new MWPoint2D(-H1 / 2 + h1, -H2 / 2 + h2),
        //        new MWPoint2D(-H1 / 2 + h1, H2 / 2),
        //        new MWPoint2D(-H1 / 2, H2 / 2)
        //    };


        //    return ContourPts.Select(p => new MWPoint2D(Math.Cos(angle) * p.X - Math.Sin(angle) * p.Y,
        //                                            Math.Sin(angle) * p.X + Math.Cos(angle) * p.Y)).ToList();
        //}

        public List<MWPoint2D> GetLShapedRebars()
        {
            if (IsAdvancedRebar) return AdvancedRebarsPos;
            double angle = Theta * Math.PI / 180;
            double H1 = HX * Math.Abs(Math.Cos(angle)) + HY * Math.Abs(Math.Sin(angle));
            double h1 = hX * Math.Abs(Math.Cos(angle)) + hY * Math.Abs(Math.Sin(angle));
            double H2 = HX * Math.Abs(Math.Sin(angle)) + HY * Math.Abs(Math.Cos(angle));
            double h2 = hX * Math.Abs(Math.Sin(angle)) + hY * Math.Abs(Math.Cos(angle));

            int NrotX = (Theta == 0 || Theta == 180) ? NRebarX : NRebarY;
            int NrotY = (Theta == 0 || Theta == 180) ? NRebarY : NRebarX;

            double d = CoverToLinks + LinkDiameter + BarDiameter / 2;
            List<MWPoint2D> rebars = new List<MWPoint2D>()
            {
                new MWPoint2D(-H1/2 + d, -H2/2 + d),
                new MWPoint2D(-H1/2 + h1 - d, -H2/2 + d),
                new MWPoint2D(-H1/2 + d, -H2/2 + h2 - d),
                new MWPoint2D(-H1/2 + h1 - d, -H2/2 + h2 - d),
            };
            if (HX - hX > 2 * BarDiameter)
            {
                rebars.Add(new MWPoint2D(H1 / 2 - d, -H2 / 2 + d));
                rebars.Add(new MWPoint2D(H1 / 2 - d, -H2 / 2 + h2 - d));
            }
            if (HY - hY > 2 * BarDiameter)
            {
                rebars.Add(new MWPoint2D(-H1 / 2 + d, H2 / 2 - d));
                rebars.Add(new MWPoint2D(-H1 / 2 + h1 - d, H2 / 2 - d));
            }

            double ly1 = H2 - h2;
            double ly2 = h2 - 2 * d;
            double lx1 = H1 - h1;
            double lx2 = h1 - 2 * d;
            int nX1 = 0;
            int nX2 = 0;
            int nY1 = 0;
            int nY2 = 0;
            List<MWPoint2D> addX1 = new List<MWPoint2D>();
            List<MWPoint2D> addX2 = new List<MWPoint2D>();
            List<MWPoint2D> addY1 = new List<MWPoint2D>();
            List<MWPoint2D> addY2 = new List<MWPoint2D>();
            for (int i = 4; i <= NrotX; i++)
            {
                if (lx1 >= lx2)
                {
                    addX1 = new List<MWPoint2D>();
                    nX1++;
                    double dx = (H1 - h1) / (nX1 + 1);
                    for (int j = 1; j <= nX1; j++)
                    {
                        addX1.Add(new MWPoint2D(-H1 / 2 + h1 + j * dx - d, -H2 / 2 + d));
                        addX1.Add(new MWPoint2D(-H1 / 2 + h1 + j * dx - d, -H2 / 2 + h2 - d));
                    }
                    lx1 = (H1 - h1) / (nX1 + 1);
                }
                else
                {
                    addX2 = new List<MWPoint2D>();
                    nX2++;
                    double dx = (h1 - 2 * d) / (nX2 + 1);
                    for (int j = 1; j <= nX2; j++)
                    {
                        addX2.Add(new MWPoint2D(-H1 / 2 + d + j * dx, -H2 / 2 + d));
                        addX2.Add(new MWPoint2D(-H1 / 2 + d + j * dx, H2 / 2 - d));
                    }
                    lx2 = (h1 - 2 * d) / (nX2 + 1);
                }
            }
            for (int i = 4; i <= NrotY; i++)
            {
                if (ly1 >= ly2)
                {
                    addY1 = new List<MWPoint2D>();
                    nY1++;
                    double dy = (H2 - h2) / (nY1 + 1);
                    for (int j = 1; j <= nY1; j++)
                    {
                        addY1.Add(new MWPoint2D(-H1 / 2 + d, -H2 / 2 + h2 + j * dy - d));
                        addY1.Add(new MWPoint2D(-H1 / 2 + h1 - d, -H2 / 2 + h2 + j * dy - d));
                    }
                    ly1 = (H2 - h2) / (nY1 + 1);
                }
                else
                {
                    addY2 = new List<MWPoint2D>();
                    nY2++;
                    double dy = (h2 - 2 * d) / (nY2 + 1);
                    for (int j = 1; j <= nY2; j++)
                    {
                        addY2.Add(new MWPoint2D(-H1 / 2 + d, -H2 / 2 + d + j * dy));
                        addY2.Add(new MWPoint2D(H1 / 2 - d, -H2 / 2 + d + j * dy));
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

            rebars = rebars.Select(p => new MWPoint2D(Math.Cos(angle) * p.X - Math.Sin(angle) * p.Y,
                                                  Math.Sin(angle) * p.X + Math.Cos(angle) * p.Y)).ToList();

            return rebars;
        }

        //public List<MWPoint2D> GetTShapedContour()
        //{
        //    List<MWPoint2D> ContourPts = new List<MWPoint2D>()
        //    {
        //        new MWPoint2D(-hX/2, -HY/2),
        //        new MWPoint2D(hX/2, -HY/2),
        //        new MWPoint2D(hX/2, HY/2-hY),
        //        new MWPoint2D(HX/2, HY/2-hY),
        //        new MWPoint2D(HX/2, HY/2),
        //        new MWPoint2D(-HX/2,HY/2),
        //        new MWPoint2D(-HX/2,HY/2-hY),
        //        new MWPoint2D(-hX/2,HY/2-hY)
        //    };

        //    return ContourPts;
        //}

        public List<MWPoint2D> GetTShapedRebars()
        {
            if (IsAdvancedRebar) return AdvancedRebarsPos;
            double d = CoverToLinks + LinkDiameter + BarDiameter / 2;
            List<MWPoint2D> rebars = new List<MWPoint2D>()
            {

                new MWPoint2D(hX/2 - d, HY/2 - hY + d),
                new MWPoint2D(hX/2 - d, HY/2 - d),
                new MWPoint2D(-hX/2 + d, HY/2 -d),
                new MWPoint2D(-hX/2 + d, HY/2 - hY + d)
            };
            if (HX - hX > 4 * BarDiameter)
            {
                rebars.Add(new MWPoint2D(HX / 2 - d, HY / 2 - hY + d));
                rebars.Add(new MWPoint2D(HX / 2 - d, HY / 2 - d));
                rebars.Add(new MWPoint2D(-HX / 2 + d, HY / 2 - d));
                rebars.Add(new MWPoint2D(-HX / 2 + d, HY / 2 - hY + d));
            }
            if (HY - hY > 2 * BarDiameter)
            {
                rebars.Add(new MWPoint2D(-hX / 2 + d, -HY / 2 + d));
                rebars.Add(new MWPoint2D(hX / 2 - d, -HY / 2 + d));
            }

            double ly1 = HY - hY;
            double ly2 = hY - 2 * d;
            double lx1 = (HX - hX) / 2;
            double lx2 = hX - 2 * d;
            int nX1 = 0;
            int nX2 = 0;
            int nY1 = 0;
            int nY2 = 0;
            List<MWPoint2D> addX1 = new List<MWPoint2D>();
            List<MWPoint2D> addX2 = new List<MWPoint2D>();
            List<MWPoint2D> addY1 = new List<MWPoint2D>();
            List<MWPoint2D> addY2 = new List<MWPoint2D>();
            for (int i = 3; i <= NRebarX; i++)
            {
                if (lx1 >= lx2)
                {
                    addX1 = new List<MWPoint2D>();
                    nX1++;
                    double dx = (HX - hX) / 2 / (nX1 + 1);
                    for (int j = 1; j <= nX1; j++)
                    {
                        addX1.Add(new MWPoint2D(-HX / 2 + d + j * dx, HY / 2 - d));
                        addX1.Add(new MWPoint2D(-HX / 2 + d + j * dx, HY / 2 - hY + d));
                        addX1.Add(new MWPoint2D(HX / 2 - d - j * dx, HY / 2 - d));
                        addX1.Add(new MWPoint2D(HX / 2 - d - j * dx, HY / 2 - hY + d));
                    }
                    lx1 = (HX - hX) / 2 / (nX1 + 1);
                }
                else
                {
                    addX2 = new List<MWPoint2D>();
                    nX2++;
                    double dx = (hX - 2 * d) / (nX2 + 1);
                    for (int j = 1; j <= nX2; j++)
                    {
                        addX2.Add(new MWPoint2D(-hX / 2 + d + j * dx, HY / 2 - d));
                        addX2.Add(new MWPoint2D(-hX / 2 + d + j * dx, -HY / 2 + d));
                    }
                    lx2 = (hX - 2 * d) / (nX2 + 1);
                }
            }
            for (int i = 4; i <= NRebarY; i++)
            {
                if (ly1 >= ly2)
                {
                    addY1 = new List<MWPoint2D>();
                    nY1++;
                    double dy = (HY - hY) / (nY1 + 1);
                    for (int j = 1; j <= nY1; j++)
                    {
                        addY1.Add(new MWPoint2D(-hX / 2 + d, -HY / 2 + d + j * dy));
                        addY1.Add(new MWPoint2D(hX / 2 - d, -HY / 2 + d + j * dy));
                    }
                    ly1 = (HY - hY) / (nY1 + 1);
                }
                else
                {
                    addY2 = new List<MWPoint2D>();
                    nY2++;
                    double dy = (hY - 2 * d) / (nY2 + 1);
                    for (int j = 1; j <= nY2; j++)
                    {
                        addY2.Add(new MWPoint2D(-HX / 2 + d, HY / 2 - hY + d + j * dy));
                        addY2.Add(new MWPoint2D(HX / 2 - d, HY / 2 - hY + d + j * dy));
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

            return rebars;
        }

        public List<MWPoint2D> GetCustomShapeContour()
        {
            List<MWPoint2D> ContourPts = new List<MWPoint2D>();

            if(d1x != 0 && d1y != 0)
            {
                ContourPts.Add(new MWPoint2D(-customLX / 2, -customLY / 2 + d1y));
                ContourPts.Add(new MWPoint2D(-customLX / 2 + d1x, -customLY / 2 + d1y));
                ContourPts.Add(new MWPoint2D(-customLX / 2 + d1x, -customLY / 2));
            }
            else
                ContourPts.Add(new MWPoint2D(-customLX / 2, -customLY / 2));

            if (d2x != 0 && d2y != 0)
            {
                ContourPts.Add(new MWPoint2D(customLX / 2 - d2x, -customLY / 2));
                ContourPts.Add(new MWPoint2D(customLX / 2 - d2x, -customLY / 2 + d2y));
                ContourPts.Add(new MWPoint2D(customLX / 2, -customLY / 2 + d2y));
            }
            else
                ContourPts.Add(new MWPoint2D(customLX / 2, -customLY / 2));

            if (d3x != 0 && d3y != 0)
            {
                ContourPts.Add(new MWPoint2D(customLX / 2, customLY / 2 - d3y));
                ContourPts.Add(new MWPoint2D(customLX / 2 - d3x, customLY / 2 - d3y));
                ContourPts.Add(new MWPoint2D(customLX / 2 - d3x, customLY / 2));
            }
            else
                ContourPts.Add(new MWPoint2D(customLX / 2, customLY / 2));

            if (d4x != 0 && d4y != 0)
            {
                ContourPts.Add(new MWPoint2D(-customLX / 2 + d4x, customLY / 2));
                ContourPts.Add(new MWPoint2D(-customLX / 2 + d4x, customLY / 2 - d4y));
                ContourPts.Add(new MWPoint2D(-customLX / 2, customLY / 2 - d4y));
            }
            else
                ContourPts.Add(new MWPoint2D(-customLX / 2, customLY / 2));

            return ContourPts;
        }
        public bool isInsideCapacity(bool GetMde = true)
        {
            return isInsideInteractionDiagram(this.diagramFaces, this.diagramVertices, GetMde : GetMde);
        }

        public bool isInsideInteractionDiagram(List<Tri3D> faces, List<MWPoint3D> vertices, bool firecheck = false, bool GetMde = true)
        {
            //bool all = (SelectedLoad.Name == "ALL LOADS" || allLoads) ? true : false;
            if(GetMde) GetDesignMoments();
            MWPoint3D p0 = MWPoint3D.point3DByCoordinates
            (
                2 * vertices.Min(x => x.X),
                0,
                0
            );

            List<MWPoint3D> points = new List<MWPoint3D>();

            if (firecheck)
            {
                if(AllLoads)
                {
                    for (int i = 1; i < Loads.Count; i++)
                        points.Add(new MWPoint3D(0.7 * Loads[i].MEdx, 0.7 * Loads[i].MEdy, -0.7 * Loads[i].P));
                }
                else
                    points.Add(new MWPoint3D(FireLoad.MEdx, FireLoad.MEdy, -FireLoad.P));
            }
            else if (AllLoads)
            {
                for (int i = 1; i < Loads.Count; i++)
                    points.Add(new MWPoint3D(Loads[i].MEdx, Loads[i].MEdy, -Loads[i].P));
            }
            else
                points.Add(new MWPoint3D(SelectedLoad.MEdx, SelectedLoad.MEdy, -SelectedLoad.P));

            for (int k = 0; k < points.Count; k++)
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

        public void GetTP()
        {
            if (Shape == GeoShape.Rectangular)
                TP = new TemperatureProfile(LX / 1e3, LY / 1e3, R * 60, FireCurve);
            else if (Shape == GeoShape.LShaped)
                TP = new TemperatureProfile(HX / 1e3, HY / 1e3, hX / 1e3, hY / 1e3, Theta, R * 60, FireCurve);
            else if (Shape == GeoShape.TShaped)
                TP = new TemperatureProfile(HX / 1e3, HY / 1e3, hX / 1e3, hY / 1e3, R * 60, FireCurve);
            else if (Shape == GeoShape.CustomShape)
                TP = new TemperatureProfile(customLX / 1e3, customLY / 1e3, d1x / 1e3, d1y / 1e3, d2x / 1e3, d2y / 1e3, d3x / 1e3, d3y / 1e3, d4x / 1e3, d4y / 1e3, R * 60, FireCurve);
        }

        //public bool CheckSpacing()
        //{
        //    List<double> sizes = new List<double>() { BarDiameter, MaxAggSize + 5, 20 };
        //    double smin = sizes.Max();
        //    if (Shape == GeoShape.Rectangular)
        //    {
        //        double sx = (LX - 2 * (CoverToLinks + LinkDiameter) - BarDiameter) / (NRebarX - 1);
        //        double sy = (LY - 2 * (CoverToLinks + LinkDiameter) - BarDiameter) / (NRebarY - 1);
        //        if (sx >= smin && sy >= smin)
        //            return true;
        //        else
        //            return false;
        //    }
        //    else if (Shape == GeoShape.Circular)
        //    {
        //        double x0 = Diameter / 2.0 - CoverToLinks - LinkDiameter - BarDiameter / 2.0;
        //        double x1 = x0 * Math.Cos(2 * Math.PI / NRebarCirc);
        //        double y1 = x0 * Math.Sin(2 * Math.PI / NRebarCirc);
        //        double s = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1, 2));
        //        if (s >= smin)
        //            return true;
        //        else
        //            return false;
        //    }
        //    else if (Shape == GeoShape.Polygonal)
        //    {
        //        double x0 = Diameter / 2 - CoverToLinks - LinkDiameter - BarDiameter / 2.0;
        //        double x1 = x0 * Math.Cos(2 * Math.PI / Edges);
        //        double y1 = x0 * Math.Sin(2 * Math.PI / Edges);
        //        double s = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1, 2));
        //        if (s >= smin)
        //            return true;
        //        else
        //            return false;
        //    }
        //    else if (Shape == GeoShape.LShaped)
        //    {
        //        if (sx1 < smin || sx2 < smin || sy1 < smin || sy2 < smin)
        //            return false;
        //        else
        //            return true;
        //    }
        //    return false;
        //}

        public MWPoint2D GetLShapeCOG() // center on (LX/2, LY/2)
        {
            double x = -(HX - hX) * (HY - hY) * hX / 2;
            x /= (HX * HY - (HX - hX) * (HY - hY));
            double y = -(HX - hX) * (HY - hY) * hY / 2;
            y /= (HX * HY - (HX - hX) * (HY - hY));
            return new MWPoint2D(x, y);
        }

        public MWPoint2D GetTShapeCOG()
        {
            double y = 0.5 * hY * (HX - hX) * (HY - hY) / (HX * hY + hX * (HY - hY));
            return new MWPoint2D(0, y);
        }

        public MWPoint2D GetCustomShapeCOG()
        {
            double y = customLX * customLY * customLY / 2 - d1x * d1y * d1y / 2 - d2x * d2y * d2y / 2 - d3x * d3y * (customLY - d3y / 2) - d4x * d4y * (customLY - d4y / 2);
            y /= (customLX * customLY - d1x * d1y - d2x * d2y - d3x * d3y - d4x * d4y);
            double x = customLX * customLY * customLX / 2 - d1x * d1y * d1x / 2 - d2x * d2y * (customLX - d2x / 2) - d3x * d3y * (customLX - d3x / 2) - d4x * d4y * d4y / 2;
            x /= (customLX * customLY - d1x * d1y - d2x * d2y - d3x * d3y - d4x * d4y);
            return new MWPoint2D(x - customLX/2, y - customLY/2);
        }

        public MWPoint2D GetCOG()
        {
            switch(Shape)
            {
                case (GeoShape.Rectangular):
                    return new MWPoint2D(0, 0);
                case (GeoShape.Circular):
                    return new MWPoint2D(0, 0);
                case (GeoShape.Polygonal):
                    return new MWPoint2D(0, 0);
                case (GeoShape.LShaped):
                    return GetLShapeCOG();
                case (GeoShape.TShaped):
                    return GetTShapeCOG();
                case (GeoShape.CustomShape):
                    return GetCustomShapeCOG();
            }
            return new MWPoint2D(0,0);
        }

        public void UpdateTP(bool newdesign = true)
        {
            if (newdesign || (!TP?.TempMap.Keys.Contains(R) ?? true) )
                GetTP();

            TP.GetContours(R, Shape.ToString(), this);
        }

        public double getTemp(MWPoint2D pt)
        {
            for (int j = TP.ContourPts.Count - 1; j > 0; j--)
            {
                bool inner = Polygons.isInside(TP.ContourPts[j - 1].Points, pt);
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

        public bool CheckIsInsideFireID(bool getMde = true)
        {
            if (fireDiagramFaces.Count > 0)
                return isInsideInteractionDiagram(fireDiagramFaces, fireDiagramVertices, firecheck: true, GetMde : getMde);
            else
                return false;
        }

        public double GetRebarTemp(MWPoint2D pt)
        {
            if (Polygons.isInside(TP.ContourPts[0].Points, pt)) return 100;
            double level = 200;
            for (int i = 1; i < TP.ContourPts.Count; i++)
            {
                if (Polygons.isInside(TP.ContourPts[i].Points.Concat(TP.ContourPts[i - 1].Points.Reverse<MWPoint2D>()).ToList(), pt))
                    return level;
                else
                    level += 100;
            }
            return level;
        }

        public bool CheckSteelQtty(bool allLoads = false)
        {
            double Ac = LX * LY;
            double Asmax = 0.04 * Ac;
            double As = Nrebars * Math.PI * Math.Pow(BarDiameter / 2.0, 2);
            if (allLoads)
            {
                foreach(var l in Loads)
                {
                    double Asmin = Math.Max(0.1 * l.P / (SteelGrade.Fy * 1e-3 / gs), 0.002 * Ac);

                    if (As > Asmax || As < Asmin)
                        return false;
                }
            }
            else
            {
                double Asmin = Math.Max(0.1 * SelectedLoad.P / (SteelGrade.Fy * 1e-3 / gs), 0.002 * Ac);

                if ( As > Asmax || As < Asmin)
                    return false;
            }
            
            return true;
        }

        public void GetDesignMoments()
        {
            List<Load> loadsToCheck;
            loadsToCheck = (AllLoads) ? Loads : new List<Load>() { SelectedLoad };

            for (int i = 0; i < loadsToCheck.Count; i++)
            {
                List<double> sizes = new List<double>() { this.BarDiameter, this.MaxAggSize + 5, 20 };

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

                double[] axialDist = GetAxialLength();
                double peri = GetPerimeter();
                if (!secondorderx)
                {
                    double Medx = Math.Max(M02x, loadsToCheck[i].P * Math.Max(axialDist[1] * 1E-3 / 30, 20 * 1E-3));
                    loadsToCheck[i].MEdx = Math.Round(Medx, 1);
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
                    loadsToCheck[i].MEdx = Math.Round(Medx, 1);

                }

                if (!secondordery)
                {
                    double Medy = Math.Max(M02y, loadsToCheck[i].P * Math.Max(axialDist[0] * 1E-3 / 30, 20 * 1E-3));
                    loadsToCheck[i].MEdy = Math.Round(Medy, 1);
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
                    loadsToCheck[i].MEdy = Math.Round(Medy, 1);

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
                        if (i == 0 || i == NRebarX - 1 || j == 0 || j == NRebarY - 1)
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
            else if (Shape == GeoShape.Polygonal || Shape == GeoShape.LShaped || Shape == GeoShape.TShaped || Shape == GeoShape.CustomShape)
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
            else if (Shape == GeoShape.CustomShape)
                return customLX * customLY - d1x * d1y - d2x * d2y - d3x * d3y - d4x * d4y
;            return 0;
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
            else if (Shape == GeoShape.CustomShape)
                n = AdvancedRebarsPos.Count;
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
            else if (Shape == GeoShape.CustomShape)
                return new double[] { customLX, customLY };
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
            else if (Shape == GeoShape.CustomShape)
                return 2 * (customLX + customLY);
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

        public void GetUtilisation()
        {
            double bigN = 1E6;
            MWPoint3D p0 = new MWPoint3D(SelectedLoad.MEdx, SelectedLoad.MEdy, -SelectedLoad.P);

            MWPoint3D pMX_pos = new MWPoint3D(SelectedLoad.MEdx + 10e5, SelectedLoad.MEdy, -SelectedLoad.P);
            MWPoint3D pMX_neg = new MWPoint3D(SelectedLoad.MEdx - 10e5, SelectedLoad.MEdy, -SelectedLoad.P);
            MWPoint3D pMY_pos = new MWPoint3D(SelectedLoad.MEdx, SelectedLoad.MEdy + 10e5, -SelectedLoad.P);
            MWPoint3D pMY_neg = new MWPoint3D(SelectedLoad.MEdx, SelectedLoad.MEdy - 10e5, -SelectedLoad.P);
            MWPoint3D pP_pos = new MWPoint3D(SelectedLoad.MEdx, SelectedLoad.MEdy, -SelectedLoad.P - 10e5);
            MWPoint3D pP_neg = new MWPoint3D(SelectedLoad.MEdx, SelectedLoad.MEdy, -SelectedLoad.P + 10e5);
            MWPoint3D p3D_pos = new MWPoint3D(1e5 * SelectedLoad.MEdx, 1e5 * SelectedLoad.MEdy, - 1e5 * SelectedLoad.P);
            MWPoint3D p3D_neg = new MWPoint3D(-1e5 * SelectedLoad.MEdx, -1e5 * SelectedLoad.MEdy, 1e5 * SelectedLoad.P);

            MWPoint3D[] points = new MWPoint3D[] { pMX_pos, pMX_neg, pMY_pos, pMY_neg, pP_pos, pP_neg, p3D_pos, p3D_neg };
            double[] Util = new double[] { bigN, bigN, bigN, bigN, bigN, bigN, bigN, bigN };
            double[] interDist = new double[] { bigN, bigN, bigN, bigN, bigN, bigN, bigN, bigN };

            for (int i = 0; i < this.diagramFaces.Count; i++)
            {
                for (int j = 0; j < points.Length; j++)
                {
                    MWPoint3D pInter0 = Polygon3D.PlaneLineIntersection(new MWPoint3D[] { p0, points[j] }, this.diagramFaces[i].Points);
                    if (pInter0.X != double.NaN)
                    {
                        MWPoint3D pInter = pInter0;
                        MWVector3D v = Vectors3D.VectorialProduct(new MWVector3D(p0.X - pInter.X, p0.Y - pInter.Y, p0.Z - pInter.Z),
                                                                  new MWVector3D(points[j].X - pInter.X, points[j].Y - pInter.Y, points[j].Z - pInter.Z));

                        List<MWPoint3D> pts = this.diagramFaces[i].Points;
                        double a1 = Vectors3D.TriangleArea(new MWVector3D(pts[0].X - pInter.X, pts[0].Y - pInter.Y, pts[0].Z - pInter.Z),
                                                           new MWVector3D(pts[1].X - pInter.X, pts[1].Y - pInter.Y, pts[1].Z - pInter.Z));
                        double a2 = Vectors3D.TriangleArea(new MWVector3D(pts[1].X - pInter.X, pts[1].Y - pInter.Y, pts[1].Z - pInter.Z),
                                                           new MWVector3D(pts[2].X - pInter.X, pts[2].Y - pInter.Y, pts[2].Z - pInter.Z));
                        double a3 = Vectors3D.TriangleArea(new MWVector3D(pts[2].X - pInter.X, pts[2].Y - pInter.Y, pts[2].Z - pInter.Z),
                                                           new MWVector3D(pts[0].X - pInter.X, pts[0].Y - pInter.Y, pts[0].Z - pInter.Z));
                        double a0 = Vectors3D.TriangleArea(new MWVector3D(pts[1].X - pts[0].X, pts[1].Y - pts[0].Y, pts[1].Z - pts[0].Z),
                                                           new MWVector3D(pts[2].X - pts[0].X, pts[2].Y - pts[0].Y, pts[2].Z - pts[0].Z));
                        if (Math.Abs(a1 + a2 + a3 - a0) < 1E-3)
                        {
                            double inter = Points.Distance3D(pInter, p0);
                            if (inter < interDist[j])
                            {
                                interDist[j] = inter;
                                if (j == 0 || j == 1)
                                    Util[j] = Math.Abs(p0.X / pInter.X) * 100;
                                else if (j == 2 || j == 3)
                                    Util[j] = Math.Abs(p0.Y / pInter.Y) * 100;
                                else if (j == 4 || j == 5)
                                    Util[j] = Math.Abs(p0.Z / pInter.Z) * 100;
                                else if(j == 6 || j == 7)
                                {
                                    double dist0 = Points.Distance3D(new MWPoint3D(0, 0, 0), p0);
                                    double dist1 = Points.Distance3D(new MWPoint3D(0, 0, 0), pInter);
                                    Util[j] = Math.Abs(dist0 / dist1);
                                }

                            }
                        }
                    }
                }
            }

            UtilMx = (Util[0] != bigN || Util[1] != bigN) ? Math.Round(Math.Min(Util[0], Util[1]), 1) : -1;
            UtilMy = (Util[2] != bigN || Util[3] != bigN) ? Math.Round(Math.Min(Util[2], Util[3]), 1) : -1;
            UtilP = (Util[4] != bigN || Util[5] != bigN) ? Math.Round(Math.Min(Util[4], Util[5]), 1) : -1;
            Util3D = (Util[6] != bigN || Util[7] != bigN) ? Math.Round(Math.Min(Util[6], Util[7]), 1) : -1;
        }

        public void Get2DMaps()
        {
            GetMxMyMap();
            GetMxNMap();
            GetMyNMap();

            if(FireDesignMethod == FDesignMethod.Advanced)
            {
                GetFireMxMyMap();
                GetFireMxNMap();
                GetFireMyNMap();
            }
        }
        public void GetMxMyMap()
        {
            double N = -SelectedLoad.P;
            List<MWPoint2D> mapPoints = new List<MWPoint2D>();
            
            for (int i = 0; i < this.diagramFaces.Count; i++)
            {
                Tri3D face = diagramFaces[i];
                for (int j = 0; j < face.Points.Count; j++)
                {
                    int k = j == face.Points.Count - 1 ? 0 : j + 1;
                    if ((face.Points[j].Z - N) * (face.Points[k].Z - N) < 0)
                    {
                        MWPoint3D p1 = face.Points[j];
                        MWPoint3D p2 = face.Points[k];
                        MWVector3D v = new MWVector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
                        v = v.Normalised();
                        if (v.Z == 0) continue;

                        double d = (N - p1.Z) / v.Z;
                        mapPoints.Add(new MWPoint2D(p1.X + d * v.X, p1.Y + d * v.Y));
                    }
                }
            }
            if (mapPoints.Count > 0)
            {
                mapPoints = ClearDuplicatePoints(mapPoints);
                double xmax = mapPoints.Max(p => Math.Abs(p.X));
                double ymax = mapPoints.Max(p => Math.Abs(p.Y));
                mapPoints = mapPoints.Select(p => new MWPoint2D(p.X / xmax, p.Y / ymax)).ToList();
                mapPoints = TemperatureProfile.OrderInDistance(mapPoints);
                mapPoints.Add(mapPoints[0]);
                mapPoints = mapPoints.Select(p => new MWPoint2D(p.X * xmax, p.Y * ymax)).ToList();
            }
            MxMyPts = mapPoints;
        }

        public void GetMxNMap()
        {
            double My = SelectedLoad.MEdy;
            List<MWPoint2D> mapPoints = new List<MWPoint2D>();

            for (int i = 0; i < this.diagramFaces.Count; i++)
            {
                Tri3D face = diagramFaces[i];
                for (int j = 0; j < face.Points.Count; j++)
                {
                    int k = j == face.Points.Count - 1 ? 0 : j + 1;
                    if ((face.Points[j].Y - My) * (face.Points[k].Y - My) < 0)
                    {
                        MWPoint3D p1 = face.Points[j];
                        MWPoint3D p2 = face.Points[k];
                        MWVector3D v = new MWVector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
                        v = v.Normalised();
                        if (v.Y == 0) continue;

                        double d = (My - p1.Y) / v.Y;
                        mapPoints.Add(new MWPoint2D(p1.X + d * v.X, p1.Z + d * v.Z));
                    }
                }
            }
            if (mapPoints.Count > 0)
            {
                mapPoints = ClearDuplicatePoints(mapPoints);
                double xmax = mapPoints.Max(p => Math.Abs(p.X));
                double ymax = mapPoints.Max(p => Math.Abs(p.Y));
                mapPoints = mapPoints.Select(p => new MWPoint2D(p.X / xmax, p.Y / ymax)).ToList();
                mapPoints = TemperatureProfile.OrderInDistance(mapPoints);
                mapPoints.Add(mapPoints[0]);
                mapPoints = mapPoints.Select(p => new MWPoint2D(p.X * xmax, p.Y * ymax)).ToList();
            }
            MxNPts = mapPoints;
        }

        public void GetMyNMap()
        {
            double Mx = SelectedLoad.MEdx;
            List<MWPoint2D> mapPoints = new List<MWPoint2D>();

            for (int i = 0; i < this.diagramFaces.Count; i++)
            {
                Tri3D face = diagramFaces[i];
                for (int j = 0; j < face.Points.Count; j++)
                {
                    int k = j == face.Points.Count - 1 ? 0 : j + 1;
                    if ((face.Points[j].X - Mx) * (face.Points[k].X - Mx) < 0)
                    {
                        MWPoint3D p1 = face.Points[j];
                        MWPoint3D p2 = face.Points[k];
                        MWVector3D v = new MWVector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
                        v = v.Normalised();
                        if (v.X == 0) continue;

                        double d = (Mx - p1.X) / v.X;
                        mapPoints.Add(new MWPoint2D(p1.Y + d * v.Y, p1.Z + d * v.Z));
                    }
                }
            }
            if (mapPoints.Count > 0)
            {
                mapPoints = ClearDuplicatePoints(mapPoints);
                double xmax = mapPoints.Max(p => Math.Abs(p.X));
                double ymax = mapPoints.Max(p => Math.Abs(p.Y));
                mapPoints = mapPoints.Select(p => new MWPoint2D(p.X / xmax, p.Y / ymax)).ToList();
                mapPoints = TemperatureProfile.OrderInDistance(mapPoints);
                mapPoints.Add(mapPoints[0]);
                mapPoints = mapPoints.Select(p => new MWPoint2D(p.X * xmax, p.Y * ymax)).ToList();
            }
            MyNPts = mapPoints;
        }

        public void GetFireMxMyMap()
        {
            double N = -FireLoad.P;
            List<MWPoint2D> mapPoints = new List<MWPoint2D>();

            for (int i = 0; i < this.fireDiagramFaces.Count; i++)
            {
                Tri3D face = fireDiagramFaces[i];
                for (int j = 0; j < face.Points.Count; j++)
                {
                    int k = j == face.Points.Count - 1 ? 0 : j + 1;
                    if ((face.Points[j].Z - N) * (face.Points[k].Z - N) < 0)
                    {
                        MWPoint3D p1 = face.Points[j];
                        MWPoint3D p2 = face.Points[k];
                        MWVector3D v = new MWVector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
                        v = v.Normalised();
                        if (v.Z == 0) continue;

                        double d = (N - p1.Z) / v.Z;
                        mapPoints.Add(new MWPoint2D(p1.X + d * v.X, p1.Y + d * v.Y));
                    }
                }
            }
            if (mapPoints.Count > 0)
            {
                mapPoints = ClearDuplicatePoints(mapPoints);
                double xmax = mapPoints.Max(p => Math.Abs(p.X));
                double ymax = mapPoints.Max(p => Math.Abs(p.Y));
                mapPoints = mapPoints.Select(p => new MWPoint2D(p.X / xmax, p.Y / ymax)).ToList();
                mapPoints = TemperatureProfile.OrderInDistance(mapPoints);
                mapPoints.Add(mapPoints[0]);
                mapPoints = mapPoints.Select(p => new MWPoint2D(p.X * xmax, p.Y * ymax)).ToList();
            }
            fireMxMyPts = mapPoints;
        }

        public void GetFireMxNMap()
        {
            double My = FireLoad.MEdy;
            List<MWPoint2D> mapPoints = new List<MWPoint2D>();

            for (int i = 0; i < this.fireDiagramFaces.Count; i++)
            {
                Tri3D face = fireDiagramFaces[i];
                for (int j = 0; j < face.Points.Count; j++)
                {
                    int k = j == face.Points.Count - 1 ? 0 : j + 1;
                    if ((face.Points[j].Y - My) * (face.Points[k].Y - My) < 0)
                    {
                        MWPoint3D p1 = face.Points[j];
                        MWPoint3D p2 = face.Points[k];
                        MWVector3D v = new MWVector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
                        v = v.Normalised();
                        if (v.Y == 0) continue;

                        double d = (My - p1.Y) / v.Y;
                        mapPoints.Add(new MWPoint2D(p1.X + d * v.X, p1.Z + d * v.Z));
                    }
                }
            }
            if (mapPoints.Count > 0)
            {
                mapPoints = ClearDuplicatePoints(mapPoints);
                double xmax = mapPoints.Max(p => Math.Abs(p.X));
                double ymax = mapPoints.Max(p => Math.Abs(p.Y));
                mapPoints = mapPoints.Select(p => new MWPoint2D(p.X / xmax, p.Y / ymax)).ToList();
                mapPoints = TemperatureProfile.OrderInDistance(mapPoints);
                mapPoints.Add(mapPoints[0]);
                mapPoints = mapPoints.Select(p => new MWPoint2D(p.X * xmax, p.Y * ymax)).ToList();
            }
            fireMxNPts = mapPoints;
        }

        public void GetFireMyNMap()
        {
            double Mx = FireLoad.MEdx;
            List<MWPoint2D> mapPoints = new List<MWPoint2D>();

            for (int i = 0; i < this.fireDiagramFaces.Count; i++)
            {
                Tri3D face = fireDiagramFaces[i];
                for (int j = 0; j < face.Points.Count; j++)
                {
                    int k = j == face.Points.Count - 1 ? 0 : j + 1;
                    if ((face.Points[j].X - Mx) * (face.Points[k].X - Mx) < 0)
                    {
                        MWPoint3D p1 = face.Points[j];
                        MWPoint3D p2 = face.Points[k];
                        MWVector3D v = new MWVector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
                        v = v.Normalised();
                        if (v.X == 0) continue;

                        double d = (Mx - p1.X) / v.X;
                        mapPoints.Add(new MWPoint2D(p1.Y + d * v.Y, p1.Z + d * v.Z));
                    }
                }
            }
            if (mapPoints.Count > 0)
            {
                mapPoints = ClearDuplicatePoints(mapPoints);
                double xmax = mapPoints.Max(p => Math.Abs(p.X));
                double ymax = mapPoints.Max(p => Math.Abs(p.Y));
                mapPoints = mapPoints.Select(p => new MWPoint2D(p.X / xmax, p.Y / ymax)).ToList();
                mapPoints = TemperatureProfile.OrderInDistance(mapPoints);
                mapPoints.Add(mapPoints[0]);
                mapPoints = mapPoints.Select(p => new MWPoint2D(p.X * xmax, p.Y * ymax)).ToList();
            }
            fireMyNPts = mapPoints;
        }

        public List<MWPoint2D> ClearDuplicatePoints(List<MWPoint2D> pts, double tol = 1e-5)
        {
            List<MWPoint2D> res = new List<MWPoint2D> { pts[0] };
            for(int i = 1; i < pts.Count; i++)
            {
                bool add = true;
                for(int j = 0; j < res.Count; j++)
                {
                    if (Points.Distance(pts[i], res[j]) < tol)
                    {
                        add = false;
                        break;
                    }
                }
                if (add) res.Add(pts[i]);
            }
            return res;
        }

        public bool CheckMinRebarNo()
        {
            if (R < 120)
            {
                if (Shape == GeoShape.Circular)
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
                else if (Shape == GeoShape.CustomShape)
                    return (Nrebars >= 8) ? true : false;
            }
            return false;
        }

        public void CheckGuidances()
        {
            // bar spacing
            StringBuilder message = new StringBuilder();
            if (Shape == GeoShape.Rectangular)
            {
                double d = 2 * (CoverToLinks + LinkDiameter) + BarDiameter;
                double sx = (LX - d) / (NRebarX - 1);
                double sy = (LY - d) / (NRebarY - 1);
                if (sx < 75 || sy < 75)
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

        public void SetChecksToFalse()
        {
            this.CapacityCheck = false;
            this.MinRebarCheck = false;
            this.MinMaxSteelCheck = false;
            this.FireCheck = false;
            this.SpacingCheck = false;
        }
        //private Assembly LoadIDdll()
        //{
        //    string assemblyPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Magma Works/Scaffold/Libraries/InteractionDiagram3D.dll";
        //    if (!File.Exists(assemblyPath)) return null;
        //    Assembly assembly = Assembly.LoadFrom(assemblyPath);
        //    return assembly;
        //}
    }
}
