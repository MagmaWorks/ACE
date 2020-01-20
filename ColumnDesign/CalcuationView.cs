using MWGeometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ColumnDesign
{
    public class CalcuationView : ViewModelBase
    {
        const double gs = 1.15;
        const double gc = 1.5;
        const double acc = 0.85;

        List<FormulaeVM> formulae;
        public List<FormulaeVM> Formulae
        {
            get { return formulae; }
            set { formulae = value; RaisePropertyChanged(nameof(Formulae)); }
        }

        public Column column;

        public List<FireData> fireTable = new List<FireData>();

        public CalcuationView()
        {
            SetFireData();
        }

        public bool UpdateMinMaxSteelCheck(Column c)
        {
            double Ac = GetConcreteArea(c);
            double Asmin = Math.Max(0.1*c.SelectedLoad.P/(c.SteelGrade.Fy*1e-3/gs) ,0.002 * Ac);
            double Asmax = 0.04 * Ac;
            double As = 0;
            if (c.Shape == GeoShape.Rectangular)
                As = 2 * (c.NRebarX + c.NRebarY - 2) * Math.PI * Math.Pow(c.BarDiameter / 2.0, 2);
            else if (c.Shape == GeoShape.Circular)
                As = c.NRebarCirc * Math.PI * Math.Pow(c.BarDiameter / 2.0, 2);
            else if (c.Shape == GeoShape.Polygonal)
                As = c.Edges * Math.PI * Math.Pow(c.BarDiameter / 2.0, 2);
            else if (c.Shape == GeoShape.LShaped)
                As = (8 + (c.NRebarX - 3) * 2 + (c.NRebarY - 3) * 2) * Math.PI * Math.Pow(c.BarDiameter / 2.0, 2);

            FormulaeVM f1 = new FormulaeVM();
            f1.Narrative = "Min / Max steel";
            f1.Expression = new List<string>();
            f1.Expression.Add(@"A_{s,min} = max\left(0.1N_{Ed}/f_{yd};0.002A_c\right) = " + Math.Round(Asmin) + "mm^2");
            f1.Expression.Add(@"A_{s,max} = 0.04A_c = " + Math.Round(Asmax) + "mm^2");
            f1.Expression.Add(@"A_s = " + Math.Round(As) + "mm^2");
            
            RaisePropertyChanged(nameof(Formulae));

            if (As > Asmax)
            {
                f1.Conclusion = "FAIL --> too much reinforcement";
                f1.Status = CalcStatus.FAIL;
                formulae.Add(f1);
                return false;
            }
            else if(As < Asmin)
            {
                f1.Conclusion = "FAIL --> not enough reinforcement";
                f1.Status = CalcStatus.FAIL;
                formulae.Add(f1);
                return false;
            }

            f1.Conclusion = "PASS";
            f1.Status = CalcStatus.PASS;
            formulae.Add(f1);
            return true;
        }

        public bool UpdateFireDesign(Column col)
        {
            bool res = false;
            //FormulaeVM f1 = formulae.FirstOrDefault(f => f.Narrative == "Nominal cover for fire and bond requirements") ?? new FormulaeVM();
            FormulaeVM f1 = new FormulaeVM();
            f1.Narrative = "Nominal cover for fire and bond requirements";
            
            f1.Expression = new List<string>();
            switch(col.FireDesignMethod)
            {
                case (FDesignMethod.Table):
                    f1.Ref = "EN1992-1-2 5.3.2.";
                    res = UpdateFireDesignTable(col, ref f1);
                    break;
                case (FDesignMethod.Isotherm_500):
                    var checkIso = col.CheckFireIsotherm500();
                    res = checkIso.Item1;
                    f1 = checkIso.Item2;
                    break;
                case (FDesignMethod.Zone_Method):
                    var checkZone = col.CheckFireZoneMethod();
                    res = checkZone.Item1;
                    f1 = checkZone.Item2;
                    break;
                case (FDesignMethod.Advanced):
                    f1.Ref = "MAGMA WORKS FIRE DESIGN";
                    f1.Expression.Add(@"AdvancedMethod");
                    res = UpdateAdvancedFireCheck(col);
                    f1.Status = res ? CalcStatus.PASS : CalcStatus.FAIL;
                    f1.Conclusion = res ? "PASS" : "FAIL";
                    break;
            }
            
            formulae.Insert(0,f1);
            
            RaisePropertyChanged(nameof(Formulae));
            
            return res;
        }

        public bool UpdateFireDesignTable(Column col, ref FormulaeVM f1)
        {
            Column c = col;
            double Nrd = (c.GetSteelArea() * c.SteelGrade.Fy / gs + c.GetConcreteArea() * acc * c.ConcreteGrade.Fc / gc) / 1E3;
            double mufi = c.FireLoad.P / Nrd;

            // Eurocode Table 5.2.1a
            double afi = 0;
            mufi = (mufi <= 0.35) ? 0.2 : ((mufi <= 0.6) ? 0.5 : (mufi <= 0.7 ? 0.7 : 2));
            if (mufi == 2)
            {
                f1.Expression.Add(@"N_{Ed,fi} = "+ Math.Round(c.FireLoad.P) + " kN");
                f1.Expression.Add(@"N_{Rd} = A_s f_{yd} + A_cf_{cd} = "+ Math.Round(Nrd) + " kN");
                f1.Expression.Add(@"N_{Ed,fi} > 0.7 N_{Rd}");

                f1.Status = CalcStatus.FAIL;
                f1.Conclusion = "FAIL";
                RaisePropertyChanged(nameof(Formulae));
                return false;
            }
            List<FireData> fdata = fireTable.Where(x => x.mu == mufi && x.R == column.R && x.sidesExposed == column.SidesExposed).ToList();
            fdata = fdata.OrderByDescending(x => x.minDimension).ToList();
            switch (c.Shape)
            {
                case (GeoShape.Rectangular):
                    for (int i = 0; i < fdata.Count; i++)
                    {
                        if (column.LX >= fdata[i].minDimension && column.LY >= fdata[i].minDimension)
                        {
                            afi = fdata[i].axisDistance;
                            break;
                        }
                    }
                    break;
                case (GeoShape.Circular):
                    for (int i = 0; i < fdata.Count; i++)
                    {
                        if (column.Diameter >= fdata[i].minDimension)
                        {
                            afi = fdata[i].axisDistance;
                            break;
                        }
                    }
                    break;
                case (GeoShape.Polygonal):
                    for (int i = 0; i < fdata.Count; i++)
                    {
                        if (2 * column.Radius >= fdata[i].minDimension)
                        {
                            afi = fdata[i].axisDistance;
                            break;
                        }
                    }
                    break;
            }

            double cminb = Math.Max(column.LinkDiameter, column.BarDiameter - column.LinkDiameter);
            double cnommin = Math.Max(afi - c.BarDiameter / 2.0 - c.LinkDiameter, cminb + 10);


            f1.Expression.Add(@"c_{min,b} = max\left(\phi_v,\phi-\phi_v\right) = " + cminb + "mm");

            f1.Expression.Add(@"\mu_{fi}=0.7\frac{N_{Ed}}{N_{Rd}} \simeq " + Math.Round(mufi, 1));
            if (afi == 0)
            {
                f1.Status = CalcStatus.FAIL;
                f1.Conclusion = "FAIL --> minimum dimension is " + fireTable.Where(x => x.mu == mufi && x.R == column.R).Min(x => x.minDimension) + " mm";
                return false;
            }
            else
            {
                f1.Expression.Add(@"a_{fi} = " + afi + " mm");
                f1.Expression.Add(@"\Delta c_{dev} = 10 mm");
                f1.Expression.Add(@"c_{nom_{min}} = max \left(a_{fi} - \phi/2-\phi_v, c_{min,b} + \Delta c_{dev}\right) = " + cnommin + "mm");
                if (c.CoverToLinks >= cnommin)
                {
                    f1.Status = CalcStatus.PASS;
                    f1.Conclusion = "PASS";
                    return true;
                }
                else
                {
                    f1.Conclusion = "FAIL";
                    f1.Status = CalcStatus.FAIL;
                    return false;
                }
            }
        }

        public bool UpdateAdvancedFireCheck(Column c)
        {
            return c.CheckIsInsideFireID();
        }

        public bool UpdateSecondOrderCheck()
        {
            Column c = column;

            Load load;

            if (c.AllLoads) c.GetDesignMoments();

            load = (c.AllLoads) ? c.Loads.Aggregate(c.Loads[0], ((current, next) =>
                      (   (Math.Abs(next.Mxd) > Math.Abs(current.Mxd) && Math.Abs(next.Mxd) > Math.Abs(current.Myd))
                       || (Math.Abs(next.Myd) > Math.Abs(current.Mxd) && Math.Abs(next.Myd) > Math.Abs(current.Myd))
                      ) ? next : current)) : c.SelectedLoad;

            FormulaeVM f0 = new FormulaeVM();

            bool spacing = CheckSpacing(c, f0);
            
            formulae.Add(f0);

            double abar = Math.PI * Math.Pow(c.BarDiameter / 2.0, 2);
            double As = (c.NRebarX * c.NRebarY - (c.NRebarX - 2) * (c.NRebarY - 2)) * Math.PI * Math.Pow(c.BarDiameter / 2.0, 2);
            double[] dxs = new double[c.NRebarY];
            dxs[0] = c.LY - c.CoverToLinks - c.LinkDiameter - c.BarDiameter / 2.0;

            FormulaeVM f0x = new FormulaeVM();
            f0x.Narrative = "Effective depth for bending about x axis";
            f0x.Expression = new List<string>();
            
            FormulaeVM f0y = new FormulaeVM();
            f0y.Narrative = "Effective depth for bending about y axis";
            f0y.Expression = new List<string>();

            double[] effDepths = GetEffectiveDepths(column, f0x,f0y);
            double dx = effDepths[0];
            double dy = effDepths[1];

            formulae.Add(f0x);
            formulae.Add(f0y);

            double l0 = c.EffectiveLength * c.Length;
            FormulaeVM f1 = new FormulaeVM();
            f1.Narrative = "Column effective length";
            f1.Expression = new List<string>();
            f1.Expression.Add(@"l_{0} = f \times l = " + l0 + " mm");
            formulae.Add(f1);
            
            FormulaeVM f2 = new FormulaeVM();
            f2.Narrative = "Column slenderness about x axis";
            f2.Ref = "5.8.3.2";
            f2.Expression = new List<string>();
            FormulaeVM f3 = new FormulaeVM();
            f3.Narrative = "Column slenderness about y axis";
            f3.Ref = "5.8.3.2";
            f3.Expression = new List<string>();
            double[] Inertias = GetSecondMomentInertia(column,f2,f3);
            double Ix = Inertias[0];
            double Iy = Inertias[1];
            double A = GetColumnArea(c);
            double Ac = GetConcreteArea(c);
            
            double ix = Math.Sqrt(Ix / A);
            double iy = Math.Sqrt(Iy / A);

            f2.Expression.Add(@"i_x = \sqrt{I_x/A} = " + Math.Round(ix) + " mm");
            f3.Expression.Add(@"i_y = \sqrt{I_y/A} = " + Math.Round(iy) + " mm");

            double lambdax = l0 / ix;
            f2.Expression.Add(@"\lambda_x = l_0/i_x = " + Math.Round(lambdax, 1));
            formulae.Add(f2);

            double lambday = l0 / iy;
            f3.Expression.Add(@"\lambda_y = l_0/i_y = " + Math.Round(lambday, 1));
            formulae.Add(f3);

            double ei = l0 / 400;
            int kx = load.MxTop * load.MxBot >= 0 ? 1 : -1;
            double M01x = kx * Math.Min(Math.Abs(load.MxTop), Math.Abs(load.MxBot)) + ei * load.P / 1E3;
            double M02x = Math.Max(Math.Abs(load.MxTop), Math.Abs(load.MxBot)) + ei * load.P / 1E3;
            FormulaeVM f4 = new FormulaeVM();
            f4.Narrative = "Moments about x axis including imperfections";
            f4.Expression = new List<string>();
            f4.Expression.Add(@"e_{i} = l_0/400 = " + Math.Round(ei,1) + " mm");
            f4.Expression.Add(@"k = M_x^{top}/\left|M_x^{top}\right|*M_x^{bot}/\left|M_x^{bot}\right| = " + kx);
            f4.Expression.Add(@"M_{01x} = k \times min\left(\left|M_x^{top}\right|,\left|M_x^{bot}\right|\right) + e_iN_{Ed} = " + Math.Round(M01x,1) + " kN.m");
            f4.Expression.Add(@"M_{02x} = max\left(\left|M_x^{top}\right|,\left|M_x^{bot}\right|\right) + e_iN_{Ed} = " + Math.Round(M02x,1) + " kN.m");
            formulae.Add(f4);
            
            double omega = As * c.SteelGrade.Fy / gs / 
                (Ac * acc * c.ConcreteGrade.Fc / gc);
            double B = Math.Sqrt(1 + 2 * omega);
            double rmx = M01x / M02x;
            double C = 1.7 - rmx;
            double n = load.P / (Ac * acc * c.ConcreteGrade.Fc / gc) * 1E3;
            double lambdaxlim = 20 * 0.7 * B * C / Math.Sqrt(n);
            bool secondorderx = false;
            FormulaeVM f5 = new FormulaeVM();
            f5.Narrative = "Slenderness limit about x axis";
            f5.Expression = new List<string>();
            f5.Expression.Add(@"A = 0.7");
            f5.Expression.Add(@"\omega = A_s f_{yd} / \left( A_c f_{cd} \right) = "+ Math.Round(omega,3));
            f5.Expression.Add(@"B = \sqrt{\left(1+2\omega\right)} = " + Math.Round(B,2));
            f5.Expression.Add(@"r_{mx} = " + Math.Round(rmx,3));
            f5.Expression.Add(@"C = 1.7 - r_mx = "+ Math.Round(C, 3));
            f5.Expression.Add(@"n = N_{Ed} / \left( A_cf_{cd} \right) = "+ Math.Round(n,3));
            f5.Expression.Add(@"\lambda_{limx} = 20ABC/\sqrt{n} = " + Math.Round(lambdaxlim,1));
            if (lambdax < lambdaxlim)
            {
                f5.Expression.Add(@"\lambda_x < \lambda_{xlim}");
                f5.Conclusion = "Second order effects may be ignored";
            }
            else
            {
                f5.Expression.Add(@"\lambda_x >= \lambda_{xlim}");
                f5.Conclusion = "Second order effects must be considered";
                secondorderx = true;
            }
            formulae.Add(f5);
            int ky = (load.MyTop * load.MyBot >= 0) ? 1 : -1;
            double M01y = ky * Math.Min(Math.Abs(load.MyTop), Math.Abs(load.MyBot)) + ei * load.P / 1E3;
            double M02y = Math.Max(Math.Abs(load.MyTop), Math.Abs(load.MyBot)) + ei * load.P / 1E3;
            FormulaeVM f6 = new FormulaeVM();
            f6.Narrative = "Moments about y axis including imperfections";
            f6.Expression = new List<string>();
            f6.Expression.Add(@"e_{i} = l_0/400 = " + Math.Round(ei, 1) + " mm");
            f6.Expression.Add(@"k = M_y^{top}/\left|M_y^{top}\right|*M_y^{bot}/\left|M_y^{bot}\right| = " + ky);
            f6.Expression.Add(@"M_{01y} =  k \times min\left(\left|M_y^{top}\right|,\left|M_y^{bot}\right|\right) + e_iN_{Ed} = " + Math.Round(M01y, 1) + " kN.m");
            f6.Expression.Add(@"M_{02y} = max\left(\left|M_y^{top}\right|,\left|M_y^{bot}\right|\right) + e_iN_{Ed} = " + Math.Round(M02y, 1) + " kN.m");
            formulae.Add(f6);

            double rmy = M01y / M02y;
            C = 1.7 - rmy;
            double lambdaylim = 20 * 0.7 * B * C / Math.Sqrt(n);
            bool secondordery = false;
            FormulaeVM f7 = new FormulaeVM();
            f7.Narrative = "Slenderness limit about y axis";
            f7.Expression = new List<string>();
            f7.Expression.Add(@"A = 0.7");
            f7.Expression.Add(@"\omega = A_s f_{yd} / \left( A_c f_{cd} \right) = " + Math.Round(omega, 3));
            f7.Expression.Add(@"B = \sqrt{\left(1+2\omega\right)} = " + Math.Round(B, 2));
            f7.Expression.Add(@"r_{my} = " + Math.Round(rmy, 3));
            f7.Expression.Add(@"C = 1.7 - r_{my} = " + Math.Round(C, 3));
            f7.Expression.Add(@"n = N_{Ed} / \left( A_cf_{cd} \right) = " + Math.Round(n, 3));
            f7.Expression.Add(@"\lambda_{limx} = 20ABC/\sqrt{n} = " + Math.Round(lambdaylim, 1));
            if (lambday < lambdaylim)
            {
                f7.Expression.Add(@"\lambda_y < \lambda_{ylim}");
                f7.Conclusion = "Second order effects may be ignored";
            }
            else
            {
                f7.Expression.Add(@"\lambda_y >= \lambda_{ylim}");
                f7.Conclusion = "Second order effects must be considered";
                secondordery = true;
            }
            formulae.Add(f7);

            FormulaeVM f8 = new FormulaeVM();
            double[] axialDist = GetAxialLength(column);
            double peri = GetPerimeter(column);
            if (!secondorderx)
            {
                double Medx = Math.Max(M02x, load.P * Math.Max(axialDist[1] * 1E-3 / 30, 20 * 1E-3));
                c.SelectedLoad.Mxd = Math.Round(Medx,1);
                f8.Narrative = "Design moment about x for a stocky column";
                f8.Expression = new List<string>();
                f8.Expression.Add(@"M_{Edx} = max \left( M_{02x}, N_{Ed}\times max \left(h/30,20 mm\right)\right) = " + Math.Round(Medx) + "kN.m");
            }
            else
            {
                double u = peri;
                double nu = 1 + omega;
                double Kr = Math.Min(1, (nu - n) / (nu - 0.4));
                //double d = c.LY - c.CoverToLinks - c.LinkDiameter - c.BarDiameter / 2;
                double eyd = c.SteelGrade.Fy / gs / c.SteelGrade.E / 1E3;
                double r0 = 0.45 * dx / eyd;
                double h0 = 2 * Ac / u;
                double alpha1 = Math.Pow(35 / (c.ConcreteGrade.Fc + 8), 0.7);
                double alpha2 = Math.Pow(35 / (c.ConcreteGrade.Fc + 8), 0.2);
                double phiRH = (1 + (0.5 / (0.1 * Math.Pow(h0, 1.0 / 3))) * alpha1) * alpha2;
                double bfcm = 16.8 / Math.Sqrt(c.ConcreteGrade.Fc + 8);
                double bt0 = 1 / (0.1 + Math.Pow(28, 0.2));
                double phi0 = phiRH * bfcm * bt0;
                double phiInf = phi0;
                double phiefy = phiInf * 0.8;
                double betay = 0.35 + c.ConcreteGrade.Fc / 200 - lambdax / 150;
                double kphiy = Math.Max(1, 1 + betay * phiefy);
                double r = r0 / (Kr * kphiy);
                double e2x = Math.Pow(l0, 2) / (r * 10);
                double m2x = load.P * e2x * 1E-3;
                double M0e = 0.6 * M02x + 0.4 * M01x;
                List<double> Ms = new List<double>() { M02x, M0e + m2x, M01x + 0.5 * m2x, Math.Max(axialDist[1] * 1E-3 / 30, 20 * 1E-3) * load.P };
                double Medx = Ms.Max();
                c.SelectedLoad.Mxd = Math.Round(Medx,1);

                f8.Narrative = "Design moment about x for a slender column";
                f8.Ref = "5.8.8";
                f8.Expression = new List<string>();
                f8.Expression.Add(@"RH = 50\%");
                f8.Expression.Add(@"u = " + Math.Round(u) + " mm");
                f8.Expression.Add(@"t_{0} = 28 days");
                f8.Expression.Add(@"n_u = 1 + \omega = " + Math.Round(nu,3));
                f8.Expression.Add(@"n_{bal} = 0.4");
                f8.Expression.Add(@"K_r = min\left(1.0,(n_u-n)/(n_u-n_{bal})\right) = " + Math.Round(Kr,3));
                f8.Expression.Add(@"\varepsilon_{yd} = f_{yd}/E_s = " + Math.Round(eyd,5));
                f8.Expression.Add(@"1/r_0 = \varepsilon_{yd}/\left(0.45d\right) = " + Math.Round(1/r0,7) + @"mm^{-1}");
                f8.Expression.Add(@"h_0 = 2 A_c/u = " + Math.Round(h0));
                f8.Expression.Add(@"\alpha_1 = (35 / f_{cm})^{0.7} = " + Math.Round(alpha1,3));
                f8.Expression.Add(@"\alpha_2 = (35 / f_{cm})^{0.2} = " + Math.Round(alpha2,3));
                f8.Expression.Add(@"\phi_{RH}=\left[1+\left((1-RH/100\%)/(0.1\times(h_0)^{1/3})\right)\times \alpha1\right]\times \alpha2 = " + Math.Round(phiRH,3));
                f8.Expression.Add(@"\beta_{fcm}=16.8/\sqrt{f_{cm}} = " + Math.Round(bfcm,3));
                f8.Expression.Add(@"\beta_{t_0} = 1/(0.1 + t_0^{0.2} = "+ Math.Round(bt0,3));
                f8.Expression.Add(@"\phi_0 = \phi_{RH}\beta_{fcm}\beta{t0} = " + Math.Round(phi0,3));
                f8.Expression.Add(@"\beta_{c\infty} = 1.00");
                f8.Expression.Add(@"\phi_{\infty} = \phi_0\beta_{c\infty} = "+ Math.Round(phiInf,3));
                f8.Expression.Add(@"r_{Mx} = 0.80");
                f8.Expression.Add(@"\phi_{efy}=\phi_{\infty}r_{My} = " + Math.Round(phiefy,3));
                f8.Expression.Add(@"\beta_y = 0.35 + f_{ck}/200 - \lambda_x/150 = " + Math.Round(betay,3));
                f8.Expression.Add(@"K_{\phi x} = max(1, 1 + \beta_y\phi_{efy}) = " + Math.Round(kphiy,3));
                f8.Expression.Add(@"1/r = K_rK_{\phi x}/r_0 = "+Math.Round(1/r,7)+@"mm^{-1}");
                f8.Expression.Add("c = 10");
                f8.Expression.Add("e_{2x} = l_0^2/(rc) = " + Math.Round(e2x) + " mm");
                f8.Expression.Add(@"M_{2x} = N_{Ed} \times e_{2x} = " + Math.Round(m2x,1) + "kN.m");
                f8.Expression.Add(@"M_{0e} = 0.6M_{02x} + 0.4M_{01x} = " + Math.Round(M0e,1) + "kN.m");
                f8.Expression.Add(@"M_{Edx} = max\left(M_{02}, M_{0e}+M_{2x},M_{01}+0.5M_{2x},e_0N_{Ed}\right) = " + Math.Round(Medx,1) + " kN.m");
            }
            formulae.Add(f8);

            FormulaeVM f9 = new FormulaeVM();
            if (!secondordery)
            {
                double Medy = Math.Max(M02y, load.P * Math.Max(axialDist[0] * 1E-3 / 30, 20 * 1E-3));
                c.SelectedLoad.Myd = Math.Round(Medy,1);
                f9.Narrative = "Design moment about y for a stocky column";
                f9.Expression = new List<string>();
                f9.Expression.Add(@"M_{Edy} = max \left( M_{02y}, N_{Ed}\times max \left(h/30,20 mm\right)\right) = " + Math.Round(Medy) + "kN.m");
            }
            else
            {
                double u = peri;
                double nu = 1 + omega;
                double Kr = Math.Min(1, (nu - n) / (nu - 0.4));
                //double d = c.LY - c.CoverToLinks - c.LinkDiameter - c.BarDiameter / 2;
                double eyd = c.SteelGrade.Fy / gs / c.SteelGrade.E / 1E3;
                double r0 = 0.45 * dy / eyd;
                double h0 = 2 * Ac / u;
                double alpha1 = Math.Pow(35 / (c.ConcreteGrade.Fc + 8), 0.7);
                double alpha2 = Math.Pow(35 / (c.ConcreteGrade.Fc + 8), 0.2);
                double phiRH = (1 + (0.5 / (0.1 * Math.Pow(h0, 1.0 / 3))) * alpha1) * alpha2;
                double bfcm = 16.8 / Math.Sqrt(c.ConcreteGrade.Fc + 8);
                double bt0 = 1 / (0.1 + Math.Pow(28, 0.2));
                double phi0 = phiRH * bfcm * bt0;
                double phiInf = phi0;
                double phiefy = phiInf * 0.8;
                double betay = 0.35 + c.ConcreteGrade.Fc / 200 - lambday / 150;
                double kphiy = Math.Max(1, 1 + betay * phiefy);
                double r = r0 / (Kr * kphiy);
                double e2y = Math.Pow(l0, 2) / (r * 10);
                double m2y = load.P * e2y * 1E-3;
                double M0e = 0.6 * M02y + 0.4 * M01y;
                List<double> Ms = new List<double>() { M02y, M0e + m2y, M01y + 0.5 * m2y, Math.Max(axialDist[0] * 1E-3 / 30, 20 * 1E-3) * load.P };
                double Medy = Ms.Max();
                c.SelectedLoad.Myd = Math.Round(Medy,1);

                f9.Narrative = "Design moment about y for a slender column";
                f9.Ref = "5.8.8";
                f9.Expression = new List<string>();
                f9.Expression.Add(@"RH = 50\%");
                f9.Expression.Add(@"u = " + u + " mm");
                f9.Expression.Add(@"t_{0} = 28 days");
                f9.Expression.Add(@"n_u = 1 + \omega = " + Math.Round(nu, 3));
                f9.Expression.Add(@"n_{bal} = 0.4");
                f9.Expression.Add(@"K_r = min\left(1.0,(n_u-n)/(n_u-n_{bal})\right) = " + Math.Round(Kr, 3));
                f9.Expression.Add(@"\varepsilon_{yd} = f_{yd}/E_s = " + Math.Round(eyd, 5));
                f9.Expression.Add(@"1/r_0 = \varepsilon_{yd}/\left(0.45d\right) = " + Math.Round(1 / r0, 7) + @"mm^{-1}");
                f9.Expression.Add(@"h_0 = 2 A_c/u = " + Math.Round(h0));
                f9.Expression.Add(@"\alpha_1 = (35 / f_{cm})^{0.7} = " + Math.Round(alpha1,3));
                f9.Expression.Add(@"\alpha_2 = (35 / f_{cm})^{0.2} = " + Math.Round(alpha2,3));
                f9.Expression.Add(@"\phi_{RH}=\left[1+\left((1-RH/100\%)/(0.1\times(h_0)^{1/3})\right)\times \alpha1\right]\times \alpha2 = " + Math.Round(phiRH,3));
                f9.Expression.Add(@"\beta_{fcm}=16.8/\sqrt{f_{cm}} = " + Math.Round(bfcm,3));
                f9.Expression.Add(@"\beta_{t_0} = 1/(0.1 + t_0^{0.2} = " + Math.Round(bt0,3));
                f9.Expression.Add(@"\phi_0 = \phi_{RH}\beta_{fcm}\beta{t0} = " + Math.Round(phi0,3));
                f9.Expression.Add(@"\beta_{c\infty} = 1.00");
                f9.Expression.Add(@"\phi_{\infty} = \phi_0\beta_{c\infty} = " + Math.Round(phiInf,3));
                f9.Expression.Add(@"r_{Mx} = 0.80");
                f9.Expression.Add(@"\phi_{efy}=\phi_{\infty}r_{My} = " + Math.Round(phiefy,2));
                f9.Expression.Add(@"\beta_y = 0.35 + f_{ck}/200 - \lambda_y/150 = " + Math.Round(betay,3));
                f9.Expression.Add(@"K_{\phi x} = max(1, 1 + \beta_y\phi_{efy}) = " + Math.Round(kphiy, 3));
                f9.Expression.Add(@"1/r = K_rK_{\phi x}/r_0 = " + Math.Round(1 / r, 7) + @"mm^{-1}");
                f9.Expression.Add("c = 10");
                f9.Expression.Add("e_{2y} = l_0^2/(rc) = " + Math.Round(e2y) + " mm");
                f9.Expression.Add(@"M_{2y} = N_{Ed}\times e_{2y} = " + Math.Round(m2y,1) + "kN.m");
                f9.Expression.Add(@"M_{0e} = 0.6M_{02y} + 0.4M_{01y} = " + Math.Round(M0e,1) + "kN.m");
                f9.Expression.Add(@"M_{Edy} = max\left(M_{02}, M_{0e}+M_{2y},M_{01y}+0.5M_{2y},e_0N_{Ed}\right) = " + Math.Round(Medy,1) + " kN.m");
            }
            formulae.Add(f9);

            if (c?.FireLoad?.Name == "0.7*[selected]")
            {
                c.FireLoad.P = 0.7 * c.SelectedLoad.P;
                c.FireLoad.Mxd = 0.7 * c.SelectedLoad.Mxd;
                c.FireLoad.Myd = 0.7 * c.SelectedLoad.Myd;
            }

            return spacing;
        }

        /// <summary>
        /// Calculates the effective depth according to X and Y of the column. It is assumed that the barycenter of the column is in (0,0).
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public double[] GetEffectiveDepths(Column col, FormulaeVM f0x, FormulaeVM f0y)
        {
            List<MWPoint2D> rebars = new List<MWPoint2D>();
            double minX = 0;
            double maxX = 0;
            double minY = 0;
            double maxY = 0;
            MWPoint2D bary = new MWPoint2D();
            if (col.Shape == GeoShape.Rectangular)
            {
                bary = new MWPoint2D(col.LX / 2, col.LY / 2);
                // Creation of the rebar positions
                double xspace = (col.LX - 2 * (col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0)) / (col.NRebarX - 1);
                double yspace = (col.LY - 2 * (col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0)) / (col.NRebarY - 1);
                for (int i = 0; i < col.NRebarX; i++)
                {
                    var x = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 + i * xspace;
                    for (int j = 0; j < col.NRebarY; j++)
                    {
                        if(i == 0 || i == col.NRebarX-1 || j == 0 || j == col.NRebarY-1)
                        {
                            var y = col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0 + j * yspace;
                            rebars.Add(new MWPoint2D(x, y));
                        }
                    }
                }
                minX = -col.LX / 2;
                maxX = col.LX / 2;
                minY = -col.LY / 2;
                maxY = col.LY / 2;
            }
            else if(col.Shape == GeoShape.Circular)
            {
                bary = new MWPoint2D(col.Diameter / 2, col.Diameter / 2);
                // Creation of the rebar positions
                double inc = 2 * Math.PI / col.NRebarCirc;
                double theta = 0;
                for(int i = 0; i < col.NRebarCirc; i++)
                {
                    theta = i * inc;
                    double x = (col.Diameter / 2.0 - col.CoverToLinks - col.LinkDiameter - col.BarDiameter / 2.0) * Math.Cos(theta);
                    double y = (col.Diameter / 2.0 - col.CoverToLinks - col.LinkDiameter - col.BarDiameter / 2.0) * Math.Sin(theta);
                    rebars.Add(new MWPoint2D(x, y));
                }
                minX = -col.Diameter / 2.0;
                maxX = col.Diameter / 2.0;
                minY = -col.Diameter / 2.0;
                maxY = col.Diameter / 2.0;
            }
            else if(col.Shape == GeoShape.Polygonal)
            {
                List<MWPoint2D> concPoints = new List<MWPoint2D>();
                // Creation of the rebar positions
                double inc = 2 * Math.PI / col.Edges;
                double theta = 0;
                double dd = (col.CoverToLinks + col.LinkDiameter + col.BarDiameter / 2.0) / Math.Sin((col.Edges - 2.0) * Math.PI / (2.0 * col.Edges));
                for (int i = 0; i < col.Edges; i++)
                {
                    theta = i * inc;
                    double x = (col.Radius - dd) * Math.Cos(theta);
                    double y = (col.Radius - dd) * Math.Sin(theta);
                    rebars.Add(new MWPoint2D(x, y));
                    double xx = col.Radius * Math.Cos(theta);
                    double yy = col.Radius * Math.Sin(theta);
                    concPoints.Add(new MWPoint2D(xx, yy));
                }
                bary = Points.GetBarycenter(concPoints);
                minX = concPoints.Min(p => p.X);
                maxX = concPoints.Max(p => p.X);
                minY = concPoints.Min(p => p.Y);
                maxY = concPoints.Max(p => p.Y);
            }

            double area = Math.PI * Math.Pow(col.BarDiameter / 2.0, 2);

            f0x.Expression.Add(@"A_{bar} = \pi\phi^2/4 = " + Math.Round(area) + " mm^2");
            f0y.Expression.Add(@"A_{bar} = \pi\phi^2/4 = " + Math.Round(area) + " mm^2");

            // calculation of moments of inertia
            double inertiaX = 0;
            double inertiaY = 0;
            for(int i = 0; i < rebars.Count; i++)
            {
                MWPoint2D pt = rebars[i];
                // according to X
                inertiaX += area * Math.Pow(pt.Y - bary.Y, 2);
                f0x.Expression.Add(@"I{x" + i + "} = A_{bar} * y_{bar," + i + "}^2 = " + Math.Round(area * Math.Pow(pt.Y - bary.Y, 2) / 1e4) + " cm^4");
                // according to Y
                inertiaY += area * Math.Pow(pt.X - bary.X, 2);
                f0y.Expression.Add(@"I{y" + i + "} = A_{bar} * x_{bar," + i + "}^2 = " + Math.Round(area * Math.Pow(pt.X - bary.X, 2) / 1e4) + " cm^4");
            }
            double totArea = rebars.Count * Math.PI * Math.Pow(col.BarDiameter / 2.0, 2);
            f0x.Expression.Add(@"I_{sx} = " + Math.Round(inertiaX/1E4) + " cm^4");
            f0y.Expression.Add(@"I_{sy} = " + Math.Round(inertiaY/1E4) + " cm^4");

            // Radius of gyration
            double gyradiusX = Math.Sqrt(inertiaX / totArea);
            double gyradiusY = Math.Sqrt(inertiaY / totArea);
            f0x.Expression.Add(@"i_{sx} = \sqrt{I_{sx}/A_s} = " + Math.Round(gyradiusX) + " mm");
            f0y.Expression.Add(@"i_{sy} = \sqrt{I_{sy}/A_s} = " + Math.Round(gyradiusY) + " mm");

            // Effective depths
            double dx = Math.Min(Math.Abs(minY), maxY) + gyradiusX; // it's conservative to take the min
            double dy = Math.Min(Math.Abs(minX), maxX) + gyradiusY;
            f0x.Expression.Add(@"h_x = " + Math.Round(Math.Min(Math.Abs(minY), maxY)) + " mm");
            f0y.Expression.Add(@"h_y = " + Math.Round(Math.Min(Math.Abs(minX), maxX)) + " mm");

            f0x.Expression.Add(@"d_x = h_x + i_{sx} = " + Math.Round(dx) + " mm");
            f0y.Expression.Add(@"d_y = h_y + i_{sy} = " + Math.Round(dy) + " mm");

            return new double[] { dx, dy };
        }

        public double[] GetSecondMomentInertia(Column col, FormulaeVM fx, FormulaeVM fy)
        {
            if(col.Shape == GeoShape.Rectangular)
            {
                double Ix = (col.LX * Math.Pow(col.LY, 3)) / 12;
                double Iy = (col.LY * Math.Pow(col.LX, 3)) / 12;
                fx.Expression.Add(@"I_x = \frac{bh^3}{12} = "+ Math.Round(Ix/1e4) +" cm^4");
                fy.Expression.Add(@"I_y = \frac{hb^3}{12} = "+ Math.Round(Iy/1e4) +" cm^4");
                return new double[] { Ix, Iy, col.LX * col.LY};
            }
            else if(col.Shape == GeoShape.Circular)
            {
                double I = Math.PI * Math.Pow(col.Diameter, 4) / 64;
                fx.Expression.Add(@"I_x = \frac{\pi D^4}{64} = " + Math.Round(I/1e4) + " cm^4");
                fy.Expression.Add(@"I_y = \frac{\pi D^4}{64} = " + Math.Round(I/1e4) + " cm^4");
                return new double[] { I, I, Math.PI * Math.Pow(col.Diameter/2.0,2) };
            }
            else if(col.Shape == GeoShape.Polygonal || col.Shape == GeoShape.LShaped)
            {
                double Ix = 0;
                double Iy = 0;
                List<MWPoint2D> points = new List<MWPoint2D>();
                double theta = 0;
                double inc = 2 * Math.PI / col.Edges;
                for(int i = 0; i < col.Edges; i++)
                {
                    theta = i * inc;
                    double x = col.Radius * Math.Cos(theta);
                    double y = col.Radius * Math.Sin(theta);
                    points.Add(new MWPoint2D(x, y));
                }
                points.Add(points[0]);
                for(int i = 0; i < col.Edges; i++)
                {
                    Ix += (points[i].X * points[i + 1].Y - points[i + 1].X * points[i].Y) * (Math.Pow(points[i].X, 2) + points[i].X * points[i + 1].X + Math.Pow(points[i + 1].X, 2)) / 12;
                    Iy += (points[i].X * points[i + 1].Y - points[i + 1].X * points[i].Y) * (Math.Pow(points[i].Y, 2) + points[i].Y * points[i + 1].Y + Math.Pow(points[i + 1].Y, 2)) / 12;
                }

                fx.Expression.Add(@"I_x = " + Math.Round(Ix / 1e4) + " cm^4");
                fy.Expression.Add(@"I_y = " + Math.Round(Iy / 1e4) + " cm^4");

                return new double[] { Ix, Iy };
            }
            return null;
        }

        public double GetColumnArea(Column col)
        {
            if (col.Shape == GeoShape.Rectangular)
                return col.LX * col.LY;
            else if (col.Shape == GeoShape.Circular)
                return Math.PI * Math.Pow(col.Diameter / 2.0, 2);
            else if (col.Shape == GeoShape.Polygonal)
                return col.Edges * Math.Pow(col.Radius, 2) * Math.Cos(Math.PI / col.Edges) * Math.Sin(Math.PI / col.Edges);
            else if (col.Shape == GeoShape.LShaped)
                return col.HX * col.HY - (col.HX - col.hX) * (col.HY - col.hY);
            return 0;
        }

        public double GetSteelArea(Column col)
        {
            int n = 0;
            if (col.Shape == GeoShape.Rectangular)
                n = 2 * (col.NRebarX + col.NRebarY - 2);
            else if (col.Shape == GeoShape.Circular)
                n = col.NRebarCirc;
            else if (col.Shape == GeoShape.Polygonal)
                n = col.Edges;
            else if (col.Shape == GeoShape.LShaped)
                n = 8 + (col.NRebarX - 3) * 2 + (col.NRebarY - 3) * 2;
            return n * Math.PI * Math.Pow(col.BarDiameter / 2.0, 2);
        }

        public double GetConcreteArea(Column col)
        {
            return GetColumnArea(col) - GetSteelArea(col);
        }

        public double[] GetAxialLength(Column col)
        {
            if (col.Shape == GeoShape.Rectangular)
                return new double[] { col.LX, col.LY };
            else if (col.Shape == GeoShape.Circular)
                return new double[] { col.Diameter, col.Diameter };
            else if(col.Shape == GeoShape.Polygonal)
            {
                List<MWPoint2D> points = new List<MWPoint2D>();
                double theta = 0;
                double inc = 2 * Math.PI / col.Edges;
                for (int i = 0; i < col.Edges; i++)
                {
                    theta = i * inc;
                    double x = col.Radius * Math.Cos(theta);
                    double y = col.Radius * Math.Sin(theta);
                    points.Add(new MWPoint2D(x, y));
                }
                return new double[] { Math.Abs(points.Min(p => p.X) - points.Max(p => p.X)), Math.Abs(points.Min(p => p.Y) - points.Max(p => p.Y)) };
            }
            else if (col.Shape == GeoShape.LShaped)
                return new double[] { col.HX, col.HY };
            return null;
        }

        public double GetPerimeter(Column col)
        {
            if (col.Shape == GeoShape.Rectangular)
                return 2 * ( col.LX + col.LY );
            else if (col.Shape == GeoShape.Circular)
                return 2 * Math.PI * col.Diameter/2.0;
            else if (col.Shape == GeoShape.Polygonal)
            {
                double n = col.Edges;
                return n * col.Diameter * Math.Sin(Math.PI / n);
            }
            else if (col.Shape == GeoShape.LShaped)
                return 2 * (col.HX + col.HY);
            return 0;
        }

        public bool CheckSpacing(Column c, FormulaeVM f0)
        {
            bool spacing = false;
            List<double> sizes = new List<double>() { c.BarDiameter, c.MaxAggSize + 5, 20 };
            double smin = sizes.Max();
            f0.Narrative = "Bar spacing";
            f0.Expression = new List<string>();
            f0.Expression.Add(@"k_1 = 1.0 mm");
            f0.Expression.Add(@"k_2 = 5.0 mm");
            f0.Expression.Add(@"s_{min} = max\left(k_1\phi,\phi_{agg}+k_2,20 mm\right) = " + smin + " mm");
            if (c.Shape == GeoShape.Rectangular)
            {
                double sx = (c.LX - 2.0 * (c.CoverToLinks + c.LinkDiameter) - c.BarDiameter) / (c.NRebarX - 1);
                double sy = (c.LY - 2.0 * (c.CoverToLinks + c.LinkDiameter) - c.BarDiameter) / (c.NRebarY - 1);
                f0.Expression.Add(@"s_x = " + Math.Round(sx) + " mm");
                f0.Expression.Add(@"s_y = " + Math.Round(sy) + " mm");
                if (sx >= smin && sy >= smin)
                {
                    f0.Conclusion = "PASS";
                    f0.Status = CalcStatus.PASS;
                    spacing = true;
                }
                else
                {
                    f0.Conclusion = "FAIL";
                    f0.Status = CalcStatus.FAIL;
                }
            }
            else if (c.Shape == GeoShape.Circular)
            {
                double x0 = c.Diameter / 2 - c.CoverToLinks - c.LinkDiameter - c.BarDiameter / 2.0;
                double x1 = x0 * Math.Cos(2 * Math.PI / c.NRebarCirc);
                double y1 = x0 * Math.Sin(2 * Math.PI / c.NRebarCirc);
                double s = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1, 2));
                f0.Expression.Add(@"s = " + Math.Round(s) + " mm");
                if (s >= smin)
                {
                    f0.Conclusion = "PASS";
                    f0.Status = CalcStatus.PASS;
                    spacing = true;
                }
                else
                {
                    f0.Conclusion = "FAIL";
                    f0.Status = CalcStatus.FAIL;
                }
            }
            else if (c.Shape == GeoShape.Polygonal)
            {
                double x0 = c.Diameter / 2 - c.CoverToLinks - c.LinkDiameter - c.BarDiameter / 2.0;
                double x1 = x0 * Math.Cos(2 * Math.PI / c.Edges);
                double y1 = x0 * Math.Sin(2 * Math.PI / c.Edges);
                double s = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1, 2));
                f0.Expression.Add(@"s = " + Math.Round(s) + " mm");
                if (s >= smin)
                {
                    f0.Conclusion = "PASS";
                    f0.Status = CalcStatus.PASS;
                    spacing = true;
                }
                else
                {
                    f0.Conclusion = "FAIL";
                    f0.Status = CalcStatus.FAIL;
                }
            }
            else if (c.Shape == GeoShape.LShaped)
            {
                if (c.sx1 < smin || c.sx2 < smin || c.sy1 < smin || c.sy2 < smin)
                {
                    f0.Conclusion = "FAIL";
                    f0.Status = CalcStatus.FAIL;
                }
                else
                {
                    f0.Conclusion = "PASS";
                    f0.Status = CalcStatus.PASS;
                    spacing = true;
                }
            }
            return spacing;
        }

        public void SetFireData()
        {
            fireTable.Add(new FireData(30, 0.2, 200, 25));
            fireTable.Add(new FireData(30, 0.5, 200, 25));
            fireTable.Add(new FireData(30, 0.7, 200, 32));
            fireTable.Add(new FireData(30, 0.7, 300, 27));
            fireTable.Add(new FireData(60, 0.2, 200, 25));
            fireTable.Add(new FireData(60, 0.5, 200, 36));
            fireTable.Add(new FireData(60, 0.5, 300, 31));
            fireTable.Add(new FireData(60, 0.7, 250, 46));
            fireTable.Add(new FireData(60, 0.7, 350, 40));
            fireTable.Add(new FireData(90, 0.2, 200, 31));
            fireTable.Add(new FireData(90, 0.2, 300, 25));
            fireTable.Add(new FireData(90, 0.5, 300, 45));
            fireTable.Add(new FireData(90, 0.5, 400, 38));
            fireTable.Add(new FireData(90, 0.7, 350, 53));
            fireTable.Add(new FireData(90, 0.7, 450, 40));
            fireTable.Add(new FireData(120, 0.2, 250, 40));
            fireTable.Add(new FireData(120, 0.2, 350, 35));
            fireTable.Add(new FireData(120, 0.5, 350, 45));
            fireTable.Add(new FireData(120, 0.5, 450, 40));
            fireTable.Add(new FireData(120, 0.7, 350, 57));
            fireTable.Add(new FireData(120, 0.7, 450, 51));
            fireTable.Add(new FireData(180, 0.2, 350, 45));
            fireTable.Add(new FireData(180, 0.5, 350, 63));
            fireTable.Add(new FireData(180, 0.7, 450, 70));
            fireTable.Add(new FireData(240, 0.2, 350, 61));
            fireTable.Add(new FireData(240, 0.5, 350, 75));
            fireTable.Add(new FireData(30, 0.7, 155, 25, FireExposition.OneSide));
            fireTable.Add(new FireData(60, 0.7, 155, 25, FireExposition.OneSide));
            fireTable.Add(new FireData(90, 0.7, 155, 25, FireExposition.OneSide));
            fireTable.Add(new FireData(120, 0.7, 175, 35, FireExposition.OneSide));
            fireTable.Add(new FireData(180, 0.7, 230, 55, FireExposition.OneSide));
            fireTable.Add(new FireData(240, 0.7, 295, 70, FireExposition.OneSide));
        }

        public List<CalcValue> GetInputs()
        {
            List<CalcValue> inputTable = new List<CalcValue>();

            if(column.Shape == GeoShape.Rectangular)
            {
                inputTable.Add(new CalcValue()
                {
                    Name = "LX",
                    Symbol = "LX",
                    Unit = "mm",
                    ValueAsString = column.LX.ToString(),
                    Type = CalcValueType.DOUBLE
                });

                inputTable.Add(new CalcValue()
                {
                    Name = "LY",
                    Symbol = "LY",
                    Unit = "mm",
                    ValueAsString = column.LY.ToString(),
                    Type = CalcValueType.DOUBLE
                });
            }
            else if(column.Shape == GeoShape.Circular)
            {
                inputTable.Add(new CalcValue()
                {
                    Name = "Diameter",
                    Symbol = @"\Phi",
                    Unit = "mm",
                    ValueAsString = column.Diameter.ToString(),
                    Type = CalcValueType.DOUBLE
                });
            }
            else if(column.Shape == GeoShape.Polygonal)
            {
                inputTable.Add(new CalcValue()
                {
                    Name = "Radius",
                    Symbol = "R",
                    Unit = "mm",
                    ValueAsString = column.Radius.ToString(),
                    Type = CalcValueType.DOUBLE
                });
            }

            inputTable.Add(new CalcValue()
            {
                Name = "Length",
                Symbol = "L",
                Unit = "mm",
                ValueAsString = column.Length.ToString(),
                Type = CalcValueType.DOUBLE
            });

            inputTable.Add(new CalcValue()
            {
                Name = "Angle",
                Symbol = @"\alpha",
                Unit = "deg",
                ValueAsString = column.Angle.ToString(),
                Type = CalcValueType.DOUBLE
            });

            inputTable.Add(new CalcValue()
            {
                Name = "Concrete Grade",
                Symbol = "Concrete",
                Unit = "",
                ValueAsString = column.ConcreteGrade.Name,
                Type = CalcValueType.SELECTIONLIST
            });

            inputTable.Add(new CalcValue()
            {
                Name = "Max agg size",
                Symbol = "A_g",
                Unit = "mm",
                ValueAsString = column.MaxAggSize.ToString(),
                Type = CalcValueType.DOUBLE
            });

            inputTable.Add(new CalcValue()
            {
                Name = "MxTop",
                Symbol = "M_x^{top}",
                Unit = "kN.m",
                ValueAsString = column.SelectedLoad.MxTop.ToString(),
                Type = CalcValueType.DOUBLE
            });

            inputTable.Add(new CalcValue()
            {
                Name = "MxBot",
                Symbol = "M_x^{bot}",
                Unit = "kN.m",
                ValueAsString = column.SelectedLoad.MxBot.ToString(),
                Type = CalcValueType.DOUBLE
            });

            inputTable.Add(new CalcValue()
            {
                Name = "MyTop",
                Symbol = "M_y^{top}",
                Unit = "kN.m",
                ValueAsString = column.SelectedLoad.MyTop.ToString(),
                Type = CalcValueType.DOUBLE
            });

            inputTable.Add(new CalcValue()
            {
                Name = "MyBot",
                Symbol = "M_y^{bot}",
                Unit = "kN.m",
                ValueAsString = column.SelectedLoad.MyBot.ToString(),
                Type = CalcValueType.DOUBLE
            });

            inputTable.Add(new CalcValue()
            {
                Name = "Axial Load",
                Symbol = "P",
                Unit = "KN",
                ValueAsString = column.SelectedLoad.P.ToString(),
                Type = CalcValueType.DOUBLE
            });

            inputTable.Add(new CalcValue()
            {
                Name = "Effective length",
                Symbol = "L_{eff}",
                Unit = "",
                ValueAsString = column.EffectiveLength.ToString(),
                Type = CalcValueType.DOUBLE
            });

            inputTable.Add(new CalcValue()
            {
                Name = "Cover to links",
                Symbol = @"c",
                Unit = "mm",
                ValueAsString = column.CoverToLinks.ToString(),
                Type = CalcValueType.DOUBLE
            });

            inputTable.Add(new CalcValue()
            {
                Name = "Bar diameter",
                Symbol = @"D",
                Unit = "mm",
                ValueAsString = column.BarDiameter.ToString(),
                Type = CalcValueType.DOUBLE
            });

            inputTable.Add(new CalcValue()
            {
                Name = "Link diameter",
                Symbol = @"\phi_l",
                Unit = "mm",
                ValueAsString = column.LinkDiameter.ToString(),
                Type = CalcValueType.DOUBLE
            });

            inputTable.Add(new CalcValue()
            {
                Name = "NRebarX",
                Symbol = @"NX",
                Unit = "",
                ValueAsString = column.NRebarX.ToString(),
                Type = CalcValueType.INT
            });

            inputTable.Add(new CalcValue()
            {
                Name = "NRebarY",
                Symbol = @"NY",
                Unit = "",
                ValueAsString = column.NRebarY.ToString(),
                Type = CalcValueType.INT
            });

            inputTable.Add(new CalcValue()
            {
                Name = "Fire resistance",
                Symbol = @"R_f",
                Unit = "min",
                ValueAsString = column.R.ToString(),
                Type = CalcValueType.INT
            });

            return inputTable;
        }

        public List<CalcValue> GetOutputs()
        {
            List<CalcValue> outputTable = new List<CalcValue>();

            outputTable.Add(new CalcValue()
            {
                Name = "Mxd",
                Symbol = @"M_x^d",
                Unit = "kN.m",
                ValueAsString = column.SelectedLoad.Mxd.ToString(),
                Type = CalcValueType.DOUBLE
            });

            outputTable.Add(new CalcValue()
            {
                Name = "Myd",
                Symbol = @"M_y^d",
                Unit = "kN.m",
                ValueAsString = column.SelectedLoad.Myd.ToString(),
                Type = CalcValueType.DOUBLE
            });

            var carbon = column.GetEmbodiedCarbon();

            outputTable.Add(new CalcValue()
            {
                Name = "Concrete carbon",
                Symbol = @"Concrete C0_2",
                Unit = "kg C02",
                ValueAsString = carbon[0].ToString(),
                Type = CalcValueType.DOUBLE
            });

            outputTable.Add(new CalcValue()
            {
                Name = "Rebars carbon",
                Symbol = @"Rebars C0_2",
                Unit = "kg C02",
                ValueAsString = carbon[1].ToString(),
                Type = CalcValueType.DOUBLE
            });

            outputTable.Add(new CalcValue()
            {
                Name = "Total carbon",
                Symbol = @"Total C0_2",
                Unit = "kg C02",
                ValueAsString = carbon[2].ToString(),
                Type = CalcValueType.DOUBLE
            });

            return outputTable;
        }

        public class CalcValue
        {
            public string Name { get; set; } = "";
            public string Symbol { get; set; } = "";
            public string Unit { get; set; } = "";
            public string ValueAsString { get; set; }
            public CalcStatus Status { get; set; } = CalcStatus.NONE;
            public CalcValueType Type { get; set; }
            // description
        }

        public enum CalcValueType
        {
            DOUBLE,
            SELECTIONLIST,
            FILEPATH,
            FOLDERPATH,
            LISTOFDOUBLEARRAYS,
            INT
        }
    }

    public enum FireExposition { OneSide, MoreThanOneSide };
    public enum AggregateType { Siliceous, Calcareous };

    public class FireData
    {
        public int R;
        public double mu;
        public FireExposition sidesExposed;
        public int minDimension;
        public int axisDistance;

        public FireData(int r, double m, int mindim, int a, FireExposition e = FireExposition.MoreThanOneSide)
        {
            R = r;
            mu = m;
            sidesExposed = e;
            minDimension = mindim;
            axisDistance = a;
        }
    }

    public class ConcreteData
    {
        public double Temp;
        public AggregateType type;
        public double k;
        public double Ec1;
        public double Ecu1;
        public double density;

        public ConcreteData(double temp, double kk, double ec1, double ecu1, AggregateType t = AggregateType.Siliceous, double dens = 2500)
        {
            Temp = temp;
            type = t;
            k = kk;
            Ec1 = ec1;
            Ecu1 = ecu1;
            density = dens;
        }
    }

    public class SteelData
    {
        public double Temp;
        public double kf;
        public double kE;

        public SteelData(double t, double f, double E)
        {
            Temp = t;
            kf = f;
            kE = E;
        }
    }
}
