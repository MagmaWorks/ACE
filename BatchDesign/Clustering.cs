using ColumnDesignCalc;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MWGeometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BatchDesign
{
    public static class Clustering
    {
        public enum SpectralNorm { None, Division, SymmetricDivision, Additive }
        public static List<List<MWPoint3D>> KMeans(List<MWPoint3D> data, int Nc)
        {
            // data normalization
            double maxX = data.Max(p => p.X);
            double maxY = data.Max(p => p.Y);
            double maxZ = data.Max(p => p.Z);
            List<MWPoint3D> dataN = data.Select(p => p = new MWPoint3D(p.X / maxX, p.Y / maxY, p.Z / maxZ) ).ToList();

            // means and cluster initializations
            List<MWPoint3D> means = new List<MWPoint3D>();
            List<List<MWPoint3D>> clusters = new List<List<MWPoint3D>>();
            Random r = new Random();
            for(int i = 0; i < Nc; i++)
            {
                int n = (int)Math.Truncate(data.Count * r.NextDouble());
                means.Add(dataN[n]);
                clusters.Add(new List<MWPoint3D>());
            }

            // assignement initialization
            foreach (var p in dataN)
            {
                List<double> distances = new List<double>();
                for (int i = 0; i < Nc; i++)
                    distances.Add(Points.Distance3D(p, means[i]));
                int imin = distances.IndexOf(distances.Min());
                clusters[imin].Add(p);
            }

            int moves = 1;
            while(moves > 0)
            {
                // means update
                for(int i = 0; i < Nc; i++)
                {
                    double x = clusters[i].Sum(p => p.X) / clusters[i].Count;
                    double y = clusters[i].Sum(p => p.Y) / clusters[i].Count;
                    double z = clusters[i].Sum(p => p.Z) / clusters[i].Count;
                    means[i] = new MWPoint3D(x, y, z);
                }

                // assignment update
                moves = 0;
                List<List<MWPoint3D>> clustersTemp = new List<List<MWPoint3D>>();
                for (int i = 0; i < Nc; i++)
                    clustersTemp.Add(new List<MWPoint3D>());

                for (int i = 0; i < Nc; i++)
                {
                    for(int k = 0; k < clusters[i].Count; k++)
                    {
                        List<double> distances = new List<double>();
                        for (int j = 0; j < Nc; j++)
                            distances.Add(Points.Distance3D(clusters[i][k], means[j]));
                        int imin = distances.IndexOf(distances.Min());
                        if (imin != i)
                        {
                            clustersTemp[imin].Add(clusters[i][k]);
                            moves++;
                        }
                        else
                            clustersTemp[i].Add(clusters[i][k]);
                    }
                }
                for (int i = 0; i < Nc; i++)
                    clusters[i] = clustersTemp[i];

            }

            return clusters;
        }

        public static (List<Cluster>, List<MWPoint3D>) KMeans(List<Column> cols, int Nc)
        {
            // data
            //List<ClusterLoad> data = cols.SelectMany(c => c.Loads.Select(l => new ClusterLoad(new MWPoint3D(l.MEdx, l.MEdy, l.P), c, "C"+c.Name + " - " + l.Name))).ToList();

            // columns are clustered according to their mean load
            List<ClusterLoad> loadAvg = cols.Select(c => GetAverageLoad(c)).ToList();

            // means and cluster initializations
            List<MWPoint3D> means = new List<MWPoint3D>();
            List<Cluster> clusters = new List<Cluster>();
            Random r = new Random();
            List<ClusterLoad> dataTemp = loadAvg.Select(d => d.Clone()).ToList();
            for (int i = 0; i < Nc; i++)
            {
                int n = (int)Math.Truncate(dataTemp.Count * r.NextDouble());
                means.Add(dataTemp[n].Load);
                dataTemp.RemoveAt(n);
                clusters.Add(new Cluster());
            }

            // assignement initialization
            foreach (var p in loadAvg)
            {
                List<double> distances = new List<double>();
                for (int i = 0; i < Nc; i++)
                    distances.Add(Points.Distance3D(p.Load, means[i]));
                int imin = distances.IndexOf(distances.Min());
                clusters[imin].Loads.Add(p);
            }

            int moves = 1;
            while (moves > 0)
            {
                // means update
                for (int i = 0; i < Nc; i++)
                {
                    double x = clusters[i].Loads.Sum(p => p.Load.X) / clusters[i].Loads.Count;
                    double y = clusters[i].Loads.Sum(p => p.Load.Y) / clusters[i].Loads.Count;
                    double z = clusters[i].Loads.Sum(p => p.Load.Z) / clusters[i].Loads.Count;
                    means[i] = new MWPoint3D(x, y, z);

                    // particular case where a cluster has no element
                    if (Double.IsNaN(means[i].X))
                        means[i] = new MWPoint3D(0, 0, 0);
                }

                // assignment update
                moves = 0;
                List<Cluster> clustersTemp = new List<Cluster>();
                for (int i = 0; i < Nc; i++)
                    clustersTemp.Add(new Cluster());

                for (int i = 0; i < Nc; i++)
                {
                    for (int k = 0; k < clusters[i].Loads.Count; k++)
                    {
                        List<double> distances = new List<double>();
                        for (int j = 0; j < Nc; j++)
                            distances.Add(Points.Distance3D(clusters[i].Loads[k].Load, means[j]));
                        int imin = distances.IndexOf(distances.Min());
                        if (imin != i)
                        {
                            clustersTemp[imin].Loads.Add(clusters[i].Loads[k]);
                            moves++;
                        }
                        else
                            clustersTemp[i].Loads.Add(clusters[i].Loads[k]);
                    }
                }
                for (int i = 0; i < Nc; i++)
                    clusters[i] = clustersTemp[i];


            }

            // Distribute columns load into clusters
            List<Cluster> clustersOut = clusters.Select(c => new Cluster()
            {
                Loads = c.Loads.SelectMany(
                cl => cl.ParentColumn.Loads.Select(
                    l => new ClusterLoad(new MWPoint3D(l.MEdx, l.MEdy, l.P), cl.ParentColumn, cl.ParentColumn.Name + " - " + l.Name))).ToList()
            }).ToList();

            if (!clustersOut.All(c => c.Loads.Count > 0))
                return KMeans(cols, Nc);
            else
                return (clustersOut, means);
        }

        public static List<Cluster> RegroupColumns(List<Column> cols, List<Cluster> clusters)
        {
            for(int i = 0; i < cols.Count; i++)
            {
                string name = "C" + cols[i].Name + " ";
                Cluster cl0 = clusters.First(x => x.Loads.Select(l => l.Name).Any(l => l.Contains(name)));
                foreach(var cl in clusters)
                {
                    if(cl != cl0)
                    {
                        var loads = cl.Loads.Where(x => x.Name.Contains(name)).ToList();
                        for(int j = 0; j < loads.Count; j++)
                        {
                            cl.Loads.Remove(loads[j]);
                            cl0.Loads.Add(loads[j]);
                        }
                    }
                }
            }
            return clusters;
        }
    
        public static ClusterLoad GetAverageLoad(Column col)
        {
            double MEdx = 0;
            double MEdy = 0;
            double P = 0;
            int N = col.Loads.Count;
            for (int i = 0; i < N; i++)
            {
                MEdx += col.Loads[i].MEdx;
                MEdy += col.Loads[i].MEdy;
                P += col.Loads[i].P;
            }
            MEdx /= N;
            MEdy /= N;
            P /= N;
            return new ClusterLoad(new MWPoint3D(MEdx, MEdy, P), col, col.Name);
        }

        public static List<Cluster> SpectralClustering(List<Column> cols, int Nc, SpectralNorm norm, double sigma = 1e3)
        {
            // data
            List<ClusterLoad> dataN = cols.SelectMany(c => c.Loads.Select(l => new ClusterLoad(new MWPoint3D(l.MEdx, l.MEdy, l.P), c, l.Name))).ToList();

            int N = dataN.Count;

            // Similarity matrix construction
            Matrix<double> S = Matrix<double>.Build.Dense(N, N);
            for(int i = 0; i < N; i++)
            {
                for(int j = i; j < N; j++)
                {
                    double dist = Points.Distance3D(dataN[i].Load, dataN[j].Load);
                    S[i, j] = Math.Exp( - Math.Pow(dist, 2) / (2 * Math.Pow(sigma, 2)));
                    //S[i, j] = dist;
                    S[j, i] = S[i, j];
                }
            }

            // Diagonal matrix of degrees D
            Matrix<double> D = Matrix<double>.Build.Dense(N, N);
            for(int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                    D[i, i] += S[i, j];
            }

            // Laplacian matrix
            Matrix<double> L = Matrix<double>.Build.Dense(N, N);
            Matrix<double> I = Matrix<double>.Build.DenseIdentity(N);
            switch (norm)
            {
                case (SpectralNorm.None):
                    L = D.Subtract(S);
                    break;
                case (SpectralNorm.Division):
                    Matrix<double> Dinv = Matrix<double>.Build.Dense(N, N, (i, j) => i == j ? 1 / D[i, j] : 0);
                    L = I.Subtract(Dinv.Multiply(S));
                    break;
                case (SpectralNorm.SymmetricDivision):
                    Matrix<double> Dhalfinv = Matrix<double>.Build.Dense(N, N, (i, j) => i == j ? 1 / Math.Sqrt(D[i, i]) : 0);
                    L = I.Subtract(Dhalfinv.Multiply(S.Multiply(Dhalfinv)));
                    break;
                case (SpectralNorm.Additive):
                    double dmax = D.Enumerate().Max();
                    L = I.Subtract(S.Add(I.Multiply(dmax).Subtract(D)).Divide(dmax));
                    break;
            }
            
           
            //L = D.Subtract(S);

            // Diagonalisation of L
            Evd<double> eigen = L.Evd(Symmetricity.Symmetric);
            Matrix<double> eigenVectors = eigen.EigenVectors;
            Vector<double> eigenValues = eigen.EigenValues.Real();

            // Construction of matrix X
            Matrix<double> X = eigenVectors.SubMatrix(0, N, 0, Nc);

            // K means applied to the lines of X
            List<List<Vector<double>>> clusters = KMeans(X);

            // Assignment of column loads to their cluster
            List<Cluster> res = new List<Cluster>();
            List<Vector<double>> rows = X.EnumerateRows().ToList();
            if(norm == SpectralNorm.SymmetricDivision) X = X.NormalizeRows(2);
                
            for(int i = 0; i < Nc; i++)
            {
                res.Add(new Cluster());
                List<Vector<double>> cluster = clusters[i];
                for(int j = 0; j < clusters[i].Count; j++)
                {
                    int ind = rows.IndexOf(cluster[j]);
                    res[i].Loads.Add(dataN[ind]);
                }
            }

            return res;
        }

        public static List<List<Vector<double>>> KMeans(Matrix<double> X)
        {
            int Nc = X.ColumnCount;
            int N = X.RowCount;
            List<Vector<double>> means = new List<Vector<double>>();
            List<List<Vector<double>>> clusters = new List<List<Vector<double>>>();
            List<Vector<double>> data = X.EnumerateRows().ToList();
            Random r = new Random();
            for (int i = 0; i < Nc; i++)
            {
                int n = (int)Math.Truncate(N * r.NextDouble());
                means.Add(X.Row(n));
                clusters.Add(new List<Vector<double>>());
            }

            // assignement initialization
            foreach (var p in data)
            {
                List<double> distances = new List<double>();
                for (int i = 0; i < Nc; i++)
                    distances.Add(Distance.Euclidean<double>(means[i], p));
                int imin = distances.IndexOf(distances.Min());
                clusters[imin].Add(p);
            }

            int moves = 1;
            while (moves > 0)
            {
                // means update
                for (int i = 0; i < Nc; i++)
                {
                    for (int j = 0; j < Nc; j++)
                        means[i][j] = clusters[i].Sum(v => v[j]) / clusters[i].Count;
                }

                // assignment update
                moves = 0;
                List<List<Vector<double>>> clustersTemp = new List<List<Vector<double>>>();
                for (int i = 0; i < Nc; i++)
                    clustersTemp.Add(new List<Vector<double>>());

                for (int i = 0; i < Nc; i++)
                {
                    for (int k = 0; k < clusters[i].Count; k++)
                    {
                        List<double> distances = new List<double>();
                        for (int j = 0; j < Nc; j++)
                            distances.Add(Distance.Euclidean(clusters[i][k], means[j]));
                        int imin = distances.IndexOf(distances.Min());
                        if (imin != i)
                        {
                            clustersTemp[imin].Add(clusters[i][k]);
                            moves++;
                        }
                        else
                            clustersTemp[i].Add(clusters[i][k]);
                    }
                }
                for (int i = 0; i < Nc; i++)
                    clusters[i] = clustersTemp[i];
            }

            return clusters;
        }

    }

    public class ClusterLoad
    {
        public string Name { get; set; }
        public MWPoint3D Load { get; set; }
        public Column ParentColumn { get; set; }

        public ClusterLoad(MWPoint3D load, Column col, string name)
        {
            Load = load;
            ParentColumn = col;
            Name = name;
        }

        public ClusterLoad Clone()
        {
            return new ClusterLoad(this.Load, this.ParentColumn.Clone(), this.Name);
        }

    }

    public class Cluster
    {
        public List<ClusterLoad> Loads { get; set; }
        public string Name { get; set; }

        public Cluster()
        {
            Loads = new List<ClusterLoad>();
            Name = "Untitled";
        }
    }
}
