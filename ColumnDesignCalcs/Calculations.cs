using CalcCore;
using InteractionDiagram3D;
using MWGeometry;
using netDxf;
using StructuralDrawing2D;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ColumnDesignCalc
{
    [CalcName("Advanced Column Design")]
    [CalcAlternativeName("ACE.ColumnDesign")]
    public class Calculations : CalcBase
    {
        // Column object
        public Column Column;

        // Geometry
        CalcDouble Lx;
        CalcDouble Ly;
        CalcDouble Length;
        CalcDouble Angle;

        // Material
        CalcSelectionList ConcreteGrade;
        CalcSelectionList SteelGrade;
        CalcDouble MaxAggSize;

        // Loads
        CalcDouble MxTop;
        CalcDouble MxBot;
        CalcDouble MyTop;
        CalcDouble MyBot;
        CalcDouble P;

        // Design
        CalcDouble EffectiveLength;
        CalcDouble CoverToLinks;
        CalcSelectionList BarDiameter;
        CalcSelectionList LinkDiameter;
        CalcSelectionList NRebarX;
        CalcSelectionList NRebarY;
        CalcSelectionList R; // fire resistance in min
        CalcSelectionList FireDesignMethod;

        //Checks
        CalcBool CapacityCheck;
        CalcBool FireCheck;
        CalcBool SpacingCheck;
        CalcBool MinMaxSteelCheck;

        // Outputs
        CalcDouble Nd;
        CalcDouble MEdx;
        CalcDouble MEdy;
        CalcDouble LinkSpacing;

        CalcDouble CapacityCheckO;
        //CalcDouble FireCheckO;
        //CalcDouble SpacingCheckO;
        //CalcDouble MinMaxSteelCheckO;

        CalcDouble ConcreteCarbon;
        CalcDouble RebarCarbon;
        CalcDouble TotalCarbon;
        
        public List<Formula> Expressions { get; set; }
        public List<Concrete> ConcreteGrades = new List<Concrete>()
        {
            new Concrete("Custom",50,37),
            //new Concrete("32/40",32,33),
            //new Concrete("35/45",35,34),
            //new Concrete("40/50",40,35),
            ////new Concrete("45/55",45,36),
            //new Concrete("50/60",50,37),
        };
        public List<Steel> SteelGrades = new List<Steel>()
        {
            new Steel("500B",500),
            new Steel("Custom",415)
        };
        public List<int> BarDiameters = new List<int>();
        public List<int> LinkDiameters = new List<int>();
        public List<int> NRebars = new List<int>();
        public List<int> FireResistances = new List<int>();

        // data sets
        List<FireData> fireTable = new List<FireData>();
        List<ConcreteData> concreteData = new List<ConcreteData>();
        List<SteelData> steelData = new List<SteelData>();
        // Constants for calculations
        const double gs = 1.15;
        const double gc = 1.5;
        const double acc = 0.85;
        const double concreteVolMass = 2.5e3;
        const double steelVolMass = 7.5e3;
        // Carbon and costs
        public Dictionary<double, double> CarbonData = new Dictionary<double, double>();
        public Dictionary<double, double[]> SteelCosts = new Dictionary<double, double[]>();
        public Dictionary<double, double[]> ConcreteCosts = new Dictionary<double, double[]>();

        public string DataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Magma Works/Scaffold/Data/";

        public Calculations()
        {
            ImportConcreteGrades();

            Console.WriteLine("Entering Calculations");
            // Geometry
            InstanceName = "My Column Design";
            Lx = inputValues.CreateDoubleCalcValue("Lx", "L_x", "mm", 900);
            Ly = inputValues.CreateDoubleCalcValue("Ly", "L_y", "mm", 300);
            Length = inputValues.CreateDoubleCalcValue("Length", "L", "mm", 3150);
            Angle = inputValues.CreateDoubleCalcValue("Angle", @"\alpha", @"\deg", 0);

            // Material
            ConcreteGrade = inputValues.CreateCalcSelectionList("Concrete grade", "40/50", ConcreteGrades.Select(c => c.Name).ToList());
            SteelGrade = inputValues.CreateCalcSelectionList("Steel grade", "500B", SteelGrades.Select(c => c.Name).ToList());
            MaxAggSize = inputValues.CreateDoubleCalcValue("Max agg. size", "Ag", "mm", 20);

            // Loads
            MxTop = inputValues.CreateDoubleCalcValue("MxTop", "M_x^{Top}", "kN/m", 0);
            MxBot = inputValues.CreateDoubleCalcValue("MxBot", "M_x^{Bot}", "kN/m", 0);
            MyTop = inputValues.CreateDoubleCalcValue("MyTop", "M_y^{Top}", "kN/m", 0);
            MyBot = inputValues.CreateDoubleCalcValue("MyBot", "M_y^{Bot}", "kN/m", 0);
            P = inputValues.CreateDoubleCalcValue("AxialLoad", "P", "kN", 500);

            //Design
            EffectiveLength = inputValues.CreateDoubleCalcValue("Effective length", "L_{eff}", "", 0.7);
            CoverToLinks = inputValues.CreateDoubleCalcValue("Cover to links", "c", "mm", 40);
            BarDiameter = inputValues.CreateCalcSelectionList("Bar diameter", "16", new List<string> { "10", "12", "16", "20", "25", "32", "40" });
            LinkDiameter = inputValues.CreateCalcSelectionList("Link diameter", "10", new List<string> { "10", "12", "16", "20", "25", "32", "40" });
            NRebarX = inputValues.CreateCalcSelectionList("NRebarX", "3", new List<string> { "2", "3", "4", "5", "6", "7", "8", "9", "10" });
            NRebarY = inputValues.CreateCalcSelectionList("NRebarY", "3", new List<string> { "2", "3", "4", "5", "6", "7", "8", "9", "10" });
            R = inputValues.CreateCalcSelectionList("R", "120", new List<string> { "60", "90", "120", "150", "180", "240" });
            FireDesignMethod = inputValues.CreateCalcSelectionList("Fire design method", "Table", new List<string> { "Table", "Isotherm 500", "Zone method", "Advanced" });

            // Outputs
            Nd = outputValues.CreateDoubleCalcValue("Nd", "N_d", "kN", 0);
            MEdx = outputValues.CreateDoubleCalcValue("MEdx", "M_{Ed,x}", "kN/m", 0);
            MEdy = outputValues.CreateDoubleCalcValue("MEdy", "M_{Ed,y}", "kN/m", 0);
            LinkSpacing = outputValues.CreateDoubleCalcValue("sLink", "s_{link}", "mm", 0);

            MinMaxSteelCheck = outputValues.CreateBoolCalcValue("Steel Check", "", "", false);
            FireCheck = outputValues.CreateBoolCalcValue("Fire Check", "", "", false);
            SpacingCheck = outputValues.CreateBoolCalcValue("Spacing Check", "", "", false);
            CapacityCheck = outputValues.CreateBoolCalcValue("Capacity Check", "", "", false);
            CapacityCheckO = outputValues.CreateDoubleCalcValue("Capacity Util", "", "", 0);

            ConcreteCarbon = outputValues.CreateDoubleCalcValue("Concrete carbon", "", @"kg\text{ } CO_2", 0);
            RebarCarbon = outputValues.CreateDoubleCalcValue("Rebar carbon", "", @"kg\text{ } CO_2", 0);
            TotalCarbon = outputValues.CreateDoubleCalcValue("Total carbon", "", @"kg\text{ } CO_2", 0);

            BarDiameters = new List<int> { 10, 12, 16, 20, 25, 32, 40 };
            LinkDiameters = new List<int> { 10, 12, 16, 20, 25, 32, 40 };
            NRebars = new List<int> { 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            FireResistances = new List<int> { 60, 90, 120, 150, 180, 240 };

            UpdateCalc();

        }

        public Calculations(Column c)
        {
            Column = c;
        }

        public void UpdateInputOuput()
        {
            // Geometry
            InstanceName = Column.Name;
            switch(Column.Shape)
            {
                case (GeoShape.Rectangular):
                    Lx.Value = Column.LX;
                    Ly.Value = Column.LY;
                    break;
                case (GeoShape.Circular):
                    Lx.Value = Column.Diameter;
                    Ly.Value = Column.Diameter;
                    break;
                case (GeoShape.TShaped):
                    Lx.Value = Column.HX;
                    Ly.Value = Column.HY;
                    break;
                case (GeoShape.LShaped):
                    Lx.Value = Column.HX;
                    Ly.Value = Column.HY;
                    break;
                case (GeoShape.CustomShape):
                    Lx.Value = Column.customLX;
                    Ly.Value = Column.customLY;
                    break;
            }
            Length.Value = Column.Length;
            Angle.Value = Column.Angle;
            NRebarX.ValueAsString = Column.NRebarX.ToString();
            NRebarY.ValueAsString = Column.NRebarY.ToString();
            BarDiameter.ValueAsString = Column.BarDiameter.ToString();

            // Material
            MaxAggSize.Value = Column.MaxAggSize;
            ConcreteGrade.ValueAsString = Column.ConcreteGrade.Name;
            SteelGrade.ValueAsString = Column.SteelGrade.Name;

            // Loads
            MxTop.Value = Column.SelectedLoad.MxTop;
            MxBot.Value = Column.SelectedLoad.MxBot;
            MyTop.Value = Column.SelectedLoad.MyTop;
            MyBot.Value = Column.SelectedLoad.MyBot;
            P.Value = Column.SelectedLoad.P;

            //Design
            EffectiveLength.Value = Column.EffectiveLength;
            CoverToLinks.Value = Column.CoverToLinks;
            FireDesignMethod.ValueAsString = Column.FireDesignMethod.ToString().Replace("_"," ");
            R.ValueAsString = Column.R.ToString();

            // Outputs
            Nd.Value = Column.SelectedLoad.P;
            MEdx.Value = Column.SelectedLoad.MEdx;
            MEdy.Value = Column.SelectedLoad.MEdy;
            LinkSpacing.Value = Column.LinkSpacing;

            //MinMaxSteelCheckO.Value = Column.MinMaxSteelCheck ?? false ? 1 : 0;
            //FireCheckO.Value = Column.FireCheck ?? false ? 1 : 0;
            //SpacingCheckO.Value = SpacingCheck.Value ? 1 : 0;
            CapacityCheckO.Value = Column.Util3D;

            var carb = GetEmbodiedCarbon();
            ConcreteCarbon.Value = carb[0];
            RebarCarbon.Value = carb[1];
            TotalCarbon.Value = carb[2];

        }

        public void InitExpressions()
        {
            formulae = null;
            Expressions = new List<Formula>();
        }

        // This method should only be used in the Scaffold framework as it imports the CalcDouble values
        public override void UpdateCalc()
        {
            InitExpressions();
            Console.WriteLine("Expressions initialized");
            Console.WriteLine(ConcreteGrades.Count);
            // Import Scaffold data
            Column = new Column()
            {
                LX = Lx.Value,
                LY = Ly.Value,
                Length = Length.Value,
                Angle = Angle.Value,
                ConcreteGrade = ConcreteGrades.First(c => c.Name == ConcreteGrade.ValueAsString),
                SteelGrade = SteelGrades.First(c => c.Name == SteelGrade.ValueAsString),
                MaxAggSize = MaxAggSize.Value,
                EffectiveLength = EffectiveLength.Value,
                CoverToLinks = CoverToLinks.Value,
                BarDiameter = BarDiameters.First(b => b == Convert.ToInt32(BarDiameter.ValueAsString)),
                LinkDiameter = BarDiameters.First(b => b == Convert.ToInt32(LinkDiameter.ValueAsString)),
                NRebarX = NRebars.First(n => n == Convert.ToInt32(NRebarX.ValueAsString)),
                NRebarY = NRebars.First(n => n == Convert.ToInt32(NRebarY.ValueAsString)),
                R = FireResistances.First(f => f == Convert.ToInt32(R.ValueAsString)),
                FDMStr = FireDesignMethod.ValueAsString.Replace(" ", "_"),
                SelectedLoad = new Load()
                {
                    MxTop = MxTop.Value,
                    MxBot = MxBot.Value,
                    MyTop = MyTop.Value,
                    MyBot = MyBot.Value,
                    P = P.Value,
                },
                FireLoad = new Load()
                {
                    MxTop = 0.7 * MxTop.Value,
                    MxBot = 0.7 * MxBot.Value,
                    MyTop = 0.7 * MyTop.Value,
                    MyBot = 0.7 * MyBot.Value,
                    P = 0.7 * P.Value,
                },
            };

            if (Lx.Value > 0 && Ly.Value > 0)
            {
                Nd.Value = Column.SelectedLoad.P;
                ConcreteCarbon.Value = GetEmbodiedCarbon()[0];
                RebarCarbon.Value = GetEmbodiedCarbon()[1];
                TotalCarbon.Value = GetEmbodiedCarbon()[2];

                Column.GetInteractionDiagram();
                MinMaxSteelCheck.Value = CheckMinMaxSteel();
                FireCheck.Value = UpdateFireDesign();
                SpacingCheck.Value = UpdateSecondOrderCheck();
                CapacityCheck.Value = Column.isInsideCapacity();
                GetLinkSpacing();
                AddInteractionDiagramFormulae();

                MinMaxSteelCheck.Status = MinMaxSteelCheck.Value ? CalcStatus.PASS : CalcStatus.FAIL;
                FireCheck.Status = FireCheck.Value ? CalcStatus.PASS : CalcStatus.FAIL;
                SpacingCheck.Status = SpacingCheck.Value ? CalcStatus.PASS : CalcStatus.FAIL;
                CapacityCheck.Status = CapacityCheck.Value ? CalcStatus.PASS : CalcStatus.FAIL;

                Column.GetUtilisation();
                CapacityCheckO.Value = Column.Util3D;

            }
        }

        public void GetLinkSpacing()
        {
            List<double> spacings = new List<double>
            {
                20 * Column.BarDiameter,
                400,
                Column.LX,
                Column.LY
            };
            Column.LinkSpacing = spacings.Min();

            Formula f = new Formula();
            f.Narrative = "Link spacing";
            f.AddExpression(@"s_{link} = min(20\times \phi_{rebar}, L_x, L_y, 400) = "+Math.Round(Column.LinkSpacing,0)+" mm");
            f.Narrative = @"Link spacing
            Please note : The maximum spacing required should be reduced by a factor 0.6:
            (i) in sections within a distance equal to the larger dimensions of the column cross-section
            above or below a beam or slab;
            (ii) near lapped joints, if the maximum diameter of the longitudinal bars is greater than 14
            mm. A minimum of 3 bars evenly placed in the lap length is required.";
            Expressions.Add(f);
        }

        public void AddInteractionDiagramFormulae()
        {
            Formula f = new Formula();

            f.Narrative = "3D interaction diagram definition";
            f.AddExpression(@"S_i = \text{ region occupied by material } i");
            f.AddExpression(@"\sigma_t^i = \text{ tension strength of material } i");
            f.AddExpression(@"\sigma_c^i = \text{ compression strength of material } i");
            f.AddExpression(@"u_z(x,y) = \delta - y\chi_x - x\chi_y \text{  }\forall (x,y) \in \cup_i S_i");
            f.AddExpression(@"S_{i}^{+} = S_i \cap \left\{ \delta - y\chi_x - x\chi_y > 0 \right\}");
            f.AddExpression(@"S_{i}^{-} = S_i \cap \left\{ \delta - y\chi_x - x\chi_y < 0 \right\}");
            f.AddExpression(@"G_u = \text{ interaction diagram of } (N,M_x,M_y)");
            f.AddExpression(@"(N,M_x,M_y) \in G_u \Leftrightarrow \exists (\delta, \chi_x, \chi_y) \text{ such that }");
            f.AddExpression(@"N = \sum_i \left(\int_{S_i^+} \sigma_t^i dS - \int_{S_i^-}\sigma_c^i dS\right)");
            f.AddExpression(@"M_x = \sum_i \left(\int_{S_i^-} y\sigma_c^i dS - \int_{S_i^+} y\sigma_t^i dS\right)");
            f.AddExpression(@"M_y = \sum_i \left(\int_{S_i^-} x\sigma_c^i dS - \int_{S_i^+} x\sigma_t^i dS\right)");

            Expressions.Add(f);

        }

        public void AddInteractionDiagrams()
        {
            var plots = Get2DIDPictures().ToList();
            int i = Expressions.IndexOf(Expressions.First(f => f.Narrative == "3D interaction diagram definition"));
            foreach (var f in plots)
            {
                Expressions.Insert(i, f);
                i++;
            }
        }

        public IEnumerable<Formula> Get2DIDPictures()
        {
            string path = Path.GetTempPath();
            string[] filenames = new string[] { "MxMy.tmp", "MxN.tmp", "MyN.tmp" };

            foreach(var name in filenames)
            {
                var filepath = path + name;
                if (File.Exists(filepath))
                {
                    Formula f = new Formula();

                    byte[] arr = File.ReadAllBytes(filepath);
                    SkiaSharp.SKBitmap pic = SkiaSharp.SKBitmap.Decode(arr);
                    //SkiaSharp.SKImageInfo info = new SkiaSharp.SKImageInfo(3000, 3000);
                    //pic = pic.Resize(info, SkiaSharp.SKFilterQuality.High);
                    Console.WriteLine("width = {0}", pic.Width);

                    f.AddImage(pic);
                    File.Delete(filepath);

                    //var keyImage = new SkiaSharp.SKBitmap(3000, 3000);
                    //using (SkiaSharp.SKCanvas canvas = new SkiaSharp.SKCanvas(keyImage))
                    //{
                    //    var paintText = new SkiaSharp.SKPaint { TextSize = 250, TextAlign = SkiaSharp.SKTextAlign.Left };
                    //    var paint = new SkiaSharp.SKPaint { StrokeWidth = 15, Color = SkiaSharp.SKColors.Red };
                    //    canvas.DrawLine(0, 0, 3000, 3000, paint);
                    //    canvas.DrawText("TEST TEXT", 250, 35, paintText);

                    //}
                    //f.AddImage(keyImage);
                    yield return f;
                }
            }

        }

        public bool UpdateSecondOrderCheck()
        {
            Column c = Column;

            Load load;

            if (c.AllLoads) c.GetDesignMoments();

            load = (c.AllLoads) ? c.Loads.Aggregate(c.Loads[0], ((current, next) =>
                      ((Math.Abs(next.MEdx) > Math.Abs(current.MEdx) && Math.Abs(next.MEdx) > Math.Abs(current.MEdy))
                       || (Math.Abs(next.MEdy) > Math.Abs(current.MEdx) && Math.Abs(next.MEdy) > Math.Abs(current.MEdy))
                      ) ? next : current)) : c.SelectedLoad;

            Formula f0 = new Formula();

            bool spacing = CheckSpacing(f0);

            Expressions.Add(f0);

            double abar = Math.PI * Math.Pow(c.BarDiameter / 2.0, 2);
            double As = (c.NRebarX * c.NRebarY - (c.NRebarX - 2) * (c.NRebarY - 2)) * Math.PI * Math.Pow(c.BarDiameter / 2.0, 2);
            double[] dxs = new double[c.NRebarY];
            dxs[0] = c.LY - c.CoverToLinks - c.LinkDiameter - c.BarDiameter / 2.0;

            Formula f0x = new Formula();
            f0x.Narrative = "Effective depth for bending about x axis";
            f0x.Expression = new List<string>();

            Formula f0y = new Formula();
            f0y.Narrative = "Effective depth for bending about y axis";
            f0y.Expression = new List<string>();

            double[] effDepths = GetEffectiveDepths(f0x, f0y);
            double dx = effDepths[0];
            double dy = effDepths[1];

            Expressions.Add(f0x);
            Expressions.Add(f0y);

            double l0 = c.EffectiveLength * c.Length;
            Formula f1 = new Formula();
            f1.Narrative = "Column effective length";
            f1.Expression = new List<string>();
            f1.Expression.Add(@"l_{0} = f \times l = " + l0 + " mm");
            Expressions.Add(f1);

            Formula f2 = new Formula();
            f2.Narrative = "Column slenderness about x axis";
            f2.Ref = "5.8.3.2";
            f2.Expression = new List<string>();
            Formula f3 = new Formula();
            f3.Narrative = "Column slenderness about y axis";
            f3.Ref = "5.8.3.2";
            f3.Expression = new List<string>();
            double[] Inertias = GetSecondMomentInertia(f2, f3);
            double Ix = Inertias[0];
            double Iy = Inertias[1];
            double A = c.GetColumnArea();
            double Ac = c.GetConcreteArea();

            double ix = Math.Sqrt(Ix / A);
            double iy = Math.Sqrt(Iy / A);

            f2.Expression.Add(@"i_x = \sqrt{I_x/A} = " + Math.Round(ix) + " mm");
            f3.Expression.Add(@"i_y = \sqrt{I_y/A} = " + Math.Round(iy) + " mm");

            double lambdax = l0 / ix;
            f2.Expression.Add(@"\lambda_x = l_0/i_x = " + Math.Round(lambdax, 1));
            Expressions.Add(f2);

            double lambday = l0 / iy;
            f3.Expression.Add(@"\lambda_y = l_0/i_y = " + Math.Round(lambday, 1));
            Expressions.Add(f3);

            double ei = l0 / 400;
            int kx = load.MxTop * load.MxBot >= 0 ? 1 : -1;
            double M01x = kx * Math.Min(Math.Abs(load.MxTop), Math.Abs(load.MxBot)) + ei * load.P / 1E3;
            double M02x = Math.Max(Math.Abs(load.MxTop), Math.Abs(load.MxBot)) + ei * load.P / 1E3;
            Formula f4 = new Formula();
            f4.Narrative = "Moments about x axis including imperfections";
            f4.Expression = new List<string>();
            f4.Expression.Add(@"e_{i} = l_0/400 = " + Math.Round(ei, 1) + " mm");
            f4.Expression.Add(@"k = M_x^{top}/\left|M_x^{top}\right|*M_x^{bot}/\left|M_x^{bot}\right| = " + kx);
            f4.Expression.Add(@"M_{01x} = k \times min\left(\left|M_x^{top}\right|,\left|M_x^{bot}\right|\right) + e_iN_{Ed} = " + Math.Round(M01x, 1) + " kN.m");
            f4.Expression.Add(@"M_{02x} = max\left(\left|M_x^{top}\right|,\left|M_x^{bot}\right|\right) + e_iN_{Ed} = " + Math.Round(M02x, 1) + " kN.m");
            Expressions.Add(f4);

            double omega = As * c.SteelGrade.Fy / gs /
                (Ac * acc * c.ConcreteGrade.Fc / gc);
            double B = Math.Sqrt(1 + 2 * omega);
            double rmx = M01x / M02x;
            double C = 1.7 - rmx;
            double n = load.P / (Ac * acc * c.ConcreteGrade.Fc / gc) * 1E3;
            double lambdaxlim = 20 * 0.7 * B * C / Math.Sqrt(n);
            bool secondorderx = false;
            Formula f5 = new Formula();
            f5.Narrative = "Slenderness limit about x axis";
            f5.Expression = new List<string>();
            f5.Expression.Add(@"A = 0.7");
            f5.Expression.Add(@"\omega = A_s f_{yd} / \left( A_c f_{cd} \right) = " + Math.Round(omega, 3));
            f5.Expression.Add(@"B = \sqrt{\left(1+2\omega\right)} = " + Math.Round(B, 2));
            f5.Expression.Add(@"r_{mx} = " + Math.Round(rmx, 3));
            f5.Expression.Add(@"C = 1.7 - r_mx = " + Math.Round(C, 3));
            f5.Expression.Add(@"n = N_{Ed} / \left( A_cf_{cd} \right) = " + Math.Round(n, 3));
            f5.Expression.Add(@"\lambda_{xlim} = 20ABC/\sqrt{n} = " + Math.Round(lambdaxlim, 1));
            if (lambdax < lambdaxlim)
            {
                f5.Expression.Add(@"\lambda_x < \lambda_{xlim}");
                f5.Conclusion = "Second order effects may be ignored";
            }
            else
            {
                f5.Expression.Add(@"\lambda_x >= \lambda_{xlim}");
                f5.Conclusion = "Second order effects must be considered";
                secondorderx = true;
            }
            Expressions.Add(f5);
            int ky = (load.MyTop * load.MyBot >= 0) ? 1 : -1;
            double M01y = ky * Math.Min(Math.Abs(load.MyTop), Math.Abs(load.MyBot)) + ei * load.P / 1E3;
            double M02y = Math.Max(Math.Abs(load.MyTop), Math.Abs(load.MyBot)) + ei * load.P / 1E3;
            Formula f6 = new Formula();
            f6.Narrative = "Moments about y axis including imperfections";
            f6.Expression = new List<string>();
            f6.Expression.Add(@"e_{i} = l_0/400 = " + Math.Round(ei, 1) + " mm");
            f6.Expression.Add(@"k = M_y^{top}/\left|M_y^{top}\right|*M_y^{bot}/\left|M_y^{bot}\right| = " + ky);
            f6.Expression.Add(@"M_{01y} =  k \times min\left(\left|M_y^{top}\right|,\left|M_y^{bot}\right|\right) + e_iN_{Ed} = " + Math.Round(M01y, 1) + " kN.m");
            f6.Expression.Add(@"M_{02y} = max\left(\left|M_y^{top}\right|,\left|M_y^{bot}\right|\right) + e_iN_{Ed} = " + Math.Round(M02y, 1) + " kN.m");
            Expressions.Add(f6);

            double rmy = M01y / M02y;
            C = 1.7 - rmy;
            double lambdaylim = 20 * 0.7 * B * C / Math.Sqrt(n);
            bool secondordery = false;
            Formula f7 = new Formula();
            f7.Narrative = "Slenderness limit about y axis";
            f7.Expression = new List<string>();
            f7.Expression.Add(@"A = 0.7");
            f7.Expression.Add(@"\omega = A_s f_{yd} / \left( A_c f_{cd} \right) = " + Math.Round(omega, 3));
            f7.Expression.Add(@"B = \sqrt{\left(1+2\omega\right)} = " + Math.Round(B, 2));
            f7.Expression.Add(@"r_{my} = " + Math.Round(rmy, 3));
            f7.Expression.Add(@"C = 1.7 - r_{my} = " + Math.Round(C, 3));
            f7.Expression.Add(@"n = N_{Ed} / \left( A_cf_{cd} \right) = " + Math.Round(n, 3));
            f7.Expression.Add(@"\lambda_{ylim} = 20ABC/\sqrt{n} = " + Math.Round(lambdaylim, 1));
            if (lambday < lambdaylim)
            {
                f7.Expression.Add(@"\lambda_y < \lambda_{ylim}");
                f7.Conclusion = "Second order effects may be ignored";
            }
            else
            {
                f7.Expression.Add(@"\lambda_y >= \lambda_{ylim}");
                f7.Conclusion = "Second order effects must be considered";
                secondordery = true;
            }
            Expressions.Add(f7);

            Formula f8 = new Formula();
            double[] axialDist = GetAxialLength();
            double peri = GetPerimeter();
            if (!secondorderx)
            {
                double Medx = Math.Max(M02x, load.P * Math.Max(axialDist[1] * 1E-3 / 30, 20 * 1E-3));
                c.SelectedLoad.MEdx = Math.Round(Medx, 1);
                if(MEdx != null) MEdx.Value = Math.Round(Medx, 1);
                f8.Narrative = "Design moment about x for a stocky column";
                f8.Expression = new List<string>();
                f8.Expression.Add(@"M_{Ed,x} = max \left( M_{02x}, N_{Ed}\times max \left(h/30,20 mm\right)\right) ");
                f8.Expression.Add(@"M_{Ed,x} = " + Math.Round(Medx) + "kN.m");
            }
            else
            {
                double u = peri;
                double nu = 1 + omega;
                double Kr = Math.Min(1, (nu - n) / (nu - 0.4));
                //double d = c.LY - c.CoverToLinks - c.LinkDiameter - c.BarDiameter / 2;
                double eyd = c.SteelGrade.Fy / gs / c.SteelGrade.E / 1E3;
                double r0 = 0.45 * dx / eyd;
                double h0 = 2 * Ac / u;
                double alpha1 = Math.Pow(35 / (c.ConcreteGrade.Fc + 8), 0.7);
                double alpha2 = Math.Pow(35 / (c.ConcreteGrade.Fc + 8), 0.2);
                double phiRH = (1 + (0.5 / (0.1 * Math.Pow(h0, 1.0 / 3))) * alpha1) * alpha2;
                double bfcm = 16.8 / Math.Sqrt(c.ConcreteGrade.Fc + 8);
                double bt0 = 1 / (0.1 + Math.Pow(28, 0.2));
                double phi0 = phiRH * bfcm * bt0;
                double phiInf = phi0;
                double phiefy = phiInf * 0.8;
                double betay = 0.35 + c.ConcreteGrade.Fc / 200 - lambdax / 150;
                double kphiy = Math.Max(1, 1 + betay * phiefy);
                double r = r0 / (Kr * kphiy);
                double e2x = Math.Pow(l0, 2) / (r * 10);
                double m2x = load.P * e2x * 1E-3;
                double M0e = 0.6 * M02x + 0.4 * M01x;
                List<double> Ms = new List<double>() { M02x, M0e + m2x, M01x + 0.5 * m2x, Math.Max(axialDist[1] * 1E-3 / 30, 20 * 1E-3) * load.P };
                double Medx = Ms.Max();
                c.SelectedLoad.MEdx = Math.Round(Medx, 1);
                if (MEdx != null) MEdx.Value = Math.Round(Medx, 1);

                f8.Narrative = "Design moment about x for a slender column";
                f8.Ref = "5.8.8";
                f8.Expression = new List<string>();
                f8.Expression.Add(@"RH = 50\%");
                f8.Expression.Add(@"u = " + Math.Round(u) + " mm");
                f8.Expression.Add(@"t_{0} = 28 days");
                f8.Expression.Add(@"n_u = 1 + \omega = " + Math.Round(nu, 3));
                f8.Expression.Add(@"n_{bal} = 0.4");
                f8.Expression.Add(@"K_r = min\left(1.0,(n_u-n)/(n_u-n_{bal})\right) = " + Math.Round(Kr, 3));
                f8.Expression.Add(@"\varepsilon_{yd} = f_{yd}/E_s = " + Math.Round(eyd, 5));
                f8.Expression.Add(@"1/r_0 = \varepsilon_{yd}/\left(0.45d\right) = " + Math.Round(1 / r0, 7) + @"mm^{-1}");
                f8.Expression.Add(@"h_0 = 2 A_c/u = " + Math.Round(h0));
                f8.Expression.Add(@"\alpha_1 = (35 / f_{cm})^{0.7} = " + Math.Round(alpha1, 3));
                f8.Expression.Add(@"\alpha_2 = (35 / f_{cm})^{0.2} = " + Math.Round(alpha2, 3));
                f8.Expression.Add(@"\phi_{RH}=\left[1+\left((1-RH/100\%)/(0.1\times(h_0)^{1/3})\right)\times \alpha1\right]\times \alpha2");
                f8.Expression.Add(@"\phi_{RH}=" + Math.Round(phiRH, 3));
                f8.Expression.Add(@"\beta_{fcm}=16.8/\sqrt{f_{cm}} = " + Math.Round(bfcm, 3));
                f8.Expression.Add(@"\beta_{t_0} = 1/(0.1 + t_0^{0.2} = " + Math.Round(bt0, 3));
                f8.Expression.Add(@"\phi_0 = \phi_{RH}\beta_{fcm}\beta{t0} = " + Math.Round(phi0, 3));
                f8.Expression.Add(@"\beta_{c\infty} = 1.00");
                f8.Expression.Add(@"\phi_{\infty} = \phi_0\beta_{c\infty} = " + Math.Round(phiInf, 3));
                f8.Expression.Add(@"r_{Mx} = 0.80");
                f8.Expression.Add(@"\phi_{efy}=\phi_{\infty}r_{My} = " + Math.Round(phiefy, 3));
                f8.Expression.Add(@"\beta_y = 0.35 + f_{ck}/200 - \lambda_x/150 = " + Math.Round(betay, 3));
                f8.Expression.Add(@"K_{\phi x} = max(1, 1 + \beta_y\phi_{efy}) = " + Math.Round(kphiy, 3));
                f8.Expression.Add(@"1/r = K_rK_{\phi x}/r_0 = " + Math.Round(1 / r, 7) + @"mm^{-1}");
                f8.Expression.Add("c = 10");
                f8.Expression.Add("e_{2x} = l_0^2/(rc) = " + Math.Round(e2x) + " mm");
                f8.Expression.Add(@"M_{2x} = N_{Ed} \times e_{2x} = " + Math.Round(m2x, 1) + "kN.m");
                f8.Expression.Add(@"M_{0e} = 0.6M_{02x} + 0.4M_{01x} = " + Math.Round(M0e, 1) + "kN.m");
                f8.Expression.Add(@"M_{Ed,x} = max\left(M_{02}, M_{0e}+M_{2x},M_{01}+0.5M_{2x},e_0N_{Ed}\right) ");
                f8.Expression.Add(@"M_{Ed,x} =" + Math.Round(Medx, 1) + " kN.m");
            }
            Expressions.Add(f8);

            Formula f9 = new Formula();
            if (!secondordery)
            {
                double Medy = Math.Max(M02y, load.P * Math.Max(axialDist[0] * 1E-3 / 30, 20 * 1E-3));
                c.SelectedLoad.MEdy = Math.Round(Medy, 1);
                if (MEdy != null) MEdy.Value = Math.Round(Medy, 1);
                f9.Narrative = "Design moment about y for a stocky column";
                f9.Expression = new List<string>();
                f9.Expression.Add(@"M_{Ed,y} = max \left( M_{02y}, N_{Ed}\times max \left(h/30,20 mm\right)\right) " );
                f9.Expression.Add(@"M_{Ed,y} = " + Math.Round(Medy) + "kN.m");
            }
            else
            {
                double u = peri;
                double nu = 1 + omega;
                double Kr = Math.Min(1, (nu - n) / (nu - 0.4));
                //double d = c.LY - c.CoverToLinks - c.LinkDiameter - c.BarDiameter / 2;
                double eyd = c.SteelGrade.Fy / gs / c.SteelGrade.E / 1E3;
                double r0 = 0.45 * dy / eyd;
                double h0 = 2 * Ac / u;
                double alpha1 = Math.Pow(35 / (c.ConcreteGrade.Fc + 8), 0.7);
                double alpha2 = Math.Pow(35 / (c.ConcreteGrade.Fc + 8), 0.2);
                double phiRH = (1 + (0.5 / (0.1 * Math.Pow(h0, 1.0 / 3))) * alpha1) * alpha2;
                double bfcm = 16.8 / Math.Sqrt(c.ConcreteGrade.Fc + 8);
                double bt0 = 1 / (0.1 + Math.Pow(28, 0.2));
                double phi0 = phiRH * bfcm * bt0;
                double phiInf = phi0;
                double phiefy = phiInf * 0.8;
                double betay = 0.35 + c.ConcreteGrade.Fc / 200 - lambday / 150;
                double kphiy = Math.Max(1, 1 + betay * phiefy);
                double r = r0 / (Kr * kphiy);
                double e2y = Math.Pow(l0, 2) / (r * 10);
                double m2y = load.P * e2y * 1E-3;
                double M0e = 0.6 * M02y + 0.4 * M01y;
                List<double> Ms = new List<double>() { M02y, M0e + m2y, M01y + 0.5 * m2y, Math.Max(axialDist[0] * 1E-3 / 30, 20 * 1E-3) * load.P };
                double Medy = Ms.Max();
                c.SelectedLoad.MEdy = Math.Round(Medy, 1);
                if (MEdy != null) MEdy.Value = Math.Round(Medy, 1);

                f9.Narrative = "Design moment about y for a slender column";
                f9.Ref = "5.8.8";
                f9.Expression = new List<string>();
                f9.Expression.Add(@"RH = 50\%");
                f9.Expression.Add(@"u = " + u + " mm");
                f9.Expression.Add(@"t_{0} = 28 days");
                f9.Expression.Add(@"n_u = 1 + \omega = " + Math.Round(nu, 3));
                f9.Expression.Add(@"n_{bal} = 0.4");
                f9.Expression.Add(@"K_r = min\left(1.0,(n_u-n)/(n_u-n_{bal})\right) = " + Math.Round(Kr, 3));
                f9.Expression.Add(@"\varepsilon_{yd} = f_{yd}/E_s = " + Math.Round(eyd, 5));
                f9.Expression.Add(@"1/r_0 = \varepsilon_{yd}/\left(0.45d\right) = " + Math.Round(1 / r0, 7) + @"mm^{-1}");
                f9.Expression.Add(@"h_0 = 2 A_c/u = " + Math.Round(h0));
                f9.Expression.Add(@"\alpha_1 = (35 / f_{cm})^{0.7} = " + Math.Round(alpha1, 3));
                f9.Expression.Add(@"\alpha_2 = (35 / f_{cm})^{0.2} = " + Math.Round(alpha2, 3));
                f9.Expression.Add(@"\phi_{RH}=\left[1+\left((1-RH/100\%)/(0.1\times(h_0)^{1/3})\right)\times \alpha1\right]\times \alpha2 ");
                f9.Expression.Add(@"\phi_{RH}=" + Math.Round(phiRH, 3));
                f9.Expression.Add(@"\beta_{fcm}=16.8/\sqrt{f_{cm}} = " + Math.Round(bfcm, 3));
                f9.Expression.Add(@"\beta_{t_0} = 1/(0.1 + t_0^{0.2} = " + Math.Round(bt0, 3));
                f9.Expression.Add(@"\phi_0 = \phi_{RH}\beta_{fcm}\beta{t0} = " + Math.Round(phi0, 3));
                f9.Expression.Add(@"\beta_{c\infty} = 1.00");
                f9.Expression.Add(@"\phi_{\infty} = \phi_0\beta_{c\infty} = " + Math.Round(phiInf, 3));
                f9.Expression.Add(@"r_{Mx} = 0.80");
                f9.Expression.Add(@"\phi_{efy}=\phi_{\infty}r_{My} = " + Math.Round(phiefy, 2));
                f9.Expression.Add(@"\beta_y = 0.35 + f_{ck}/200 - \lambda_y/150 = " + Math.Round(betay, 3));
                f9.Expression.Add(@"K_{\phi x} = max(1, 1 + \beta_y\phi_{efy}) = " + Math.Round(kphiy, 3));
                f9.Expression.Add(@"1/r = K_rK_{\phi x}/r_0 = " + Math.Round(1 / r, 7) + @"mm^{-1}");
                f9.Expression.Add("c = 10");
                f9.Expression.Add("e_{2y} = l_0^2/(rc) = " + Math.Round(e2y) + " mm");
                f9.Expression.Add(@"M_{2y} = N_{Ed}\times e_{2y} = " + Math.Round(m2y, 1) + "kN.m");
                f9.Expression.Add(@"M_{0e} = 0.6M_{02y} + 0.4M_{01y} = " + Math.Round(M0e, 1) + "kN.m");
                f9.Expression.Add(@"M_{Ed,y} = max\left(M_{02}, M_{0e}+M_{2y},M_{01y}+0.5M_{2y},e_0N_{Ed}\right)");
                f9.Expression.Add(@"M_{Ed,y} = " + Math.Round(Medy, 1) + " kN.m");
            }
            Expressions.Add(f9);

            if (c?.FireLoad?.Name == "0.7*[selected]")
            {
                c.FireLoad.P = 0.7 * c.SelectedLoad.P;
                c.FireLoad.MEdx = 0.7 * c.SelectedLoad.MEdx;
                c.FireLoad.MEdy = 0.7 * c.SelectedLoad.MEdy;
            }

            return spacing;
        }

        public double GetPerimeter()
        {
            Column col = Column;
            if (col.Shape == GeoShape.Rectangular)
                return 2 * (col.LX + col.LY);
            else if (col.Shape == GeoShape.Circular)
                return 2 * Math.PI * col.Diameter / 2.0;
            else if (col.Shape == GeoShape.Polygonal)
            {
                double n = col.Edges;
                return n * col.Diameter * Math.Sin(Math.PI / n);
            }
            else if (col.Shape == GeoShape.LShaped)
                return 2 * (col.HX + col.HY);
            else if (col.Shape == GeoShape.TShaped)
                return 2 * (col.HX + col.HY);
            else if (col.Shape == GeoShape.CustomShape)
                return 2 * (col.customLX + col.customLY);
            return 0;
        }

        public double[] GetAxialLength()
        {
            Column col = Column;
            if (col.Shape == GeoShape.Rectangular)
                return new double[] { col.LX, col.LY };
            else if (col.Shape == GeoShape.Circular)
                return new double[] { col.Diameter, col.Diameter };
            else if (col.Shape == GeoShape.Polygonal)
            {
                List<MWPoint2D> points = new List<MWPoint2D>();
                double theta = 0;
                double inc = 2 * Math.PI / col.Edges;
                for (int i = 0; i < col.Edges; i++)
                {
                    theta = i * inc;
                    double x = col.Radius * Math.Cos(theta);
                    double y = col.Radius * Math.Sin(theta);
                    points.Add(new MWPoint2D(x, y));
                }
                return new double[] { Math.Abs(points.Min(p => p.X) - points.Max(p => p.X)), Math.Abs(points.Min(p => p.Y) - points.Max(p => p.Y)) };
            }
            else if (col.Shape == GeoShape.LShaped)
                return new double[] { col.HX, col.HY };
            else if (col.Shape == GeoShape.TShaped)
                return new double[] { col.HX, col.HY };
            else if (col.Shape == GeoShape.CustomShape)
                return new double[] { col.customLX, col.customLY };
            return null;
        }


        public double[] GetSecondMomentInertia(Formula fx, Formula fy)
        {
            Column col = Column;
            if (col.Shape == GeoShape.Rectangular)
            {
                double Ix = (col.LX * Math.Pow(col.LY, 3)) / 12;
                double Iy = (col.LY * Math.Pow(col.LX, 3)) / 12;
                fx.Expression.Add(@"I_x = \frac{bh^3}{12} = " + Math.Round(Ix / 1e4) + " cm^4");
                fy.Expression.Add(@"I_y = \frac{hb^3}{12} = " + Math.Round(Iy / 1e4) + " cm^4");
                return new double[] { Ix, Iy, col.LX * col.LY };
            }
            else if (col.Shape == GeoShape.Circular)
            {
                double I = Math.PI * Math.Pow(col.Diameter, 4) / 64;
                fx.Expression.Add(@"I_x = \frac{\pi D^4}{64} = " + Math.Round(I / 1e4) + " cm^4");
                fy.Expression.Add(@"I_y = \frac{\pi D^4}{64} = " + Math.Round(I / 1e4) + " cm^4");
                return new double[] { I, I, Math.PI * Math.Pow(col.Diameter / 2.0, 2) };
            }
            else if (col.Shape == GeoShape.Polygonal || col.Shape == GeoShape.LShaped || col.Shape == GeoShape.TShaped || col.Shape == GeoShape.CustomShape)
            {
                double Ix = 0;
                double Iy = 0;
                List<MWPoint2D> points = new List<MWPoint2D>();
                double theta = 0;
                double inc = 2 * Math.PI / col.Edges;
                for (int i = 0; i < col.Edges; i++)
                {
                    theta = i * inc;
                    double x = col.Radius * Math.Cos(theta);
                    double y = col.Radius * Math.Sin(theta);
                    points.Add(new MWPoint2D(x, y));
                }
                points.Add(points[0]);
                for (int i = 0; i < col.Edges; i++)
                {
                    Ix += (points[i].X * points[i + 1].Y - points[i + 1].X * points[i].Y) * (Math.Pow(points[i].X, 2) + points[i].X * points[i + 1].X + Math.Pow(points[i + 1].X, 2)) / 12;
                    Iy += (points[i].X * points[i + 1].Y - points[i + 1].X * points[i].Y) * (Math.Pow(points[i].Y, 2) + points[i].Y * points[i + 1].Y + Math.Pow(points[i + 1].Y, 2)) / 12;
                }

                fx.Expression.Add(@"I_x = " + Math.Round(Ix / 1e4) + " cm^4");
                fy.Expression.Add(@"I_y = " + Math.Round(Iy / 1e4) + " cm^4");

                return new double[] { Ix, Iy };
            }
            return null;
        }


        public double[] GetEffectiveDepths(Formula f0x, Formula f0y)
        {
            Column col = Column;
            //List<MWPoint2D> rebars = new List<MWPoint2D>();
            double minX = col.ContourPoints.Min(c => c.X);
            double maxX = col.ContourPoints.Max(c => c.X);
            double minY = col.ContourPoints.Min(c => c.Y);
            double maxY = col.ContourPoints.Max(c => c.Y);
            MWPoint2D bary = col.GetCOG();
            //if (col.Shape == GeoShape.Rectangular)
            //{
            //    bary = new MWPoint2D(col.LX / 2, col.LY / 2);
            //    // Creation of the rebar positions
            //    double xspace = (col.LX - 2 * (col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0)) / (col.NRebarX - 1);
            //    double yspace = (col.LY - 2 * (col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0)) / (col.NRebarY - 1);
            //    for (int i = 0; i < col.NRebarX; i++)
            //    {
            //        var x = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 + i * xspace;
            //        for (int j = 0; j < col.NRebarY; j++)
            //        {
            //            if (i == 0 || i == col.NRebarX - 1 || j == 0 || j == col.NRebarY - 1)
            //            {
            //                var y = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 + j * yspace;
            //                rebars.Add(new MWPoint2D(x, y));
            //            }
            //        }
            //    }
            //    minX = -col.LX / 2;
            //    maxX = col.LX / 2;
            //    minY = -col.LY / 2;
            //    maxY = col.LY / 2;
            //}
            //else if (col.Shape == GeoShape.Circular)
            //{
            //    bary = new MWPoint2D(col.Diameter / 2, col.Diameter / 2);
            //    // Creation of the rebar positions
            //    double inc = 2 * Math.PI / col.NRebarCirc;
            //    double theta = 0;
            //    for (int i = 0; i < col.NRebarCirc; i++)
            //    {
            //        theta = i * inc;
            //        double x = (col.Diameter / 2.0 - col.CoverToLinks - col.LinkDiameter - col.BarDiameter / 2.0) * Math.Cos(theta);
            //        double y = (col.Diameter / 2.0 - col.CoverToLinks - col.LinkDiameter - col.BarDiameter / 2.0) * Math.Sin(theta);
            //        rebars.Add(new MWPoint2D(x, y));
            //    }
            //    minX = -col.Diameter / 2.0;
            //    maxX = col.Diameter / 2.0;
            //    minY = -col.Diameter / 2.0;
            //    maxY = col.Diameter / 2.0;
            //}
            //else if (col.Shape == GeoShape.Polygonal)
            //{
            //    List<MWPoint2D> concPoints = new List<MWPoint2D>();
            //    // Creation of the rebar positions
            //    double inc = 2 * Math.PI / col.Edges;
            //    double theta = 0;
            //    double dd = (col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0) / Math.Sin((col.Edges - 2.0) * Math.PI / (2.0 * col.Edges));
            //    for (int i = 0; i < col.Edges; i++)
            //    {
            //        theta = i * inc;
            //        double x = (col.Radius - dd) * Math.Cos(theta);
            //        double y = (col.Radius - dd) * Math.Sin(theta);
            //        rebars.Add(new MWPoint2D(x, y));
            //        double xx = col.Radius * Math.Cos(theta);
            //        double yy = col.Radius * Math.Sin(theta);
            //        concPoints.Add(new MWPoint2D(xx, yy));
            //    }
            //    bary = Points.GetBarycenter(concPoints);
            //    minX = concPoints.Min(p => p.X);
            //    maxX = concPoints.Max(p => p.X);
            //    minY = concPoints.Min(p => p.Y);
            //    maxY = concPoints.Max(p => p.Y);
            //}
            //else if (col.Shape == GeoShape.LShaped)
            //{
            //    rebars = col.GetLShapedRebars().Select(x => new MWPoint2D(x.X, x.Y)).ToList();
            //    var p = col.GetLShapeCOG();
            //    bary = new MWPoint2D(p.X, p.Y);
            //    minX = -col.HX / 2;
            //    maxX = col.HX / 2;
            //    minY = -col.HY / 2;
            //    maxY = col.HY / 2;
            //}
            //else if (col.Shape == GeoShape.TShaped)
            //{
            //    rebars = col.GetTShapedRebars().Select(x => new MWPoint2D(x.X, x.Y)).ToList();
            //    var p = col.GetTShapeCOG();
            //    bary = new MWPoint2D(p.X, p.Y);
            //    minX = -col.HX / 2;
            //    maxX = col.HX / 2;
            //    minY = -col.HY / 2;
            //    maxY = col.HY / 2;
            //}
            //else if(col.Shape == GeoShape.CustomShape)
            //{
            //    rebars = col.GetCustomShapeRebars().Select(x => new MWPoint2D(x.X, x.Y)).ToList();
            //    bary = col.GetCustomShapeCOG();
            //    minX = -col.customLX / 2;
            //    maxX = col.customLX / 2;
            //    minY = -col.customLY / 2;
            //    maxY = col.customLY / 2;
            //}

            double area = Math.PI * Math.Pow(col.BarDiameter / 2.0, 2);

            f0x.Expression.Add(@"A_{bar} = \pi\phi^2/4 = " + Math.Round(area) + " mm^2");
            f0y.Expression.Add(@"A_{bar} = \pi\phi^2/4 = " + Math.Round(area) + " mm^2");

            // calculation of moments of inertia
            double inertiaX = 0;
            double inertiaY = 0;
            for (int i = 0; i < col.Nrebars; i++)
            {
                MWPoint2D pt = col.RebarsPos[i];
                // according to X
                inertiaX += area * Math.Pow(pt.Y - bary.Y, 2);
                f0x.Expression.Add(@"I_{x" + i + @"} = A_{bar} \times y_{bar," + i + "}^2 = " + Math.Round(area * Math.Pow(pt.Y - bary.Y, 2) / 1e4) + " cm^4");
                // according to Y
                inertiaY += area * Math.Pow(pt.X - bary.X, 2);
                f0y.Expression.Add(@"I_{y" + i + @"} = A_{bar} \times x_{bar," + i + "}^2 = " + Math.Round(area * Math.Pow(pt.X - bary.X, 2) / 1e4) + " cm^4");
            }
            double totArea = col.Nrebars * Math.PI * Math.Pow(col.BarDiameter / 2.0, 2);
            f0x.Expression.Add(@"I_{sx} = " + Math.Round(inertiaX / 1E4) + " cm^4");
            f0y.Expression.Add(@"I_{sy} = " + Math.Round(inertiaY / 1E4) + " cm^4");

            // Radius of gyration
            double gyradiusX = Math.Sqrt(inertiaX / totArea);
            double gyradiusY = Math.Sqrt(inertiaY / totArea);
            f0x.Expression.Add(@"i_{sx} = \sqrt{I_{sx}/A_s} = " + Math.Round(gyradiusX) + " mm");
            f0y.Expression.Add(@"i_{sy} = \sqrt{I_{sy}/A_s} = " + Math.Round(gyradiusY) + " mm");

            // Effective depths
            double dx = Math.Min(Math.Abs(minY), maxY) + gyradiusX; // it's conservative to take the min
            double dy = Math.Min(Math.Abs(minX), maxX) + gyradiusY;
            f0x.Expression.Add(@"h_x = " + Math.Round(Math.Min(Math.Abs(minY), maxY)) + " mm");
            f0y.Expression.Add(@"h_y = " + Math.Round(Math.Min(Math.Abs(minX), maxX)) + " mm");

            f0x.Expression.Add(@"d_x = h_x + i_{sx} = " + Math.Round(dx) + " mm");
            f0y.Expression.Add(@"d_y = h_y + i_{sy} = " + Math.Round(dy) + " mm");

            return new double[] { dx, dy };
        }


        public bool CheckSpacing(Formula f0 = null)
        {
            if (f0 == null) f0 = new Formula();
            if (Column.RebarsPos.Count <= 1) return true;
            Column c = Column;
            List<double> sizes = new List<double>() { c.BarDiameter, c.MaxAggSize + 5, 20 };
            double smin = sizes.Max();
            f0.Narrative = "Bar spacing";
            f0.Expression = new List<string>();
            f0.Expression.Add(@"k_1 = 1.0 mm");
            f0.Expression.Add(@"k_2 = 5.0 mm");
            f0.Expression.Add(@"s_{min} = max\left(k_1\phi,\phi_{agg}+k_2,20 mm\right) = " + smin + " mm");
            List<double> distances = new List<double>();
            List<MWPoint2D> rebars = c.RebarsPos;
            for(int i = 0; i < c.Nrebars; i++)
                for(int j = i+1; j < c.Nrebars; j++)
                    distances.Add(Points.Distance(rebars[i], rebars[j]));
            double max = distances.Min();
            f0.Expression.Add(@"min(s) = " + Math.Round(max) + " mm");
            if(distances.Max() > smin)
            {
                f0.Conclusion = "PASS";
                f0.Status = CalcStatus.PASS;
                return true;

            }
            else
            {
                f0.Conclusion = "FAIL";
                f0.Status = CalcStatus.FAIL;
                return false;
            }
            //if (c.Shape == GeoShape.Rectangular)
            //{
            //    double sx = (c.LX - 2.0 * (c.CoverToLinks + c.LinkDiameter) - c.BarDiameter) / (c.NRebarX - 1);
            //    double sy = (c.LY - 2.0 * (c.CoverToLinks + c.LinkDiameter) - c.BarDiameter) / (c.NRebarY - 1);
            //    f0.Expression.Add(@"s_x = " + Math.Round(sx) + " mm");
            //    f0.Expression.Add(@"s_y = " + Math.Round(sy) + " mm");
            //    if (sx >= smin && sy >= smin)
            //    {
            //        f0.Conclusion = "PASS";
            //        f0.Status = CalcStatus.PASS;
            //        spacing = true;
            //    }
            //    else
            //    {
            //        f0.Conclusion = "FAIL";
            //        f0.Status = CalcStatus.FAIL;
            //    }
            //}
            //else if (c.Shape == GeoShape.Circular)
            //{
            //    double x0 = c.Diameter / 2 - c.CoverToLinks - c.LinkDiameter - c.BarDiameter / 2.0;
            //    double x1 = x0 * Math.Cos(2 * Math.PI / c.NRebarCirc);
            //    double y1 = x0 * Math.Sin(2 * Math.PI / c.NRebarCirc);
            //    double s = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1, 2));
            //    f0.Expression.Add(@"s = " + Math.Round(s) + " mm");
            //    if (s >= smin)
            //    {
            //        f0.Conclusion = "PASS";
            //        f0.Status = CalcStatus.PASS;
            //        spacing = true;
            //    }
            //    else
            //    {
            //        f0.Conclusion = "FAIL";
            //        f0.Status = CalcStatus.FAIL;
            //    }
            //}
            //else if (c.Shape == GeoShape.Polygonal)
            //{
            //    double x0 = c.Diameter / 2 - c.CoverToLinks - c.LinkDiameter - c.BarDiameter / 2.0;
            //    double x1 = x0 * Math.Cos(2 * Math.PI / c.Edges);
            //    double y1 = x0 * Math.Sin(2 * Math.PI / c.Edges);
            //    double s = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1, 2));
            //    f0.Expression.Add(@"s = " + Math.Round(s) + " mm");
            //    if (s >= smin)
            //    {
            //        f0.Conclusion = "PASS";
            //        f0.Status = CalcStatus.PASS;
            //        spacing = true;
            //    }
            //    else
            //    {
            //        f0.Conclusion = "FAIL";
            //        f0.Status = CalcStatus.FAIL;
            //    }
            //}
            //else if (c.Shape == GeoShape.LShaped || c.Shape == GeoShape.TShaped)
            //{
            //    if (c.sx1 < smin || c.sx2 < smin || c.sy1 < smin || c.sy2 < smin)
            //    {
            //        f0.Conclusion = "FAIL";
            //        f0.Status = CalcStatus.FAIL;
            //    }
            //    else
            //    {
            //        f0.Conclusion = "PASS";
            //        f0.Status = CalcStatus.PASS;
            //        spacing = true;
            //    }
            //}
            //return spacing;
        }

        
        public bool CheckMinMaxSteel(bool calcOnly = false)
        {
            Column c = Column;
            double Ac = c.GetConcreteArea();
            double Asmin = Math.Max(0.1 * c.SelectedLoad.P / (c.SteelGrade.Fy * 1e-3 / gs), 0.002 * Ac);
            double Asmax = 0.04 * Ac;
            double As = 0;
            if (c.Shape == GeoShape.Rectangular)
                As = 2 * (c.NRebarX + c.NRebarY - 2) * Math.PI * Math.Pow(c.BarDiameter / 2.0, 2);
            else if (c.Shape == GeoShape.Circular)
                As = c.NRebarCirc * Math.PI * Math.Pow(c.BarDiameter / 2.0, 2);
            else if (c.Shape == GeoShape.Polygonal)
                As = c.Edges * Math.PI * Math.Pow(c.BarDiameter / 2.0, 2);
            else if (c.Shape == GeoShape.LShaped)
                As = (8 + (c.NRebarX - 3) * 2 + (c.NRebarY - 3) * 2) * Math.PI * Math.Pow(c.BarDiameter / 2.0, 2);
            else if (c.Shape == GeoShape.TShaped)
                As = c.Nrebars * Math.PI * Math.Pow(c.BarDiameter / 2.0, 2);
            else if (c.Shape == GeoShape.CustomShape)
                As = c.AdvancedRebarsPos.Count * Math.PI * Math.Pow(c.BarDiameter / 2.0, 2);

            if (calcOnly)
            {
                if (As > Asmax)
                    return false;
                else if (As < Asmin)
                    return false;
                
                return true;
            }
            else
            {
                Formula f1 = new Formula();
                f1.Narrative = "Min / Max steel";
                f1.Expression = new List<string>();
                f1.Expression.Add(@"A_{s,min} = max\left(0.1N_{Ed}/f_{yd};0.002A_c\right) = " + Math.Round(Asmin) + "mm^2");
                f1.Expression.Add(@"A_{s,max} = 0.04A_c = " + Math.Round(Asmax) + "mm^2");
                f1.Expression.Add(@"A_s = " + Math.Round(As) + "mm^2");

                if (As > Asmax)
                {
                    f1.Conclusion = "FAIL --> too much reinforcement";
                    f1.Status = CalcStatus.FAIL;
                    Expressions.Add(f1);
                    return false;
                }
                else if (As < Asmin)
                {
                    f1.Conclusion = "FAIL --> not enough reinforcement";
                    f1.Status = CalcStatus.FAIL;
                    Expressions.Add(f1);
                    return false;
                }

                f1.Conclusion = "PASS";
                f1.Status = CalcStatus.PASS;
                Expressions.Add(f1);
                return true;
            }
            
        }

        public bool UpdateFireDesign()
        {
            Column col = Column;
            bool res = false;
            //FormulaeVM f1 = Expressions.FirstOrDefault(f => f.Narrative == "Nominal cover for fire and bond requirements") ?? new FormulaeVM();
            Formula f1 = new Formula();
            f1.Narrative = "Nominal cover for fire and bond requirements";
            Console.WriteLine("UpdateFireDesign entered");
            Console.WriteLine("column fire design method: {0}", col.FireDesignMethod);
            f1.Expression = new List<string>();
            switch (col.FireDesignMethod)
            {
                case (FDesignMethod.Table):
                    f1.Ref = "EN1992-1-2 5.3.2.";
                    res = CheckFireDesignTable(ref f1);
                    break;
                case (FDesignMethod.Isotherm_500):
                    var checkIso = CheckFireIsotherm500();
                    res = checkIso.Item1;
                    f1 = checkIso.Item2;
                    break;
                case (FDesignMethod.Zone_Method):
                    var checkZone = CheckFireZoneMethod();
                    res = checkZone.Item1;
                    f1 = checkZone.Item2;
                    break;
                case (FDesignMethod.Advanced):
                    f1.Ref = "MAGMA WORKS FIRE DESIGN";
                    f1.Expression.Add(@"AdvancedMethod");
                    res = UpdateAdvancedFireCheck();
                    f1.Status = res ? CalcStatus.PASS : CalcStatus.FAIL;
                    f1.Conclusion = res ? "PASS" : "FAIL";
                    break;
            }

            Expressions.Insert(0, f1);
            

            return res;
        }
        
        public (bool, Formula) CheckFireZoneMethod(bool newdesign = false, bool calcOnly = false)
        {
            Column col = Column;
            if (newdesign || (!col.TP?.TempMap.Keys.Contains(col.R) ?? true))
            {
                col.GetTP();
            }

            col.TP.GetContours(col.R, col.Shape.ToString(),col);

            if (steelData.Count == 0) SetSteelData();
            if (concreteData.Count == 0) SetConcreteData();

            int nDiv = 5;
            double w = (col.LX <= col.LY) ? col.LX / 2 : col.LY / 2;
            double e = w / nDiv;

            double sumK = 0;

            for (int i = 0; i < nDiv; i++)
            {
                double x = (col.LX <= col.LY) ? -col.LX / 2 + (i + 0.5) * e : 0;
                double y = (col.LY < col.LX) ? -col.LY / 2 + (i + 0.5) * e : 0;

                double temp = col.getTemp(new MWPoint2D(x, y));
                sumK += concreteData.First(c => c.Temp == temp).k;
            }
            double kcm = (1 - 0.2 / nDiv) / nDiv * sumK;
            double kctm = concreteData.First(c => c.Temp == col.getTemp(new MWPoint2D(0, 0))).k;

            double az = w * (1 - Math.Pow((kcm / kctm), 1.3));

            double NRfi = (col.LX - 2 * az) * (col.LY - 2 * az) * 0.85 * col.ConcreteGrade.Fc / 1.5 / 1e3;

            double xspace = (col.LX - 2 * (col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0)) / (col.NRebarX - 1);
            double yspace = (col.LY - 2 * (col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0)) / (col.NRebarY - 1);
            double area = Math.PI * Math.Pow(col.BarDiameter / 2, 2) / 1e6;
            for (int i = 0; i < col.NRebarX; i++)
            {
                var x = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 + i * xspace - col.LX / 2;
                for (int j = 0; j < col.NRebarY; j++)
                {
                    var y = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 + j * yspace - col.LY / 2;
                    if (i == 0 || i == col.NRebarX - 1 || j == 0 || j == col.NRebarY - 1)
                    {

                        double temp = col.getTemp(new MWPoint2D(x, y));
                        SteelData sd = steelData.First(s => s.Temp == temp);
                        NRfi += area * col.SteelGrade.Fy / 1.15 * 1e3 * sd.kf;
                    }
                }
            }

            // check moments
            double Mx = 0;
            double My = 0;

            double As = Math.Pow(col.BarDiameter / 2, 2) * Math.PI / 1e6;

            double As1Fsd = 0;
            var yy = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 - col.LY / 2;
            for (int i = 0; i < col.NRebarX; i++)
            {
                var x = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 + i * xspace - col.LX / 2;
                double temp = col.getTemp(new MWPoint2D(x, yy));
                As1Fsd += As * col.SteelGrade.Fy / gs * steelData.First(s => s.Temp == temp).kf;
            }
            double X = As1Fsd / (col.LX - 2 * az) / (acc * col.ConcreteGrade.Fc / gc);

            Mx = As1Fsd * Math.Abs(yy - (col.LY / 2 - X / 2)) + As1Fsd * (col.LY - 2 * yy);

            As1Fsd = 0;
            var xx = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 - col.LX / 2;
            for (int i = 0; i < col.NRebarY; i++)
            {
                var y = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 + i * yspace - col.LY / 2;
                double temp = col.getTemp(new MWPoint2D(xx, y));
                As1Fsd += As * col.SteelGrade.Fy / gs * steelData.First(s => s.Temp == temp).kf;
            }
            X = As1Fsd / (col.LY - 2 * az) / (acc * col.ConcreteGrade.Fc / gc);

            My = As1Fsd * Math.Abs(xx - (col.LX / 2 - X / 2)) + As1Fsd * (col.LX - 2 * xx);

            // Expressions
            bool res = (NRfi > col.FireLoad.P && Mx > col.FireLoad.MEdx && My > col.FireLoad.MEdy);

            if (!calcOnly)
            {
                Formula f = new Formula();
                f.Narrative = "Nominal cover for fire and bond requirements";
                f.Expression = new List<string>();
                f.Ref = "EN1992-1-2 ANNEX B.1";
                f.Expression.Add(@"ZoneMethod");
                f.Expression.Add(@"a_z = " + Math.Round(az) + " mm");
                f.Expression.Add(@"N_{R,fi} = (L_x - 2 a_z)(L_y - 2 a_z)f_{cd} + A_s\times f_{yd,fi}= " + Math.Round(NRfi) + " kN");
                f.Expression.Add(@"N_{Ed,fi} = " + Math.Round(col.FireLoad.P) + " kN");
                f.Expression.Add(@"M_{xR,fi} = " + Math.Round(Mx) + " kN.m");
                f.Expression.Add(@"M_{xd,fi} = " + Math.Round(col.FireLoad.MEdx) + "kN.m");
                f.Expression.Add(@"M_{yR,fi} = " + Math.Round(My) + " kN.m");
                f.Expression.Add(@"M_{yd,fi} = " + Math.Round(col.FireLoad.MEdy) + "kN.m");

                f.Status = res ? CalcStatus.PASS : CalcStatus.FAIL;
                f.Conclusion = res ? "PASS" : "FAIL";

                return (res, f);
            }

            return (res, null);
        }

        public (bool, Formula) CheckFireIsotherm500(bool allLoads = false, bool newdesign = false)
        {
            if(allLoads)
            {
                foreach(var l in Column.Loads)
                {
                    Column.FireLoad = new Load() { MEdx = 0.7 * l.MEdx, MEdy = 0.7 * l.MEdy, P = 0.7 * l.P };
                    if (!CheckFireIsotherm500().Item1)
                        return (false, new Formula());
                }
                return (true, new Formula());
            }
            if (Column.Shape == GeoShape.Rectangular)
                return CheckFireIsotherm500_Rectangular();
            //else if (Shape == GeoShape.LShaped)
            //    return CheckFireIsotherm500_LShaped();
            return (false, null);
        }

        // Isotherm 500 method for rectangular columns
        public (bool, Formula) CheckFireIsotherm500_Rectangular(bool newdesign = false)
        {
            Column col = Column;
            if (newdesign || (!col.TP?.TempMap.Keys.Contains(col.R) ?? true))
                col.GetTP();

            col.TP.GetContours(col.R, GeoShape.Rectangular.ToString());

            if (steelData.Count == 0) SetSteelData();

            Contour iso500 = col.TP.ContourPts.First(c => c.Level == 500);
            double maxX = iso500.Points.Max(x => x.X);
            double minX = iso500.Points.Min(x => x.X);
            double maxY = iso500.Points.Max(x => x.Y);
            double minY = iso500.Points.Min(x => x.Y);

            double dX = Math.Min(Math.Abs(minX - col.LX / 2), Math.Abs(maxX - col.LX / 2));
            double dY = Math.Min(Math.Abs(minY - col.LY / 2), Math.Abs(maxY - col.LY / 2));

            double NRfi = (col.LX - 2 * dX) * (col.LY - 2 * dY) * 0.85 * col.ConcreteGrade.Fc / 1.5 / 1e3;

            double xspace = (col.LX - 2 * (col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0)) / (col.NRebarX - 1);
            double yspace = (col.LY - 2 * (col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0)) / (col.NRebarY - 1);
            double area = Math.PI * Math.Pow(col.BarDiameter / 2, 2) / 1e6;
            for (int i = 0; i < col.NRebarX; i++)
            {
                var x = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 + i * xspace - col.LX / 2;
                for (int j = 0; j < col.NRebarY; j++)
                {
                    var y = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 + j * yspace - col.LY / 2;
                    if (i == 0 || i == col.NRebarX - 1 || j == 0 || j == col.NRebarY - 1)
                    {

                        double temp = col.getTemp(new MWPoint2D(x, y));
                        SteelData sd = steelData.First(s => s.Temp == temp);
                        NRfi += area * col.SteelGrade.Fy / 1.15 * 1e3 * sd.kf;
                    }
                }
            }

            // check moments
            double Mx = 0;
            double My = 0;

            double As = Math.Pow(col.BarDiameter / 2, 2) * Math.PI / 1e6;

            double As1Fsd = 0;
            var yy = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 - col.LY / 2;
            for (int i = 0; i < col.NRebarX; i++)
            {
                var x = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 + i * xspace - col.LX / 2;
                double temp = col.getTemp(new MWPoint2D(x, yy));
                As1Fsd += As * col.SteelGrade.Fy * steelData.First(s => s.Temp == temp).kf;
            }
            double X = As1Fsd / (col.LX - 2 * dX) / col.ConcreteGrade.Fc;

            Mx = As1Fsd * Math.Abs(yy - (col.LY / 2 - X / 2)) + As1Fsd * (col.LY - 2 * yy);

            As1Fsd = 0;
            var xx = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 - col.LX / 2;
            for (int i = 0; i < col.NRebarY; i++)
            {
                var y = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 + i * yspace - col.LY / 2;
                double temp = col.getTemp(new MWPoint2D(xx, y));
                As1Fsd += As * col.SteelGrade.Fy * steelData.First(s => s.Temp == temp).kf;
            }
            X = As1Fsd / (col.LY - 2 * dY) / col.ConcreteGrade.Fc;

            My = As1Fsd * Math.Abs(xx - (col.LX / 2 - X / 2)) + As1Fsd * (col.LX - 2 * xx);

            // Expressions
            Formula f = new Formula();
            f.Narrative = "Nominal cover for fire and bond requirements";
            f.Expression = new List<string>();
            f.Ref = "EN1992-1-2 ANNEX B.2";
            f.Expression.Add(@"Isotherm 500");
            f.Expression.Add(@"a_x = " + Math.Round(dX) + " mm");
            f.Expression.Add(@"a_y = " + Math.Round(dY) + " mm");
            f.Expression.Add(@"N_{R,fi} = (L_x - 2 a_x)(L_y - 2 a_y)f_{cd} + A_s\times f_{yd,fi}= " + Math.Round(NRfi) + " kN");
            f.Expression.Add(@"N_{Ed,fi} = " + Math.Round(col.FireLoad.P) + " kN");
            f.Expression.Add(@"M_{xR,fi} = " + Math.Round(Mx) + " kN.m");
            f.Expression.Add(@"M_{xd,fi} = " + Math.Round(col.FireLoad.MEdx) + "kN.m");
            f.Expression.Add(@"M_{yR,fi} = " + Math.Round(My) + " kN.m");
            f.Expression.Add(@"M_{yd,fi} = " + Math.Round(col.FireLoad.MEdy) + "kN.m");

            bool res = (NRfi > col.FireLoad.P && Mx > col.FireLoad.MEdx && My > col.FireLoad.MEdy);

            f.Status = res ? CalcStatus.PASS : CalcStatus.FAIL;
            f.Conclusion = res ? "PASS" : "FAIL";

            return (res, f);
        }

        //public (bool, Formula) CheckFireIsotherm500_LShaped(Column col, bool newdesign = false)
        //{
        //    if (newdesign || (!col.TP?.TempMap.Keys.Contains(col.R) ?? true))
        //        col.GetTP();

        //    col.TP.GetContours(col.R, col.Shape.ToString());

        //    if (steelData.Count == 0) SetSteelData();

        //    Contour iso500 = col.TP.ContourPts.First(c => c.Level == 500);
        //    double maxX = iso500.Points.Max(x => x.X);
        //    double minX = iso500.Points.Min(x => x.X);
        //    double maxY = iso500.Points.Max(x => x.Y);
        //    double minY = iso500.Points.Min(x => x.Y);

        //    double dX = Math.Min(Math.Abs(minX - col.HX / 2), Math.Abs(maxX - col.HX / 2));
        //    double dY = Math.Min(Math.Abs(minY - col.HY / 2), Math.Abs(maxY - col.HY / 2));

        //    double NRfi = (col.HX - 2 * dX) * (col.HY - 2 * dY) * 0.85 * col.ConcreteGrade.Fc / 1.5 / 1e3;

        //    List<Point> rebars = col.GetLShapedRebars();

        //    double area = Math.PI * Math.Pow(col.BarDiameter / 2, 2) / 1e6;
        //    for (int i = 0; i < rebars.Count; i++)
        //    {
        //        double temp = col.getTemp(new MWPoint2D(rebars[i].X, rebars[i].Y));
        //        SteelData sd = steelData.First(s => s.Temp == temp);
        //        NRfi += area * col.SteelGrade.Fy / 1.15 * 1e3 * sd.kf;
        //    }

        //    // check moments
        //    double Mx = 0;
        //    double My = 0;

        //    Point COG = col.GetLShapeCOG();

        //    double As1Fsd = 0;
        //    for (int i = 0; i < rebars.Count; i++)
        //    {
        //        double temp = col.getTemp(new MWPoint2D(rebars[i].X, rebars[i].Y));
        //        As1Fsd += area * col.SteelGrade.Fy * steelData.First(s => s.Temp == temp).kf;
        //    }
        //    double X = As1Fsd / (col.HX - 2 * dX) / col.ConcreteGrade.Fc;


        //    var yy = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 - col.HY / 2;

        //    Mx = As1Fsd * Math.Abs(yy - (col.HY / 2 - X / 2)) + As1Fsd * (col.HY - 2 * yy);

        //    As1Fsd = 0;
        //    var xx = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 - col.LX / 2;
        //    for (int i = 0; i < col.NRebarY; i++)
        //    {
        //        var y = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 + i * yspace - col.LY / 2;
        //        double temp = col.getTemp(new MWPoint2D(xx, y));
        //        As1Fsd += area * col.SteelGrade.Fy * steelData.First(s => s.Temp == temp).kf;
        //    }
        //    X = As1Fsd / (LY - 2 * dY) / ConcreteGrade.Fc;

        //    My = As1Fsd * Math.Abs(xx - (LX / 2 - X / 2)) + As1Fsd * (LX - 2 * xx);

        //    // Expressions
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
        //    f.Expression.Add(@"M_{xd,fi} = " + Math.Round(FireLoad.MEdx) + "kN.m");
        //    f.Expression.Add(@"M_{yR,fi} = " + Math.Round(My) + " kN.m");
        //    f.Expression.Add(@"M_{yd,fi} = " + Math.Round(FireLoad.MEdy) + "kN.m");

        //    bool res = (NRfi > FireLoad.P && Mx > FireLoad.MEdx && My > FireLoad.MEdy);

        //    f.Status = res ? CalcStatus.PASS : CalcStatus.FAIL;
        //    f.Conclusion = res ? "PASS" : "FAIL";

        //    return (res, f);
        //}

        public bool UpdateAdvancedFireCheck()
        {
            return Column.CheckIsInsideFireID();
        }

        public bool CheckFireDesignTable(bool allLoads = false)
        {
            Formula f1 = new Formula();
            if (allLoads)
            {
                foreach(var l in Column.Loads)
                {
                    Column.FireLoad = new Load() { MEdx = 0.7 * l.MEdx, MEdy = 0.7 * l.MEdy, P = 0.7 * l.P };
                    if (!CheckFireDesignTable(ref f1, true))
                        return false;
                }
                return true;
            }
            else
            {
                return CheckFireDesignTable(ref f1, true);
            }
        }

        //public bool CheckFireDesignTable(ref Formula f1, bool calcOnly = false)
        //{
        //}

        public bool CheckFireDesignTable(ref Formula f1, bool calcOnly = false)
        {
            Column c = Column;
            double Nrd = (c.GetSteelArea() * c.SteelGrade.Fy / gs + c.GetConcreteArea() * acc * c.ConcreteGrade.Fc / gc) / 1E3;
            double mufi = c.FireLoad.P / Nrd;
            // Eurocode Table 5.2.1a
            double afi = 0;
            mufi = (mufi <= 0.35) ? 0.2 : ((mufi <= 0.6) ? 0.5 : (mufi <= 0.7 ? 0.7 : 2));
            if (mufi == 2)
            {
                if(!calcOnly)
                {
                    f1.Expression.Add(@"N_{Ed,fi} = " + Math.Round(c.FireLoad.P) + " kN");
                    f1.Expression.Add(@"N_{Rd} = A_s f_{yd} + A_cf_{cd} = " + Math.Round(Nrd) + " kN");
                    f1.Expression.Add(@"N_{Ed,fi} > 0.7 N_{Rd}");

                    f1.Status = CalcStatus.FAIL;
                    f1.Conclusion = "FAIL";
                }
                return false;
            }
            if (fireTable.Count == 0) SetFireData();
            List<FireData> fdata = fireTable.Where(x => x.mu == mufi && x.R == Column.R && x.sidesExposed == Column.SidesExposed).ToList();
            fdata = fdata.OrderByDescending(x => x.minDimension).ToList();
            switch (c.Shape)
            {
                case (GeoShape.Rectangular):
                    for (int i = 0; i < fdata.Count; i++)
                    {
                        if (Column.LX >= fdata[i].minDimension && Column.LY >= fdata[i].minDimension)
                        {
                            afi = fdata[i].axisDistance;
                            break;
                        }
                    }
                    break;
                case (GeoShape.Circular):
                    for (int i = 0; i < fdata.Count; i++)
                    {
                        if (Column.Diameter >= fdata[i].minDimension)
                        {
                            afi = fdata[i].axisDistance;
                            break;
                        }
                    }
                    break;
                case (GeoShape.Polygonal):
                    for (int i = 0; i < fdata.Count; i++)
                    {
                        if (2 * Column.Radius >= fdata[i].minDimension)
                        {
                            afi = fdata[i].axisDistance;
                            break;
                        }
                    }
                    break;
            }

            double cminb = Math.Max(Column.LinkDiameter, Column.BarDiameter - Column.LinkDiameter);
            double cnommin = Math.Max(afi - c.BarDiameter / 2.0 - c.LinkDiameter, cminb + 10);

            if(!calcOnly)
            {
                f1.Expression.Add(@"c_{min,b} = max\left(\phi_v,\phi-\phi_v\right) = " + cminb + "mm");
                f1.Expression.Add(@"\mu_{fi}=0.7\frac{N_{Ed}}{N_{Rd}} \simeq " + Math.Round(mufi, 1));
                if (afi == 0)
                {
                    f1.Status = CalcStatus.FAIL;
                    f1.Conclusion = "FAIL --> minimum dimension is " + fireTable.Where(x => x.mu == mufi && x.R == Column.R).Min(x => x.minDimension) + " mm";
                }
                else
                {
                    f1.Expression.Add(@"a_{fi} = " + afi + " mm");
                    f1.Expression.Add(@"\Delta c_{dev} = 10 mm");
                    f1.Expression.Add(@"c_{nom_{min}} = max \left(a_{fi} - \phi/2-\phi_v, c_{min,b} + \Delta c_{dev}\right) = " + cnommin + "mm");
                    if (c.CoverToLinks >= cnommin)
                    {
                        f1.Status = CalcStatus.PASS;
                        f1.Conclusion = "PASS";
                    }
                    else
                    {
                        f1.Conclusion = "FAIL";
                        f1.Status = CalcStatus.FAIL;
                    }
                }
            }

            if (afi == 0)
                return false;
            else
            {
                if (c.CoverToLinks >= cnommin)
                    return true;
                else
                    return false;
            }
        }

        public void UpdateFireID(bool newdesign = false)
        {
            Column c = Column;
            
            if (concreteData.Count == 0) SetConcreteData();
            if (steelData.Count == 0) SetSteelData();

            c.UpdateTP(newdesign);

            List<Composite> composites = new List<Composite>();

            // Materials
            //Material concrete = new Material(ConcreteGrade.Name, MatYpe.Concrete, 0.85 * ConcreteGrade.Fc / 1.5, 0, ConcreteGrade.E);

            // Creation of the concrete sections
            for (int i = 1; i < c.TP.ContourPts.Count - 1; i++)
            {
                ConcreteData cd = concreteData.First(x => x.Temp == c.TP.ContourPts[i].Level);
                Material concrete = new Material(c.ConcreteGrade.Name + "_" + c.TP.ContourPts[i].Level, MatYpe.Concrete, cd.k * 0.85 * c.ConcreteGrade.Fc / 1.5, 0, c.ConcreteGrade.E, epsMax: cd.Ec1, epsMax2: cd.Ecu1, dens: cd.density);
                composites.Add(new ConcreteSection(c.TP.ContourPts[i].Points.Where((p, index) => index % 5 == 0).ToList(), concrete)); // reduces the number of points by 5
            }
            int n_last = c.TP.ContourPts.Count - 1;
            ConcreteData cd_last = concreteData.First(x => x.Temp == c.TP.ContourPts[n_last].Level);
            Material concrete_last = new Material(c.ConcreteGrade.Name + "_" + c.TP.ContourPts[n_last].Level, MatYpe.Concrete, cd_last.k * 0.85 * c.ConcreteGrade.Fc / 1.5, 0, c.ConcreteGrade.E, epsMax: cd_last.Ec1, epsMax2: cd_last.Ecu1, dens: cd_last.density);
            composites.Add(new ConcreteSection(c.TP.ContourPts[n_last].Points.Distinct().ToList(), concrete_last));

            // Creation of the rebars
            for (int i = 0; i < c.Nrebars; i++)
            {
                double temp = c.GetRebarTemp(new MWPoint2D(c.RebarsPos[i].X, c.RebarsPos[i].Y));
                SteelData sd = steelData.First(s => s.Temp == temp);
                Material steel = new Material(c.SteelGrade.Name, MatYpe.Steel, sd.kf * c.SteelGrade.Fy / 1.15, sd.kf * c.SteelGrade.Fy / 1.15, sd.kE * c.SteelGrade.E);
                Rebar r = new Rebar(MWPoint2D.Point2DByCoordinates(c.RebarsPos[i].X, c.RebarsPos[i].Y), Math.PI * Math.Pow(c.BarDiameter / 2.0, 2), steel);
                composites.Add(r);
            }

            //if (c.Shape == GeoShape.Rectangular)
            //{
            //    // Creation of the concrete sections
            //    for (int i = 1; i < c.TP.ContourPts.Count - 1; i++)
            //    {
            //        ConcreteData cd = concreteData.First(x => x.Temp == c.TP.ContourPts[i].Level);
            //        Material concrete = new Material(c.ConcreteGrade.Name + "_" + c.TP.ContourPts[i].Level, MatYpe.Concrete, cd.k * 0.85 * c.ConcreteGrade.Fc / 1.5, 0, c.ConcreteGrade.E, epsMax: cd.Ec1, epsMax2: cd.Ecu1, dens: cd.density);
            //        //composites.Add(new ConcreteSection(TP.ContourPts[i].Points.Concat(TP.ContourPts[i-1].Points.Reverse<MWPoint2D>()).ToList(),concrete));
            //        composites.Add(new ConcreteSection(c.TP.ContourPts[i].Points.Where((p, index) => index % 5 == 0).ToList(), concrete));
            //    }
            //    int n_last = c.TP.ContourPts.Count - 1;
            //    ConcreteData cd_last = concreteData.First(x => x.Temp == c.TP.ContourPts[n_last].Level);
            //    Material concrete_last = new Material(c.ConcreteGrade.Name + "_" + c.TP.ContourPts[n_last].Level, MatYpe.Concrete, cd_last.k * 0.85 * c.ConcreteGrade.Fc / 1.5, 0, c.ConcreteGrade.E, epsMax: cd_last.Ec1, epsMax2: cd_last.Ecu1, dens: cd_last.density);
            //    composites.Add(new ConcreteSection(c.TP.ContourPts[n_last].Points.Distinct().ToList(), concrete_last));


            //    // Creation of the rebars
            //    double xspace = (c.LX - 2 * (c.CoverToLinks + c.LinkDiameter + c.BarDiameter / 2.0)) / (c.NRebarX - 1);
            //    double yspace = (c.LY - 2 * (c.CoverToLinks + c.LinkDiameter + c.BarDiameter / 2.0)) / (c.NRebarY - 1);
            //    for (int i = 0; i < c.NRebarX; i++)
            //    {
            //        var x = c.CoverToLinks + c.LinkDiameter + c.BarDiameter / 2.0 + i * xspace - c.LX / 2;
            //        for (int j = 0; j < c.NRebarY; j++)
            //        {
            //            if (i == 0 || i == c.NRebarX - 1 || j == 0 || j == c.NRebarY - 1)
            //            {
            //                var y = c.CoverToLinks + c.LinkDiameter + c.BarDiameter / 2.0 + j * yspace - c.LY / 2;
            //                double temp = c.GetRebarTemp(new MWPoint2D(x, y));
            //                SteelData sd = steelData.First(s => s.Temp == temp);
            //                Material steel = new Material(c.SteelGrade.Name, MatYpe.Steel, sd.kf * c.SteelGrade.Fy / 1.15, sd.kf * c.SteelGrade.Fy / 1.15, sd.kE * c.SteelGrade.E);
            //                Rebar r = new Rebar(MWPoint2D.Point2DByCoordinates(x, y), Math.PI * Math.Pow(c.BarDiameter / 2.0, 2), steel);
            //                composites.Add(r);
            //            }
            //        }
            //    }
            //}
            //else if (c.Shape == GeoShape.LShaped)
            //{
            //    // Creation of the concrete sections
            //    for (int i = 1; i < c.TP.ContourPts.Count - 1; i++)
            //    {
            //        ConcreteData cd = concreteData.First(x => x.Temp == c.TP.ContourPts[i].Level);
            //        Material concrete = new Material(c.ConcreteGrade.Name + "_" + c.TP.ContourPts[i].Level, MatYpe.Concrete, cd.k * 0.85 * c.ConcreteGrade.Fc / 1.5, 0, c.ConcreteGrade.E, epsMax: cd.Ec1, epsMax2: cd.Ecu1, dens: cd.density);
            //        composites.Add(new ConcreteSection(c.TP.ContourPts[i].Points.Where((p, index) => index % 5 == 0).ToList(), concrete)); // reduces the number of points by 5
            //    }
            //    int n_last = c.TP.ContourPts.Count - 1;
            //    ConcreteData cd_last = concreteData.First(x => x.Temp == c.TP.ContourPts[n_last].Level);
            //    Material concrete_last = new Material(c.ConcreteGrade.Name + "_" + c.TP.ContourPts[n_last].Level, MatYpe.Concrete, cd_last.k * 0.85 * c.ConcreteGrade.Fc / 1.5, 0, c.ConcreteGrade.E, epsMax: cd_last.Ec1, epsMax2: cd_last.Ecu1, dens: cd_last.density);
            //    composites.Add(new ConcreteSection(c.TP.ContourPts[n_last].Points.Distinct().ToList(), concrete_last));

            //    // Creation of the rebars
            //    List<MWPoint2D> rebars = c.GetLShapedRebars();
            //    for (int i = 0; i < rebars.Count; i++)
            //    {
            //        double temp = c.GetRebarTemp(new MWPoint2D(rebars[i].X, rebars[i].Y));
            //        SteelData sd = steelData.First(s => s.Temp == temp);
            //        Material steel = new Material(c.SteelGrade.Name, MatYpe.Steel, sd.kf * c.SteelGrade.Fy / 1.15, sd.kf * c.SteelGrade.Fy / 1.15, sd.kE * c.SteelGrade.E);
            //        Rebar r = new Rebar(MWPoint2D.Point2DByCoordinates(rebars[i].X, rebars[i].Y), Math.PI * Math.Pow(c.BarDiameter / 2.0, 2), steel);
            //        composites.Add(r);
            //    }

            //}
            //else if (c.Shape == GeoShape.TShaped)
            //{
            //    // Creation of the concrete sections
            //    for (int i = 1; i < c.TP.ContourPts.Count - 1; i++)
            //    {
            //        ConcreteData cd = concreteData.First(x => x.Temp == c.TP.ContourPts[i].Level);
            //        Material concrete = new Material(c.ConcreteGrade.Name + "_" + c.TP.ContourPts[i].Level, MatYpe.Concrete, cd.k * 0.85 * c.ConcreteGrade.Fc / 1.5, 0, c.ConcreteGrade.E, epsMax: cd.Ec1, epsMax2: cd.Ecu1, dens: cd.density);
            //        composites.Add(new ConcreteSection(c.TP.ContourPts[i].Points.Where((p, index) => index % 5 == 0).ToList(), concrete)); // reduces the number of points by 5
            //    }
            //    int n_last = c.TP.ContourPts.Count - 1;
            //    ConcreteData cd_last = concreteData.First(x => x.Temp == c.TP.ContourPts[n_last].Level);
            //    Material concrete_last = new Material(c.ConcreteGrade.Name + "_" + c.TP.ContourPts[n_last].Level, MatYpe.Concrete, cd_last.k * 0.85 * c.ConcreteGrade.Fc / 1.5, 0, c.ConcreteGrade.E, epsMax: cd_last.Ec1, epsMax2: cd_last.Ecu1, dens: cd_last.density);
            //    composites.Add(new ConcreteSection(c.TP.ContourPts[n_last].Points.Distinct().ToList(), concrete_last));

            //    // Creation of the rebars
            //    List<MWPoint2D> rebars = c.GetTShapedRebars();
            //    for (int i = 0; i < rebars.Count; i++)
            //    {
            //        double temp = c.GetRebarTemp(new MWPoint2D(rebars[i].X, rebars[i].Y));
            //        SteelData sd = steelData.First(s => s.Temp == temp);
            //        Material steel = new Material(c.SteelGrade.Name, MatYpe.Steel, sd.kf * c.SteelGrade.Fy / 1.15, sd.kf * c.SteelGrade.Fy / 1.15, sd.kE * c.SteelGrade.E);
            //        Rebar r = new Rebar(MWPoint2D.Point2DByCoordinates(rebars[i].X, rebars[i].Y), Math.PI * Math.Pow(c.BarDiameter / 2.0, 2), steel);
            //        composites.Add(r);
            //    }

            //}
            //else if (c.Shape == GeoShape.CustomShape)
            //{
            //    // Creation of the concrete sections
            //    for (int i = 1; i < c.TP.ContourPts.Count - 1; i++)
            //    {
            //        ConcreteData cd = concreteData.First(x => x.Temp == c.TP.ContourPts[i].Level);
            //        Material concrete = new Material(c.ConcreteGrade.Name + "_" + c.TP.ContourPts[i].Level, MatYpe.Concrete, cd.k * 0.85 * c.ConcreteGrade.Fc / 1.5, 0, c.ConcreteGrade.E, epsMax: cd.Ec1, epsMax2: cd.Ecu1, dens: cd.density);
            //        composites.Add(new ConcreteSection(c.TP.ContourPts[i].Points.Where((p, index) => index % 5 == 0).ToList(), concrete)); // reduces the number of points by 5
            //    }
            //    int n_last = c.TP.ContourPts.Count - 1;
            //    ConcreteData cd_last = concreteData.First(x => x.Temp == c.TP.ContourPts[n_last].Level);
            //    Material concrete_last = new Material(c.ConcreteGrade.Name + "_" + c.TP.ContourPts[n_last].Level, MatYpe.Concrete, cd_last.k * 0.85 * c.ConcreteGrade.Fc / 1.5, 0, c.ConcreteGrade.E, epsMax: cd_last.Ec1, epsMax2: cd_last.Ecu1, dens: cd_last.density);
            //    composites.Add(new ConcreteSection(c.TP.ContourPts[n_last].Points.Distinct().ToList(), concrete_last));

            //    // Creation of the rebars
            //    List<MWPoint2D> rebars = c.GetCustomShapeRebars();
            //    for (int i = 0; i < rebars.Count; i++)
            //    {
            //        double temp = c.GetRebarTemp(new MWPoint2D(rebars[i].X, rebars[i].Y));
            //        SteelData sd = steelData.First(s => s.Temp == temp);
            //        Material steel = new Material(c.SteelGrade.Name, MatYpe.Steel, sd.kf * c.SteelGrade.Fy / 1.15, sd.kf * c.SteelGrade.Fy / 1.15, sd.kE * c.SteelGrade.E);
            //        Rebar r = new Rebar(MWPoint2D.Point2DByCoordinates(rebars[i].X, rebars[i].Y), Math.PI * Math.Pow(c.BarDiameter / 2.0, 2), steel);
            //        composites.Add(r);
            //    }

            //}

            Diagram d = new Diagram(composites, c.DiagramDisc);
            c.fireDiagramFaces = d.faces;
            c.fireDiagramVertices = d.vertices;

        }

        public double[] GetEmbodiedCarbon()
        {
            Column c = Column;
            if (CarbonData.Count == 0) InitializeCarbonData();
            double Fc = c.ConcreteGrade.Fc;

            double rebarCarbon = Math.Round(1.46 * c.SteelVol() / 1E9 * steelVolMass, 1);

            // the concrte embodied carbon is taken from the table or interpolated if necessary
            double concreteRatio = 0;
            if (CarbonData.Keys.Contains(c.ConcreteGrade.Fc))
                concreteRatio = CarbonData[Fc];
            else if (Fc < CarbonData.Min(x => x.Key))
            {
                double y0 = CarbonData.ElementAt(0).Value;
                double x0 = CarbonData.ElementAt(0).Key;
                double y1 = CarbonData.ElementAt(1).Value;
                double x1 = CarbonData.ElementAt(1).Key;
                concreteRatio = y0 - (y1 - y0) / (x1 - x0) * (x0 - Fc);
            }
            else if (Fc > CarbonData.Max(x => x.Key))
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
            double concreteCarbon = Math.Round(concreteRatio * c.ConcreteVol() / 1E9 * concreteVolMass, 1);

            return new double[] { concreteCarbon, rebarCarbon, concreteCarbon + rebarCarbon };

        }

        public double[] GetCost()
        {
            Column c = Column;
            if (SteelCosts.Count == 0) InitializeSteelCosts();
            if (ConcreteCosts.Count == 0) InitializeConcreteCosts();
            double steel = c.SteelVol() / 1e9 * steelVolMass / 1e3 * SteelCosts.FirstOrDefault(x => x.Key == c.BarDiameter).Value[0];
            double concrete = c.ConcreteVol() / 1e9 * ConcreteCosts.FirstOrDefault(x => x.Key == Math.Round(c.ConcreteGrade.Fc)).Value[0];
            double formwork = c.GetPerimeter() * c.Length * 45 / 1e6;

            return new double[] { concrete, steel, formwork, steel + concrete + formwork };
        }

        public void SetFireData()
        {
            string path = DataPath + "FireData.csv";
            try
            {
                using (var reader = new StreamReader(path))
                {
                    reader.ReadLine();
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');

                        fireTable.Add(new FireData(
                            Convert.ToInt32(values[0]),
                            Convert.ToDouble(values[1]),
                            Convert.ToInt32(values[2]),
                            Convert.ToInt32(values[3]),
                            (FireExposition)Enum.Parse(typeof(FireExposition), values[4])
                            ));
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        public void SetConcreteData()
        {
            double d = Column.ConcreteGrade.Density;
            string path = DataPath + "ConcreteTemperatureData.csv";
            try
            {
                using (var reader = new StreamReader(path))
                {
                    reader.ReadLine();
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');

                        concreteData.Add(new ConcreteData(
                            Convert.ToDouble(values[0]), 
                            Convert.ToDouble(values[1]), 
                            Convert.ToDouble(values[2]),
                            Convert.ToDouble(values[3]),
                            (AggregateType)Enum.Parse(typeof(AggregateType),values[4]),
                            d * Convert.ToDouble(values[5])
                            ));
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

        }

        public void SetSteelData()
        {
            string path = DataPath + "SteelTemperatureData.csv";
            try
            {
                using (var reader = new StreamReader(path))
                {
                    reader.ReadLine();
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');

                        steelData.Add(new SteelData(Convert.ToDouble(values[0]), Convert.ToDouble(values[1]), Convert.ToDouble(values[2])));
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
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

        //----------------------
        //  SCAFFOLD ONLY
        //----------------------
        public override List<Formula> GenerateFormulae()
        {
            Console.WriteLine("GenerateFormulae entered");
            var image = generateImage();
            Expressions.Insert(0, new Formula
            {
                Narrative = "Geometry:",
                Image = image[0],
            });
            return Expressions;
        }

        private List<SkiaSharp.SKBitmap> generateImage()
        {
            Console.WriteLine("generateImage entered");
            double sf = 1;
            Column c = Column;

            DisplayDataSet dataset = DisplayDataSet.GetDisplayDataSet();
            dataset.AddFormattingInstruction("ColumnOutline",
                                             new SkiaSharp.SKPaint { StrokeWidth = 2, Color = SkiaSharp.SKColors.Gray },
                                             new SkiaSharp.SKPaint { StrokeWidth = 6, Color = SkiaSharp.SKColors.Transparent });
            dataset.AddFormattingInstruction("Rebar",
                                             new SkiaSharp.SKPaint { StrokeWidth = 2, Color = SkiaSharp.SKColors.Brown },
                                             new SkiaSharp.SKPaint { StrokeWidth = 15, Color = SkiaSharp.SKColors.Transparent });
            StructuralDrawing2D.StructuralDrawing2D drawing = new StructuralDrawing2D.StructuralDrawing2D(dataset);

            // axis
            var pathX = new SkiaSharp.SKPath();
            pathX.MoveTo((float)(-c.LX * sf), 0);
            pathX.LineTo((float)(c.LX * sf), 0);
            drawing.AddElement(DisplayFormatPreset.Gridline, pathX, false);

            var pathY = new SkiaSharp.SKPath();
            pathY.MoveTo(0, (float)(-c.LY * sf));
            pathY.LineTo(0, (float)(c.LY * sf));
            drawing.AddElement(DisplayFormatPreset.Gridline, pathY, false);

            // column shape
            var path = new SkiaSharp.SKPath();
            var rect = new SkiaSharp.SKRect((float)(-c.LX / 2 * sf), (float)(c.LY / 2 * sf), (float)(c.LX / 2 * sf), (float)(-c.LY / 2 * sf));
            path.AddRect(rect);
            drawing.AddElement("ColumnOutline", path, true);

            drawing.AddText(new DrawingText("X", drawing.GetBounds().Right + 50, 5, 25));
            drawing.AddText(new DrawingText("X", drawing.GetBounds().Left - 50, 5, 25));
            drawing.AddText(new DrawingText("Y", 0, drawing.GetBounds().Top - 50, 25));
            drawing.AddText(new DrawingText("Y", 0, drawing.GetBounds().Bottom + 50, 25));

            // rebars
            double dx = (c.LX - 2 * (c.CoverToLinks + c.LinkDiameter) - c.BarDiameter) / (c.NRebarX - 1);
            double dy = (c.LY - 2 * (c.CoverToLinks + c.LinkDiameter) - c.BarDiameter) / (c.NRebarY - 1);

            for (int i = 0; i < c.NRebarX; i++)
            {
                for (int j = 0; j < c.NRebarY; j++)
                {
                    if (i == 0 || i == c.NRebarX - 1 || j == 0 || j == c.NRebarY - 1)
                    {
                        var pathR = new SkiaSharp.SKPath();

                        double x = c.CoverToLinks + c.LinkDiameter + c.BarDiameter / 2 + i * dx - c.LX / 2;
                        double y = c.CoverToLinks + c.LinkDiameter + c.BarDiameter / 2 + j * dy - c.LY / 2;
                        pathR.AddCircle((float)(x * sf), (float)(y * sf), (float)(c.BarDiameter / 2 * sf));

                        drawing.AddElement("Rebar", pathR, true);
                    }
                }
            }


            var bitmap = drawing.GenerateBitmapImage(600, 600, 100);
            return new List<SkiaSharp.SKBitmap> { bitmap };
        }

        public override List<MW3DModel> Get3DModels()
        {
            Column c = Column;

            c.GetInteractionDiagram();

            List<MW3DModel> Models = new List<MW3DModel>();

            // current load state represented as a diamond
            MWMesh loadStateMesh = new MWMesh();

            double size = 100;
            loadStateMesh.addNode(c.SelectedLoad.MEdx - size, c.SelectedLoad.MEdy - size, -c.SelectedLoad.P, new MWPoint2D(0.5, 0.5));
            loadStateMesh.addNode(c.SelectedLoad.MEdx - size, c.SelectedLoad.MEdy + size, -c.SelectedLoad.P, new MWPoint2D(0.5, 0.5));
            loadStateMesh.addNode(c.SelectedLoad.MEdx + size, c.SelectedLoad.MEdy + size, -c.SelectedLoad.P, new MWPoint2D(0.5, 0.5));
            loadStateMesh.addNode(c.SelectedLoad.MEdx + size, c.SelectedLoad.MEdy - size, -c.SelectedLoad.P, new MWPoint2D(0.5, 0.5));
            loadStateMesh.addNode(c.SelectedLoad.MEdx, c.SelectedLoad.MEdy, -c.SelectedLoad.P - size, new MWPoint2D(0.5, 0.5));
            loadStateMesh.addNode(c.SelectedLoad.MEdx, c.SelectedLoad.MEdy, -c.SelectedLoad.P + size, new MWPoint2D(0.5, 0.5));

            List<int[]> indicesList2 = new List<int[]>();
            indicesList2.Add(new int[] { 5, 1, 0 });
            indicesList2.Add(new int[] { 5, 2, 1 });
            indicesList2.Add(new int[] { 5, 3, 2 });
            indicesList2.Add(new int[] { 5, 0, 3 });
            indicesList2.Add(new int[] { 0, 1, 4 });
            indicesList2.Add(new int[] { 1, 2, 4 });
            indicesList2.Add(new int[] { 2, 3, 4 });
            indicesList2.Add(new int[] { 3, 0, 4 });

            loadStateMesh.setIndices(indicesList2);

            loadStateMesh.Brush = new MWBrush(50, 255, 128);
            loadStateMesh.Opacity = 1;

            MW3DModel myID = new MW3DModel(loadStateMesh);

            // interaction diagram
            MWMesh myMesh = new MWMesh();

            for (int i = 0; i < c.diagramVertices.Count; i++)
            {
                MWPoint3D pt = c.diagramVertices[i];
                myMesh.addNode(pt.X, pt.Y, pt.Z, new MWPoint2D(0.5, 0.5));
            }

            List<int[]> indicesList = new List<int[]>();
            for (int i = 0; i < c.diagramFaces.Count; i++)
            {
                var f = c.diagramFaces[i];
                int[] indices = new int[]
                {
                    c.diagramVertices.IndexOf(f.Points[0]),
                    c.diagramVertices.IndexOf(f.Points[1]),
                    c.diagramVertices.IndexOf(f.Points[2])
                };
                indicesList.Add(indices);
            }
            myMesh.setIndices(indicesList);

            myMesh.Brush = new MWBrush(255, 200, 50, 50);
            myMesh.Opacity = 0.4;

            var edges = myMesh.GetUniqueEdges();
            foreach (var edge in edges)
            {
                MWPoint3D p1 = myMesh.Nodes[edge[0]].Point;
                MWPoint3D p2 = myMesh.Nodes[edge[1]].Point;
                var stickMesh = MWMesh.makeExtrudedPolygon(p1, p2, 8, 6);
                stickMesh.Brush = new MWBrush(255, 128, 0);
                var checkedges = stickMesh.GetOuterEdges();
                var checkalledges = stickMesh.GetAllEdges();
                var checkunique = stickMesh.GetUniqueEdges();
                var outerEdges = stickMesh.GetMeshOutlines();
                myID.Meshes.Add(stickMesh);
            }

            var minPoint = myID.GetMinimumCorner();
            var maxPoint = myID.GetMaximumCorner();
            double axisOverun = 1000;

            // calc scale for axes
            var xAxisMarks = getAxisMarks(minPoint.X - axisOverun, maxPoint.X + axisOverun);
            var yAxisMarks = getAxisMarks(minPoint.Y - axisOverun, maxPoint.Y + axisOverun);
            var zAxisMarks = getAxisMarks(minPoint.Z - axisOverun, maxPoint.Z + axisOverun);
            //List<MWText3D> axisText = new List<MWText3D>();

            List<MWMesh> axisMeshes = new List<MWMesh>();

            for (int i = 0; i < xAxisMarks.Item3; i++)
            {
                double s = xAxisMarks.Item1 + xAxisMarks.Item2 * i;
                axisMeshes.Add(MWMesh.makeExtrudedPolygon(new MWPoint3D(s, minPoint.Y - axisOverun, 0), new MWPoint3D(s, maxPoint.Y + axisOverun, 0), 1, 5));
                axisMeshes.Add(MWMesh.makeExtrudedPolygon(new MWPoint3D(s, 0, minPoint.Z - axisOverun), new MWPoint3D(s, 0, maxPoint.Z + axisOverun), 1, 5));
                myID.Text.Add(new MWText3D(s.ToString() + "kNm", new MWPoint3D(s, 0, maxPoint.Z + axisOverun), new MWVector3D(0, 0, 1), 50, new MWVector3D(-1, 0, 0)));
            }
            for (int i = 0; i < yAxisMarks.Item3; i++)
            {
                double s = yAxisMarks.Item1 + yAxisMarks.Item2 * i;
                axisMeshes.Add(MWMesh.makeExtrudedPolygon(new MWPoint3D(minPoint.X - axisOverun, s, 0), new MWPoint3D(maxPoint.X + axisOverun, s, 0), 1, 5));
                axisMeshes.Add(MWMesh.makeExtrudedPolygon(new MWPoint3D(0, s, minPoint.Z - axisOverun), new MWPoint3D(0, s, maxPoint.Z + axisOverun), 1, 5));
                myID.Text.Add(new MWText3D(s.ToString() + "kNm", new MWPoint3D(0, s, maxPoint.Z + axisOverun), new MWVector3D(0, 0, 1), 50, new MWVector3D(0, -1, 0)));

            }
            for (int i = 0; i < zAxisMarks.Item3; i++)
            {
                double s = zAxisMarks.Item1 + zAxisMarks.Item2 * i;
                axisMeshes.Add(MWMesh.makeExtrudedPolygon(new MWPoint3D(minPoint.X - axisOverun, 0, s), new MWPoint3D(maxPoint.X + axisOverun, 0, s), 1, 5));
                axisMeshes.Add(MWMesh.makeExtrudedPolygon(new MWPoint3D(0, minPoint.Y - axisOverun, s), new MWPoint3D(0, maxPoint.Y + axisOverun, s), 1, 5));
                myID.Text.Add(new MWText3D(s.ToString() + "kN", new MWPoint3D(maxPoint.X + axisOverun, 0, s), new MWVector3D(1, 0, 0), 50, new MWVector3D(0, 0, 1)));
                myID.Text.Add(new MWText3D(s.ToString() + "kN", new MWPoint3D(0, maxPoint.Y + axisOverun, s), new MWVector3D(0, 1, 0), 50, new MWVector3D(0, 0, 1)));
            }
            foreach (var mesh in axisMeshes)
            {
                mesh.Brush = new MWBrush(255, 0, 255, 255);
                myID.Meshes.Add(mesh);
            }

            myID.Text.Add(new MWText3D("3D Interaction Diagram", new MWPoint3D(0, 0, maxPoint.Z + 2000), new MWVector3D(1, 0, 0), 200));
            myID.Text.Add(new MWText3D("The green octahedron represents current design", new MWPoint3D(0, 0, maxPoint.Z + 1800), new MWVector3D(1, 0, 0), 100));

            List<MWMesh> mainAxisMeshes = new List<MWMesh>();
            mainAxisMeshes.Add(MWMesh.makeExtrudedPolygon(new MWPoint3D(minPoint.X - 1000, 0, 0), new MWPoint3D(maxPoint.X + 1000, 0, 0), 5, 5));
            mainAxisMeshes.Add(MWMesh.makeExtrudedPolygon(new MWPoint3D(0, minPoint.Y - 1000, 0), new MWPoint3D(0, maxPoint.Y + 1000, 0), 5, 5));
            mainAxisMeshes.Add(MWMesh.makeExtrudedPolygon(new MWPoint3D(0, 0, minPoint.Z - 1000), new MWPoint3D(0, 0, maxPoint.Z + 1000), 5, 5));
            foreach (var mesh in mainAxisMeshes)
            {
                mesh.Brush = new MWBrush(255, 0, 255, 255);
                myID.Meshes.Add(mesh);
            }

            myID.Meshes.Add(myMesh);

            Models.Add(myID);

            return Models;
        }

        private Tuple<double, double, int> getAxisMarks(double min, double max)
        {
            double range = max - min;
            var stepNumber = MWGeometry.Utilities.RoundToSignificantFigure(range / 10, 1);
            var step = int.Parse(stepNumber.ToString()[0].ToString());
            int newStep = 0;
            if (step == 3 || step == 4)
                newStep = 2;
            else if (step == 6 || step == 7 || step == 8 || step == 9)
                newStep = 5;
            else
                newStep = step;

            double newStepNumber = (stepNumber / step) * newStep;

            double startNumber = Math.Ceiling(min / newStepNumber) * newStepNumber;
            double endNumber = Math.Ceiling(max / newStepNumber) * newStepNumber;
            int numberOfSteps = (int)((endNumber - startNumber) / newStepNumber);
            return new Tuple<double, double, int>(startNumber, newStepNumber, numberOfSteps);
        }

        private void ImportConcreteGrades()
        {
            Console.WriteLine("Importing concrete grades");
            string path = DataPath + "ConcreteGrades.csv";
            try
            {
                using(var reader = new StreamReader(path))
                {
                    reader.ReadLine();
                    while(!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');

                        ConcreteGrades.Add(new Concrete(values[0], Convert.ToDouble(values[1]), Convert.ToDouble(values[2])));
                    }
                }
                Console.WriteLine("concrete grades ok");
            }
            catch(Exception ex) 
            {
                Console.WriteLine("concrete grades problem");
                Console.WriteLine(ex.Message);
            }
        }

        

    }
}
