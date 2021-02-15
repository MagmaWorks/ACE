using ColumnDesignCalc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimisation
{
    public class DesignOptimiser
    {
        public Column column;
        public double T0 = 3000;
        public int Maxiter = 200;
        public double Alpha = 0.9;
        public double Variance;
        public bool newDesign;
        //public double Wcost;
        //public double Wcarb;
        public bool[] Shapes;
        public bool[] Activations;
        public string[] Mins;
        public string[] Maxs;
        public string[] Incrs;
        public double[] DriversWeight;
        public double[] Drivers;
        bool AllLoads;

        public List<Tuple<double, double>> Sizes;
        public List<Concrete> ConcreteGrades;
        public List<Steel> SteelGrades;
        public List<int> BarDiameters;
        public List<int> LinkDiameters;

        double fBest;
        Column BestCol;
        double CarbonRef;
        double CostRef;
        bool[] fireMethods;
        //private BackgroundWorker backgroundWorker1;

        int iter = 0;

        public int Index { get; set; }

        public DesignOptimiser() { }

        public DesignOptimiser(Column col)
        {
            column = col;
        }

        //public void ColumnDesignOpti(bool[] sh, bool[] activ, string[] mins, string[] maxs, string[] incrs, int maxiter, double al, double vari,
        //    double[] drivers, double[] driversWeight, List<Concrete> cg, List<int> bd, List<int> ld)
        //{
        //    this.backgroundWorker1 = new BackgroundWorker();
        //    InitializeBackgroundWorker();

        //    // Initialization
        //    N = maxiter;
        //    alpha = al;
        //    variance = vari;
        //    weights = driversWeight;
        //    factors = drivers;
        //    shapes = sh;
        //    AllLoads = column.AllLoads;

        //    Wcost = driversWeight[0] / (driversWeight.Sum());
        //    Wcarb = driversWeight[1] / (driversWeight.Sum());

        //    activInputs = activ;
        //    minis = mins;
        //    maxis = maxs;
        //    incres = incrs;

        //    concreteGrades = cg;
        //    barDiameters = bd;
        //    linkDiameters = ld;

        //    // Async process 
        //    backgroundWorker1.RunWorkerAsync();

        //}

        //private void InitializeBackgroundWorker()
        //{
        //    backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
        //    backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
        //    backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
        //    backgroundWorker1.WorkerReportsProgress = true;
        //}

        //private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    BackgroundWorker worker = sender as BackgroundWorker;

        //    column = AsyncOptimisation.Optimise(worker, column, shapes, activInputs, minis, maxis, incres, concreteGrades, barDiameters, linkDiameters, N, T0,
        //        weights, factors, alpha, false, variance, AllLoads);

        //}

        //private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        //{
        //    Calculations calc = new Calculations(column);
        //    double[] Carb = calc.GetEmbodiedCarbon();
        //    double carb = Carb[2];

        //    double[] Costs = calc.GetCost();
        //    double cost = Costs[3];
        //    Console.WriteLine("Optimisation completed");
        //}

        //private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        //{
        //    //object[] report = e.UserState as object[];
        //    Column[] report = e.UserState as Column[];
        //    iter++;
        //    //Column BestCol = report[2] as Column;
        //    Column BestCol = report[2] as Column;
        //    Console.WriteLine("iteration {0}", iter);

        //    column = BestCol;

        //}

        public void Initialize()
        {
            fireMethods = new bool[] { true, false, false, false };
            Calculations calc = new Calculations(column);
            CarbonRef = calc.GetEmbodiedCarbon()[2]; ;
            CostRef = calc.GetCost()[3];
            double f = Objective(column); //, CarbonRef, CostRef, DriversWeight[0], Drivers[0], DriversWeight[1], Drivers[1], fireMethods);
            fBest = f;
            BestCol = column.Clone();
        }

        public  async Task Optimise_Simulated_Annealing_Async(IProgress<TaskAsyncProgress> progress)
        {
            Column col = column.Clone();

            double Wcarb = DriversWeight[1];
            double Wcost = DriversWeight[0];
            double Fcarb = Drivers[1];
            double Fcost = Drivers[0];
            bool square = false;
            double T = T0;
            int t = 0;
            double f = fBest;
            //

            while (t < Maxiter)
            {
                int NN = Activations.Count();

                // update of X
                Column ColTemp = col.Clone();
                ColTemp.SetChecksToFalse();
                ColTemp.DiagramDisc = Convert.ToInt32(15 + 15 * t * 1.0 / (Maxiter - 1)); // linear increase of the interaction diagram discretisation (for speed purposes)
                List<int> eps = NormalRand(NN, Variance);

                if (Activations[0])
                    ColTemp.LX = Math.Max(Convert.ToDouble(Mins[0]), Math.Min(col.LX + eps[0] * Convert.ToDouble(Incrs[0]), Convert.ToDouble(Maxs[0])));
                if (Activations[1])
                    ColTemp.LY = square ? ColTemp.LX : Math.Max(Convert.ToDouble(Mins[1]), Math.Min(col.LY + eps[1] * Convert.ToDouble(Incrs[1]), Convert.ToDouble(Maxs[1])));
                if (Activations[2])
                    ColTemp.NRebarX = Math.Max(Convert.ToInt32(Mins[2]), Math.Min(col.NRebarX + eps[2], Convert.ToInt32(Maxs[2])));
                if (Activations[3])
                    ColTemp.NRebarY = Math.Max(Convert.ToInt32(Mins[3]), Math.Min(col.NRebarY + eps[3], Convert.ToInt32(Maxs[3])));
                if (Activations[4])
                    ColTemp.Diameter = Math.Max(Convert.ToDouble(Mins[4]), Math.Min(col.Diameter + eps[4] * Convert.ToDouble(Incrs[4]), Convert.ToDouble(Maxs[4])));
                if (Activations[5])
                    ColTemp.NRebarCirc = Math.Max(Convert.ToInt32(Mins[5]), Math.Min(col.NRebarCirc + eps[5] * Convert.ToInt32(Incrs[5]), Convert.ToInt32(Maxs[5])));
                if (Activations[6])
                    ColTemp.Radius = Math.Max(Convert.ToDouble(Mins[6]), Math.Min(col.Radius + eps[6] * Convert.ToDouble(Incrs[6]), Convert.ToDouble(Maxs[6])));
                if (Activations[7])
                    ColTemp.Edges = Math.Max(Convert.ToInt32(Mins[7]), Math.Min(col.Edges + eps[7] * Convert.ToInt32(Incrs[7]), Convert.ToInt32(Maxs[7])));
                
                if (Activations[8])
                {
                    int minidx = BarDiameters.IndexOf(Convert.ToInt32(Mins[8]));
                    int maxidx = BarDiameters.IndexOf(Convert.ToInt32(Maxs[8]));
                    int idx = BarDiameters.IndexOf(col.BarDiameter);
                    ColTemp.BarDiameter = BarDiameters[Math.Max(minidx, Math.Min(idx + eps[8], maxidx))];
                }
                if (Activations[9])
                {
                    int minidx = LinkDiameters.IndexOf(Convert.ToInt32(Mins[9]));
                    int maxidx = LinkDiameters.IndexOf(Convert.ToInt32(Maxs[9]));
                    int idx = LinkDiameters.IndexOf(col.BarDiameter);
                    ColTemp.LinkDiameter = LinkDiameters[Math.Max(minidx, Math.Min(idx + eps[9], maxidx))];
                }
                if (Activations[10])
                {
                    List<string> grades = ConcreteGrades.Select(x => x.Name).ToList();
                    int minidx = grades.IndexOf(Mins[10]);
                    int maxidx = grades.IndexOf(Maxs[10]);
                    int idx = grades.IndexOf(col.ConcreteGrade.Name);
                    ColTemp.ConcreteGrade = ConcreteGrades[Math.Max(minidx, Math.Min(idx + eps[10], maxidx))];
                }

                // we check there has been a change in the column properties compared to last iteration (on a limited number of prop at the moment)
                bool b1 = col.LX == ColTemp.LX;
                bool b2 = col.LY == ColTemp.LY;
                bool b3 = col.NRebarX == ColTemp.NRebarX;
                bool b4 = col.NRebarY == ColTemp.NRebarY;
                bool b5 = col.BarDiameter == ColTemp.BarDiameter;
                bool b6 = col.ConcreteGrade.Name == ColTemp.ConcreteGrade.Name;
                if (b1 && b2 && b3 && b4 && b5 && b6) continue;

                // delta f
                var res = Objective(ColTemp); //, CarbonRef, CostRef, Wcost, Fcost, Wcarb, Fcarb, fireMethods);
                double fp = res;
                double Df = fp - f;
                if (Df < 0)
                {
                    col = ColTemp;
                    f += Df;
                }
                else
                {
                    Random rand = new Random();
                    double r = rand.NextDouble();
                    double p = Math.Exp(-Df / T);
                    if (p > r)
                    {
                        col = ColTemp;
                        f += Df;
                    }
                }

                T = Alpha * T;
                t++;

                if (fp < fBest)
                {
                    Console.WriteLine("old obj : {0}", BestCol.Cost);
                    Console.WriteLine("new obj : {0}", ColTemp.Cost);
                    Calculations calc0 = new Calculations(BestCol);
                    Console.WriteLine("old carbon : {0}", calc0.GetEmbodiedCarbon()[2]);
                    calc0 = new Calculations(ColTemp);
                    Console.WriteLine("new carbon : {0}", calc0.GetEmbodiedCarbon()[2]);
                    BestCol = ColTemp.Clone();
                    fBest = fp;

                    Console.WriteLine("iteration {0}", t);
                    TaskAsyncProgress report = new TaskAsyncProgress()
                    {
                        ProgressPercentage = (int)(t * 1.0 / Maxiter * 100),
                        Col = BestCol,
                        Updated = true,
                    };
                    progress.Report(report);
                }
                else if (t % 4 == 0)
                {
                    Console.WriteLine("iteration {0}", t);
                    TaskAsyncProgress report = new TaskAsyncProgress()
                    {
                        ProgressPercentage = (int)(t * 1.0 / Maxiter * 100),
                        Col = BestCol,
                        Updated = false,
                    };
                    progress.Report(report);
                }

            }

            //return col;
            //return column;
        }

        public async Task Optimise_Brute_Force_Async(IProgress<TaskAsyncProgress> progress)
        {
            Column col = column.Clone();

            Calculations calc = new Calculations(col);
            double costRef = calc.GetCost()[3];
            double carbonRef = calc.GetEmbodiedCarbon()[2];
            bool[] fireMethods = new bool[] { true, false, false, false };
            double Wcarb = DriversWeight[1];
            double Wcost = DriversWeight[0];
            double Fcarb = Drivers[1];
            double Fcost = Drivers[0];
            bool square = false;
            double T = T0;
            int t = 0;


            int LX0 = -1;
            int LX1 = -1;
            int LY0 = -1;
            int LY1 = -1;
            int NX0 = -1;
            int NX1 = -1;
            int NY0 = -1;
            int NY1 = -1;
            int phi0 = -1;
            int phi1 = -1;
            int CG0 = -1;
            int CG1 = -1;
            List<Column> possibleDesigns = new List<Column>();
            if(Activations[0])
            {
                LX0 = Convert.ToInt32(Mins[0]) / Convert.ToInt32(Incrs[0]);
                LX1 = Convert.ToInt32(Maxs[0]) / Convert.ToInt32(Incrs[0]);
            }
            if (Activations[1])
            {
                LY0 = Convert.ToInt32(Mins[1]) / Convert.ToInt32(Incrs[1]);
                LY1 = Convert.ToInt32(Maxs[1]) / Convert.ToInt32(Incrs[1]);
            }
            if (Activations[2])
            {
                NX0 = Convert.ToInt32(Mins[2]);
                NX1 = Convert.ToInt32(Maxs[2]);
            }
            if (Activations[3])
            {
                NY0 = Convert.ToInt32(Mins[3]);
                NY1 = Convert.ToInt32(Maxs[3]);
            }
            if (Activations[8])
            {
                phi0 = BarDiameters.IndexOf(Convert.ToInt32(Mins[8]));
                phi1 = BarDiameters.IndexOf(Convert.ToInt32(Maxs[8]));
            }
            if(Activations[10])
            {
                CG0 = ConcreteGrades.IndexOf(ConcreteGrades.First(c => c.Name == Mins[10]));
                CG1 = ConcreteGrades.IndexOf(ConcreteGrades.First(c => c.Name == Maxs[10]));
            }

            if(Sizes.Count > 0)
            {
                for(int i = 0; i < Sizes.Count; i++)
                {
                    for(int nx = NX0; nx <= NX1; nx++)
                    {
                        for(int ny = NY0; ny <= NY1; ny++)
                        {
                            for(int phi = phi0; phi <= phi1; phi++)
                            {
                                for(int cg = CG0; cg <= CG1; cg++)
                                {
                                    Column c = column.Clone();
                                    c.LX = Sizes[i].Item1;
                                    c.LY = Sizes[i].Item2;
                                    c.NRebarX = nx;
                                    c.NRebarY = ny;
                                    c.BarDiameter = BarDiameters[phi];
                                    c.ConcreteGrade = ConcreteGrades[cg];
                                    possibleDesigns.Add(c);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                double incr = Convert.ToDouble(Incrs[0]);
                for (int lx = LX0; lx <= LX1; lx++)
                {
                    for (int ly = LY0; ly <= LY1; ly++)
                    {
                        for (int nx = NX0; nx <= NX1; nx++)
                        {
                            for (int ny = NY0; ny <= NY1; ny++)
                            {
                                for (int phi = phi0; phi <= phi1; phi++)
                                {
                                    for (int cg = CG0; cg <= CG1; cg++)
                                    {
                                        Column c = column.Clone();
                                        c.LX = incr * lx;
                                        c.LY = incr * ly;
                                        c.NRebarX = nx;
                                        c.NRebarY = ny;
                                        c.BarDiameter = BarDiameters[phi];
                                        c.ConcreteGrade = ConcreteGrades[cg];
                                        possibleDesigns.Add(c);
                                    }
                                }
                            }
                        }
                    }
                }
            }


            Column BestCol = possibleDesigns[0];
            double fBest = Objective(col); //, carbonRef, costRef, Wcost, Fcost, Wcarb, Fcarb, fireMethods);

            for (int i = 0; i < possibleDesigns.Count; i++)
            {
                Column ColTemp = possibleDesigns[i];
                // delta f
                var res = Objective(ColTemp); //, carbonRef, costRef, Wcost, Fcost, Wcarb, Fcarb, fireMethods);
                double fp = res;
                if (fp < fBest)
                {
                    BestCol = ColTemp.Clone();
                    fBest = fp;
                }
                T = Alpha * T;
                t++;

                if (t % 4 == 0)
                {
                    Console.WriteLine("iteration {0}", t);
                    TaskAsyncProgress report = new TaskAsyncProgress()
                    {
                        ProgressPercentage = (int)(t * 1.0 / possibleDesigns.Count * 100),
                        Col = BestCol,
                    };
                    progress.Report(report);
                }
            }
        }
        private static List<int> NormalRand(int n, double variance)
        {
            List<int> rands = new List<int>();
            Random rand = new Random();
            for (int i = 0; i < n; i++)
            {
                double randNormal = BoxMuller(rand, variance);
                rands.Add(Convert.ToInt32(Math.Round(randNormal)));
            }
            return rands;
        }

        private static double BoxMuller(Random rand, double variance)
        {
            double u1 = 1.0 - rand.NextDouble();
            double u2 = 1.0 - rand.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2) * Math.Sqrt(variance); //random normal(0,1)
        }

        //private static double Objective(Column c, double carbonRef, double costRef, double Wcost, double Fcost, double Wcarb, double Fcarb, bool[] fireM)
        private double Objective(Column c)
        {
            double Wcarb = DriversWeight[1];
            double Wcost = DriversWeight[0];
            double Fcarb = Drivers[1];
            double Fcost = Drivers[0];
            bool fireCheck0 = false;
            bool fireCheck1 = false;
            bool fireCheck2 = false;
            bool fireCheck3 = false;
            c.MinRebarCheck = c.CheckMinRebarNo();
            if (c.MinRebarCheck ?? false)
            {
                c.MinMaxSteelCheck = c.CheckSteelQtty(c.AllLoads);
                if (c.MinMaxSteelCheck == true)
                {
                    Calculations calc = new Calculations(c);
                    c.SpacingCheck = calc.CheckSpacing();
                    if (c.SpacingCheck == true)
                    {

                        if (fireMethods[0]) fireCheck0 = calc.CheckFireDesignTable(c.AllLoads) ;
                        if (fireMethods[1]) fireCheck1 = calc.CheckFireIsotherm500().Item1;
                        if (fireMethods[2]) fireCheck2 = calc.CheckFireZoneMethod().Item1;
                        if (!fireCheck0 && fireMethods[3])
                        {
                            calc.UpdateFireID(true);
                            fireCheck3 = c.CheckIsInsideFireID(getMde : false);
                        }
                        c.FireCheck = (fireCheck0 || fireCheck1 || fireCheck2 || fireCheck3);
                        if (c.FireCheck == true)
                        {
                            c.GetInteractionDiagram();
                            c.CapacityCheck = c.isInsideCapacity(false);
                           Console.WriteLine(c.CapacityCheck ?? false ? "capacity check : TRUE" : "capacity check : FALSE");
                        }
                    }
                }
            }
            
            double penCapacity = c.CapacityCheck == true ? 1 : (c.CapacityCheck == null ? 5 : 50);
            double penFire = c.FireCheck == true ? 1 : (c.FireCheck == null ? 5 : 10);
            double penSpacing = c.SpacingCheck == true ? 1 : (c.SpacingCheck == null ? 5 : 10);
            double penSteelQtty = c.MinMaxSteelCheck == true ? 1 : (c.MinMaxSteelCheck == null ? 5 : 10);
            double penMinRebar = c.MinRebarCheck == true ? 1 : 10;

            Calculations calc2 = new Calculations(c);
            double f = (Wcarb * Fcarb * calc2.GetEmbodiedCarbon()[2] / CarbonRef + Wcost * Fcost * calc2.GetCost()[3] / CostRef) * penCapacity * penFire * penSpacing * penSteelQtty * penMinRebar;

            c.Cost = f;

            return f;

        }

    }

    public class TaskAsyncProgress
    {
        public Column Col { get; set; }
        public int ProgressPercentage { get; set; }
        public bool Updated { get; set; }
    }
}
