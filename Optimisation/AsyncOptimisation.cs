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
//using InteractionDiagram3D_Windows;
using ColumnDesignCalc;

namespace Optimisation
{
    public static class AsyncOptimisation
    {
        public static Column Optimise(BackgroundWorker worker, Column column, bool[] shapes, bool[] activ, string[] mins, string[] maxs, string[] incrs, List<Concrete> concreteGrades, List<int> barDiameters,
            List<int> linkDiameters, int Nmax, double Tinit, double[] Ws, double[] Fs, double alpha, bool square, double variance = 1, bool allLoads = false, bool[] fireMethods = null)

        {
            Calculations calc = new Calculations(column);
            double costRef = calc.GetCost()[3];
            double carbonRef = calc.GetEmbodiedCarbon()[2];

            fireMethods = fireMethods ?? new bool[] { true, false, false, false };

            double Wcarb = Ws[1];
            double Wcost = Ws[0];
            double Fcarb = Fs[1];
            double Fcost = Fs[0];

            var chekcs = Objective(column, carbonRef, costRef, Wcost, Fcost, Wcarb, Fcarb, allLoads, fireMethods);
            double f = chekcs.Item1;
            column.CapacityCheck = chekcs.Item2;
            column.FireCheck = chekcs.Item3;
            column.SpacingCheck = chekcs.Item4;
            column.MinMaxSteelCheck = chekcs.Item5;
            column.MinRebarCheck = chekcs.Item6;

            double T = Tinit;
            int t = 0;

            Column BestCol = column.Clone();
            BestCol.Cost = f;
            double fBest = f;

            List<int> shapesInd = new List<int>();
            for (int i = 0; i < shapes.Length; i++)
                if (shapes[i]) shapesInd.Add(i);
            int shapeIdx = 0;
            column.Shape = (shapesInd[shapeIdx] == 0) ? GeoShape.Rectangular : ((shapesInd[shapeIdx] == 1) ? GeoShape.Circular : GeoShape.Polygonal);

            if (activ[6])
                BestCol.ConcreteGrade = concreteGrades[1];

            Column[] report = new Column[4];

            while (t < Nmax)
            {
                int NN = activ.Count();

                // update of X
                Column ColTemp = column.Clone();
                List<int> eps = NormalRand(NN+1, variance);

                if (activ[0])
                    ColTemp.LX = Math.Max(Convert.ToDouble(mins[0]), Math.Min(column.LX + eps[0] * Convert.ToDouble(incrs[0]), Convert.ToDouble(maxs[0])));
                if (activ[1])
                    ColTemp.LY = square ? ColTemp.LX : Math.Max(Convert.ToDouble(mins[1]), Math.Min(column.LY + eps[1] * Convert.ToDouble(incrs[1]), Convert.ToDouble(maxs[1])));
                if (activ[2])
                    ColTemp.NRebarX = Math.Max(Convert.ToInt32(mins[2]), Math.Min(column.NRebarX + eps[2], Convert.ToInt32(maxs[2])));
                if (activ[3])
                    ColTemp.NRebarX = Math.Max(Convert.ToInt32(mins[3]), Math.Min(column.NRebarX + eps[3], Convert.ToInt32(maxs[3])));
                if (activ[4])
                    ColTemp.Diameter = Math.Max(Convert.ToDouble(mins[4]), Math.Min(column.Diameter + eps[4] * Convert.ToDouble(incrs[2]), Convert.ToDouble(maxs[4])));
                if (activ[5])
                    ColTemp.NRebarCirc = Math.Max(Convert.ToInt32(mins[5]), Math.Min(column.NRebarX + eps[5], Convert.ToInt32(maxs[5])));
                if (activ[6])
                    ColTemp.Radius = Math.Max(Convert.ToDouble(mins[6]), Math.Min(column.Radius + eps[6] * Convert.ToDouble(incrs[3]), Convert.ToDouble(maxs[6])));
                if (activ[7])
                    ColTemp.Edges = Math.Max(Convert.ToInt32(mins[7]), Math.Min(column.Edges + eps[7], Convert.ToInt32(maxs[7])));
                
                if (activ[8])
                {
                    int minidx = barDiameters.IndexOf(Convert.ToInt32(mins[8]));
                    int maxidx = barDiameters.IndexOf(Convert.ToInt32(maxs[8]));
                    int idx = barDiameters.IndexOf(column.BarDiameter);
                    ColTemp.BarDiameter = barDiameters[Math.Max(minidx, Math.Min(idx + eps[8], maxidx))];
                }
                if (activ[9])
                {
                    int minidx = linkDiameters.IndexOf(Convert.ToInt32(mins[9]));
                    int maxidx = linkDiameters.IndexOf(Convert.ToInt32(maxs[9]));
                    int idx = linkDiameters.IndexOf(column.BarDiameter);
                    ColTemp.LinkDiameter = linkDiameters[Math.Max(minidx, Math.Min(idx + eps[9], maxidx))];
                }
                if (activ[10])
                {
                    List<string> grades = concreteGrades.Select(x => x.Name).ToList();
                    int minidx = grades.IndexOf(mins[10]);
                    int maxidx = grades.IndexOf(maxs[10]);
                    int idx = grades.IndexOf(column.ConcreteGrade.Name);
                    ColTemp.ConcreteGrade = concreteGrades[Math.Max(minidx, Math.Min(idx + eps[10], maxidx))];
                }

                if(shapesInd.Count > 1)
                {
                    int n = shapesInd.Count;
                    int xx = shapeIdx + eps[10];
                    shapeIdx = ((xx >= 0) ? xx % n : Math.Min(0,n - Math.Abs(xx) % n));

                    ColTemp.Shape = (shapesInd[shapeIdx] == 0) ? GeoShape.Rectangular : ((shapesInd[shapeIdx] == 1) ? GeoShape.Circular : GeoShape.Polygonal);
                }

                // delta f
                var res = Objective(ColTemp, carbonRef, costRef, Wcost, Fcost, Wcarb, Fcarb, allLoads, fireMethods);
                ColTemp.CapacityCheck = res.Item2;
                ColTemp.FireCheck = res.Item3;
                ColTemp.SpacingCheck = res.Item4;
                ColTemp.MinMaxSteelCheck = res.Item5;
                ColTemp.MinRebarCheck = res.Item6;
                double fp = res.Item1;
                double Df = fp - f;
                if (Df < 0)
                {
                    column = ColTemp;
                    f += Df;
                    column.Cost = f;
                }
                else
                {
                    Random rand = new Random();
                    double r = rand.NextDouble();
                    double p = Math.Exp(-Df / T);
                    if (p > r)
                    {
                        column = ColTemp;
                        f += Df;
                        column.Cost = f;
                    }
                }
                if (fp < fBest)
                {
                    BestCol = ColTemp.Clone();
                    fBest = fp;
                    BestCol.Cost = fBest;
                }
                T = alpha * T;
                t++;
                //Console.WriteLine(f);
                // transmission of results to main thread
                report[0] = column;
                //report[1] = f;
                report[2] = BestCol;
                //report[3] = fBest;

                worker.ReportProgress(0, report);
            }
            return column;
        }

        public static Column Optimise(Column column, bool[] activ, string[] mins, string[] maxs, string[] incrs, List<Concrete> concreteGrades, List<int> barDiameters,
            List<int> linkDiameters, int Nmax, double Tinit, double[] Ws, double[] Fs, double alpha, bool square, double variance = 1, bool allLoads = false, bool[] fireMethods = null)

        {
            Calculations calc = new Calculations(column);
            double costRef = calc.GetCost()[3];

            double carbonRef = calc.GetEmbodiedCarbon()[2];

            fireMethods = fireMethods ?? new bool[] { true, false, false, false };

            double Wcarb = Ws[1];
            double Wcost = Ws[0];
            double Fcarb = Fs[1];
            double Fcost = Fs[0];

            double f = Objective(column, carbonRef, costRef, Wcost, Fcost, Wcarb, Fcarb, allLoads, fireMethods).Item1;

            double T = Tinit;
            int t = 0;
            
            Column BestCol = column.Clone();
            double fBest = f;

            if (activ[6])
                BestCol.ConcreteGrade = concreteGrades[1];

            object[] report = new object[4];

            while (t < Nmax)
            {
                int NN = activ.Count();

                // update of X
                Column ColTemp = column.Clone();
                List<int> eps = NormalRand(NN, variance);

                if (activ[0])
                    ColTemp.LX = Math.Max(Convert.ToDouble(mins[0]), Math.Min(column.LX + eps[0] * Convert.ToDouble(incrs[0]), Convert.ToDouble(maxs[0])));
                if (activ[1])
                    ColTemp.LY = square ? ColTemp.LX : Math.Max(Convert.ToDouble(mins[1]), Math.Min(column.LY + eps[1] * Convert.ToDouble(incrs[1]), Convert.ToDouble(maxs[1])));
                if (activ[2])
                    ColTemp.Diameter = Math.Max(Convert.ToDouble(mins[2]), Math.Min(column.Diameter + eps[2] * Convert.ToDouble(incrs[2]), Convert.ToDouble(maxs[2])));
                if (activ[3])
                    ColTemp.Radius = Math.Max(Convert.ToDouble(mins[3]), Math.Min(column.Radius + eps[3] * Convert.ToDouble(incrs[3]), Convert.ToDouble(maxs[3])));
                if (activ[4])
                    ColTemp.Edges = Math.Max(Convert.ToInt32(mins[4]), Math.Min(column.Edges + eps[4] * Convert.ToInt32(incrs[4]), Convert.ToInt32(maxs[4])));
                if (activ[5])
                    ColTemp.NRebarX = Math.Max(Convert.ToInt32(mins[5]), Math.Min(column.NRebarX + eps[5], Convert.ToInt32(maxs[5])));
                if (activ[6])
                    ColTemp.NRebarX = Math.Max(Convert.ToInt32(mins[6]), Math.Min(column.NRebarX + eps[6], Convert.ToInt32(maxs[6])));
                if (activ[7])
                {
                    int minidx = barDiameters.IndexOf(Convert.ToInt32(mins[7]));
                    int maxidx = barDiameters.IndexOf(Convert.ToInt32(maxs[7]));
                    int idx = barDiameters.IndexOf(column.BarDiameter);
                    ColTemp.BarDiameter = barDiameters[Math.Max(minidx, Math.Min(idx + eps[7], maxidx))];
                }
                if (activ[8])
                {
                    int minidx = linkDiameters.IndexOf(Convert.ToInt32(mins[8]));
                    int maxidx = linkDiameters.IndexOf(Convert.ToInt32(maxs[8]));
                    int idx = linkDiameters.IndexOf(column.BarDiameter);
                    ColTemp.LinkDiameter = linkDiameters[Math.Max(minidx, Math.Min(idx + eps[8], maxidx))];
                }
                if (activ[9])
                {
                    List<string> grades = concreteGrades.Select(x => x.Name).ToList();
                    int minidx = grades.IndexOf(mins[9]);
                    int maxidx = grades.IndexOf(maxs[9]);
                    int idx = grades.IndexOf(column.ConcreteGrade.Name);
                    ColTemp.ConcreteGrade = concreteGrades[Math.Max(minidx, Math.Min(idx + eps[9], maxidx))];
                }

                // delta f
                var res = Objective(ColTemp, carbonRef, costRef, Wcost, Fcost, Wcarb, Fcarb, allLoads, fireMethods);
                ColTemp.CapacityCheck = res.Item2;
                ColTemp.FireCheck = res.Item3;
                ColTemp.SpacingCheck = res.Item4;
                ColTemp.MinMaxSteelCheck = res.Item5;
                ColTemp.MinRebarCheck = res.Item6;
                double fp = res.Item1;
                double Df = fp - f;
                if (Df < 0)
                {
                    column = ColTemp;
                    f += Df;
                }
                else
                {
                    Random rand = new Random();
                    double r = rand.NextDouble();
                    double p = Math.Exp(-Df / T);
                    if (p > r)
                    {
                        column = ColTemp;
                        f += Df;
                    }
                }
                if (fp < fBest)
                {
                    BestCol = ColTemp.Clone();
                    fBest = fp;
                }
                T = alpha * T;
                t++;
                
            }

            return column;
        }

        
        //public static void OptimiseGroup(BackgroundWorker worker, List<Column> columns, int[] indices, int Ndmin, int Ndmax, int Nmax, double Tinit, 
        //    double[] Ws, double[] Fs, double alpha, double variance, List<Concrete> concreteGrades, List<int> barDiameters, List<int> linkDiameters, OptiState initialState, bool allLoads, bool[] fireMethods = null)
        //{
        //    int Nc = columns.Count;

        //    var M = Matrix<double>.Build;

        //    // Cost of each interaction diagram
        //    double[] costs = new double[Nc];
        //    double carbonRef = 239; // --> ref carbon for 350x350 col with 3x3 H16 C50/60. //columns[0].GetEmbodiedCarbon()[2]; // this sets a carbon reference value for all the columns
        //    double costRef = 267; //  --> ref cost for 350x350 col with 3x3 H16 C50/60. /columns[0].GetCost()[3]; // this sets a cost reference value for all the columns
        //    double Wcost = Ws[0];
        //    double Wcarb = Ws[1];
        //    double Fcost = Fs[0];
        //    double Fcarb = Fs[1];
        //    int Nceff = indices.Length;
        //    fireMethods = fireMethods ?? new bool[] { true, false, false, false };

        //    for (int i = 0; i < Nceff; i++)
        //        columns[indices[i]].Cost = Objective(columns[i], carbonRef, costRef, Wcost, Fcost, Wcarb, Fcarb, allLoads, fireMethods).Item1;


        //    Column[] optiColumns = new Column[Nceff];
        //    for (int i = 0; i < Nceff; i++)
        //        optiColumns[i] = columns[indices[i]];

        //    //columns = columns.OrderBy(c => c.Cost).ToList();
        //    optiColumns = optiColumns.OrderBy(c => c.Cost).ToArray();

        //    // Construction of the correlation matrix
        //    //Matrix<double> C = M.Dense(Nc, Nc);
        //    Matrix<double> C = M.Dense(Nc, Nceff);

        //    var watch = Stopwatch.StartNew();
        //    double time;
        //    Console.WriteLine("Nceff = {0}", Nceff);
        //    Console.WriteLine("Assembly of correlation matrix has started");
        //    for (int j = 0; j < Nceff; j++)
        //    {
        //        if (optiColumns[j].diagramFaces.Count == 0)
        //            optiColumns[j].GetInteractionDiagram();
        //        List<Tri3D> faces = optiColumns[j].diagramFaces;
        //        List<MWPoint3D> vertices = optiColumns[j].diagramVertices;
        //        //List<Point3D> vertices = optiColumns[j].diagramVertices;

        //        List<Tri3D> faces90 = optiColumns[j].diagramFaces.Select(f => new Tri3D() { Points = f.Points.Select(p => MWPoint3D.point3DByCoordinates(-p.Y, p.X, p.Z)).ToList()} ).ToList();
        //        //List<Tri3D> faces90 = optiColumns[j].diagramFaces.Select(f => new Tri3D() { Points = f.Points.Select(p => new Point3D(-p.Y, p.X, p.Z)).ToList()} ).ToList();
        //        List<MWPoint3D> vertices90 = optiColumns[j].diagramVertices.Select(p => MWPoint3D.point3DByCoordinates(-p.Y, p.X, p.Z)).ToList();
        //        //List<Point3D> vertices90 = optiColumns[j].diagramVertices.Select(p => new Point3D(-p.Y, p.X, p.Z)).ToList();

        //        Parallel.For(0, Nc, (i) =>
        //        {
        //            //C[i, j] = columns[i].isInsideInteractionDiagram(faces, vertices) ? 1 : (columns[i].isInsideInteractionDiagram(faces90, vertices90) ? 2 : 0);
        //            C[i, j] = columns[i].isInsideInteractionDiagram(faces, vertices, allLoads) ? 1 : 0;
        //        });
        //    }
        //    time = watch.ElapsedMilliseconds;
        //    Console.WriteLine("Assembly of correlation matrix : {0} s", Math.Round(time / 1000.0));

        //    for (int i = 0; i < Nc; i++)
        //    {
        //        Console.WriteLine();
        //        Console.Write(columns[i].Name + " : ");
        //        for (int j = 0; j < Nceff; j++)
        //            Console.Write(C[i, j]);
        //        Console.Write("Cost : {0}", columns[i].Cost);
        //    }
        //    Console.WriteLine();

        //    // Selection of the admissible interaction diagrams
        //    //List<int> slctdInd = new List<int>();
        //    //for (int i = 0; i < Nceff; i++)
        //    //    slctdInd.Add(i);

        //    //----------------------------------------------//
        //    //     Loop over the different values of Nd     //
        //    //----------------------------------------------//

        //    for (int Nd = Ndmin; Nd <= Ndmax; Nd++)
        //    {
        //        // Simulated annealing algorithm
        //        int[] x = new int[Nd];
        //        Matrix<double> R = M.Dense(Nc, Nd);
        //        Matrix<double> P = M.Dense(Nceff, Nd);

        //        double f;
        //        double fBest;
        //        int[] bestDesign = new int[Nc];
        //        int[] currentDesign = new int[Nc];
        //        int[] tempDesign = new int[Nc];
        //        List<Column> tempDesigns = new List<Column>();
        //        List<Column> currentDesigns = new List<Column>();
        //        List<Column> bestDesigns = new List<Column>();

        //        object[,] results = new object[Nd, 4];

        //        // Initialization
        //        if (initialState == null)
        //        {
        //            f = Nc * 100;
        //            fBest = Nc * 100;
        //            //var y = GetRandomInt(Nd, slctdInd.Count).ToArray();
        //            //x = GetRandomInt(Nd, Math.Max(2*Nd,Nceff/5)).ToArray();
        //            x = GetRandomInt(Nd, Math.Min(3*Nd,Nceff)).ToArray();
        //            //for (int i = 0; i < Nd; i++)
        //            //    x[i] = slctdInd[y[i]];
        //        }
        //        else
        //        {
        //            f = initialState.fBest;
        //            fBest = initialState.fBest;
        //            x = initialState.x;
        //            currentDesign = initialState.bestDesign;
        //            currentDesigns = initialState.bestDesigns;
        //            bestDesign = initialState.bestDesign;
        //            bestDesigns = initialState.bestDesigns;
        //            for (int i = 0; i < Nd; i++)
        //            {
        //                results[i, 0] = bestDesigns[i].Cost;
        //                results[i, 1] = bestDesign.Where(c => c == x[i] || c == -x[i]).Count();
        //            }
        //        }
        //        double T = Tinit;

        //        List<int> dx;
        //        int t = 0;

        //        object[] report = new object[11];

        //        //var comb = Combinations(Nd,slctdInd.Count);
        //        //int Ncomb = comb.Count();

        //        //int n = 0;
        //        /*while(n < Ncomb && t < 10000)
        //        //for(int n = 0; n < Ncomb; n++)
        //        {
        //            x = comb.ElementAt(n);

        //            P.Clear();
        //            for (int i = 0; i < Nd; i++)
        //                P[x[i], i] = 1;
        //            R = C * P;

        //            // we check if the solution is acceptable
        //            for (int i = 0; i < Nc; i++)
        //                if (R.RowSums()[i] == 0) goto end;

        //            // if yes, we update the chosen designs ...
        //            tempDesigns.Clear();
        //            for (int i = 0; i < Nd; i++)
        //            {
        //                Column col = optiColumns[x[i]].Clone();
        //                //col.Name = "Design " + i;
        //                tempDesigns.Add(col);
        //            }

        //            // ... and calculate the objective function
        //            double fp = 0;
        //            for (int i = 0; i < Nc; i++)
        //            {
        //                int idx = R.Row(i).ToList().IndexOf(1);
        //                tempDesign[i] = x[idx];
        //                fp += optiColumns[x[idx]].Cost;
        //            }

        //            // Is it the best design ?
        //            double Df = fp - f;

        //            currentDesigns = tempDesigns.Select(c => c.Clone()).ToList(); // deep copy
        //            currentDesign = tempDesign.Select(v => v).ToArray(); // deep copy
        //            f += Df;

        //            for (int i = 0; i < Nd; i++)
        //            {
        //                results[i, 2] = currentDesigns[i].Cost;
        //                results[i, 3] = currentDesign.Where(c => c == x[i]).Count();
        //            }

        //            if (fp < fBest)
        //            {
        //                bestDesigns = tempDesigns.Select(c => c.Clone()).ToList();
        //                bestDesign = tempDesign.Select(v => v).ToArray(); // deep copy
        //                fBest = fp;
        //                for (int i = 0; i < Nd; i++)
        //                {
        //                    results[i, 0] = bestDesigns[i].Cost;
        //                    results[i, 1] = bestDesign.Where(c => c == x[i]).Count();
        //                }
        //            }
        //            T = alpha * T;
        //            t++;

        //            report[0] = bestDesigns;
        //            report[1] = currentDesigns;
        //            report[2] = bestDesign;
        //            report[3] = currentDesign;
        //            report[4] = fBest;
        //            report[5] = f;
        //            report[6] = results;
        //            report[7] = columns.Select(c => c.Name).ToArray();
        //            report[8] = Nd;
        //            report[9] = t;

        //            worker.ReportProgress(0, report);

        //        end:
        //            n++;
        //            //Console.WriteLine(t);
        //        }*/

        //        while (t < Nmax && T >= 1e-7)
        //        {
        //            // Update of the considered interaction diagrams
        //            bool different = false;
        //            int[] xtemp = x.Select(a => a).ToArray();
        //            int kk = 0;
        //            var ytemp = new int[Nd];
        //            //for(int i = 0; i < Nd; i++)
        //            //    ytemp[i] = slctdInd.IndexOf(xtemp[i]);
        //            while (!different && kk < 200)
        //            {
        //                dx = NormalRand(Nd, variance);
        //                for (int i = 0; i < Nd; i++)
        //                {
        //                    //ytemp[i] = Math.Min(slctdInd.Count - 1, Math.Max(0, ytemp[i] + dx[i]));
        //                    //xtemp[i] = slctdInd[ytemp[i]];
        //                    xtemp[i] = Math.Min(Nceff - 1, Math.Max(0, x[i] + dx[i]));
        //                }
        //                //Console.WriteLine("ytemp : {0} {1} {2}", ytemp[0], ytemp[1], ytemp[2]);
        //                if (x.Distinct().Count() == Nd)
        //                {
        //                    different = true;
        //                }
        //                kk++;
        //            }
        //            if (different)
        //                x = xtemp;
        //            else
        //            {
        //                //Console.WriteLine("Gets random list");
        //                x = GetRandomInt(Nd, Nceff).ToArray();
        //                //x = GetRandomInt(Nd, Math.Min(2 * Nd, Nceff)).ToArray();
        //                //var y = GetRandomInt(Nd, slctdInd.Count).ToArray();
        //                //for (int i = 0; i < Nd; i++)
        //                //    x[i] = slctdInd[y[i]];
        //            }
        //            x = x.OrderBy(v => v).ToArray();

        //            // Construction of R
        //            P.Clear();
        //            for (int i = 0; i < Nd; i++)
        //                P[x[i], i] = 1;
        //            R = C * P;
        //            // we check if the solution is acceptable
        //            for (int i = 0; i < Nc; i++)
        //                if (R.RowSums()[i] == 0) goto end;

        //            // if yes, we update the chosen designs ...
        //            tempDesigns.Clear();
        //            for (int i = 0; i < Nd; i++)
        //            {
        //                Column col = optiColumns[x[i]].Clone();
        //                //col.Name = "Design " + i;
        //                tempDesigns.Add(col);
        //            }
        //            // ... and calculate the objective function
        //            double fp = 0;
        //            for (int i = 0; i < Nc; i++)
        //            {
        //                int idx1 = R.Row(i).ToList().IndexOf(1);
        //                int idx2 = R.Row(i).ToList().IndexOf(2);
        //                idx1 = (idx1 == -1) ? Nc : idx1;
        //                idx2 = (idx2 == -1) ? Nc : idx2;
        //                int idx = Math.Min(idx1, idx2);
        //                tempDesign[i] = (idx == idx1)? x[idx] : -x[idx]; // a minus means the column needs to be rotated
        //                fp += optiColumns[x[idx]].Cost;
        //            }
        //            // Is it the best design ?
        //            double Df = fp - f;
        //            if (Df < 0)
        //            {
        //                currentDesigns = tempDesigns.Select(c => c.Clone()).ToList(); // deep copy
        //                currentDesign = tempDesign.Select(v => v).ToArray(); // deep copy
        //                f += Df;
        //                for (int i = 0; i < Nd; i++)
        //                {
        //                    results[i, 2] = currentDesigns[i].Cost;
        //                    results[i, 3] = currentDesign.Where(c => c == x[i] || c == -x[i]).Count();
        //                }

        //            }
        //            else
        //            {
        //                Random rand = new Random();
        //                double r = rand.NextDouble();
        //                double p = Math.Exp(-Df / T);
        //                if (p > r)
        //                {
        //                    currentDesigns = tempDesigns.Select(c => c.Clone()).ToList(); // deep copy
        //                    currentDesign = tempDesign.Select(v => v).ToArray(); // deep copy
        //                    f += Df;
        //                    for (int i = 0; i < Nd; i++)
        //                    {
        //                        results[i, 2] = currentDesigns[i].Cost;
        //                        results[i, 3] = currentDesign.Where(c => c == x[i] || c == -x[i]).Count();
        //                    }
        //                }
        //            }
        //            if (fp < fBest)
        //            {
        //                bestDesigns = tempDesigns.Select(c => c.Clone()).ToList();
        //                bestDesign = tempDesign.Select(v => v).ToArray(); // deep copy
        //                fBest = fp;
        //                for (int i = 0; i < Nd; i++)
        //                {
        //                    results[i, 0] = bestDesigns[i].Cost;
        //                    results[i, 1] = bestDesign.Where(c => c == x[i] || c == -x[i]).Count();
        //                }
        //            }
        //            T = alpha * T;
        //            report[0] = bestDesigns;
        //            report[1] = currentDesigns;
        //            report[2] = bestDesign;
        //            report[3] = currentDesign;
        //            report[4] = fBest;
        //            report[5] = f;
        //            report[6] = results;
        //            report[7] = columns.Select(c => c.Name).ToArray();
        //            report[8] = Nd;
        //            report[9] = t;
        //            report[10] = indices.Length * 1.0/ Nc * 100;

        //            worker.ReportProgress(0, report);
        //            t++;
        //        end:
        //            ;
        //            //Console.WriteLine(t);
        //        }

        //        // We initialize the initial state of next iteration with the best config of this iteration
        //        /*initialState = new OptiState();

        //        initialState.fBest = fBest;

        //        List<int> temp = new List<int>();
        //        for (int i = 0; i < Nd; i++)
        //            temp.Add(optiColumns.ToList().IndexOf(optiColumns.First(c => c.Name == bestDesigns[i].Name)));
        //        temp.Add(GetRandomInt(1, Nceff)[0]);
        //        temp = temp.OrderBy(v => v).ToList();
        //        initialState.x = temp.ToArray();

        //        initialState.bestDesign = bestDesign.Select(v => v).ToArray();
        //        List<Column> tempD = new List<Column>();
        //        for (int i = 0; i < Nd + 1; i++)
        //            tempD.Add(optiColumns[initialState.x[i]]);
        //        initialState.bestDesigns = tempD;*/

        //    }

        //}

        private static List<int> NormalRand(int n, double variance)
        {
            List<int> rands = new List<int>();
            Random rand = new Random();
            for (int i = 0; i < n; i++)
            {
                double randNormal = BoxMuller(rand, variance);
                //double xmin = deltaXmin - 0.5;
                //double xmax = deltaXmax + 0.5;

                //double randomX = (xmax - xmin) * randNormal + xmin; // random normal (-xmin,xmax);

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

        //public static IEnumerable<int[]> Combinations(int m, int n)
        //{
        //    int[] result = new int[m];
        //    Stack<int> stack = new Stack<int>();
        //    stack.Push(0);

        //    while (stack.Count > 0)
        //    {
        //        int index = stack.Count - 1;
        //        int value = stack.Pop();

        //        while (value < n)
        //        {
        //            result[index++] = ++value-1;
        //            stack.Push(value);

        //            if (index == m)
        //            {
        //                yield return result.Select(x => x).ToArray();
        //                break;
        //            }
        //        }
        //    }
        //}

        private static (double,bool?,bool?,bool?,bool?,bool?) Objective(Column c, double carbonRef, double costRef, double Wcost, double Fcost, double Wcarb, double Fcarb, bool allLoads, bool[] fireM)
        {
            bool? minRebarCheck = c.CheckMinRebarNo();
            bool? minmaxCheck = null;
            bool? spacingCheck = null;
            bool? fireCheck = null;
            bool fireCheck0 = false;
            bool fireCheck1 = false;
            bool fireCheck2 = false;
            bool fireCheck3 = false;
            bool? capacityCheck = null;

            if (minRebarCheck == true)
            {
                minmaxCheck = c.CheckSteelQtty(allLoads);
                if(minmaxCheck == true)
                {
                    Calculations calc = new Calculations(c);
                    spacingCheck = calc.CheckSpacing();
                    if(spacingCheck == true)
                    {
                        
                        if (fireM[0]) fireCheck0 = calc.CheckFireDesignTable(allLoads);
                        if (fireM[1]) fireCheck1 = calc.CheckFireIsotherm500().Item1;
                        if (fireM[2]) fireCheck2 = calc.CheckFireZoneMethod().Item1;
                        if (fireM[3])
                        {
                            calc.UpdateFireID(true);
                            fireCheck3 = c.CheckIsInsideFireID();
                        }
                        fireCheck = (fireCheck0 || fireCheck1 || fireCheck2 || fireCheck3);
                        if(fireCheck == true)
                        {
                            c.GetInteractionDiagram();
                            capacityCheck = c.isInsideCapacity(false);
                            Console.WriteLine("coucou");
                        }
                        else
                            System.Threading.Thread.Sleep(5);
                    }
                    else
                        System.Threading.Thread.Sleep(5);
                }
                else
                    System.Threading.Thread.Sleep(5);
            }
            else
                System.Threading.Thread.Sleep(5);

            double penCapacity =  capacityCheck == true ? 1 : (capacityCheck == null ? 5 : 10);
            double penFire =  fireCheck == true ? 1 : (fireCheck == null ? 5 : 10);
            double penSpacing =  spacingCheck == true ? 1 : (spacingCheck == null ? 5 : 10);
            double penSteelQtty =  minmaxCheck == true ? 1 : (minmaxCheck == null ? 5 : 10);
            double penMinRebar = minRebarCheck == true ? 1 : 10;

            Calculations calc2 = new Calculations(c);
            double f = (Wcarb * Fcarb * calc2.GetEmbodiedCarbon()[2] / carbonRef + Wcost * Fcost * calc2.GetCost()[3] / costRef) * penCapacity * penFire * penSpacing * penSteelQtty;

            return (f,capacityCheck,fireCheck,spacingCheck,minmaxCheck,minRebarCheck);

        }
    }
}
