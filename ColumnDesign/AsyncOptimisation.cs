using MathNet.Numerics.LinearAlgebra;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InteractionDiagram3D;
//using InteractionDiagram3D_Windows;
using MWGeometry;
using System.Windows.Media.Media3D;
using ColumnDesignCalc;

namespace ColumnDesign
{
    //public static class AsyncOptimisation
    //{
    //    public static Column Optimise(BackgroundWorker worker, Column column, bool[] shapes, bool[] activ, string[] mins, string[] maxs, string[] incrs, List<Concrete> concreteGrades, List<int> barDiameters,
    //        List<int> linkDiameters, int Nmax, double Tinit, double[] Ws, double[] Fs, double alpha, bool square, double variance = 1, bool allLoads = false, bool[] fireMethods = null)

    //    {
    //        Calculations calc = new Calculations(column);
    //        double costRef = calc.GetCost()[3];
    //        double carbonRef = calc.GetEmbodiedCarbon()[2];

    //        fireMethods = fireMethods ?? new bool[] { true, false, false, false };

    //        double Wcarb = Ws[1];
    //        double Wcost = Ws[0];
    //        double Fcarb = Fs[1];
    //        double Fcost = Fs[0];

    //        var chekcs = Objective(column, carbonRef, costRef, Wcost, Fcost, Wcarb, Fcarb, allLoads, fireMethods);
    //        double f = chekcs.Item1;
    //        column.CapacityCheck = chekcs.Item2;
    //        column.FireCheck = chekcs.Item3;
    //        column.SpacingCheck = chekcs.Item4;
    //        column.MinMaxSteelCheck = chekcs.Item5;
    //        column.MinRebarCheck = chekcs.Item6;

    //        double T = Tinit;
    //        int t = 0;

    //        Column BestCol = column.Clone();
    //        BestCol.Cost = f;
    //        double fBest = f;

    //        List<int> shapesInd = new List<int>();
    //        for (int i = 0; i < shapes.Length; i++)
    //            if (shapes[i]) shapesInd.Add(i);
    //        int shapeIdx = 0;
    //        column.Shape = (shapesInd[shapeIdx] == 0) ? GeoShape.Rectangular : ((shapesInd[shapeIdx] == 1) ? GeoShape.Circular : GeoShape.Polygonal);

    //        if (activ[6])
    //            BestCol.ConcreteGrade = concreteGrades[1];

    //        Column[] report = new Column[4];

    //        while (t < Nmax)
    //        {
    //            int NN = activ.Count();

    //            // update of X
    //            Column ColTemp = column.Clone();
    //            List<int> eps = NormalRand(NN+1, variance);

    //            if (activ[0])
    //                ColTemp.LX = Math.Max(Convert.ToDouble(mins[0]), Math.Min(column.LX + eps[0] * Convert.ToDouble(incrs[0]), Convert.ToDouble(maxs[0])));
    //            if (activ[1])
    //                ColTemp.LY = square ? ColTemp.LX : Math.Max(Convert.ToDouble(mins[1]), Math.Min(column.LY + eps[1] * Convert.ToDouble(incrs[1]), Convert.ToDouble(maxs[1])));
    //            if (activ[2])
    //                ColTemp.NRebarX = Math.Max(Convert.ToInt32(mins[2]), Math.Min(column.NRebarX + eps[2], Convert.ToInt32(maxs[2])));
    //            if (activ[3])
    //                ColTemp.NRebarX = Math.Max(Convert.ToInt32(mins[3]), Math.Min(column.NRebarX + eps[3], Convert.ToInt32(maxs[3])));
    //            if (activ[4])
    //                ColTemp.Diameter = Math.Max(Convert.ToDouble(mins[4]), Math.Min(column.Diameter + eps[4] * Convert.ToDouble(incrs[2]), Convert.ToDouble(maxs[4])));
    //            if (activ[5])
    //                ColTemp.NRebarCirc = Math.Max(Convert.ToInt32(mins[5]), Math.Min(column.NRebarX + eps[5], Convert.ToInt32(maxs[5])));
    //            if (activ[6])
    //                ColTemp.Radius = Math.Max(Convert.ToDouble(mins[6]), Math.Min(column.Radius + eps[6] * Convert.ToDouble(incrs[3]), Convert.ToDouble(maxs[6])));
    //            if (activ[7])
    //                ColTemp.Edges = Math.Max(Convert.ToInt32(mins[7]), Math.Min(column.Edges + eps[7], Convert.ToInt32(maxs[7])));
                
    //            if (activ[8])
    //            {
    //                int minidx = barDiameters.IndexOf(Convert.ToInt32(mins[8]));
    //                int maxidx = barDiameters.IndexOf(Convert.ToInt32(maxs[8]));
    //                int idx = barDiameters.IndexOf(column.BarDiameter);
    //                ColTemp.BarDiameter = barDiameters[Math.Max(minidx, Math.Min(idx + eps[8], maxidx))];
    //            }
    //            if (activ[9])
    //            {
    //                int minidx = linkDiameters.IndexOf(Convert.ToInt32(mins[9]));
    //                int maxidx = linkDiameters.IndexOf(Convert.ToInt32(maxs[9]));
    //                int idx = linkDiameters.IndexOf(column.BarDiameter);
    //                ColTemp.LinkDiameter = linkDiameters[Math.Max(minidx, Math.Min(idx + eps[9], maxidx))];
    //            }
    //            if (activ[10])
    //            {
    //                List<string> grades = concreteGrades.Select(x => x.Name).ToList();
    //                int minidx = grades.IndexOf(mins[10]);
    //                int maxidx = grades.IndexOf(maxs[10]);
    //                int idx = grades.IndexOf(column.ConcreteGrade.Name);
    //                ColTemp.ConcreteGrade = concreteGrades[Math.Max(minidx, Math.Min(idx + eps[10], maxidx))];
    //            }

    //            if(shapesInd.Count > 1)
    //            {
    //                int n = shapesInd.Count;
    //                int xx = shapeIdx + eps[10];
    //                shapeIdx = ((xx >= 0) ? xx % n : Math.Min(0,n - Math.Abs(xx) % n));

    //                ColTemp.Shape = (shapesInd[shapeIdx] == 0) ? GeoShape.Rectangular : ((shapesInd[shapeIdx] == 1) ? GeoShape.Circular : GeoShape.Polygonal);
    //            }

    //            // delta f
    //            var res = Objective(ColTemp, carbonRef, costRef, Wcost, Fcost, Wcarb, Fcarb, allLoads, fireMethods);
    //            ColTemp.CapacityCheck = res.Item2;
    //            ColTemp.FireCheck = res.Item3;
    //            ColTemp.SpacingCheck = res.Item4;
    //            ColTemp.MinMaxSteelCheck = res.Item5;
    //            ColTemp.MinRebarCheck = res.Item6;
    //            double fp = res.Item1;
    //            double Df = fp - f;
    //            if (Df < 0)
    //            {
    //                column = ColTemp;
    //                f += Df;
    //                column.Cost = f;
    //            }
    //            else
    //            {
    //                Random rand = new Random();
    //                double r = rand.NextDouble();
    //                double p = Math.Exp(-Df / T);
    //                if (p > r)
    //                {
    //                    column = ColTemp;
    //                    f += Df;
    //                    column.Cost = f;
    //                }
    //            }
    //            if (fp < fBest)
    //            {
    //                BestCol = ColTemp.Clone();
    //                fBest = fp;
    //                BestCol.Cost = fBest;
    //            }
    //            T = alpha * T;
    //            t++;
    //            //Console.WriteLine(f);
    //            // transmission of results to main thread
    //            report[0] = column;
    //            //report[1] = f;
    //            report[2] = BestCol;
    //            //report[3] = fBest;

    //            worker.ReportProgress(0, report);
    //        }
    //        return column;
    //    }

    //    public static Column Optimise(Column column, bool[] activ, string[] mins, string[] maxs, string[] incrs, List<Concrete> concreteGrades, List<int> barDiameters,
    //        List<int> linkDiameters, int Nmax, double Tinit, double[] Ws, double[] Fs, double alpha, bool square, double variance = 1, bool allLoads = false, bool[] fireMethods = null)

    //    {
    //        Calculations calc = new Calculations(column);
    //        double costRef = calc.GetCost()[3];

    //        double carbonRef = calc.GetEmbodiedCarbon()[2];

    //        fireMethods = fireMethods ?? new bool[] { true, false, false, false };

    //        double Wcarb = Ws[1];
    //        double Wcost = Ws[0];
    //        double Fcarb = Fs[1];
    //        double Fcost = Fs[0];

    //        double f = Objective(column, carbonRef, costRef, Wcost, Fcost, Wcarb, Fcarb, allLoads, fireMethods).Item1;

    //        double T = Tinit;
    //        int t = 0;
            
    //        Column BestCol = column.Clone();
    //        double fBest = f;

    //        if (activ[6])
    //            BestCol.ConcreteGrade = concreteGrades[1];

    //        object[] report = new object[4];

    //        while (t < Nmax)
    //        {
    //            int NN = activ.Count();

    //            // update of X
    //            Column ColTemp = column.Clone();
    //            List<int> eps = NormalRand(NN, variance);

    //            if (activ[0])
    //                ColTemp.LX = Math.Max(Convert.ToDouble(mins[0]), Math.Min(column.LX + eps[0] * Convert.ToDouble(incrs[0]), Convert.ToDouble(maxs[0])));
    //            if (activ[1])
    //                ColTemp.LY = square ? ColTemp.LX : Math.Max(Convert.ToDouble(mins[1]), Math.Min(column.LY + eps[1] * Convert.ToDouble(incrs[1]), Convert.ToDouble(maxs[1])));
    //            if (activ[2])
    //                ColTemp.Diameter = Math.Max(Convert.ToDouble(mins[2]), Math.Min(column.Diameter + eps[2] * Convert.ToDouble(incrs[2]), Convert.ToDouble(maxs[2])));
    //            if (activ[3])
    //                ColTemp.Radius = Math.Max(Convert.ToDouble(mins[3]), Math.Min(column.Radius + eps[3] * Convert.ToDouble(incrs[3]), Convert.ToDouble(maxs[3])));
    //            if (activ[4])
    //                ColTemp.Edges = Math.Max(Convert.ToInt32(mins[4]), Math.Min(column.Edges + eps[4] * Convert.ToInt32(incrs[4]), Convert.ToInt32(maxs[4])));
    //            if (activ[5])
    //                ColTemp.NRebarX = Math.Max(Convert.ToInt32(mins[5]), Math.Min(column.NRebarX + eps[5], Convert.ToInt32(maxs[5])));
    //            if (activ[6])
    //                ColTemp.NRebarX = Math.Max(Convert.ToInt32(mins[6]), Math.Min(column.NRebarX + eps[6], Convert.ToInt32(maxs[6])));
    //            if (activ[7])
    //            {
    //                int minidx = barDiameters.IndexOf(Convert.ToInt32(mins[7]));
    //                int maxidx = barDiameters.IndexOf(Convert.ToInt32(maxs[7]));
    //                int idx = barDiameters.IndexOf(column.BarDiameter);
    //                ColTemp.BarDiameter = barDiameters[Math.Max(minidx, Math.Min(idx + eps[7], maxidx))];
    //            }
    //            if (activ[8])
    //            {
    //                int minidx = linkDiameters.IndexOf(Convert.ToInt32(mins[8]));
    //                int maxidx = linkDiameters.IndexOf(Convert.ToInt32(maxs[8]));
    //                int idx = linkDiameters.IndexOf(column.BarDiameter);
    //                ColTemp.LinkDiameter = linkDiameters[Math.Max(minidx, Math.Min(idx + eps[8], maxidx))];
    //            }
    //            if (activ[9])
    //            {
    //                List<string> grades = concreteGrades.Select(x => x.Name).ToList();
    //                int minidx = grades.IndexOf(mins[9]);
    //                int maxidx = grades.IndexOf(maxs[9]);
    //                int idx = grades.IndexOf(column.ConcreteGrade.Name);
    //                ColTemp.ConcreteGrade = concreteGrades[Math.Max(minidx, Math.Min(idx + eps[9], maxidx))];
    //            }

    //            // delta f
    //            var res = Objective(ColTemp, carbonRef, costRef, Wcost, Fcost, Wcarb, Fcarb, allLoads, fireMethods);
    //            ColTemp.CapacityCheck = res.Item2;
    //            ColTemp.FireCheck = res.Item3;
    //            ColTemp.SpacingCheck = res.Item4;
    //            ColTemp.MinMaxSteelCheck = res.Item5;
    //            ColTemp.MinRebarCheck = res.Item6;
    //            double fp = res.Item1;
    //            double Df = fp - f;
    //            if (Df < 0)
    //            {
    //                column = ColTemp;
    //                f += Df;
    //            }
    //            else
    //            {
    //                Random rand = new Random();
    //                double r = rand.NextDouble();
    //                double p = Math.Exp(-Df / T);
    //                if (p > r)
    //                {
    //                    column = ColTemp;
    //                    f += Df;
    //                }
    //            }
    //            if (fp < fBest)
    //            {
    //                BestCol = ColTemp.Clone();
    //                fBest = fp;
    //            }
    //            T = alpha * T;
    //            t++;
                
    //        }

    //        return column;
    //    }

        
    //    private static List<int> NormalRand(int n, double variance)
    //    {
    //        List<int> rands = new List<int>();
    //        Random rand = new Random();
    //        for (int i = 0; i < n; i++)
    //        {
    //            double randNormal = BoxMuller(rand, variance);
    //            rands.Add(Convert.ToInt32(Math.Round(randNormal)));
    //        }
    //        return rands;
    //    }

    //    private static double BoxMuller(Random rand, double variance)
    //    {
    //        double u1 = 1.0 - rand.NextDouble();
    //        double u2 = 1.0 - rand.NextDouble();
    //        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2) * Math.Sqrt(variance); //random normal(0,1)
    //    }

    //    private static (double,bool?,bool?,bool?,bool?,bool?) Objective(Column c, double carbonRef, double costRef, double Wcost, double Fcost, double Wcarb, double Fcarb, bool allLoads, bool[] fireM)
    //    {
            
            
    //        bool? minRebarCheck = c.CheckMinRebarNo();
    //        bool? minmaxCheck = null;
    //        bool? spacingCheck = null;
    //        bool? fireCheck = null;
    //        bool fireCheck0 = false;
    //        bool fireCheck1 = false;
    //        bool fireCheck2 = false;
    //        bool fireCheck3 = false;
    //        bool? capacityCheck = null;

    //        if (minRebarCheck == true)
    //        {
    //            minmaxCheck = c.CheckSteelQtty();
    //            if(minmaxCheck == true)
    //            {
    //                Calculations calc = new Calculations(c);
    //                spacingCheck = calc.CheckSpacing();
    //                if(spacingCheck == true)
    //                {
                        
    //                    if (fireM[0]) fireCheck0 = calc.CheckFireDesignTable();
    //                    if (fireM[1]) fireCheck1 = calc.CheckFireIsotherm500().Item1;
    //                    if (fireM[2]) fireCheck2 = calc.CheckFireZoneMethod().Item1;
    //                    if (fireM[3])
    //                    {
    //                        calc.UpdateFireID(true);
    //                        fireCheck3 = c.CheckIsInsideFireID();
    //                    }
    //                    fireCheck = (fireCheck0 || fireCheck1 || fireCheck2 || fireCheck3);
    //                    if(fireCheck == true)
    //                    {
    //                        c.GetInteractionDiagram();
    //                        capacityCheck = c.isInsideCapacity(allLoads);
    //                        Console.WriteLine("coucou");
    //                    }
    //                    else
    //                        System.Threading.Thread.Sleep(10);
    //                }
    //                else
    //                    System.Threading.Thread.Sleep(10);
    //            }
    //            else
    //                System.Threading.Thread.Sleep(10);
    //        }
    //        else
    //            System.Threading.Thread.Sleep(10);

    //        double penCapacity =  capacityCheck == true ? 1 : (capacityCheck == null ? 5 : 10);
    //        double penFire =  fireCheck == true ? 1 : (fireCheck == null ? 5 : 10);
    //        double penSpacing =  spacingCheck == true ? 1 : (spacingCheck == null ? 5 : 10);
    //        double penSteelQtty =  minmaxCheck == true ? 1 : (minmaxCheck == null ? 5 : 10);
    //        double penMinRebar = minRebarCheck == true ? 1 : 10;

    //        Calculations calc2 = new Calculations(c);
    //        double f = (Wcarb * Fcarb * calc2.GetEmbodiedCarbon()[2] / carbonRef + Wcost * Fcost * calc2.GetCost()[3] / costRef) * penCapacity * penFire * penSpacing * penSteelQtty;

    //        return (f,capacityCheck,fireCheck,spacingCheck,minmaxCheck,minRebarCheck);

    //    }
    //}

    //public class OptiState
    //{
    //    public double fBest { get; set; }
    //    public int[] x { get; set; }
    //    public int[] bestDesign { get; set; }
    //    public List<Column> bestDesigns { get; set; }
    //}
}
