using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MWGeometry;

namespace FireDesign
{
    public enum FCurve { Standard, Hydrocarbon }

    public class TemperatureProfile
    {
        double Rho20 = 2500;

        int NT = 1800;
        int NX = 30;
        int NY = 30;
        public Matrix<double> Temp;
        public Vector<double> X;
        public Vector<double> Y;

        double Lx;
        double Ly;
        double lx;
        double ly;

        public Dictionary<double, Matrix<double>> TempMap = new Dictionary<double, Matrix<double>>();
        //public List<Matrix<double>> Contours;
        public List<Contour> ContourPts { get; set; } = new List<Contour>();
        public List<double> Levels { get; set; } = new List<double>();

        public TemperatureProfile(double lx, double ly, double time, FCurve fcurve)
        {
            NT = Convert.ToInt32((time / 7200) * 1800);
            double dt = time / NT;
            double dx = lx / 2 / (NX - 1);
            double dy = ly / 2 / (NY - 1);

            double[] fireResistances = new double[] { 30, 60, 90, 120, 180, 240 };

            Lx = lx / 2;
            Ly = ly / 2;

            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;

            Temp = M.Dense(NX, NY, 20.0);

            Matrix<double> A;
            Matrix<double> B;

            X = V.Dense(NX, i => i * dx + dx / 2);
            Y = V.Dense(NY, i => i * dy + dy / 2);

            double Tfire = 0;

            for (int n = 0; n <= NT; n++)
            {
                double t = n * dt / 60;
                if (fcurve == FCurve.Standard)
                    Tfire = 20 + 345 * Math.Log10(8 * t + 1);
                else if (fcurve == FCurve.Hydrocarbon)
                    Tfire = 1080 * (1 - 0.325 * Math.Exp(-0.167 * t) - 0.675 * Math.Exp(-2.5 * t)) + 20;
                // conditions due to symmetry
                Temp.SetColumn(NX - 1, Temp.Column(NX - 2));
                Temp.SetRow(NY - 1, Temp.Row(NY - 2));

                A = M.Dense(NX, NY, (i, j) => (GetA(i, j)));
                B = M.Dense(NX, NY, (i, j) => (GetB(i, j)));

                Temp.SetRow(0, V.Dense(NY, j =>
                {
                    double l = 1.36 - 0.136 * Temp[0, j] / 100 + 0.0057 * (Temp[0, j] / 100) * (Temp[0, j] / 100);
                    return Temp[0, j] + 25 * dx / l * (Tfire - Temp[0, j]) - dt * B[0, j] / l * 0.7 * 5.6703E-8 * Math.Pow(Temp[0, j] + 273, 4);
                }));

                //Temp.SetColumn(0, V.Dense(NX, i =>
                //{
                //    double l = 1.36 - 0.136 * Temp[0, i] / 100 + 0.0057 * (Temp[0, i] / 100) * (Temp[0, i] / 100);
                //    return Temp[i, 0] + 25 * dy / l * (Tfire - Temp[i, 0]) - dt * B[i, 0] / l * 0.7 * 5.6703E-8 * Math.Pow(Temp[i, 0] + 273, 4);
                //}));

                Temp.SetColumn(0, V.Dense(NX, i =>
                {
                    double l = 1.36 - 0.136 * Temp[i, 0] / 100 + 0.0057 * (Temp[i, 0] / 100) * (Temp[i, 0] / 100);
                    return Temp[i, 0] + 25 * dy / l * (Tfire - Temp[i, 0]) - dt * B[i, 0] / l * 0.7 * 5.6703E-8 * Math.Pow(Temp[i, 0] + 273, 4);
                }));

                var Temp2 = Temp.SubMatrix(1, NX - 2, 1, NY - 2)
                          + dt / (4 * dx * dx) * A.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(2, NX - 2, 1, NY - 2).PointwisePower(2) - 2 * Temp.SubMatrix(2, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(0, NX - 2, 1, NY - 2)) + Temp.SubMatrix(0, NX - 2, 1, NY - 2).PointwisePower(2))
                          + dt / (4 * dy * dy) * A.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(1, NX - 2, 2, NY - 2).PointwisePower(2) - 2 * Temp.SubMatrix(1, NX - 2, 2, NY - 2).PointwiseMultiply(Temp.SubMatrix(1, NX - 2, 0, NY - 2)) + Temp.SubMatrix(1, NX - 2, 0, NY - 2).PointwisePower(2))
                          + dt / (dx * dx) * B.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(2, NX - 2, 1, NY - 2) - 2 * Temp.SubMatrix(1, NX - 2, 1, NY - 2) + Temp.SubMatrix(0, NX - 2, 1, NY - 2))
                          + dt / (dy * dy) * B.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(1, NX - 2, 2, NY - 2) - 2 * Temp.SubMatrix(1, NX - 2, 1, NY - 2) + Temp.SubMatrix(1, NX - 2, 0, NY - 2));
                //for (int i = 1; i < NX-1; i++)
                //    for(int j = 1; j < NY-1; j++)
                //    {
                //        Temp[i, j] += dt / (4 * dx * dx) * A[i, j] * (Temp[i + 1, j] * Temp[i + 1, j] - 2 * Temp[i + 1, j] * Temp[i - 1, j] + Temp[i - 1, j] * Temp[i - 1, j])
                //                    + dt / (4 * dy * dy) * A[i, j] * (Temp[i, j + 1] * Temp[i, j + 1] - 2 * Temp[i, j + 1] * Temp[i, j - 1] + Temp[i, j - 1] * Temp[i, j - 1])
                //                    + dt / (dx * dx) * B[i, j] * (Temp[i + 1, j] - 2 * Temp[i, j] + Temp[i - 1, j])
                //                    + dt / (dy * dy) * B[i, j] * (Temp[i, j + 1] - 2 * Temp[i, j] + Temp[i, j - 1]);
                //    }

                Temp.SetSubMatrix(1, 1, Temp2);

                //double t = (n + 1) * dt / 60;
                if (fireResistances.Contains(t))
                    TempMap.Add(t, Temp.Clone());
            }

        }

        public TemperatureProfile(double Hx, double Hy, double hx, double hy, double time, FCurve fcurve)
        {
            NT = Convert.ToInt32((time / 7200) * 1800);
            double dt = time / NT;
            while (NX < 3000)
            {
                double t = (NX - 1) * hx / Hx;
                if (t - Convert.ToInt32(t) == 0)
                    break;
                else
                    NX++;
            }
            while (NY < 3000)
            {
                double t = (NY - 1) * hy / Hy;
                if (t - Convert.ToInt32(t) == 0)
                    break;
                else
                    NY++;
            }
            double dx = Hx / (NX - 1);
            double dy = Hy / (NY - 1);

            int nx = Convert.ToInt32(hx / dx);
            int ny = Convert.ToInt32(hy / dy);

            double[] fireResistances = new double[] { 30, 60, 90, 120, 180, 240 };

            Lx = Hx;
            Ly = Hy;

            lx = hx;
            ly = hy;

            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;

            Temp = M.Dense(NX, NY, 20.0);

            Matrix<double> A;
            Matrix<double> B;

            X = V.Dense(NX, i => i * dx + dx / 2);
            Y = V.Dense(NY, i => i * dy + dy / 2);

            double Tfire = 0;

            for (int n = 0; n <= NT; n++)
            {
                double t = n * dt / 60;
                if (fcurve == FCurve.Standard)
                    Tfire = 20 + 345 * Math.Log10(8 * t + 1);
                else if (fcurve == FCurve.Hydrocarbon)
                    Tfire = 1080 * (1 - 0.325 * Math.Exp(-0.167 * t) - 0.675 * Math.Exp(-2.5 * t)) + 20;
                // conditions due to symmetry
                //Temp.SetColumn(NX - 1, Temp.Column(NX - 2));
                //Temp.SetRow(NY - 1, Temp.Row(NY - 2));

                A = M.Dense(NX, NY, (i, j) => (GetA(i, j)));
                B = M.Dense(NX, NY, (i, j) => (GetB(i, j)));

                // Boundary conditions

                Temp.SetRow(0, V.Dense(NY, j =>
                {
                    double l = 1.36 - 0.136 * Temp[0, j] / 100 + 0.0057 * (Temp[0, j] / 100) * (Temp[0, j] / 100);
                    return Temp[0, j] + 25 * dx / l * (Tfire - Temp[0, j]) - dt * B[0, j] / l * 0.7 * 5.6703E-8 * Math.Pow(Temp[0, j] + 273, 4);
                }));

                Temp.SetRow(NX - 1, V.Dense(NY, j =>
                {
                    double l = 1.36 - 0.136 * Temp[NX - 1, j] / 100 + 0.0057 * (Temp[NX - 1, j] / 100) * (Temp[NX - 1, j] / 100);
                    return Temp[NX - 1, j] + 25 * dx / l * (Tfire - Temp[NX - 1, j]) - dt * B[NX - 1, j] / l * 0.7 * 5.6703E-8 * Math.Pow(Temp[NX - 1, j] + 273, 4);
                }));

                Temp.SetColumn(0, V.Dense(NX, i =>
                {
                    double l = 1.36 - 0.136 * Temp[i, 0] / 100 + 0.0057 * (Temp[i, 0] / 100) * (Temp[i, 0] / 100);
                    return Temp[i, 0] + 25 * dy / l * (Tfire - Temp[i, 0]) - dt * B[i, 0] / l * 0.7 * 5.6703E-8 * Math.Pow(Temp[i, 0] + 273, 4);
                }));

                Temp.SetColumn(NY - 1, V.Dense(NX, i =>
                {
                    double l = 1.36 - 0.136 * Temp[i, NY - 1] / 100 + 0.0057 * (Temp[i, NY - 1] / 100) * (Temp[i, NY - 1] / 100);
                    return Temp[i, NY - 1] + 25 * dy / l * (Tfire - Temp[i, NY - 1]) - dt * B[i, NY - 1] / l * 0.7 * 5.6703E-8 * Math.Pow(Temp[i, NY - 1] + 273, 4);
                }));

                Temp.SetSubMatrix(nx, ny, M.Dense(NX - nx, NY - ny, Tfire));

                var Temp2 = Temp.SubMatrix(1, NX - 2, 1, NY - 2)
                          + dt / (4 * dx * dx) * A.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(2, NX - 2, 1, NY - 2).PointwisePower(2) - 2 * Temp.SubMatrix(2, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(0, NX - 2, 1, NY - 2)) + Temp.SubMatrix(0, NX - 2, 1, NY - 2).PointwisePower(2))
                          + dt / (4 * dy * dy) * A.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(1, NX - 2, 2, NY - 2).PointwisePower(2) - 2 * Temp.SubMatrix(1, NX - 2, 2, NY - 2).PointwiseMultiply(Temp.SubMatrix(1, NX - 2, 0, NY - 2)) + Temp.SubMatrix(1, NX - 2, 0, NY - 2).PointwisePower(2))
                          + dt / (dx * dx) * B.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(2, NX - 2, 1, NY - 2) - 2 * Temp.SubMatrix(1, NX - 2, 1, NY - 2) + Temp.SubMatrix(0, NX - 2, 1, NY - 2))
                          + dt / (dy * dy) * B.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(1, NX - 2, 2, NY - 2) - 2 * Temp.SubMatrix(1, NX - 2, 1, NY - 2) + Temp.SubMatrix(1, NX - 2, 0, NY - 2));

                Temp.SetSubMatrix(1, 1, Temp2);

                //double t = (n + 1) * dt / 60;
                if (fireResistances.Contains(t))
                    TempMap.Add(t, Temp.Clone());
            }
        }

        private double GetA(int i, int j)
        {
            double res = 0;
            double dl = -0.136 / 100 + 2 * 0.0057 / 100 * Temp[i, j] / 100;
            if (Temp[i, j] <= 100)
                res = dl / (900 * Rho20);
            else if (Temp[i, j] <= 115)
                res = dl / (1470 * Rho20);
            else if (Temp[i, j] <= 200)
                res = dl / ((1470 - 470 * (Temp[i, j] - 115) / 85) * Rho20 * (1 - 0.02 * (Temp[i, j] - 115) / 85));
            else if (Temp[i, j] <= 400)
                res = dl / ((1000 + (Temp[i, j] - 200) / 2) * Rho20 * (0.98 - 0.03 * (Temp[i, j] - 200) / 200));
            else
                res = dl / (1100 * Rho20 * (0.95 - 0.07 * (Temp[i, j] - 400) / 800));
            if (Double.IsNaN(res))
                Console.WriteLine("res is NaN");
            return res;
        }

        private double GetB(int i, int j)
        {
            double res = 0;
            double l = 1.36 - 0.136 * Temp[i, j] / 100 + 0.0057 * (Temp[i, j] / 100) * (Temp[i, j] / 100);
            if (Temp[i, j] <= 100)
                res = l / (900 * Rho20);
            else if (Temp[i, j] <= 115)
                res = l / (1470 * Rho20);
            else if (Temp[i, j] <= 200)
                res = l / ((1470 - 470 * (Temp[i, j] - 115) / 85) * Rho20 * (1 - 0.02 * (Temp[i, j] - 115) / 85));
            else if (Temp[i, j] <= 400)
                res = l / ((1000 + (Temp[i, j] - 200) / 2) * Rho20 * (0.98 - 0.03 * (Temp[i, j] - 200) / 200));
            else
                res = l / (1100 * Rho20 * (0.95 - 0.07 * (Temp[i, j] - 400) / 800));
            if (Double.IsNaN(res))
                Console.WriteLine("res is NaN");
            return res;
        }


        public void GetContours(double R, bool symmetry = false)
        {
            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;

            ContourPts = new List<Contour>();

            Temp = TempMap.First(x => x.Key == R).Value.Clone();
            Levels = new List<double>();

            bool stop = false;
            double level = 25;
            while (!stop)
            {
                //Matrix<int> Threshold = M.Dense(NX,NY,(i, j) => Temp[i, j] >= level ? 1 : 0);
                Matrix<double> Binary = M.Dense(NX - 1, NY - 1, (i, j) =>
                {
                    string bin = Temp[i, j] >= level ? "1" : "0";
                    bin += Temp[i, j + 1] >= level ? "1" : "0";
                    bin += Temp[i + 1, j + 1] >= level ? "1" : "0";
                    bin += Temp[i + 1, j] >= level ? "1" : "0";
                    return BinToInt(bin);
                });
                if (Binary.Enumerate().Max() == 0)
                    break;
                else if (Binary.Enumerate().Min() == 15)
                {
                    level += (level == 25) ? 75 : 100;
                    continue;
                }
                List<MWPoint2D> pts = new List<MWPoint2D>();
                var sparseBin = Binary.EnumerateIndexed(Zeros.AllowSkip);
                sparseBin.ToList().ForEach(x =>
                {
                    if (x.Item3 == 1 || x.Item3 == 14)
                    {
                        pts.Add(GetLPoint(x.Item1, x.Item2, x.Item1 + 1, x.Item2, level));
                        pts.Add(GetLPoint(x.Item1 + 1, x.Item2 + 1, x.Item1 + 1, x.Item2, level));
                    }
                    else if (x.Item3 == 2 || x.Item3 == 13)
                    {
                        pts.Add(GetLPoint(x.Item1 + 1, x.Item2, x.Item1 + 1, x.Item2 + 1, level));
                        pts.Add(GetLPoint(x.Item1, x.Item2 + 1, x.Item1 + 1, x.Item2 + 1, level));
                    }
                    else if (x.Item3 == 3 || x.Item3 == 12)
                    {
                        pts.Add(GetLPoint(x.Item1, x.Item2, x.Item1 + 1, x.Item2, level));
                        pts.Add(GetLPoint(x.Item1, x.Item2 + 1, x.Item1 + 1, x.Item2 + 1, level));
                    }
                    else if (x.Item3 == 4 || x.Item3 == 11)
                    {
                        pts.Add(GetLPoint(x.Item1, x.Item2, x.Item1, x.Item2 + 1, level));
                        pts.Add(GetLPoint(x.Item1, x.Item2 + 1, x.Item1 + 1, x.Item2 + 1, level));
                    }
                    else if (x.Item3 == 5 || x.Item3 == 10)
                    {
                        pts.Add(GetLPoint(x.Item1, x.Item2, x.Item1 + 1, x.Item2, level));
                        pts.Add(GetLPoint(x.Item1, x.Item2, x.Item1, x.Item2 + 1, level));
                        pts.Add(GetLPoint(x.Item1, x.Item2 + 1, x.Item1 + 1, x.Item2 + 1, level));
                        pts.Add(GetLPoint(x.Item1 + 1, x.Item2, x.Item1 + 1, x.Item2 + 1, level));
                    }
                    else if (x.Item3 == 6 || x.Item3 == 9)
                    {
                        pts.Add(GetLPoint(x.Item1, x.Item2, x.Item1, x.Item2 + 1, level));
                        pts.Add(GetLPoint(x.Item1 + 1, x.Item2, x.Item1 + 1, x.Item2 + 1, level));
                    }
                    else if (x.Item3 == 7 || x.Item3 == 8)
                    {
                        pts.Add(GetLPoint(x.Item1, x.Item2, x.Item1, x.Item2 + 1, level));
                        pts.Add(GetLPoint(x.Item1, x.Item2, x.Item1 + 1, x.Item2, level));
                    }
                });
                ContourPts.Add(new Contour() { Points = OrderInDistance(pts), Level = level });
                Levels.Add(level);
                level += (level == 25) ? 75 : 100;
            }
            if (symmetry)
            {
                ContourPts.Add(new Contour() { Points = new List<MWPoint2D>() { new MWPoint2D(Lx, 0), new MWPoint2D(0, 0), new MWPoint2D(0, Ly) }, Level = level });
                ContourPts.ForEach(d => d.Points =
                    d.Points.Select(p => new MWPoint2D((Lx - p.X) * 1e3, (Ly - p.Y) * 1e3))
                    .Concat(d.Points.Select(p => new MWPoint2D((Lx - p.X) * 1e3, -(Ly - p.Y) * 1e3)).Reverse())
                    .Concat(d.Points.Select(p => new MWPoint2D(-(Lx - p.X) * 1e3, -(Ly - p.Y) * 1e3)))
                    .Concat(d.Points.Select(p => new MWPoint2D(-(Lx - p.X) * 1e3, (Ly - p.Y) * 1e3)).Reverse()).ToList()
                    );
            }
            else
            {
                ContourPts.Add(new Contour()
                {
                    Points = new List<MWPoint2D>()
                {
                    new MWPoint2D(Lx, 0),
                    new MWPoint2D(0, 0),
                    new MWPoint2D(0, Ly),
                    new MWPoint2D(lx, Ly),
                    new MWPoint2D(lx,ly),
                    new MWPoint2D(Lx,ly)
                },
                    Level = level
                });
                ContourPts.ForEach(d => d.Points = d.Points.Select(p => new MWPoint2D((p.X - Lx / 2) * 1e3, (p.Y - Ly / 2) * 1e3)).ToList());
                ContourPts.ForEach(d => d.Points.Add(d.Points[0])); // we add the first point to have closed contours.
            }
            Levels.Add(level);

            List<List<double>> XCoords = new List<List<double>>();
            List<List<double>> YCoords = new List<List<double>>();
            for (int i = 0; i < ContourPts.Count; i++)
            {
                XCoords.Add(ContourPts[i].Points.Select(p => p.X).ToList());
                YCoords.Add(ContourPts[i].Points.Select(p => p.Y).ToList());
            }
        }

        private int BinToInt(string s)
        {
            double res = 0;
            for (int i = 0; i < s.Length; i++)
            {
                int j = s.Length - 1 - i;
                res += Convert.ToInt16(s.Substring(j, 1)) * Math.Pow(2, i);
            }
            return Convert.ToInt16(res);
        }

        private MWPoint2D GetLPoint(int i0, int j0, int i1, int j1, double level)
        {
            double x0 = j0 == j1 ? i0 * Lx / (NX - 1) : j0 * Ly / (NY - 1);
            double y0 = Temp[i0, j0];
            double x1 = j0 == j1 ? i1 * Lx / (NX - 1) : j1 * Ly / (NY - 1);
            double y1 = Temp[i1, j1];
            double a = (y1 - y0) / (x1 - x0);
            double xl = x1 + (level - y1) / a;
            double X, Y = 0;
            if (i0 == i1)
            {
                X = i1 * Lx / (NX - 1);
                Y = xl;
            }
            else
            {
                X = xl;
                Y = j1 * Ly / (NY - 1);
            }
            if (Double.IsNaN(xl) || Double.IsInfinity(xl))
                Console.WriteLine("hoho, problemo");
            if (i0 != i1 && j0 != j1)
                Console.WriteLine("hoho, problemo");
            if (X > Lx || Y > Ly)
                Console.WriteLine("hoho, problemo");
            return new MWPoint2D(X, Y);
        }

        private List<MWPoint2D> OrderInDistance(List<MWPoint2D> pts)
        {
            List<MWPoint2D> lpts = pts;
            MWPoint2D pt = lpts.First(p => p.X == lpts.Max(v => v.X));
            List<MWPoint2D> res = new List<MWPoint2D> { pt };
            lpts.Remove(pt);
            while (lpts.Count > 0)
            {
                MWPoint2D p0 = lpts.Aggregate(lpts[0], (closest, next) =>
                   Points.Distance(res[res.Count - 1], next) < Points.Distance(res[res.Count - 1], closest) ? next : closest);
                res.Add(p0);
                //lpts.RemoveAll(e => e.X == p0.X && e.Y == p0.Y);
                lpts.Remove(p0);
            }
            return res;
        }
    }

    public class Contour
    {
        public List<MWPoint2D> Points { get; set; }
        public double Level { get; set; }
        //public PointCollection DisplayPoints { get; set; }
        //public PointCollection ContourPolygons { get; set; }
        //public Brush Color { get; set; }
    }
}
