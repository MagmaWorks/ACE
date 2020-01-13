using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColumnDesigner
{
    public class ValidationTests
    {
        public Column testColumn;
        double refA;
        double DL = 1500;
        double LL = 500;

        double[,] mapTable = new double[21, 17];
        double[,] mapIso500 = new double[21, 17];
        double[,] mapZone = new double[21, 17];
        double[,] mapAdvanced = new double[21, 17];
        double[,] secondDim = new double[2, 17];
        double[,] axialLoads = new double[2, 21];

        public ValidationTests()
        {
            refA = 350 * 350;

            testColumn = new Column()
            {
                LX = 350,
                LY = 350,
                NRebarX = 3,
                NRebarY = 3,
                BarDiameter = 16,
                SelectedLoad = new Load(),
                ConcreteGrade = new Concrete("C50/60", 50, 37),
                SteelGrade = new Steel("S500", 500)
            };

            testColumn.SetFireData();

            for (int i = 0; i <= 20; i++)
            {
                double sdl = 200 + i * 20;
                double ULS = 1.0 * (1.35 * DL + 1.35 * sdl + 1.5 * LL);
                double fireComb = DL + sdl + 0.5 * LL;
                axialLoads[0, i] = 0.7 * ULS;
                axialLoads[1, i] = fireComb;

                for (int j = 0; j <= 16; j++)
                {
                    double a = 200 + j * 25;
                    double b = Math.Round(refA / a / 25) * 25;
                    secondDim[0, j] = b;
                    secondDim[1, j] = a * b / 1e4;
                    testColumn.LX = a;
                    testColumn.LY = b;
                    testColumn.SelectedLoad.P = ULS;

                    testColumn.GetDesignMoments();

                    // table check
                    mapTable[i, j] = testColumn.CheckFire() ? 1 : 0;

                    // iso500 check
                    mapIso500[i, j] = testColumn.CheckFireIsotherm500(true).Item1 ? 1 : 0;

                    // iso500 check
                    mapZone[i, j] = testColumn.CheckFireZoneMethod(false).Item1 ? 1 : 0;

                    // advanced check
                    testColumn.SelectedLoad.P = fireComb;
                    testColumn.UpdateFireID(true);
                    mapAdvanced[i, j] = testColumn.CheckIsInsideFireID() ? 1 : 0;


                }

            }

            StringBuilder linesTable = new StringBuilder();
            StringBuilder linesIso500 = new StringBuilder();
            StringBuilder linesZone = new StringBuilder();
            StringBuilder linesAdvanced = new StringBuilder();

            StringBuilder linesSecondDim = new StringBuilder();
            StringBuilder linesAxialLoads = new StringBuilder();
            string s = "";
            for (int i = 0; i <= 20; i++)
            {
                s = "";
                for (int j = 0; j <= 16; j++)
                    s += mapTable[i, j] + " ";
                linesTable.AppendLine(s);

                s = "";
                for (int j = 0; j <= 16; j++)
                    s += mapIso500[i, j] + " ";
                linesIso500.AppendLine(s);

                s = "";
                for (int j = 0; j <= 16; j++)
                    s += mapZone[i, j] + " ";
                linesZone.AppendLine(s);

                s = "";
                for (int j = 0; j <= 16; j++)
                    s += mapAdvanced[i, j] + " ";
                linesAdvanced.AppendLine(s);

            }

            for (int j = 0; j < 2; j++)
            {
                s = "";
                for (int i = 0; i < 17; i++)
                    s += secondDim[j, i] + " ";
                linesSecondDim.AppendLine(s);
            }

            for (int j = 0; j < 2; j++)
            {
                s = "";
                for (int i = 0; i < 21; i++)
                    s += axialLoads[j, i] + " ";
                linesAxialLoads.AppendLine(s);
            }


            string pathTable = @"C:\workspace\ColumnDesigner\ColumnDesigner\ColumnDesigner\mapTable.txt";
            string pathIso500 = @"C:\workspace\ColumnDesigner\ColumnDesigner\ColumnDesigner\mapIso500.txt";
            string pathZone = @"C:\workspace\ColumnDesigner\ColumnDesigner\ColumnDesigner\mapZone.txt";
            string pathAdvanced = @"C:\workspace\ColumnDesigner\ColumnDesigner\ColumnDesigner\mapAdvanced.txt";
            string pathSecondDim = @"C:\workspace\ColumnDesigner\ColumnDesigner\ColumnDesigner\secondDim.txt";
            string pathAxialLoads = @"C:\workspace\ColumnDesigner\ColumnDesigner\ColumnDesigner\axialLoads.txt";

            System.IO.File.WriteAllText(pathTable, linesTable.ToString());
            System.IO.File.WriteAllText(pathIso500, linesIso500.ToString());
            System.IO.File.WriteAllText(pathZone, linesZone.ToString());
            System.IO.File.WriteAllText(pathAdvanced, linesAdvanced.ToString());
            System.IO.File.WriteAllText(pathSecondDim, linesSecondDim.ToString());
            System.IO.File.WriteAllText(pathAxialLoads, linesAxialLoads.ToString());
        }

        
    }

    public class ValidationTests2
    {
        public Column testColumn;
        double refA;
        double DL = 1000;
        double SDL = 500;
        double LL = 500;
        double MxTop = 1000;
        int N = 41;

        double[] mapTable;
        double[] mapIso500;
        double[] mapZone;
        double[] mapAdvanced;

        public ValidationTests2()
        {
            mapTable = new double[N];
            mapIso500 = new double[N];
            mapZone = new double[N];
            mapAdvanced = new double[N];

            testColumn = new Column()
            {
                LX = 350,
                LY = 350,
                NRebarX = 3,
                NRebarY = 3,
                BarDiameter = 16,
                //CoverToLinks = 35,
                SelectedLoad = new Load(),
                ConcreteGrade = new Concrete("C50/60", 50, 37),
                SteelGrade = new Steel("S500", 500)
            };

            testColumn.SetFireData();

            double ULS = 1.0 * (1.35 * DL + 1.35 * SDL + 1.5 * LL);

            for (int i = 0; i < N; i++)
            {
                double a = 200 + i * 25;
                //double fireComb = DL + SDL + 0.5 * LL;

                bool table = true;
                bool iso500 = true;
                bool zone = true;
                bool advanced = true;

                for (int j = 0; j <= N; j++)
                {
                    double b = 200 + j * 25;
                    testColumn.LX = a;
                    testColumn.LY = b;
                    testColumn.SelectedLoad.P = ULS;
                    testColumn.SelectedLoad.MxTop = MxTop;

                    testColumn.GetDesignMoments();
                    testColumn.FireLoad = new Load()
                    {
                        P = ULS,
                        Mxd = testColumn.SelectedLoad.Mxd,
                        Myd = testColumn.SelectedLoad.Myd
                    };

                    // table check
                    if (table)
                    {
                        if (testColumn.CheckFire())
                        {
                            mapTable[i] = b;
                            table = false;
                        }
                    }

                    // iso500 check
                    if (iso500)
                    {
                        if (testColumn.CheckFireIsotherm500(true).Item1)
                        {
                            mapIso500[i] = b;
                            iso500 = false;
                        }
                    }

                    // zone method check
                    if (zone)
                    {
                        if (testColumn.CheckFireZoneMethod(true).Item1)
                        {
                            mapZone[i] = b;
                            zone = false;
                        }
                    }

                    // advanced check
                    if (advanced)
                    {
                        //testColumn.SelectedLoad.P = fireComb;
                        testColumn.UpdateFireID(true);
                        if (testColumn.CheckIsInsideFireID())
                        {
                            mapAdvanced[i] = b;
                            advanced = false;
                        }
                    }

                }

            }

            StringBuilder linesTable = new StringBuilder();
            StringBuilder linesIso500 = new StringBuilder();
            StringBuilder linesZone = new StringBuilder();
            StringBuilder linesAdvanced = new StringBuilder();

            StringBuilder linesSecondDim = new StringBuilder();
            StringBuilder linesAxialLoads = new StringBuilder();
            string s = "";
            for (int i = 0; i < N; i++)
            {
                linesTable.Append(mapTable[i] + " ");
                linesIso500.Append(mapIso500[i] + " ");
                linesZone.Append(mapZone[i] + " ");
                linesAdvanced.Append(mapAdvanced[i] + " ");
            }

            string pathTable = @"C:\workspace\ColumnDesigner\ColumnDesigner\ColumnDesigner\mapTable_Mx="+MxTop.ToString()+"_c="+testColumn.CoverToLinks+"H="+testColumn.BarDiameter+".txt";
            string pathIso500 = @"C:\workspace\ColumnDesigner\ColumnDesigner\ColumnDesigner\mapIso500_Mx=" + MxTop.ToString() + "_c=" + testColumn.CoverToLinks + "H=" + testColumn.BarDiameter + ".txt";
            string pathZone = @"C:\workspace\ColumnDesigner\ColumnDesigner\ColumnDesigner\mapZone_Mx=" + MxTop.ToString() + "_c=" + testColumn.CoverToLinks + "H=" + testColumn.BarDiameter + ".txt";
            string pathAdvanced = @"C:\workspace\ColumnDesigner\ColumnDesigner\ColumnDesigner\mapAdvanced_Mx=" + MxTop.ToString() + "_c=" + testColumn.CoverToLinks + "H=" + testColumn.BarDiameter + ".txt";
            //string pathSecondDim = @"C:\workspace\ColumnDesigner\ColumnDesigner\ColumnDesigner\secondDim.txt";
            //string pathAxialLoads = @"C:\workspace\ColumnDesigner\ColumnDesigner\ColumnDesigner\axialLoads.txt";

            System.IO.File.WriteAllText(pathTable, linesTable.ToString());
            System.IO.File.WriteAllText(pathIso500, linesIso500.ToString());
            System.IO.File.WriteAllText(pathZone, linesZone.ToString());
            System.IO.File.WriteAllText(pathAdvanced, linesAdvanced.ToString());
            //System.IO.File.WriteAllText(pathSecondDim, linesSecondDim.ToString());
            //System.IO.File.WriteAllText(pathAxialLoads, linesAxialLoads.ToString());
        }

    }
}
