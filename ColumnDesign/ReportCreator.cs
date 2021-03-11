using CalcCore;
using ColumnDesignCalc;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using A = DocumentFormat.OpenXml.Drawing;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;
using A14 = DocumentFormat.OpenXml.Office2010.Drawing;
using Column = ColumnDesignCalc.Column;
using MarkerType = OxyPlot.MarkerType;
using SkiaSharp;

namespace ColumnDesign
{
    public class ReportCreator
    {
        public List<Column> columns { get; set; }
        public List<ICalc> calcs { get; set; }
        public Settings settings { get; set; }

        public async Task ExportToWord(IProgress<WordReportProgress> progress)
        {
            calcs = new List<ICalc>();
            int k = 0;

            for (int n = 0; n < columns.Count; n++)
            {
                Column c = columns[n];
                if (settings.ExprtdLoads == ExportedLoads.Current)
                {
                    int nloadtot = columns.Count;
                    k++;
                    Calculations calc = new Calculations();
                    calc.Column = c;
                    calc.UpdateInputOuput();
                    calc.UpdateCalc();
                    Generate2DIDs(c);
                    calc.AddInteractionDiagrams();
                    calcs.Add(calc);
                    //progress.Report(new WordReportProgress()
                    //{
                    //    Progress = Convert.ToInt32(k * 1.0 / nloadtot) * 100,
                    //    Message = string.Format("Preparing report for column {0} - load {1}", c.Name, c.SelectedLoad.Name)
                    //});
                }
                else if (settings.ExprtdLoads == ExportedLoads.DesigningLoads)
                {
                    int nloadtot = columns.SelectMany(x => x.DesigningLoads).Count();
                    c.GetDesigningLoads(settings.NumLoads);
                    for (int i = 0; i < c.DesigningLoads.Count; i++)
                    {
                        k++;
                        c.SelectedLoad = c.DesigningLoads[i];
                        Calculations calc = new Calculations();
                        calc.Column = c;
                        calc.InstanceName += string.Format(" - ({0})", i);
                        calc.UpdateInputOuput();
                        calc.UpdateCalc();
                        Generate2DIDs(c);
                        calc.AddInteractionDiagrams();
                        calcs.Add(calc);
                        //progress.Report(new WordReportProgress()
                        //{
                        //    Progress = Convert.ToInt32(k * 1.0 / nloadtot) * 100,
                        //    Message = string.Format("Preparing report for column {0} - load {1}", c.Name, c.DesigningLoads[i].Name)
                        //});
                    }
                }
            }

            if (settings.CombinedReport || settings.ExprtdCols == ExportedColumns.Current)
            {
                OutputToODT.WriteToODT(calcs, true, true, true, settings);
            }
            else
            {
                OutputToODT.WriteToODT2(calcs, true, true, true, settings);
            }
        }

        public void Generate2DIDs(Column col)
        {
            if (col.diagramVertices.Count == 0)
            {
                col.GetInteractionDiagram();
                col.Get2DMaps();
            }
            LineSeries sp1 = new LineSeries()
            {
                Color = OxyColors.Black,
                MarkerType = MarkerType.Plus,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Black,
                MarkerFill = OxyColors.Black,
                MarkerStrokeThickness = 1,
                LabelFormatString = "({0},{1})"
            };
            sp1.Points.Add(new DataPoint(col.SelectedLoad.MEdx, col.SelectedLoad.MEdy));

            PlotModel colMxMyID = new PlotModel()
            {
                Title = "Mx-My interaction diagram",
                Subtitle = "(N = " + Math.Round(col.SelectedLoad.P) + "kN)"
            };
            colMxMyID.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                Title = "Mx",
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dash,
            });
            colMxMyID.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Left,
                Title = "My",
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dash,
            });

            LineSeries s1 = new LineSeries()
            {
                Color = OxyColors.Red,
                MarkerType = MarkerType.None,
                StrokeThickness = 1
            };
            foreach (var p in col.MxMyPts)
                s1.Points.Add(new DataPoint(p.X, p.Y));
            colMxMyID.Series.Add(s1);
            colMxMyID.Series.Add(sp1);

            LineSeries sp2 = new LineSeries()
            {
                Color = OxyColors.Black,
                MarkerType = MarkerType.Plus,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Black,
                MarkerFill = OxyColors.Black,
                MarkerStrokeThickness = 1,
                LabelFormatString = "({0},{1})"
            };
            sp2.Points.Add(new DataPoint(col.SelectedLoad.MEdx, -col.SelectedLoad.P));

            PlotModel colMxNID = new PlotModel()
            {
                Title = "Mx-N interaction diagram",
                Subtitle = "(My = " + Math.Round(col.SelectedLoad.MEdy) + "kN.m)"
            };
            colMxNID.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                Title = "Mx",
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dash,
            });
            colMxNID.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Left,
                Title = "N",
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dash,
            });

            LineSeries s2 = new LineSeries()
            {
                Color = OxyColors.Blue,
                MarkerType = MarkerType.None,
                StrokeThickness = 1
            };
            foreach (var p in col.MxNPts)
                s2.Points.Add(new DataPoint(p.X, p.Y));
            colMxNID.Series.Add(s2);
            colMxNID.Series.Add(sp2);

            LineSeries sp3 = new LineSeries()
            {
                Color = OxyColors.Black,
                MarkerType = MarkerType.Plus,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Black,
                MarkerFill = OxyColors.Black,
                MarkerStrokeThickness = 1,
                LabelFormatString = "({0},{1})",

            };
            sp3.Points.Add(new DataPoint(col.SelectedLoad.MEdy, -col.SelectedLoad.P));

            PlotModel colMyNID = new PlotModel()
            {
                Title = "My-N interaction diagram",
                Subtitle = "(Mx = " + Math.Round(col.SelectedLoad.MEdx) + "kN.m)",
            };
            colMyNID.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                Title = "My",
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dash,
            });
            colMyNID.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Left,
                Title = "N",
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dash,
            });

            LineSeries s3 = new LineSeries()
            {
                Color = OxyColors.Green,
                MarkerType = MarkerType.None,
                StrokeThickness = 1
            };
            foreach (var p in col.MyNPts)
                s3.Points.Add(new DataPoint(p.X, p.Y));
            colMyNID.Series.Add(s3);
            colMyNID.Series.Add(sp3);

            if (col.FireDesignMethod == FDesignMethod.Advanced)
            {
                LineSeries spf1 = new LineSeries()
                {
                    Color = OxyColors.Black,
                    MarkerType = MarkerType.Plus,
                    MarkerSize = 4,
                    MarkerStroke = OxyColors.Black,
                    MarkerFill = OxyColors.Black,
                    MarkerStrokeThickness = 1,
                    LabelFormatString = "fire ({0},{1})"
                };
                spf1.Points.Add(new DataPoint(col.FireLoad.MEdx, col.FireLoad.MEdy));

                LineSeries sf1 = new LineSeries()
                {
                    Color = OxyColors.DarkRed,
                    MarkerType = MarkerType.None,
                    StrokeThickness = 1
                };
                foreach (var p in col.fireMxMyPts)
                    sf1.Points.Add(new DataPoint(p.X, p.Y));
                colMxMyID.Series.Add(sf1);
                colMxMyID.Series.Add(spf1);

                LineSeries spf2 = new LineSeries()
                {
                    Color = OxyColors.Black,
                    MarkerType = MarkerType.Plus,
                    MarkerSize = 4,
                    MarkerStroke = OxyColors.Black,
                    MarkerFill = OxyColors.Black,
                    MarkerStrokeThickness = 1,
                    LabelFormatString = "fire ({0},{1})"
                };
                spf2.Points.Add(new DataPoint(col.FireLoad.MEdx, -col.FireLoad.P));

                LineSeries sf2 = new LineSeries()
                {
                    Color = OxyColors.DarkBlue,
                    MarkerType = MarkerType.None,
                    StrokeThickness = 1
                };
                foreach (var p in col.fireMxNPts)
                    sf2.Points.Add(new DataPoint(p.X, p.Y));
                colMxNID.Series.Add(sf2);
                colMxNID.Series.Add(spf2);

                LineSeries spf3 = new LineSeries()
                {
                    Color = OxyColors.Black,
                    MarkerType = MarkerType.Plus,
                    MarkerSize = 4,
                    MarkerStroke = OxyColors.Black,
                    MarkerFill = OxyColors.Black,
                    MarkerStrokeThickness = 1,
                    LabelFormatString = "fire ({0},{1})"
                };
                spf3.Points.Add(new DataPoint(col.FireLoad.MEdy, -col.FireLoad.P));
                LineSeries sf3 = new LineSeries()
                {
                    Color = OxyColors.DarkGreen,
                    MarkerType = MarkerType.None,
                    StrokeThickness = 1
                };
                foreach (var p in col.fireMyNPts)
                    sf3.Points.Add(new DataPoint(p.X, p.Y));
                colMyNID.Series.Add(sf3);
                colMyNID.Series.Add(spf3);
            }

            PlotModel[] plots = new PlotModel[] { colMxMyID, colMxNID, colMyNID };
            string[] plotNames = new string[] { "MxMy", "MxN", "MyN" };
            for (int i = 0; i < plots.Length; i++)
            {
                var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 600, Height = 400, Background = OxyColors.White };
                BitmapSource bitmap = null;
                bitmap = pngExporter.ExportToBitmap(plots[i]);
                MemoryStream stream = new MemoryStream();
                BitmapEncoder encoder = new BmpBitmapEncoder();
                var frame = BitmapFrame.Create(bitmap);
                encoder.Frames.Add(frame);
                encoder.Save(stream);

                Bitmap bmp = new Bitmap(stream);

                ImageConverter converter = new ImageConverter();
                byte[] output = (byte[])converter.ConvertTo(bmp, typeof(byte[]));

                string path = System.IO.Path.GetTempPath() + plotNames[i] + ".tmp";
                File.WriteAllBytes(path, output);
                
            }
        }

        public void ExcelReport()
        {
            string filePath;
            try
            {
                var saveDialog = new SaveFileDialog();
                saveDialog.Filter = @"Excel files |*.xlsx";
                saveDialog.FileName = "Columns intents" + @".xlsx";
                //saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (saveDialog.ShowDialog() != DialogResult.OK) return;
                filePath = saveDialog.FileName;
                Properties.Settings.Default.Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Oops..." + Environment.NewLine + ex.Message);
                return;
            }

            SpreadsheetDocument report = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
            CreateExcelFile(report);
        }

        public void CreateExcelFile(SpreadsheetDocument document)
        {
            // Add a WorkbookPart to the document
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            Sheets sheets = document.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());

            #region All columns
            //  Sheet 1 : All columns
            WorksheetPart worksheetPart1 = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart1.Worksheet = new Worksheet();
            SheetData sheetData1 = new SheetData();

            Sheet sheet1 = new Sheet()
            {
                Name = "All columns",
                SheetId = 1,
                Id = document.WorkbookPart.GetIdOfPart(worksheetPart1)
            };
            sheets.Append(sheet1);

            // header
            Row headerRow1 = new Row();
            Cell nameHeader1 = new Cell() { CellValue = new CellValue("Name"), DataType = CellValues.String };
            headerRow1.AppendChild(nameHeader1);
            Cell LXHeader1 = new Cell() { CellValue = new CellValue("LX (mm)"), DataType = CellValues.String };
            headerRow1.AppendChild(LXHeader1);
            Cell LYHeader1 = new Cell() { CellValue = new CellValue("LY (mm)"), DataType = CellValues.String };
            headerRow1.AppendChild(LYHeader1);
            Cell concreteHeader1 = new Cell() { CellValue = new CellValue("Concrete"), DataType = CellValues.String };
            headerRow1.AppendChild(concreteHeader1);
            Cell NRebarXHeader1 = new Cell() { CellValue = new CellValue("Rebar X"), DataType = CellValues.String };
            headerRow1.AppendChild(NRebarXHeader1);
            Cell NRebarYHeader1 = new Cell() { CellValue = new CellValue("Rebar Y"), DataType = CellValues.String };
            headerRow1.AppendChild(NRebarYHeader1);
            Cell SteelHeader1 = new Cell() { CellValue = new CellValue("Steel"), DataType = CellValues.String };
            headerRow1.AppendChild(SteelHeader1);
            Cell coverHeader1 = new Cell() { CellValue = new CellValue("Cover (mm)"), DataType = CellValues.String };
            headerRow1.AppendChild(coverHeader1);

            sheetData1.Append(headerRow1);

            // data for sheet 1
            List<Column> notClusterCol = columns.Where(c => !c.IsCluster).ToList();
            for(int n = 0; n < notClusterCol.Count; n++)
            {
                Column col = notClusterCol[n];
                
                Row rowCol = new Row();

                Cell nameCell = new Cell();
                nameCell.DataType = CellValues.String;
                nameCell.CellValue = new CellValue(col.Name);
                rowCol.AppendChild(nameCell);

                Cell LXCell = new Cell();
                LXCell.DataType = CellValues.Number;
                LXCell.CellValue = new CellValue(col.LX.ToString());
                rowCol.AppendChild(LXCell);

                Cell LYCell = new Cell();
                LYCell.DataType = CellValues.Number;
                LYCell.CellValue = new CellValue(col.LY.ToString());
                rowCol.AppendChild(LYCell);

                Cell concreteCell = new Cell();
                concreteCell.DataType = CellValues.String;
                concreteCell.CellValue = new CellValue(col.ConcreteGrade.Name);
                rowCol.AppendChild(concreteCell);

                Cell NRebarXCell = new Cell();
                NRebarXCell.DataType = CellValues.Number;
                NRebarXCell.CellValue = new CellValue(col.NRebarX.ToString());
                rowCol.AppendChild(NRebarXCell);

                Cell NRebarYCell = new Cell();
                NRebarYCell.DataType = CellValues.Number;
                NRebarYCell.CellValue = new CellValue(col.NRebarY.ToString());
                rowCol.AppendChild(NRebarYCell);

                Cell steelCell = new Cell();
                steelCell.DataType = CellValues.String;
                steelCell.CellValue = new CellValue(col.SteelGrade.Name);
                rowCol.AppendChild(steelCell);

                Cell coverCell = new Cell();
                coverCell.DataType = CellValues.Number;
                coverCell.CellValue = new CellValue(col.CoverToLinks.ToString());
                rowCol.AppendChild(coverCell);

                //Cell refCell = new Cell();
                //refCell.DataType = CellValues.String;
                //string refs = col.Name;
                //if (col.IsCluster)
                //    refs = col.ColsInCluster.Aggregate((i, j) => i + ", " + j);
                //refCell.CellValue = new CellValue(refs);
                //rowCol.AppendChild(refCell);

                sheetData1.AppendChild(rowCol);
            }

            worksheetPart1.Worksheet.AppendChild(sheetData1);
            #endregion


            #region Cluster columns
            //  Sheet 1 : All columns
            WorksheetPart worksheetPart2 = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart2.Worksheet = new Worksheet();
            SheetData sheetData2 = new SheetData();

            Sheet sheet2 = new Sheet()
            {
                Name = "Clusters",
                SheetId = 2,
                Id = document.WorkbookPart.GetIdOfPart(worksheetPart2)
            };
            sheets.Append(sheet2);

            // header
            Row headerRow2 = new Row();
            Cell nameHeader2 = new Cell() { CellValue = new CellValue("Name"), DataType = CellValues.String };
            headerRow2.AppendChild(nameHeader2);
            Cell LXHeader2 = new Cell() { CellValue = new CellValue("LX (mm)"), DataType = CellValues.String };
            headerRow2.AppendChild(LXHeader2);
            Cell LYHeader2 = new Cell() { CellValue = new CellValue("LY (mm)"), DataType = CellValues.String };
            headerRow2.AppendChild(LYHeader2);
            Cell concreteHeader2 = new Cell() { CellValue = new CellValue("Concrete"), DataType = CellValues.String };
            headerRow2.AppendChild(concreteHeader2);
            Cell NRebarXHeader2 = new Cell() { CellValue = new CellValue("Rebar X"), DataType = CellValues.String };
            headerRow2.AppendChild(NRebarXHeader2);
            Cell NRebarYHeader2 = new Cell() { CellValue = new CellValue("Rebar Y"), DataType = CellValues.String };
            headerRow2.AppendChild(NRebarYHeader2);
            Cell SteelHeader2 = new Cell() { CellValue = new CellValue("Steel"), DataType = CellValues.String };
            headerRow2.AppendChild(SteelHeader2);
            Cell coverHeader2 = new Cell() { CellValue = new CellValue("Cover (mm)"), DataType = CellValues.String };
            headerRow2.AppendChild(coverHeader2);

            sheetData2.Append(headerRow2);

            // data for sheet 1
            List<Column> clusterCol = columns.Where(c => c.IsCluster).ToList();
            for (int n = 0; n < clusterCol.Count; n++)
            {
                Column col = clusterCol[n];

                Row rowCol = new Row();

                Cell nameCell = new Cell();
                nameCell.DataType = CellValues.String;
                nameCell.CellValue = new CellValue(col.Name);
                rowCol.AppendChild(nameCell);

                Cell LXCell = new Cell();
                LXCell.DataType = CellValues.Number;
                LXCell.CellValue = new CellValue(col.LX.ToString());
                rowCol.AppendChild(LXCell);

                Cell LYCell = new Cell();
                LYCell.DataType = CellValues.Number;
                LYCell.CellValue = new CellValue(col.LY.ToString());
                rowCol.AppendChild(LYCell);

                Cell concreteCell = new Cell();
                concreteCell.DataType = CellValues.String;
                concreteCell.CellValue = new CellValue(col.ConcreteGrade.Name);
                rowCol.AppendChild(concreteCell);

                Cell NRebarXCell = new Cell();
                NRebarXCell.DataType = CellValues.Number;
                NRebarXCell.CellValue = new CellValue(col.NRebarX.ToString());
                rowCol.AppendChild(NRebarXCell);

                Cell NRebarYCell = new Cell();
                NRebarYCell.DataType = CellValues.Number;
                NRebarYCell.CellValue = new CellValue(col.NRebarY.ToString());
                rowCol.AppendChild(NRebarYCell);

                Cell steelCell = new Cell();
                steelCell.DataType = CellValues.String;
                steelCell.CellValue = new CellValue(col.SteelGrade.Name);
                rowCol.AppendChild(steelCell);

                Cell coverCell = new Cell();
                coverCell.DataType = CellValues.Number;
                coverCell.CellValue = new CellValue(col.CoverToLinks.ToString());
                rowCol.AppendChild(coverCell);

                Cell refCell = new Cell();
                refCell.DataType = CellValues.String;
                string refs = col.Name;
                if (col.IsCluster)
                    refs = col.ColsInCluster.Aggregate((i, j) => i + ", " + j);
                refCell.CellValue = new CellValue(refs);
                rowCol.AppendChild(refCell);

                sheetData2.AppendChild(rowCol);
            }

            worksheetPart2.Worksheet.AppendChild(sheetData2);
            #endregion

            #region Column positions
            //  Sheet 1 : All columns
            WorksheetPart worksheetPart3 = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart3.Worksheet = new Worksheet();
            SheetData sheetData3 = new SheetData();

            Sheet sheet3 = new Sheet()
            {
                Name = "Positions",
                SheetId = 3,
                Id = document.WorkbookPart.GetIdOfPart(worksheetPart3)
            };
            sheets.Append(sheet3);

            // header
            Row headerRow3 = new Row();
            Cell nameHeader = new Cell() { CellValue = new CellValue("Name"), DataType = CellValues.String };
            headerRow3.AppendChild(nameHeader);
            Cell X0Header = new Cell() { CellValue = new CellValue("X0 (mm)"), DataType = CellValues.String };
            headerRow3.AppendChild(X0Header);
            Cell Y0Header = new Cell() { CellValue = new CellValue("Y0 (mm)"), DataType = CellValues.String };
            headerRow3.AppendChild(Y0Header);
            Cell Z0Header = new Cell() { CellValue = new CellValue("Z0 (mm)"), DataType = CellValues.String };
            headerRow3.AppendChild(Z0Header);
            Cell X1Header = new Cell() { CellValue = new CellValue("X1 (mm)"), DataType = CellValues.String };
            headerRow3.AppendChild(X1Header);
            Cell Y1Header = new Cell() { CellValue = new CellValue("Y1 (mm)"), DataType = CellValues.String };
            headerRow3.AppendChild(Y1Header);
            Cell Z1Header = new Cell() { CellValue = new CellValue("Z1 (mm)"), DataType = CellValues.String };
            headerRow3.AppendChild(Z1Header);

            sheetData3.Append(headerRow3);

            // data for sheet 3
            for (int n = 0; n < notClusterCol.Count; n++)
            {
                Column col = notClusterCol[n];

                Row rowCol = new Row();

                Cell nameCell = new Cell();
                nameCell.DataType = CellValues.String;
                nameCell.CellValue = new CellValue(col.Name);
                rowCol.AppendChild(nameCell);

                Cell X0Cell = new Cell();
                X0Cell.DataType = CellValues.Number;
                X0Cell.CellValue = new CellValue(col.Point1.X.ToString());
                rowCol.AppendChild(X0Cell);

                Cell Y0Cell = new Cell();
                Y0Cell.DataType = CellValues.Number;
                Y0Cell.CellValue = new CellValue(col.Point1.Y.ToString());
                rowCol.AppendChild(Y0Cell);

                Cell Z0Cell = new Cell();
                Z0Cell.DataType = CellValues.Number;
                Z0Cell.CellValue = new CellValue(col.Point1.Z.ToString());
                rowCol.AppendChild(Z0Cell);

                Cell X1Cell = new Cell();
                X1Cell.DataType = CellValues.Number;
                X1Cell.CellValue = new CellValue(col.Point2.X.ToString());
                rowCol.AppendChild(X1Cell);

                Cell Y1Cell = new Cell();
                Y1Cell.DataType = CellValues.Number;
                Y1Cell.CellValue = new CellValue(col.Point2.Y.ToString());
                rowCol.AppendChild(Y1Cell);

                Cell Z1Cell = new Cell();
                Z1Cell.DataType = CellValues.Number;
                Z1Cell.CellValue = new CellValue(col.Point2.Z.ToString());
                rowCol.AppendChild(Z1Cell);

                sheetData3.AppendChild(rowCol);
            }

            worksheetPart3.Worksheet.AppendChild(sheetData3);
            #endregion

            workbookPart.Workbook.Save();
            document.Close();
        }

    }

    public class WordReportProgress
    {
        public int Progress { get; set; }
        public string Message { get; set; }
    }
}
