using ColumnDesignCalc;
using MWGeometry;
using Optimisation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BatchDesign
{
    public enum ClusteringMethod { KMeans, Spectral, Spectral_Div, Spectral_SymDiv, Spectral_AddDiv }
    public class BatchColumnDesign
    {
        public ClusteringMethod Method { get; set; }
        public int NClusters { get; set; }
        public List<List<MWPoint3D>> Clusters { get => LoadClusters.Select(l => l.Select(p => p.Load).ToList()).ToList(); }
        public List<List<ClusterLoad>> LoadClusters { get; set; } = new List<List<ClusterLoad>>();
        public List<List<Column>> DesignClusters { get; set; } = new List<List<Column>>();
        public List<MWPoint3D> Means = new List<MWPoint3D>();
        public List<Column> Columns { get; set; }

        public List<Column> Designs { get; set; }

        public double Sigma { get; set; } = 1e3;
        //
        public double MaxMx;
        public double MaxMy;
        public double MaxP;

        // Async opti parameters
        public bool[] Shapes;
        public bool[] Activations;
        public string[] Mins;
        public string[] Maxs;
        public string[] Incrs;
        public int Maxiter;
        public double Alpha;
        public double Variance;
        public double[] Drivers;
        public double[] DriversWeight;
        public List<Concrete> ConcreteGrades;
        public List<Steel> SteelGrades;
        public List<int> BarDiameters;
        public List<int> LinkDiameters;
        public BatchColumnDesign()
        {
            Method = ClusteringMethod.KMeans;
            NClusters = 3;
        }

        public void GetDesignClusters()
        {
            DesignClusters = new List<List<Column>>();
            for(int i = 0; i < Columns.Count; i++)
            {
                bool added = false;
                for(int j = 0; j < DesignClusters.Count; j++)
                {
                    bool b1 = Columns[i].LX == DesignClusters[j][0].LX && Columns[i].LY == DesignClusters[j][0].LY;
                    bool b2 = Columns[i].LY == DesignClusters[j][0].LX && Columns[i].LX == DesignClusters[j][0].LY;
                    if (b1 || b2)
                    {
                        DesignClusters[j].Add(Columns[i]);
                        added = true;
                        break;
                    }
                }
                if (added)
                    continue;
                else
                    DesignClusters.Add(new List<Column>() { Columns[i] });
            }
        }

        public void GenerateLoadsClustering()
        {
            foreach(var c in Columns)
            {
                double maxmx = c.Loads.Max(l => Math.Abs(l.MEdx));
                double maxmy = c.Loads.Max(l => Math.Abs(l.MEdy));
                double maxp = c.Loads.Max(l => Math.Abs(l.P));
                if (Math.Max(maxmx, maxmy) / maxp > 1)
                    Console.WriteLine("high moment col {0}", c.Name);
            }
            Columns.ForEach(c => { c.AllLoads = true; c.GetDesignMoments(); });
            Columns.ForEach(c => c.Loads.ForEach(l => { double max = Math.Max(Math.Abs(l.MEdx), Math.Abs(l.MEdy));
                                                        double min = Math.Min(Math.Abs(l.MEdx), Math.Abs(l.MEdy));
                                                        l.MEdx = max;
                                                        l.MEdy = min;
            }));
            MaxMx = Columns.Max(c => c.Loads.Max(l => Math.Abs(l.MEdx)));
            MaxMy = Columns.Max(c => c.Loads.Max(l => Math.Abs(l.MEdy)));
            MaxP = Columns.Max(c => c.Loads.Max(l => Math.Abs(l.P)));
            //Clusters = Clustering.KMeans(Columns.SelectMany(c => c.Loads.Select(l => new MWPoint3D(Math.Abs(l.MEdx) / MaxMx, 10 * Math.Abs(l.MEdy) / MaxMy, 10 * Math.Abs(l.P) / MaxP))).ToList(), NClusters);
            Console.WriteLine("Clustering method : {0}", Method.ToString());
            switch (Method)
            {
                case (ClusteringMethod.KMeans):
                    (LoadClusters, Means) = Clustering.KMeans(Columns, NClusters);
                    for (int i = 0; i < Means.Count; i++)
                        Console.WriteLine("Mean {0} : X={1}, Y={2}, Z={3}", i, Means[i].X, Means[i].Y, Means[i].Z);
                    break;
                case (ClusteringMethod.Spectral):
                    LoadClusters = Clustering.SpectralClustering(Columns, NClusters, Clustering.SpectralNorm.None, Sigma);
                    break;
                case (ClusteringMethod.Spectral_Div):
                    LoadClusters = Clustering.SpectralClustering(Columns, NClusters, Clustering.SpectralNorm.Division, Sigma);
                    break;
                case (ClusteringMethod.Spectral_SymDiv):
                    LoadClusters = Clustering.SpectralClustering(Columns, NClusters, Clustering.SpectralNorm.SymmetricDivision, Sigma);
                    break;
                case (ClusteringMethod.Spectral_AddDiv):
                    LoadClusters = Clustering.SpectralClustering(Columns, NClusters, Clustering.SpectralNorm.Additive, Sigma);
                    break;
            }

        }

        //public void OptimiseClusterDesigns()
        //{
        //    //for(int i = 0; i < NClusters; i++)
        //    Designs = new List<Column>();

        //    for (int i = 0; i < 1; i++)
        //    {
        //        Column ClusterCol = new Column()
        //        {
        //            Name = "Design "+ i,
        //            Loads = new List<Load>(),
        //            ConcreteGrade = ConcreteGrades[0],
        //            SteelGrade = SteelGrades[0],
        //            LX = 600, //Convert.ToInt32(Mins[0]),
        //            LY = 600, //Convert.ToInt32(Mins[1]),
        //            NRebarX = 5, //Convert.ToInt32(Mins[2]),
        //            NRebarY = 5, //Convert.ToInt32(Mins[3]),
        //            Diameter = Convert.ToDouble(Mins[4]),
        //            NRebarCirc = Convert.ToInt32(Mins[5]),
        //            Radius = Convert.ToDouble(Mins[6]),
        //            Edges = Convert.ToInt32(Mins[7]),
        //            BarDiameter = Convert.ToInt32(Mins[8]),
        //            LinkDiameter = Convert.ToInt32(Mins[9]),
        //            AllLoads = true
        //        };
        //        for (int j = 0; j < Clusters[i].Count; j++)
        //        {
        //            ClusterCol.Loads.Add(new Load()
        //            {
        //                MEdx = Clusters[i][j].X * MaxMx,
        //                MEdy = Clusters[i][j].Y * MaxMy,
        //                P = Clusters[i][j].Z * MaxP
        //            });
        //        }
        //        Console.WriteLine("Max iteration = {0}", Maxiter);
        //        DesignOptimiser opti = new DesignOptimiser(ClusterCol);
        //        Designs.Add(ClusterCol);
        //        //opti.ColumnDesignOpti(Shapes, Activations, Mins, Maxs, Incrs, Maxiter, Alpha, 
        //        //    Variance, Drivers, DriversWeight, ConcreteGrades, BarDiameters, LinkDiameters);
        //        _ = opti.ColumnDesignOpti_Async();


        //    }
        //}
    }
}
