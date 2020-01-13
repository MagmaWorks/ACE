using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.Windows;
using GenericViewer;
using System.Collections.ObjectModel;
using Rhino.Geometry;
using System.Windows.Media;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

namespace ColumnDesign
{
    public class GroupDesignOptimisation : ViewModelBase
    {
        public List<Column> columns;
        double globT0 = 10000;
        int maxGlobIter = 200;
        public int minNd;
        public int maxNd;
        double alpha = 0.9;
        double variance = 1;
        //double Fcost;
        //double Fcarb;
        double Wcost;
        double Wcarb;
        double[] weights;
        double[] factors;
        string[] colNames;
        int[][] bestDesign;
        int[] indices;
        ViewModel model;
        DesignOptimisationNoWindow[] DONs;
        public GroupOptimisationResults GroupOptiR;
        private BackgroundWorker backgroundWorker1;
        OptiState InitialState;
        public Stopwatch watch;

        List<Column>[] bestDesigns;
        public List<Column>[] BestDesigns
        {
            get { return bestDesigns; }
            set { bestDesigns = value; RaisePropertyChanged(nameof(BestDesigns)); }
        }

        List<Column> currentDesigns;
        public List<Column> CurrentDesigns
        {
            get { return currentDesigns; }
            set { currentDesigns = value; RaisePropertyChanged(nameof(CurrentDesigns)); }
        }

        List<OptiDesignResults> resultsTable;
        public List<OptiDesignResults> ResultsTable
        {
            get { return resultsTable; }
            set { resultsTable = value; RaisePropertyChanged(nameof(ResultsTable)); }
        }

        ObservableCollection<OptiResultsByNd> resultsTableByNd;
        public ObservableCollection<OptiResultsByNd> ResultsTableByNd
        {
            get { return resultsTableByNd; }
            set { resultsTableByNd = value; RaisePropertyChanged(nameof(ResultsTableByNd)); }
        }
        
        double bestNbTot;
        public double BestNbTot
        {
            get { return bestNbTot; }
            set { bestNbTot = value; RaisePropertyChanged(nameof(BestNbTot)); }
        }

        double bestObjectiveTot;
        public double BestObjectiveTot
        {
            get { return bestObjectiveTot; }
            set { bestObjectiveTot = value; RaisePropertyChanged(nameof(BestObjectiveTot)); }
        }

        public List<GeometricalViewModel> Columns3DViews;

        GeometricalViewModel columns3DView;
        public GeometricalViewModel Columns3DView
        {
            get { return columns3DView; }
            set { columns3DView = value; RaisePropertyChanged(nameof(Columns3DView)); }
        }

        List<Keys> keys;
        public List<Keys> Keys
        {
            get { return keys; }
            set { keys = value; RaisePropertyChanged(nameof(Keys)); }
        }

        public double[] CarbonCosts;
        public double[] CostCosts;

        SeriesCollection objectiveValues;

        public Func<double, string> Formatter { get; set; }
        public double Base { get; set; }

        int iter = 0;
        int[] iterTot;

        public bool[] asyncStatus;

        bool UseJson;
        bool Parallel;
        bool[] shapes;
        bool AllLoads;

        Color[] colors = new Color[]
        {
            Color.FromArgb(255,200,50,50),
            Color.FromArgb(255,50,200,50),
            Color.FromArgb(255,50,50,200),
            Color.FromArgb(255,50,50,50),
            Color.FromArgb(255,200,50,200),
            Color.FromArgb(255,200,200,50),
            Color.FromArgb(255,50,200,200),
            Color.FromArgb(255,100,50,200),
            Color.FromArgb(255,50,100,200),
            Color.FromArgb(255,200,50,100),
            Color.FromArgb(255,200,100,50),
            Color.FromArgb(255,50,200,100),
            Color.FromArgb(255,100,200,50),
        };

        public GroupDesignOptimisation(ViewModel vm, bool[] sh, bool[] activs, string[] mins, string[] maxs, string[] incrs, int globmaxiter, int indmaxiter, double globalpha, double indalpha, 
            double globvari, double indvari, double globTinit, double indTinit, double[] driversWeight, double[] drivers, int Ndmin, int Ndmax, double sample, bool useJson, bool parallel, bool allLoads)
        {
            this.backgroundWorker1 = new BackgroundWorker();
            InitializeBackgroundWorker();

            // Initialization
            columns = vm.MyColumns.Select(x => x.Clone()).ToList();
            columns.RemoveAt(0);

            maxGlobIter = globmaxiter; // maxiter;
            alpha = globalpha; // al;
            weights = driversWeight;
            factors = drivers;
            minNd = Ndmin;
            maxNd = Ndmax;
            globT0 = globTinit;
            iterTot = new int[maxNd - minNd+1];
            BestDesigns = new List<Column>[maxNd - minNd + 1];
            bestDesign = new int[maxNd - minNd + 1][];
            UseJson = useJson;
            Parallel = true; // parallel;
            shapes = sh;
            AllLoads = allLoads;

            resultsTableByNd = new ObservableCollection<OptiResultsByNd>();
            for (int i = Ndmin; i <= Ndmax; i++)
                resultsTableByNd.Add(new OptiResultsByNd() { Nd = i });

            Wcost = driversWeight[0] / (driversWeight.Sum());
            Wcarb = driversWeight[1] / (driversWeight.Sum());

            model = vm;
            
            // Window for results display
            GroupOptiR = new GroupOptimisationResults
            {
                DataContext = this,
                SizeToContent = System.Windows.SizeToContent.Height,
                Owner = Application.Current.MainWindow
            };

            GroupOptiR.GroupOptiChart.AxisY.Add(new Axis
            {
                Foreground = System.Windows.Media.Brushes.DodgerBlue,
                Title = "Objective"
            });

            Base = 10;

            objectiveValues = new SeriesCollection();
            
            for (int i = 0; i <= maxNd - minNd; i++)
            {
                objectiveValues.Add(new LineSeries()
                {
                    Title = "Objective",
                    LineSmoothness = 0,
                    StrokeThickness = 2,
                    Values = new ChartValues<ObservablePoint>(),
                    ScalesYAt = 0,
                    PointGeometry = null
                });
            }

            GroupOptiR.Show();

            // Individual optimisation of the columns

            int Nc = columns.Count;
            int Nceff = Convert.ToInt32(Nc * sample);

            columns = columns.OrderByDescending(c => c.SelectedLoad.P).ToList(); // the columns are ordered according to their axial load (considering then design is mainly depending on axial load).
            
            watch = Stopwatch.StartNew();
            //double time;
            
            indices = new int[Nceff];
            for(int i = 0; i < Nceff; i++)
                indices[i] = Convert.ToInt32(i * (Nc - 1) / (Nceff - 1));

            if(parallel)
            {
                if (!UseJson)
                {
                    DONs = new DesignOptimisationNoWindow[Nceff];
                    asyncStatus = new bool[Nceff];
                    for (int i = 0; i < Nceff; i++)
                    {
                        DONs[i] = new DesignOptimisationNoWindow(this, indices[i], i, Nceff, shapes, activs, mins, maxs, incrs, indmaxiter, indalpha, indvari, drivers, driversWeight,
                            model.ConcreteGrades, model.BarDiameters, model.LinkDiameters, AllLoads);
                    }
                }
                else
                {
                    RunAsync();
                }
            }
            else
            {
                Stopwatch clock = new Stopwatch();
                for (int i = 0; i < Nceff; i++)
                {
                    clock.Restart();
                    Column c = columns[indices[i]];
                    c = AsyncOptimisation.Optimise(c, activs, mins, maxs, incrs, model.ConcreteGrades, model.BarDiameters, model.LinkDiameters, indmaxiter, indTinit,
                            weights, factors, alpha, variance);
                    Console.WriteLine("{0}: capacity = {1}, fire = {2}, spacing = {3}, steel = {4}, time = {5}s", i, c.CapacityCheck, c.FireCheck,
                        c.SpacingCheck, c.MinMaxSteelCheck, clock.ElapsedMilliseconds / 1000.0);
                }
            }
            
            /*List<Task> tasks = new List<Task>();
            for (int i = 0; i < Nceff; i++)
            {
                int j = Convert.ToInt32(i*(Nc-1)/(Nceff-1));
                Console.WriteLine("Column {0}", j);
                Column col = columns[j];
                var t = Task.Factory.StartNew(() =>
                {
                    var column = AsyncOptimisation.Optimise(col, activs, mins, maxs, incrs, model.ConcreteGrades, model.BarDiameters, model.LinkDiameters, 
                        indmaxiter, indTinit, weights, factors, alpha, variance);
                    Console.WriteLine("{0} : capacity = {1}, fire = {2}, spacing = {2}", j, column.CapacityCheck, column.FireCheck, column.SpacingCheck);
                    columns[j] = col;
                });
                tasks.Add(t);
            }

            Task.Factory.ContinueWhenAll(tasks.ToArray(),
              result =>
              {
                  time = watch.ElapsedMilliseconds;
                  Console.WriteLine("Total time ellapsed : {0}", Math.Round(time/1000.0));
                  RunAsync();
              });*/

        }

        public void RunAsync()
        {
            if(!UseJson)
            {
                double time = Math.Round(watch.ElapsedMilliseconds / 1000.0);
                Console.WriteLine("Calculation of individual optimisations : {0} s", time);
                if (Parallel)
                {
                    for (int i = 0; i < indices.Length; i++)
                        columns[indices[i]] = DONs[i].column;
                }

                TextWriter writer = null;
                DirectoryInfo di = new DirectoryInfo(string.Format("C:\\Users\\Grégoire Corre\\Documents\\ColumnDesign\\Temp"));

                string cParams = di.FullName + "\\columnsOpti.json";
                try
                {
                    var settings = new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    };
                    var contentsToWriteToFile = JsonConvert.SerializeObject(columns, settings);
                    writer = new StreamWriter(cParams, false);
                    writer.Write(contentsToWriteToFile);
                }
                finally
                {
                    if (writer != null)
                        writer.Close();
                }
            }
            else
            {
                TextReader reader = null;
                string filePath = "C:\\Users\\Grégoire Corre\\Documents\\ColumnDesign\\Temp\\columnsOpti.json";
                
                try
                {
                    var settings = new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    };
                    reader = new StreamReader(filePath);
                    var fileContents = reader.ReadToEnd();
                    columns = JsonConvert.DeserializeObject<List<Column>>(fileContents, settings);
                    List<int> indicesList = new List<int>();
                    //for (int i = 0; i < columns.Count; i++)
                    //    if (columns[i]?.diagramFaces != null)
                    //        indicesList.Add(i);
                    //indices = indicesList.ToArray();
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
            }

            // Async process 
            backgroundWorker1.RunWorkerAsync();
        }

        private void InitializeBackgroundWorker()
        {
            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
            backgroundWorker1.WorkerReportsProgress = true;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            // ---- Async process ----
            AsyncOptimisation.OptimiseGroup(worker, columns, indices, minNd, maxNd, maxGlobIter, globT0, weights, factors, alpha, variance,
                model.ConcreteGrades, model.BarDiameters, model.LinkDiameters, InitialState, AllLoads);
            //--------------------
            Console.WriteLine("Job done.");
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //GroupOptiR.RepeatButton.IsEnabled = true;
            GroupOptiR.ViewMapButton.IsEnabled = true;
            GroupOptiR.CloseButton.IsEnabled = true;
            GroupOptiR.DesignResultsTable.ItemsSource = resultsTableByNd;

            var res = GetTotals();

            GroupOptiR.ConcreteVol.Text = Math.Round(res.Item1/1e9).ToString();
            GroupOptiR.SteelVol.Text = Math.Round(res.Item2/1e9).ToString();
            GroupOptiR.Carbon.Text = Math.Round(res.Item3/1e3).ToString();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            object[] report = e.UserState as object[];
            double fBest = Convert.ToDouble(report[4]);
            double f = Convert.ToDouble(report[5]);
            colNames = report[7] as string[];
            int Nd = Convert.ToInt32(report[8]);
            iter = Convert.ToInt32(report[9]);
            iterTot[Nd-minNd]++;
            BestDesigns[Nd - minNd] = report[0] as List<Column>;
            bestDesign[Nd - minNd] = report[2] as int[];
            GroupOptiR.SampleTB.Text = Convert.ToInt32(report[10]).ToString();
            //GroupOptiR.BestDesignInfo.ItemsSource = BestDesigns[Nd - minNd];
            object[,] results = report[6] as object[,];
            
            ObservableCollection<OptiDesignResults>  resultsTable0 = new ObservableCollection<OptiDesignResults>();
            for(int i=0; i < Nd; i++)
            {
                resultsTable0.Add(new OptiDesignResults()
                {
                    index = i,
                    col = BestDesigns[Nd - minNd][i].Name,
                    LX = Convert.ToInt32(BestDesigns[Nd - minNd][i].LX),
                    LY = Convert.ToInt32(BestDesigns[Nd - minNd][i].LY),
                    bestObj = Convert.ToDouble(results[i,0]),
                    bestNb = Convert.ToInt32(results[i, 1]),
                    bestObjTot = Convert.ToDouble(results[i, 0]) * Convert.ToInt32(results[i, 1]),
                });
            }
            
            double bestNbTot0 = 0;
            double bestObjectiveTot0 = 0;
            for(int i = 0; i < results.GetLength(0); i++)
            {
                bestNbTot0 += resultsTable0[i].bestNb;
                bestObjectiveTot0 += resultsTable0[i].bestObjTot;

                resultsTable0[i].bestObj = Math.Round(resultsTable0[i].bestObj, 2);
                resultsTable0[i].bestObjTot = Math.Round(resultsTable0[i].bestObjTot, 2);
            }
            resultsTableByNd[Nd - minNd].BestNbTot = bestNbTot0;
            resultsTableByNd[Nd - minNd].BestObjectiveTot = Math.Round(bestObjectiveTot0, 2);
            resultsTableByNd[Nd - minNd].DesignResults = resultsTable0;
            
            objectiveValues[Nd-minNd].Values.Add(new ObservablePoint(iterTot[Nd - minNd], f));
            GroupOptiR.GroupOptiChart.Series = objectiveValues;

            // Check if we need to update lower Nd results
            int effDesign = 0;
            for (int i = 0; i < Nd; i++)
            {
                if (resultsTable0[i].bestNb != 0) effDesign++;
            }
            if (effDesign < Nd && effDesign >= minNd)
            {
                for(int j = effDesign; j < Nd; j++)
                {
                    if (resultsTableByNd[Nd - minNd].BestObjectiveTot < resultsTableByNd[j - minNd].BestObjectiveTot)
                    {
                        ObservableCollection<OptiDesignResults> resultsTable1 = new ObservableCollection<OptiDesignResults>();
                        int k = 0;
                        for (int i = 0; i < Nd; i++)
                        {
                            if (resultsTable0[i].bestNb != 0)
                            {
                                resultsTable1.Add(new OptiDesignResults()
                                {
                                    index = k,
                                    col = resultsTable0[i].col,
                                    LX = resultsTable0[i].LX,
                                    LY = resultsTable0[i].LY,
                                    bestObj = resultsTable0[i].bestObj,
                                    bestNb = resultsTable0[i].bestNb,
                                    bestObjTot = resultsTable0[i].bestObjTot,
                                });
                                k++;
                            }
                        }
                        resultsTableByNd[j - minNd].DesignResults = resultsTable1;
                        resultsTableByNd[j - minNd].BestObjectiveTot = ResultsTableByNd[Nd - minNd].BestObjectiveTot;
                        resultsTableByNd[j - minNd].BestNbTot = ResultsTableByNd[Nd - minNd].BestNbTot;
                    }
                }

            }

            GroupOptiR.DesignResultsTable.ItemsSource = null;
            GroupOptiR.DesignResultsTable.ItemsSource = resultsTableByNd;

            //RaisePropertyChanged(nameof(ResultsTableByNd));

            GroupOptiR.NdTB.Text = "Nd = " + Nd;
            GroupOptiR.TempTB.Text = " N = " + iterTot[Nd - minNd] + "\n T = " + SetSigFigs((globT0 * Math.Pow(alpha, iter)), 3).ToString();
            GroupOptiR.ObjTB.Text = " f = " + SetSigFigs(f, 3) + "\n fBest = " + SetSigFigs(fBest, 3);

        }

        public double SetSigFigs(double d, int digits)
        {
            if (d == 0)
                return 0;

            decimal scale = (decimal)Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);

            return (double)(scale * Math.Round((decimal)d / scale, digits));
        }

        public void SetColumn3DView()
        {
            Columns3DViews = new List<GeometricalViewModel>();
            Keys = new List<Keys>();
            CarbonCosts = new double[maxNd - minNd + 1];
            CostCosts = new double[maxNd - minNd + 1];

            List<Column> costOderedCols = columns.Where(x => x.Cost != 0).OrderBy(x => x.Cost).ToList();

            for (int n = 0; n <= maxNd - minNd; n++)
            {
                ObservableCollection<Object3D> cols = new ObservableCollection<Object3D>();
                ObservableCollection<Text3D> texts = new ObservableCollection<Text3D>();
                List<Legend>  Legends = new List<Legend>();

                for (int i = 0; i < columns.Count; i++)
                {
                    Column c = columns.First(x => x.Name == colNames[i]); //columns[i];
                    //Column colDesign = columns.First(x => x.Name == colNames[bestDesign[n][i]]);
                    //Column colDesign = costOderedCols.First(x => x.Name == colNames[bestDesign[n][i]]);
                    Column colDesign = costOderedCols[Math.Abs(bestDesign[n][i])];
                    double width = 0;
                    double depth = 0;
                    if (bestDesign[n][i] > 0)
                    {
                        width = colDesign.LX;
                        depth = colDesign.LY;
                    }
                    else
                    {
                        width = colDesign.LY;
                        depth = colDesign.LX;
                    }
                    var col = new Object3D();
                    List<Point3d> points = new List<Point3d>()
                    {
                        new Point3d(-width/2,-depth/2,0),
                        new Point3d(width/2,-depth/2,0),
                        new Point3d(width/2,depth/2,0),
                        new Point3d(-width/2,depth/2,0),
                    };
                    for (int j = 0; j < points.Count; j++)
                    {
                        var X = points[j].X;
                        var Y = points[j].Y;
                        var theta = c.Angle * Math.PI / 180;
                        points[j] = new Point3d(c.Point1.X / 1000 + (X * Math.Cos(theta) - Y * Math.Sin(theta)) / 1000,
                                                c.Point1.Y / 1000 + (X * Math.Sin(theta) + Y * Math.Cos(theta)) / 1000,
                                                c.Point1.Z / 1000);
                    }

                    points.Add(points[0]);

                    col.Curve = new PolylineCurve(points);
                    col.Vector = new Vector3d(0, 0, c.Length / 1000);
                    Column tempCol = BestDesigns[n].First(x => (x.LY == colDesign.LY && x.LX == colDesign.LX));
                    int idx = BestDesigns[n].IndexOf(tempCol);
                    col.Color = colors[idx];
                    cols.Add(col);

                    //Text3D text = new Text3D(c.Name, new Point3D(c.Point2.X + c.LX * 1e-3, c.Point2.Y + c.LY * 1e-3, 0.5 * (c.Point1.Z + c.Point2.Z)));
                    //texts.Add(text);

                }

                for (int i = 0; i < BestDesigns[n].Count; i++)
                {
                    Legends.Add(new Legend()
                    {
                        Color = new SolidColorBrush(colors[i]),
                        Text = "Design " + i + " (" + BestDesigns[n][i].Name + ")"
                    });
                }

                var costs = GetDesignTotalCosts(n);

                CarbonCosts[n] = costs.Item1;
                CostCosts[n] = Math.Round(costs.Item2);

                Keys.Add(new Keys() { legends = Legends });
                Columns3DViews.Add(new GeometricalViewModel() { Objects = cols, Texts = texts });
                
            }
            

        }

        private (double, double, double) GetTotals()
        {
            int n = resultsTableByNd.IndexOf(resultsTableByNd.First(x => x.BestObjectiveTot == resultsTableByNd.Min(r => r.BestObjectiveTot)));
            List<Column> costOderedCols = columns.Where(x => x.Cost != 0).OrderBy(x => x.Cost).ToList();
            double concVol = 0;
            double steelVol = 0;
            double embodiedCarb = 0;
            for (int i = 0; i < columns.Count; i++)
            {
                Column colDesign = costOderedCols[Math.Abs(bestDesign[n][i])];
                concVol += colDesign.GetConcreteArea()*colDesign.Length;
                steelVol += colDesign.GetSteelArea()*colDesign.Length;
                embodiedCarb += colDesign.GetEmbodiedCarbon()[2];
            }

            return (concVol, steelVol, embodiedCarb);
        }

        public void RepeatOptimisation()
        {
            /*iterT = 0;

            int[,] xx = new int[maxNd-minNd,maxNd];
            for(int i = 0; i < bestDesigns.Count; i++)
            {
                xx[i] = colNames.ToList().IndexOf(BestDesigns[i].Name);
            }
            InitialState = new OptiState()
            {
                bestDesign = this.bestDesign,
                bestDesigns = this.bestDesigns,
                fBest = this.bestObjectiveTot,
                x = xx
            };
            backgroundWorker1.RunWorkerAsync();*/
        }

        public (double,double) GetDesignTotalCosts(int n)
        {
            // Total Costs
            double carbon = 0;
            double cost = 0;
            double[] carbonCosts = new double[BestDesigns[n].Count];
            double[] costCosts = new double[BestDesigns[n].Count];
            List<Column> costOderedCols = columns.Where(x => x.Cost != 0).OrderBy(x => x.Cost).ToList();
            for (int i = 0; i < BestDesigns[n].Count; i++)
            {
                carbonCosts[i] = BestDesigns[n][i].GetEmbodiedCarbon()[2];
                costCosts[i] = BestDesigns[n][i].GetCost()[3];
            }
            for (int i = 0; i < bestDesign[n].Length; i++)
            {
                int idx = BestDesigns[n].IndexOf(BestDesigns[n].First(c => c.Name == costOderedCols[Math.Abs(bestDesign[n][i])].Name));
                carbon += carbonCosts[idx];
                cost += costCosts[idx];
            }

            return (carbon, cost);
        }
    }

    public class OptiDesignResults
    {
        public int index { get; set; }
        public string col { get; set; }
        public int LX { get; set; }
        public int LY { get; set; }
        public double bestObj { get; set; }
        public int bestNb { get; set; }
        public double bestObjTot { get; set; }
    }

    public class OptiResultsByNd : ViewModelBase
    {
        public ObservableCollection<OptiDesignResults> designResults = new ObservableCollection<OptiDesignResults>();
        public ObservableCollection<OptiDesignResults> DesignResults
        {
            get { return designResults; }
            set { designResults = value; RaisePropertyChanged(nameof(DesignResults)); }
        }
        public int Nd { get; set; }
        public double BestNbTot {get; set; }
        public double BestObjectiveTot {get; set; }
    }

    public class Legend
    {
        public Brush Color { get; set; }
        public string Text { get; set; }
    }

    public class Keys
    {
        public List<Legend> legends { get; set; }
    }

    public class OptiState
    {
        public double fBest { get; set; }
        public int[] x { get; set; }
        public int[] bestDesign { get; set; }
        public List<Column> bestDesigns { get; set; }
    }
}
