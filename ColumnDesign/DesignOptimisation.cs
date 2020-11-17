using ColumnDesignCalc;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ColumnDesign
{
    enum OptiDriver { Cost, Carbon };

    public class DesignOptimisation
    {
        public Column column;
        double T0 = 3000;
        //double Tf = 1E-5;
        int N = 200;
        double alpha = 0.9;
        bool square;
        double variance;
        public bool newDesign;
        //double Fcost;
        //double Fcarb;
        double Wcost;
        double Wcarb;
        bool[] shapes;
        bool[] activInputs;
        string[] minis;
        string[] maxis;
        string[] incres;
        double[] weights;
        double[] factors;
        bool[] fireMethods;
        int mode = 0;

        double carb0;
        double cost0;

        ViewModel model;
        BitmapImage yes = new BitmapImage(new Uri(@"Resources/Yes.png", UriKind.Relative));
        BitmapImage no = new BitmapImage(new Uri(@"Resources/No.png", UriKind.Relative));
        BitmapImage question = new BitmapImage(new Uri(@"Resources/Question.png", UriKind.Relative));
        OptimisationResults OptiR;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;

        SeriesCollection objectiveValues;
        int iter = 0;

        public DesignOptimisation(ViewModel vm, bool[] sh, bool[] activ, string[] mins, string[] maxs, string[] incrs, 
            int maxiter, double al, double vari, double[] drivers, double[] driversWeight, bool sq, bool[] fmethods, int m = 0)
        {
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            InitializeBackgroundWorker();

            // Initialization
            column = vm.SelectedColumn;

            N = maxiter;
            alpha = al;
            square = sq;
            variance = vari;
            weights = driversWeight;
            factors = drivers;
            shapes = sh;
            fireMethods = fmethods;

            Wcost = driversWeight[0] / (driversWeight.Sum());
            Wcarb = driversWeight[1] / (driversWeight.Sum());

            activInputs = activ;
            minis = mins;
            maxis = maxs;
            incres = incrs;
            model = vm;
            mode = m;

            objectiveValues = new SeriesCollection();
            
            objectiveValues.Add(new LineSeries()
            {
                Title = "Objective",
                LineSmoothness = 0,
                StrokeThickness = 3,
                Values = new ChartValues<ObservablePoint>(),
                ScalesYAt = 0
            });
            /*objectiveValues.Add(new LineSeries()
            {
                Title = "Cost (£)",
                LineSmoothness = 0,
                StrokeThickness = 3,
                Values = new ChartValues<ObservablePoint>(),
                ScalesYAt = 1
            });
            objectiveValues.Add(new LineSeries()
            {
                Title = "Carbon (kg CO2)",
                LineSmoothness = 0,
                StrokeThickness = 3,
                Values = new ChartValues<ObservablePoint>(),
                ScalesYAt = 2

            });*/

            Calculations calc = new Calculations(column);
            
            OptiR = new OptimisationResults
            {
                DataContext = this,
                SizeToContent = System.Windows.SizeToContent.Height,
                Owner = Application.Current.MainWindow
            };

            OptiR.OptiChart.AxisY.Add(new Axis
            {
                Foreground = System.Windows.Media.Brushes.DodgerBlue,
                Title = "Objective"
            });
            /*OptiR.OptiChart.AxisY.Add(new Axis
            {
                Foreground = System.Windows.Media.Brushes.IndianRed,
                Title = "Cost (£)",
                Position = AxisPosition.RightTop
            });
            OptiR.OptiChart.AxisY.Add(new Axis
            {
                Foreground = System.Windows.Media.Brushes.DarkOliveGreen,
                Title = "Carbon (kg CO2)",
                Position = AxisPosition.RightTop
            });*/

            OptiR.LXtb.Text = column.LX.ToString();
            OptiR.LYtb.Text = column.LY.ToString();
            OptiR.Dtb.Text = column.Diameter.ToString();
            OptiR.Radiustb.Text = column.Radius.ToString();
            OptiR.Edgestb.Text = column.Edges.ToString();
            OptiR.NXtb.Text = column.NRebarX.ToString();
            OptiR.NYtb.Text = column.NRebarY.ToString();
            OptiR.NCirctb.Text = column.NRebarCirc.ToString();
            OptiR.BarDtb.Text = column.BarDiameter.ToString();
            OptiR.LinkDtb.Text = column.LinkDiameter.ToString();
            OptiR.CGtb.Text = column.ConcreteGrade.Name;

            OptiR.SVtb.Text = Math.Round(column.SteelVol() / 1e3).ToString();
            OptiR.CVtb.Text = Math.Round(column.ConcreteVol() / 1e9, 2).ToString();

            double[] Carb = calc.GetEmbodiedCarbon();
            OptiR.CCtb.Text = Math.Round(Carb[0]).ToString();
            OptiR.SCtb.Text = Math.Round(Carb[1]).ToString();
            carb0 = Carb[2];
            OptiR.TotCtb.Text = Math.Round(carb0).ToString();

            double[] Costs = calc.GetCost();
            OptiR.CCosttb.Text = Math.Round(Costs[0]).ToString();
            OptiR.SCosttb.Text = Math.Round(Costs[1]).ToString();
            OptiR.FCosttb.Text = Math.Round(Costs[2]).ToString();
            cost0 = Costs[3];
            OptiR.TotCosttb.Text = Math.Round(cost0).ToString();

            OptiR.CapaImage.Source = (column.isInsideCapacity()) ? yes : no;
            OptiR.FireImage.Source = (calc.CheckFireDesignTable()) ? yes : no;
            OptiR.SpacImage.Source = (column.CheckSpacing()) ? yes : no;
            OptiR.SteelImage.Source = (column.CheckSteelQtty()) ? yes : no;
            OptiR.RebarImage.Source = (column.CheckMinRebarNo()) ? yes : no;

            OptiR.Show();

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
            AsyncOptimisation.Optimise(worker, column, shapes, activInputs, minis, maxis, incres, model.ColumnCalcs.ConcreteGrades, model.BarDiameters, model.LinkDiameters, N, T0,
                weights, factors, alpha, square, variance, fireMethods: fireMethods);
            //--------------------
            
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Calculations calc = new Calculations(column);
            OptiR.OptiLXtb.Text = column.LX.ToString();
            OptiR.OptiLYtb.Text = column.LY.ToString();
            OptiR.OptiDtb.Text = column.Diameter.ToString();
            OptiR.OptiRadiustb.Text = column.Radius.ToString();
            OptiR.Edgestb.Text = column.Edges.ToString();
            OptiR.OptiNXtb.Text = column.NRebarX.ToString();
            OptiR.OptiNYtb.Text = column.NRebarY.ToString();
            OptiR.OptiNCirctb.Text = column.NRebarCirc.ToString();
            OptiR.OptiBarDtb.Text = column.BarDiameter.ToString();
            OptiR.OptiLinkDtb.Text = column.LinkDiameter.ToString();
            OptiR.OptiCGtb.Text = column.ConcreteGrade.Name;

            OptiR.OptiSVtb.Text = Math.Round(column.SteelVol() / 1e3).ToString();
            OptiR.OptiCVtb.Text = Math.Round(column.ConcreteVol() / 1e9, 2).ToString();

            double[] Carb = calc.GetEmbodiedCarbon();
            OptiR.OptiCCtb.Text = Math.Round(Carb[0]).ToString();
            OptiR.OptiSCtb.Text = Math.Round(Carb[1]).ToString();
            double carb = Carb[2];
            double gaincarb = Math.Round((carb - carb0) / carb0 * 100);
            OptiR.OptiTotCtb.Text = Math.Round(carb).ToString() + " (" + gaincarb + "%)";

            double[] Costs = calc.GetCost();
            OptiR.OptiCCosttb.Text = Math.Round(Costs[0]).ToString();
            OptiR.OptiSCosttb.Text = Math.Round(Costs[1]).ToString();
            OptiR.OptiFCosttb.Text = Math.Round(Costs[2]).ToString();
            double cost = Costs[3];
            double gain = Math.Round((cost - cost0) / cost0 * 100);
            OptiR.OptiTotCosttb.Text = Math.Round(cost).ToString() + " (" + gain + "%)";
            
            OptiR.OptiCapaImage.Source = (column.CapacityCheck == true) ? yes : ((column.CapacityCheck == null) ? question : no); //(column.isInsideCapacity()) ? yes : no;
            OptiR.OptiFireImage.Source = (column.FireCheck == true) ? yes : ((column.FireCheck == null) ? question : no);  //(column.CheckFire()) ? yes : no;
            OptiR.OptiSpacImage.Source = (column.SpacingCheck == true) ? yes : ((column.SpacingCheck == null) ? question : no);  //(column.CheckSpacing()) ? yes : no;
            OptiR.OptiSteelImage.Source = (column.MinMaxSteelCheck == true) ? yes : ((column.MinMaxSteelCheck == null) ? question : no);  //(column.CheckSteelQtty()) ? yes : no;
            OptiR.OptiRebarImage.Source = (column.MinRebarCheck == true) ? yes : ((column.MinRebarCheck == null) ? question : no);  //(column.CheckMinRebarNo()) ? yes : no;

            OptiR.AcceptBtn.IsEnabled = true;
            OptiR.KeepExistingBtn.IsEnabled = true;

            OptiR.Hide();
            OptiR.ShowDialog();

            if (this.newDesign)
            {
                model.SelectedColumn = this.column;
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //object[] report = e.UserState as object[];
            Column[] report = e.UserState as Column[];
            iter++;
            //Column Col = report[0] as Column;
            Column Col = report[0];
            //double f = Convert.ToDouble(report[1]);
            double f = Col.Cost;
            //Column BestCol = report[2] as Column;
            Column BestCol = report[2] as Column;
            //double fBest = Convert.ToDouble(report[3]);
            double fBest = BestCol.Cost;
            
            column = BestCol;

            Calculations calc = new Calculations(column);
            OptiR.OptiLXtb.Text = column.LX.ToString();
            OptiR.OptiLYtb.Text = column.LY.ToString();
            OptiR.OptiDtb.Text = column.Diameter.ToString();
            OptiR.OptiRadiustb.Text = column.Radius.ToString();
            OptiR.OptiEdgestb.Text = column.Edges.ToString();
            OptiR.OptiNXtb.Text = column.NRebarX.ToString();
            OptiR.OptiNYtb.Text = column.NRebarY.ToString();
            OptiR.OptiNCirctb.Text = column.NRebarCirc.ToString();
            OptiR.OptiBarDtb.Text = column.BarDiameter.ToString();
            OptiR.OptiLinkDtb.Text = column.LinkDiameter.ToString();
            OptiR.OptiCGtb.Text = column.ConcreteGrade.Name;

            OptiR.OptiSVtb.Text = Math.Round(column.SteelVol() / 1e3).ToString();
            OptiR.OptiCVtb.Text = Math.Round(column.ConcreteVol() / 1e9, 2).ToString();

            double[] Carb = calc.GetEmbodiedCarbon();
            OptiR.OptiCCtb.Text = Math.Round(Carb[0]).ToString();
            OptiR.OptiSCtb.Text = Math.Round(Carb[1]).ToString();
            double carb = Carb[2];
            double gaincarb = Math.Round((carb - carb0) / carb0 * 100);
            OptiR.OptiTotCtb.Text = Math.Round(carb).ToString() + " (" + gaincarb + "%)";

            double[] Costs = calc.GetCost();
            OptiR.OptiCCosttb.Text = Math.Round(Costs[0]).ToString();
            OptiR.OptiSCosttb.Text = Math.Round(Costs[1]).ToString();
            OptiR.OptiFCosttb.Text = Math.Round(Costs[2]).ToString();
            double cost = Costs[3];
            double gaincost = Math.Round((cost - cost0) / cost0 * 100);
            OptiR.OptiTotCosttb.Text = Math.Round(cost).ToString() + " (" + gaincost + "%)";

            OptiR.OptiCapaImage.Source = (column.CapacityCheck == true) ? yes : ((column.CapacityCheck == null) ? question : no); //(column.isInsideCapacity()) ? yes : no;
            OptiR.OptiFireImage.Source = (column.FireCheck == true) ? yes : ((column.FireCheck == null) ? question : no);  //(column.CheckFire()) ? yes : no;
            OptiR.OptiSpacImage.Source = (column.SpacingCheck == true) ? yes : ((column.SpacingCheck == null) ? question : no);  //(column.CheckSpacing()) ? yes : no;
            OptiR.OptiSteelImage.Source = (column.MinMaxSteelCheck == true) ? yes : ((column.MinMaxSteelCheck == null) ? question : no);  //(column.CheckSteelQtty()) ? yes : no;
            OptiR.OptiRebarImage.Source = (column.MinRebarCheck == true) ? yes : ((column.MinRebarCheck == null) ? question : no);  //(column.CheckMinRebarNo()) ? yes : no;

            objectiveValues[0].Values.Add(new ObservablePoint(iter, f));
            //objectiveValues[1].Values.Add(new ObservablePoint(iter, Col.GetCost()[2]));
            //objectiveValues[2].Values.Add(new ObservablePoint(iter, Col.GetEmbodiedCarbon()[2]));

            OptiR.OptiChart.Series = objectiveValues;

            OptiR.TempTB.Text = " N = " + iter + "\n T = " + SetSigFigs((T0 * Math.Pow(alpha, iter)),3).ToString();
            OptiR.ObjTB.Text = " f = " + SetSigFigs(f,3) + "\n fBest = " + SetSigFigs(fBest,3);
        }

        public static double SetSigFigs(double d, int digits)
        {
            if (d == 0)
                return 0;

            decimal scale = (decimal)Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);

            return (double)(scale * Math.Round((decimal)d / scale, digits));
        }
    }
}
