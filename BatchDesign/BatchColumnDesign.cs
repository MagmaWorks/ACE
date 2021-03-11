using ColumnDesignCalc;
using MWGeometry;
using Optimisation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BatchDesign
{
    public enum ClusteringType { Local, Global }
    public enum ClusteringMethod { KMeans, Spectral, Spectral_Div, Spectral_SymDiv, Spectral_AddDiv }
    public class BatchColumnDesign
    {
        public ClusteringMethod Method { get; set; }
        public ClusteringType Type { get; set; }
        public int NClusters { get; set; }

        public int NClustersTot { get => LoadClusters.Count; }
        public List<List<MWPoint3D>> Clusters { get => LoadClusters.Select(l => l.Loads.Select(p => p.Load).ToList()).ToList(); }
        public List<Cluster> LoadClusters { get; set; } = new List<Cluster>();
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
            NClusters = 2;
        }

        public void GetDesignClusters()
        {
            DesignClusters = new List<List<Column>>();
            for(int i = 0; i < Columns.Count; i++)
            {
                bool added = false;
                for(int j = 0; j < DesignClusters.Count; j++)
                {
                    if(Columns[i].IsRectangular)
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
                    else if(Columns[i].IsCircular)
                    {
                        bool b3 = Columns[i].Diameter == DesignClusters[j][0].Diameter;
                        if(b3)
                        {
                            DesignClusters[j].Add(Columns[i]);
                            added = true;
                            break;
                        }
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
            Columns.ForEach(c =>
            {
                c.AllLoads = true;
                c.GetDesignMoments();
                c.Loads.ForEach(l =>
                {
                    double max = Math.Max(Math.Abs(l.MEdx), Math.Abs(l.MEdy));
                    double min = Math.Min(Math.Abs(l.MEdx), Math.Abs(l.MEdy));
                    l.MEdx = max;
                    l.MEdy = min;
                });
            });
            MaxMx = Columns.Max(c => c.Loads.Max(l => Math.Abs(l.MEdx)));
            MaxMy = Columns.Max(c => c.Loads.Max(l => Math.Abs(l.MEdy)));
            MaxP = Columns.Max(c => c.Loads.Max(l => Math.Abs(l.P)));
            switch (Type)
            {
                case (ClusteringType.Global):
                    GenerateGlobalLoadsClustering();
                    break;
                case (ClusteringType.Local):
                    GenerateLocalLoadsClustering();
                    break;
            }
        }

        public void GenerateGlobalLoadsClustering()
        {
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

        public void GenerateLocalLoadsClustering()
        {
            LoadClusters = new List<Cluster>();
            if (DesignClusters.Count == 0) GetDesignClusters();
            for(int k = 0; k < DesignClusters.Count; k++)
            {
                List<Column> columns = DesignClusters[k];
                List<Cluster> clusters = new List<Cluster>();
                List<MWPoint3D> means = new List<MWPoint3D>();
                switch (Method)
                {
                    case (ClusteringMethod.KMeans):
                        (clusters, means) = Clustering.KMeans(columns, NClusters);
                        for (int i = 0; i < means.Count; i++)
                            Console.WriteLine("Mean {0} : X={1}, Y={2}, Z={3}", i, means[i].X, means[i].Y, means[i].Z);
                        break;
                    case (ClusteringMethod.Spectral):
                        clusters = Clustering.SpectralClustering(columns, NClusters, Clustering.SpectralNorm.None, Sigma);
                        break;
                    case (ClusteringMethod.Spectral_Div):
                        clusters = Clustering.SpectralClustering(columns, NClusters, Clustering.SpectralNorm.Division, Sigma);
                        break;
                    case (ClusteringMethod.Spectral_SymDiv):
                        clusters = Clustering.SpectralClustering(columns, NClusters, Clustering.SpectralNorm.SymmetricDivision, Sigma);
                        break;
                    case (ClusteringMethod.Spectral_AddDiv):
                        clusters = Clustering.SpectralClustering(columns, NClusters, Clustering.SpectralNorm.Additive, Sigma);
                        break;
                }
                string name0 = "";
                if (columns[0].IsRectangular)
                    name0 = columns[0].LX + "x" + columns[0].LY;
                else if (columns[0].IsCircular)
                    name0 = "D" + columns[0].Diameter;
                for(int m = 0; m < clusters.Count; m++)
                {
                    string name = name0 + " - G" + m;
                    clusters[m].Name = name;
                }
                LoadClusters.AddRange(clusters);
                Means.AddRange(means);
            }

            //foreach (var c in LoadClusters)
            //    if (c.Loads.Count == 0)
            //        LoadClusters.Remove(c);

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
