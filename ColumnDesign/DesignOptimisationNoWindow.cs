using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColumnDesigner
{
    public class DesignOptimisationNoWindow
    {
        public Column column;
        double T0 = 3000;
        //double Tf = 1E-5;
        int N = 200;
        int Nceff;
        double alpha = 0.9;
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
        int idx;
        bool AllLoads;
        GroupDesignOptimisation groupdesignopti;
        //double carb0;
        //double cost0;

        Stopwatch clock = new Stopwatch();

        List<Concrete> concreteGrades;
        List<int> barDiameters;
        List<int> linkDiameters;
        
        private BackgroundWorker backgroundWorker1;
        
        int iter = 0;

        public DesignOptimisationNoWindow(GroupDesignOptimisation gdo, int n, int neff, int nceff, bool[] sh, bool[] activ, string[] mins, string[] maxs, string[] incrs, int maxiter, double al, double vari, 
            double[] drivers, double[] driversWeight, List<Concrete> cg, List<int> bd, List<int> ld, bool allLoads)
        {
            this.backgroundWorker1 = new BackgroundWorker();
            InitializeBackgroundWorker();

            Console.WriteLine(n);

            // Initialization
            groupdesignopti = gdo;
            column = groupdesignopti.columns[n];
            idx = neff;

            N = maxiter;
            Nceff = nceff;
            alpha = al;
            variance = vari;
            weights = driversWeight;
            factors = drivers;
            shapes = sh;
            AllLoads = allLoads;

            Wcost = driversWeight[0] / (driversWeight.Sum());
            Wcarb = driversWeight[1] / (driversWeight.Sum());

            activInputs = activ;
            minis = mins;
            maxis = maxs;
            incres = incrs;

            concreteGrades = cg;
            barDiameters = bd;
            linkDiameters = ld;
            
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
            
            while(!(column.CapacityCheck ?? false) || !(column.SpacingCheck ?? false) || !(column.FireCheck ?? false) || !(column.MinMaxSteelCheck ?? false))
            {
                clock.Restart();
                // ---- Async process ----
                column = AsyncOptimisation.Optimise(worker, column, shapes, activInputs, minis, maxis, incres, concreteGrades, barDiameters, linkDiameters, N, T0,
                    weights, factors, alpha, false, variance, AllLoads);
                Console.WriteLine("{0}: capacity = {1}, fire = {2}, spacing = {3}, steel = {4}, time = {5}s", idx, column.CapacityCheck, column.FireCheck, column.SpacingCheck, column.MinMaxSteelCheck,clock.ElapsedMilliseconds/1000.0);
                //--------------------
            }

        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            double[] Carb = column.GetEmbodiedCarbon();
            double carb = Carb[2];

            double[] Costs = column.GetCost();
            double cost = Costs[3];

            groupdesignopti.asyncStatus[idx] = true;
            groupdesignopti.GroupOptiR.PreProcessPB.Value += 100 / (Nceff * 1.0);
            int compDone = groupdesignopti.asyncStatus.Where(x => x).Count();
            int compTot = groupdesignopti.asyncStatus.Count();
            groupdesignopti.GroupOptiR.ProgressInfoTB.Text = string.Format("{0}/{1}", compDone, compTot);

            // remaining time estimation
            double time = Math.Round(groupdesignopti.watch.ElapsedMilliseconds / 1000.0);
            double timePerComp = time / compDone;
            double timeEstimation = (compTot - compDone) * timePerComp;
            int tEhour = Convert.ToInt32(Math.Truncate(timeEstimation / 3600));
            int tEmin = Convert.ToInt32(Math.Truncate((timeEstimation - tEhour*3600) / 60));
            int tEsec = Convert.ToInt32(Math.Truncate(timeEstimation - tEhour * 3600 - tEmin * 60));
            if (tEhour > 0)
                groupdesignopti.GroupOptiR.ReminaingTimeTB.Text = string.Format("{0} h {1} min {2} s", tEhour, tEmin, tEsec);
            else if(tEmin > 0)
                groupdesignopti.GroupOptiR.ReminaingTimeTB.Text = string.Format("{0} min {1} s", tEmin, tEsec);
            else
                groupdesignopti.GroupOptiR.ReminaingTimeTB.Text = string.Format("{0} s", tEsec);

            int tHour = Convert.ToInt32(Math.Truncate(time / 3600));
            int tMin = Convert.ToInt32(Math.Truncate((time - tHour * 3600) / 60));
            int tSec = Convert.ToInt32(Math.Truncate(time - tHour * 3600 - tMin * 60));
            if (tHour > 0)
                groupdesignopti.GroupOptiR.EllapsedTimeTB.Text = string.Format("{0} h {1} min {2} s", tHour, tMin, tSec);
            else if (tMin > 0)
                groupdesignopti.GroupOptiR.EllapsedTimeTB.Text = string.Format("{0} min {1} s", tMin, tSec);
            else
                groupdesignopti.GroupOptiR.EllapsedTimeTB.Text = string.Format("{0} s", tSec);

            if (groupdesignopti.asyncStatus.Where(b => b == false).Count() == 0)
                groupdesignopti.RunAsync();

        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //object[] report = e.UserState as object[];
            Column[] report = e.UserState as Column[];
            iter++;
            //Column BestCol = report[2] as Column;
            Column BestCol = report[2];

            column = BestCol;
            
        }
        
    }
}
