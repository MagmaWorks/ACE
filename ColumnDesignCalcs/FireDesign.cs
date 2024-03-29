﻿using MathNet.Numerics.LinearAlgebra;
using MWGeometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace ColumnDesignCalc
{
    public enum FCurve { Standard, Hydrocarbon }

    /// <summary>
    /// Describes a temperature profile instance. Temperature profiles are calculated by solving the heat equation.
    /// </summary>
    public class TemperatureProfile
    {
        double Rho20 = 2500;

        int NT = 1800;
        int NX = 30;
        int NY = 30;

        [JsonIgnore]
        public Matrix<double> Temp;
        [JsonIgnore]
        public Vector<double> X;
        [JsonIgnore]
        public Vector<double> Y;

        double Lx;
        double Ly;
        double lx;
        double ly;
        double Theta;

        [JsonIgnore]
        public Dictionary<double, Matrix<double>> TempMap = new Dictionary<double, Matrix<double>>();
        
        public List<Contour> ContourPts { get; set; } = new List<Contour>();
        public List<double> Levels { get; set; } = new List<double>();

        // constructor for deserialization
        public TemperatureProfile()
        {

        }

        // For rectangular sections
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
                

                Temp.SetSubMatrix(1, 1, Temp2);

                if (fireResistances.Contains(t))
                    TempMap.Add(t, Temp.Clone());
            }

        }

        // For L Shaped sections
        public TemperatureProfile(double Hx, double Hy, double hx, double hy, double theta, double time, FCurve fcurve)
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
            Theta = theta;

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

                int n1 = (theta == 0 || theta == 270) ? nx : 0;
                int n2 = (theta == 0 || theta == 90) ? ny : 0;
                Temp.SetSubMatrix(n1, n2, M.Dense(NX - nx, NY - ny, Tfire));

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

        // For T Shaped sections
        public TemperatureProfile(double Hx, double Hy, double hx, double hy, double time, FCurve fcurve)
        {
            NT = Convert.ToInt32((time / 7200) * 1800);
            double dt = time / NT;
            while (NX < 3000)
            {
                double t = (NX - 1) * (Hx - hx) / 2 / Hx;
                double t2 = (NX - 1) * hx / Hx;
                if (t - Convert.ToInt32(t) == 0 && t2 - Convert.ToInt32(t2) == 0)
                    break;
                else
                    NX++;
            }
            while (NY < 3000)
            {
                double t = (NY - 1) * (Hy - hy) / 2 / Hy;
                double t2 = (NY - 1) * hy / Hy;
                if (t - Convert.ToInt32(t) == 0 && t2 - Convert.ToInt32(t2) == 0)
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

                Temp.SetSubMatrix(0, 0, M.Dense((NX - nx) / 2 + 1, NY - ny, Tfire));
                Temp.SetSubMatrix((NX + nx) / 2, 0, M.Dense((NX - nx) / 2, NY - ny, Tfire));

                var Temp2 = Temp.SubMatrix(1, NX - 2, 1, NY - 2)
                          + dt / (4 * dx * dx) * A.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(2, NX - 2, 1, NY - 2).PointwisePower(2) - 2 * Temp.SubMatrix(2, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(0, NX - 2, 1, NY - 2)) + Temp.SubMatrix(0, NX - 2, 1, NY - 2).PointwisePower(2))
                          + dt / (4 * dy * dy) * A.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(1, NX - 2, 2, NY - 2).PointwisePower(2) - 2 * Temp.SubMatrix(1, NX - 2, 2, NY - 2).PointwiseMultiply(Temp.SubMatrix(1, NX - 2, 0, NY - 2)) + Temp.SubMatrix(1, NX - 2, 0, NY - 2).PointwisePower(2))
                          + dt / (dx * dx) * B.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(2, NX - 2, 1, NY - 2) - 2 * Temp.SubMatrix(1, NX - 2, 1, NY - 2) + Temp.SubMatrix(0, NX - 2, 1, NY - 2))
                          + dt / (dy * dy) * B.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(1, NX - 2, 2, NY - 2) - 2 * Temp.SubMatrix(1, NX - 2, 1, NY - 2) + Temp.SubMatrix(1, NX - 2, 0, NY - 2));

                Temp.SetSubMatrix(1, 1, Temp2);

                if (fireResistances.Contains(t))
                    TempMap.Add(t, Temp.Clone());
            }
        }

        // For Custom Shape sections
        public TemperatureProfile(double cLx, double cLy, double d1x, double d1y, double d2x, double d2y, double d3x, double d3y, double d4x, double d4y, double time, FCurve fcurve)
        {
            NT = Convert.ToInt32((time / 7200) * 1800);
            double dt = time / NT;
            while (NX < 3000)
            {
                double t1 = (NX - 1) * d1x / cLx;
                double t2 = (NX - 1) * d2x / cLx;
                double t3 = (NX - 1) * d3x / cLx;
                double t4 = (NX - 1) * d4x / cLx;
                if (t1 - Convert.ToInt32(t1) == 0 && t2 - Convert.ToInt32(t2) == 0 && t3 - Convert.ToInt32(t3) == 0 && t4 - Convert.ToInt32(t4) == 0)
                    break;
                else
                    NX++;
            }
            while (NY < 3000)
            {
                double t1 = (NY - 1) * d1y / cLy;
                double t2 = (NY - 1) * d2y / cLy;
                double t3 = (NY - 1) * d3y / cLy;
                double t4 = (NY - 1) * d4y / cLy;
                if (t1 - Convert.ToInt32(t1) == 0 && t2 - Convert.ToInt32(t2) == 0 && t3 - Convert.ToInt32(t3) == 0 && t4 - Convert.ToInt32(t4) == 0)
                    break;
                else
                    NY++;
            }
            double dx = cLx / (NX - 1);
            double dy = cLy / (NY - 1);

            int n1x = Convert.ToInt32(d1x / dx);
            int n1y = Convert.ToInt32(d1y / dy);
            int n2x = Convert.ToInt32(d2x / dx);
            int n2y = Convert.ToInt32(d2y / dy);
            int n3x = Convert.ToInt32(d3x / dx);
            int n3y = Convert.ToInt32(d3y / dy);
            int n4x = Convert.ToInt32(d4x / dx);
            int n4y = Convert.ToInt32(d4y / dy);

            double[] fireResistances = new double[] { 30, 60, 90, 120, 180, 240 };

            Lx = cLx;
            Ly = cLy;

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

                Temp.SetSubMatrix(0, 0, M.Dense(n1x + 1, n1y + 1, Tfire));
                Temp.SetSubMatrix(NX - n2x - 1 , 0, M.Dense(n2x + 1, n2y + 1, Tfire));
                Temp.SetSubMatrix(NX - n3x - 1, NY - n3y - 1, M.Dense(n3x + 1, n3y + 1, Tfire));
                Temp.SetSubMatrix(0 , NY - n4y - 1, M.Dense(n4x + 1, n4y + 1, Tfire));

                var Temp2 = Temp.SubMatrix(1, NX - 2, 1, NY - 2)
                          + dt / (4 * dx * dx) * A.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(2, NX - 2, 1, NY - 2).PointwisePower(2) - 2 * Temp.SubMatrix(2, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(0, NX - 2, 1, NY - 2)) + Temp.SubMatrix(0, NX - 2, 1, NY - 2).PointwisePower(2))
                          + dt / (4 * dy * dy) * A.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(1, NX - 2, 2, NY - 2).PointwisePower(2) - 2 * Temp.SubMatrix(1, NX - 2, 2, NY - 2).PointwiseMultiply(Temp.SubMatrix(1, NX - 2, 0, NY - 2)) + Temp.SubMatrix(1, NX - 2, 0, NY - 2).PointwisePower(2))
                          + dt / (dx * dx) * B.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(2, NX - 2, 1, NY - 2) - 2 * Temp.SubMatrix(1, NX - 2, 1, NY - 2) + Temp.SubMatrix(0, NX - 2, 1, NY - 2))
                          + dt / (dy * dy) * B.SubMatrix(1, NX - 2, 1, NY - 2).PointwiseMultiply(Temp.SubMatrix(1, NX - 2, 2, NY - 2) - 2 * Temp.SubMatrix(1, NX - 2, 1, NY - 2) + Temp.SubMatrix(1, NX - 2, 0, NY - 2));

                Temp.SetSubMatrix(1, 1, Temp2);

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


        public void GetContours(double R, string shape, Column col = null)
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
            if (shape == "Rectangular")
            {
                ContourPts.Add(new Contour() { Points = new List<MWPoint2D>() { new MWPoint2D(Lx, 0), new MWPoint2D(0, 0), new MWPoint2D(0, Ly) }, Level = level });
                ContourPts.ForEach(d => d.Points =
                    d.Points.Select(p => new MWPoint2D((Lx - p.X) * 1e3, (Ly - p.Y) * 1e3))
                    .Concat(d.Points.Select(p => new MWPoint2D((Lx - p.X) * 1e3, -(Ly - p.Y) * 1e3)).Reverse())
                    .Concat(d.Points.Select(p => new MWPoint2D(-(Lx - p.X) * 1e3, -(Ly - p.Y) * 1e3)))
                    .Concat(d.Points.Select(p => new MWPoint2D(-(Lx - p.X) * 1e3, (Ly - p.Y) * 1e3)).Reverse()).ToList()
                    );
            }
            else if (shape == "LShaped")
            {
                double angle = Theta * Math.PI / 180;
                double H1 = Lx * Math.Abs(Math.Cos(angle)) + Ly * Math.Abs(Math.Sin(angle));
                double h1 = lx * Math.Abs(Math.Cos(angle)) + ly * Math.Abs(Math.Sin(angle));
                double H2 = Lx * Math.Abs(Math.Sin(angle)) + Ly * Math.Abs(Math.Cos(angle));
                double h2 = lx * Math.Abs(Math.Sin(angle)) + ly * Math.Abs(Math.Cos(angle));

                List<MWPoint2D> contour = new List<MWPoint2D>()
                {
                    new MWPoint2D(0, 0),
                    new MWPoint2D(H1, 0),
                    new MWPoint2D(H1, h2),
                    new MWPoint2D(h1, h2),
                    new MWPoint2D(h1, H2),
                    new MWPoint2D(0, H2)
                };
                contour = contour.Select(p => new MWPoint2D(p.X - H1 / 2, p.Y - H2 / 2)).ToList();
                contour = contour.Select(p => new MWPoint2D(Math.Cos(angle) * p.X - Math.Sin(angle) * p.Y,
                                                  Math.Sin(angle) * p.X + Math.Cos(angle) * p.Y)).ToList();
                contour = contour.Select(p => new MWPoint2D(p.X + Lx / 2, p.Y + Ly / 2)).ToList();
                ContourPts.Add(new Contour()
                {
                    Points = contour,
                    Level = level
                });
                ContourPts.ForEach(d => d.Points = d.Points.Select(p => new MWPoint2D((p.X - Lx / 2) * 1e3, (p.Y - Ly / 2) * 1e3)).ToList());
                ContourPts.ForEach(d => d.Points.Add(d.Points[0])); // we add the first point to have closed contours.
            }
            else if (shape == "TShaped")
            {
                List<MWPoint2D> contour = new List<MWPoint2D>()
                {
                    new MWPoint2D((Lx-lx)/2, 0),
                    new MWPoint2D((Lx+lx)/2, 0),
                    new MWPoint2D((Lx+lx)/2, Ly - ly),
                    new MWPoint2D(Lx, Ly - ly),
                    new MWPoint2D(Lx, Ly),
                    new MWPoint2D(0, Ly),
                    new MWPoint2D(0,Ly - ly),
                    new MWPoint2D((Lx-lx)/2,Ly - ly)
                };
                ContourPts.Add(new Contour()
                {
                    Points = contour,
                    Level = level
                });
                ContourPts.ForEach(d => d.Points = d.Points.Select(p => new MWPoint2D((p.X - Lx / 2) * 1e3, (p.Y - Ly / 2) * 1e3)).ToList());
                ContourPts.ForEach(d => d.Points.Add(d.Points[0])); // we add the first point to have closed contours.
            }
            else if (shape == "CustomShape")
            {
                ContourPts.ForEach(d => d.Points = d.Points.Select(p => new MWPoint2D((p.X - Lx / 2) * 1e3, (p.Y - Ly / 2) * 1e3)).ToList());
                List<MWPoint2D> contour = col.GetCustomShapeContour();
                ContourPts.Add(new Contour()
                {
                    Points = contour,
                    Level = level
                });
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

        public static List<MWPoint2D> OrderInDistance(List<MWPoint2D> pts)
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
