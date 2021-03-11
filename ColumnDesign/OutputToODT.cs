using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using M = DocumentFormat.OpenXml.Math;
using System.Windows.Forms;
using System.Reflection;
using WpfMath;
using CalcCore;

namespace ColumnDesign
{
    public static class OutputToODT
    {

        public static void WriteToODT(ICalc calculation, bool includeInputs, bool includeBody, bool includeOutputs, Settings sett, IProgress<WordReportProgress> progress = null)
        {
            string filePath;
            try
            {
                var saveDialog = new SaveFileDialog();
                saveDialog.Filter = @"Word files |*.docx";
                saveDialog.FileName = calculation.InstanceName + @".docx";
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

            WriteToODT(new List<ICalc> { calculation }, includeInputs, includeBody, includeOutputs, filePath, sett);

        }

        public static void WriteToODT(List<ICalc> calculation, bool includeInputs, bool includeBody, bool includeOutputs, Settings sett, IProgress<WordReportProgress> progress = null)
        {
            string filePath;
            try
            {
                var saveDialog = new SaveFileDialog();
                saveDialog.Filter = @"Word files |*.docx";
                saveDialog.FileName = "calculations" + @".docx";
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

            WriteToODT(calculation, includeInputs, includeBody, includeOutputs, filePath, sett);
        }

        public static void WriteToODT2(List<ICalc> calculation, bool includeInputs, bool includeBody, bool includeOutputs, Settings sett, IProgress<WordReportProgress> progress = null)
        {
            string filePath;
            try
            {
                var fbd = new FolderBrowserDialog();
                Properties.Settings.Default.Reload();
                if (fbd.ShowDialog() != DialogResult.OK) return;
                    filePath = fbd.SelectedPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Oops..." + Environment.NewLine + ex.Message);
                return;
            }

            foreach(var c in calculation)
            {
                string path = filePath + @"\" + c.InstanceName + ".docx";
                WriteToODT(new List<ICalc> { c }, includeInputs, includeBody, includeOutputs, path, sett);

            }
        }

        public static void WriteToODT(List<ICalc> calculations, bool includeInputs, bool includeBody, bool includeOutputs, string filePath, Settings sett = null, IProgress<WordReportProgress> progress = null)
        {
            if (sett == null) sett = new Settings();
            try
            {
                var libraryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Libraries";
                File.Copy(libraryPath + @"\Calculation_Template.docx", filePath, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Oops..." + Environment.NewLine + ex.Message);
                return;
            }

            using (WordprocessingDocument wordDocument =
                WordprocessingDocument.Open(filePath, true))
            {
                var mainPart = wordDocument.MainDocumentPart;
                Body body = mainPart.Document.AppendChild(new Body());

                var headerPart = mainPart.HeaderParts.First();
                //var line1 = new Paragraph(new Run(new Text(calculation.TypeName + " - " + calculation.InstanceName)));
                //line1.PrependChild<ParagraphProperties>(new ParagraphProperties() { ParagraphStyleId = new ParagraphStyleId() { Val = "NoSpacing" } });
                var line2 = new Paragraph(new Run(new Text("By: " + sett.ReportBy + " on " + DateTime.Today.ToLongDateString())));
                line2.PrependChild<ParagraphProperties>(new ParagraphProperties() { ParagraphStyleId = new ParagraphStyleId() { Val = "NoSpacing" } });
                var line3 = new Paragraph(new Run(new Text("Checked by : " + sett.ReportCheckedBy)));
                line3.PrependChild<ParagraphProperties>(new ParagraphProperties() { ParagraphStyleId = new ParagraphStyleId() { Val = "NoSpacing" } });
                var line4 = new Paragraph(new Run(new Text("")));
                line4.PrependChild<ParagraphProperties>(new ParagraphProperties() { ParagraphStyleId = new ParagraphStyleId() { Val = "NoSpacing" } });
                headerPart.RootElement.Append(
                    /*line1,*/
                    line2,
                    line3,
                    line4);

                bool firstCalc = true;
                foreach (var calculation in calculations)
                {
                    if (!firstCalc)
                    {
                        body.Append(new Paragraph(new Run(new Break() { Type = BreakValues.Page })));
                    }

                    Paragraph para = new Paragraph(new Run(new Text(calculation.TypeName + " - " + calculation.InstanceName)));
                    var paraProps = para.PrependChild<ParagraphProperties>(new ParagraphProperties());
                    paraProps.ParagraphStyleId = new ParagraphStyleId() { Val = "Heading1" };
                    body.Append(para);

                    if (includeInputs)
                    {
                        para = new Paragraph(new Run(new Text("Inputs")));
                        paraProps = para.PrependChild<ParagraphProperties>(new ParagraphProperties());
                        paraProps.ParagraphStyleId = new ParagraphStyleId() { Val = "Heading2" };
                        body.Append(para);
                        para = new Paragraph(new Run(new Text("Input values for calculation.")));
                        paraProps = para.PrependChild<ParagraphProperties>(new ParagraphProperties());
                        paraProps.ParagraphStyleId = new ParagraphStyleId() { Val = "Normal" };
                        body.Append(para);
                        var tableOfInputs = genTable(calculation.GetInputs(), mainPart);
                        body.Append(tableOfInputs);
                    }

                    if (includeBody)
                    {
                        para = new Paragraph(new Run(new Text("Body")));
                        paraProps = para.PrependChild<ParagraphProperties>(new ParagraphProperties());
                        paraProps.ParagraphStyleId = new ParagraphStyleId() { Val = "Heading2" };
                        body.Append(para);

                        para = new Paragraph(new Run(new Text("Main calculation including diagrams, working, narrative and conclusions.")));
                        paraProps = para.PrependChild<ParagraphProperties>(new ParagraphProperties());
                        paraProps.ParagraphStyleId = new ParagraphStyleId() { Val = "Normal" };
                        body.Append(para);


                        var FormulaeTable = genFormulaeTable(calculation.GetFormulae(), mainPart);
                        body.AppendChild(FormulaeTable);
                    }

                    if (includeOutputs)
                    {
                        para = new Paragraph(new Run(new Text("Calculated values")));
                        paraProps = para.PrependChild<ParagraphProperties>(new ParagraphProperties());
                        paraProps.ParagraphStyleId = new ParagraphStyleId() { Val = "Heading2" };
                        body.Append(para);

                        para = new Paragraph(new Run(new Text("List of calculated values.")));
                        paraProps = para.PrependChild<ParagraphProperties>(new ParagraphProperties());
                        paraProps.ParagraphStyleId = new ParagraphStyleId() { Val = "Normal" };
                        body.Append(para);

                        var tableOfOutputs = genTable(calculation.GetOutputs(), mainPart);
                        body.Append(tableOfOutputs);
                    }
                    firstCalc = false;
                }
            }
        }

        private static Table genFormulaeTable(List<Formula> formulae, MainDocumentPart mainPart)
        {
            Table tableOfInputs = new Table();
            var tableGrid = new TableGrid();
            tableGrid.AppendChild(new GridColumn());
            tableGrid.AppendChild(new GridColumn());
            tableGrid.AppendChild(new GridColumn());
            tableOfInputs.AppendChild(tableGrid);
            var tableProps = new TableProperties();
            tableProps.AppendChild(new TableLayout() { Type = TableLayoutValues.Fixed });
            tableProps.AppendChild(new TableWidth() { Width = "9000", Type = TableWidthUnitValues.Dxa });
            tableProps.AppendChild(new TableBorders() { InsideHorizontalBorder = new InsideHorizontalBorder() { Color = "c0c0c0", Size = 4, Val = BorderValues.Single } });
            tableProps.AppendChild(new TableBorders() { BottomBorder = new BottomBorder() { Color = "c0c0c0", Size = 4, Val = BorderValues.Single } });
            tableProps.AppendChild(new TableBorders() { TopBorder = new TopBorder() { Color = "c0c0c0", Size = 4, Val = BorderValues.Single } });
            //tableProps.AppendChild(new TableBorders() { LeftBorder = new LeftBorder() { Color = "c0c0c0", Size = 4, Val = BorderValues.Single } });
            //tableProps.AppendChild(new TableBorders() { InsideVerticalBorder = new InsideVerticalBorder() { Color = "c0c0c0", Size = 4, Val = BorderValues.Single } });
            //tableProps.AppendChild(new TableBorders() { RightBorder = new RightBorder() { Color = "c0c0c0", Size = 4, Val = BorderValues.Single } });
            tableOfInputs.AppendChild(tableProps);

            foreach (var item in formulae)
            {
                TableRow row = new TableRow();
                var para = new Paragraph();
                para.AppendChild((new Run(new Text(item.Ref))));
                TableCell cell1 = new TableCell();
                cell1.Append(para);
                cell1.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "1200" }));
                TableCell cell2 = new TableCell();
                cell2.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "6100" }));
                cell2.AppendChild(new Paragraph(new Run(new Text(item.Narrative))));
                //foreach (var formula in item.Expression)
                //{
                //    var mathPara = new Paragraph();
                //    var myMath = new M.OfficeMath(new M.Run(new M.Text(formula + Environment.NewLine) { Space = SpaceProcessingModeValues.Preserve }));
                //    mathPara.AppendChild(myMath);
                //    mathPara.AppendChild(new Run(new Text(" ") { Space = SpaceProcessingModeValues.Preserve }));
                //    cell2.AppendChild(mathPara);
                //}
                foreach (var formula in item.Expression)
                {
                    if (formula != "")
                    {
                        TexFormula formulaToParse = new TexFormula();
                        try
                        {
                            var parser = new TexFormulaParser();
                            formulaToParse = parser.Parse(formula);

                        }
                        catch (Exception)
                        {
                            var parser = new TexFormulaParser();
                            formulaToParse = parser.Parse("Error in LaTeX string...");
                        }
                        var test = formulaToParse.GetRenderer(TexStyle.Script, 100, "Franklin Gothic Book");
                        if (test.RenderSize.Width > 0 && test.RenderSize.Height > 0)
                        {
                            var formulaImage = formulaToParse.RenderToPng(100, 0, 0, "Franklin Gothic Book");
                            ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Png);
                            using (var stream = new MemoryStream(formulaImage))
                            {
                                imagePart.FeedData(stream);
                                var img = new BitmapImage();
                                img.BeginInit();
                                img.StreamSource = stream;
                                img.CacheOption = BitmapCacheOption.OnLoad;
                                img.EndInit();
                                img.Freeze();
                                var paraImage = AddImageToBody(mainPart.GetIdOfPart(imagePart), img.Width * 2.54 / 600, img.Height * 2.54 / 600);
                                cell2.AppendChild(new Paragraph(new Run(paraImage)));
                            }
                        }

                        GC.WaitForPendingFinalizers();
                    }
                    //var myMath = new M.OfficeMath(new M.Run(new M.Text(formula + Environment.NewLine) { Space = SpaceProcessingModeValues.Preserve }));
                    //mathPara.AppendChild(myMath);
                    //mathPara.AppendChild(new Run(new Text(" ") { Space = SpaceProcessingModeValues.Preserve }));
                    //cell2.AppendChild(mathPara);
                }

                if (item.Image != null)
                {
                    ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Png);
                    var tempFile = Path.GetTempFileName();
                    PngBitmapEncoder png = new PngBitmapEncoder();
                    //CHANGEBITMAP
                    //png.Frames.Add(BitmapFrame.Create(item.Image));
                    var width = Math.Min(10d, item.Image.Width * 2.54 / 96);
                    var height = ((double)item.Image.Height / (double)item.Image.Width) * width;
                    using (SkiaSharp.SKWStream stm = new SkiaSharp.SKFileWStream(tempFile))
                    {
                        item.Image.Encode(stm, SkiaSharp.SKEncodedImageFormat.Png, 0);
                    }
                    using (FileStream stream = new FileStream(tempFile, FileMode.Open))
                    {
                        imagePart.FeedData(stream);
                    }
                    var paraImage = AddImageToBody(mainPart.GetIdOfPart(imagePart), width, height);
                    cell2.AppendChild(new Paragraph(new Run(paraImage)));
                }

                TableCell cell3 = new TableCell();
                cell3.Append(new Paragraph(new Run(new Text(item.Conclusion))));
                cell3.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "1700" }));

                row.Append(cell1, cell2, cell3);
                tableOfInputs.AppendChild(row);
            }
            return tableOfInputs;
        }

        private static Table genTable(List<CalcCore.CalcValueBase> calcVals, MainDocumentPart mainPart)
        {
            Table tableOfInputs = new Table();
            var tableGrid = new TableGrid();
            tableGrid.AppendChild(new GridColumn());
            tableGrid.AppendChild(new GridColumn());
            tableGrid.AppendChild(new GridColumn());
            tableOfInputs.AppendChild(tableGrid);
            var tableProps = new TableProperties();
            tableProps.AppendChild(new TableLayout() { Type = TableLayoutValues.Fixed });
            tableProps.AppendChild(new TableWidth() { Width = "9000", Type = TableWidthUnitValues.Dxa });
            tableProps.AppendChild(new TableBorders() { InsideHorizontalBorder = new InsideHorizontalBorder() { Color = "c0c0c0", Size = 4, Val = BorderValues.Single } });
            tableProps.AppendChild(new TableBorders() { BottomBorder = new BottomBorder() { Color = "c0c0c0", Size = 4, Val = BorderValues.Single } });
            tableProps.AppendChild(new TableBorders() { TopBorder = new TopBorder() { Color = "c0c0c0", Size = 4, Val = BorderValues.Single } });
            //tableProps.AppendChild(new TableBorders() { LeftBorder = new LeftBorder() { Color = "c0c0c0", Size = 4, Val = BorderValues.Single } });
            //tableProps.AppendChild(new TableBorders() { InsideVerticalBorder = new InsideVerticalBorder() { Color = "c0c0c0", Size = 4, Val = BorderValues.Single } });
            //tableProps.AppendChild(new TableBorders() { RightBorder = new RightBorder() { Color = "c0c0c0", Size = 4, Val = BorderValues.Single } });

            tableOfInputs.AppendChild(tableProps);

            foreach (var item in calcVals)
            {
                TableRow row = new TableRow();
                var para1 = new Paragraph();
                TableCell cell1 = new TableCell();

                // insert symbol as image
                var parser = new TexFormulaParser();
                if (item.Symbol != "")
                {
                    TexFormula formulaToParse = new TexFormula();
                    try
                    {
                        parser = new TexFormulaParser();
                        formulaToParse = parser.Parse(item.Symbol);

                    }
                    catch (Exception)
                    {
                        parser = new TexFormulaParser();
                        formulaToParse = parser.Parse("Error in LaTeX string...");
                    }
                    var formulaImage = formulaToParse.RenderToPng(100, 0, 0, "Franklin Gothic Book");
                    ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Png);
                    using (var stream = new MemoryStream(formulaImage))
                    {
                        imagePart.FeedData(stream);
                        var img = new BitmapImage();
                        img.BeginInit();
                        img.StreamSource = stream;
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        img.Freeze();
                        var paraImage = AddImageToBody(mainPart.GetIdOfPart(imagePart), img.Width * 2.54 / 600, img.Height * 2.54 / 600);
                        para1.AppendChild(new Run(paraImage));
                    }
                }
                else
                    para1.AppendChild(new Run(new Text(" ")));


                //var myMath = new M.OfficeMath(new M.Run(new M.Text(item.Symbol) { Space = SpaceProcessingModeValues.Preserve }));
                //para1.AppendChild(myMath);
                //para1.AppendChild(new Run(new Text(" ") { Space = SpaceProcessingModeValues.Preserve }));

                cell1.Append(para1);
                cell1.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "1200" }));
                var para2 = new Paragraph(new Run(new Text(item.Name)));
                TableCell cell2 = new TableCell();
                cell2.AppendChild(para2);
                cell2.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "6100" }));
                var para3 = new Paragraph();
                if (item.Type == CalcValueType.DOUBLE)
                {
                    // insert symbol as image
                    parser = new TexFormulaParser();
                    string toRender = item.ValueAsString + item.Unit;
                    if (toRender != "")
                    {
                        TexFormula formulaToParse = new TexFormula();
                        try
                        {
                            parser = new TexFormulaParser();
                            formulaToParse = parser.Parse(toRender);

                        }
                        catch (Exception)
                        {
                            parser = new TexFormulaParser();
                            formulaToParse = parser.Parse("Error in LaTeX string...");
                        }
                        var formulaImage = formulaToParse.RenderToPng(100, 0, 0, "Franklin Gothic Book");
                        ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Png);
                        using (var stream = new MemoryStream(formulaImage))
                        {
                            imagePart.FeedData(stream);
                            var img = new BitmapImage();
                            img.BeginInit();
                            img.StreamSource = stream;
                            img.CacheOption = BitmapCacheOption.OnLoad;
                            img.EndInit();
                            img.Freeze();
                            var paraImage = AddImageToBody(mainPart.GetIdOfPart(imagePart), img.Width * 2.54 / 600, img.Height * 2.54 / 600);
                            para3.AppendChild(new Run(paraImage));
                        }
                    }
                    else
                        para3.AppendChild(new Run(new Text(" ")));
                    //myMath = new DocumentFormat.OpenXml.Math.OfficeMath(new M.Run(new M.Text(item.ValueAsString + item.Unit) { Space = SpaceProcessingModeValues.Preserve }));
                    //para3.AppendChild(myMath);
                    //para3.AppendChild(new Run(new Text(" ") { Space = SpaceProcessingModeValues.Preserve }));
                }
                else if (item.Type == CalcValueType.SELECTIONLIST)
                {
                    para3.AppendChild(new Run(new Text(item.ValueAsString)));
                }
                else
                {
                    cell2.AppendChild(new Paragraph(new Run(new Text(item.ValueAsString))));
                }
                TableCell cell3 = new TableCell();
                cell3.Append(para3);
                cell3.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "1700" }));
                row.Append(cell1, cell2, cell3);
                tableOfInputs.AppendChild(row);

            }
            return tableOfInputs;

        }

        private static Drawing AddImageToBody(string relationshipId, double width, double height)
        {
            var cx = (int)(width * 914400 / 2.54);
            var cy = (int)(height * 914400 / 2.54);

            // Define the reference of the image.
            var element =
                 new Drawing(
                     new DW.Inline(
                         new DW.Extent() { Cx = cx, Cy = cy },
                         new DW.EffectExtent()
                         {
                             LeftEdge = 0L,
                             TopEdge = 0L,
                             RightEdge = 0L,
                             BottomEdge = 0L
                         },
                         new DW.DocProperties()
                         {
                             Id = (UInt32Value)1U,
                             Name = "Picture 1"
                         },
                         new DW.NonVisualGraphicFrameDrawingProperties(
                             new A.GraphicFrameLocks() { NoChangeAspect = true }),
                         new A.Graphic(
                             new A.GraphicData(
                                 new PIC.Picture(
                                     new PIC.NonVisualPictureProperties(
                                         new PIC.NonVisualDrawingProperties()
                                         {
                                             Id = (UInt32Value)0U,
                                             Name = "New Bitmap Image.png"
                                         },
                                         new PIC.NonVisualPictureDrawingProperties()),
                                     new PIC.BlipFill(
                                         new A.Blip(
                                             new A.BlipExtensionList(
                                                 new A.BlipExtension()
                                                 {
                                                     Uri =
                                                        "{28A0092B-C50C-407E-A947-70E740481C1C}"
                                                 })
                                         )
                                         {
                                             Embed = relationshipId,
                                             CompressionState =
                                             A.BlipCompressionValues.Print
                                         },
                                         new A.Stretch(
                                             new A.FillRectangle())),
                                     new PIC.ShapeProperties(
                                         new A.Transform2D(
                                             new A.Offset() { X = 0L, Y = 0L },
                                             new A.Extents() { Cx = cx, Cy = cy }),
                                         new A.PresetGeometry(
                                             new A.AdjustValueList()
                                         )
                                         { Preset = A.ShapeTypeValues.Rectangle }))
                             )
                             { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                     )
                     {
                         DistanceFromTop = (UInt32Value)0U,
                         DistanceFromBottom = (UInt32Value)0U,
                         DistanceFromLeft = (UInt32Value)0U,
                         DistanceFromRight = (UInt32Value)0U,
                         EditId = "50D07946"
                     });

            // Append the reference to body, the element should be in a Run.
            return element;
        }
    }
}
