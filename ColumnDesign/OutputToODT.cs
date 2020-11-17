using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using M = DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Wordprocessing;
using WpfMath;
using System.Windows.Media.Imaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
//using static ColumnDesign.CalcuationView;
using ColumnDesignCalc;
using CalcCore;

namespace ColumnDesign
{
    public static class OutputToODT
    {
        public static void WriteToODT(Calculations calculation, bool includeInputs, bool includeBody, bool includeOutputs)
        {
            string filePath;
            try
            {
                var saveDialog = new SaveFileDialog();
                saveDialog.Filter = @"Word files |*.docx";
                saveDialog.FileName = "Design_Col_"+ calculation.Column.Name + @".docx";
                saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (saveDialog.ShowDialog() != DialogResult.OK) return;
                filePath = saveDialog.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Oops..." + Environment.NewLine + ex.Message);
                return;
            }

            WriteToODT(calculation, includeInputs, includeBody, includeOutputs, filePath);

        }


        public static void WriteToODT(Calculations calculation, bool includeInputs, bool includeBody, bool includeOutputs, string filePath)
        {
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
                var line1 = new Paragraph(new Run(new Text("Design of column "+calculation.Column.Name)));
                line1.PrependChild<ParagraphProperties>(new ParagraphProperties() { ParagraphStyleId = new ParagraphStyleId() { Val = "NoSpacing" } });
                var line2 = new Paragraph(new Run(new Text("By: " + Environment.UserName + " on " + DateTime.Today.ToLongDateString())));
                line2.PrependChild<ParagraphProperties>(new ParagraphProperties() { ParagraphStyleId = new ParagraphStyleId() { Val = "NoSpacing" } });
                var line3 = new Paragraph(new Run(new Text("Checked by : ")));
                line3.PrependChild<ParagraphProperties>(new ParagraphProperties() { ParagraphStyleId = new ParagraphStyleId() { Val = "NoSpacing" } });
                var line4 = new Paragraph(new Run(new Text("")));
                line4.PrependChild<ParagraphProperties>(new ParagraphProperties() { ParagraphStyleId = new ParagraphStyleId() { Val = "NoSpacing" } });
                headerPart.RootElement.Append(line1, line2, line3, line4);

                // update the SCaFFOLD inputs / outputs
                calculation.UpdateInputOuput();

                if (includeInputs)
                {
                    Paragraph para = new Paragraph(new Run(new Text("Inputs")));
                    var paraProps = para.PrependChild<ParagraphProperties>(new ParagraphProperties());
                    paraProps.ParagraphStyleId = new ParagraphStyleId() { Val = "Heading1" };
                    body.Append(para);
                    para = new Paragraph(new Run(new Text("Input values for calculation.")));
                    paraProps = para.PrependChild<ParagraphProperties>(new ParagraphProperties());
                    paraProps.ParagraphStyleId = new ParagraphStyleId() { Val = "Normal" };
                    body.Append(para);
                    var tableOfInputs = genTable(calculation.GetInputs());
                    body.Append(tableOfInputs);
                }

                if (includeBody)
                {
                    Paragraph para = new Paragraph(new Run(new Text("Body")));
                    var paraProps = para.PrependChild<ParagraphProperties>(new ParagraphProperties());
                    paraProps.ParagraphStyleId = new ParagraphStyleId() { Val = "Heading1" };
                    body.Append(para);

                    para = new Paragraph(new Run(new Text("Main calculation including diagrams, working, narrative and conclusions.")));
                    paraProps = para.PrependChild<ParagraphProperties>(new ParagraphProperties());
                    paraProps.ParagraphStyleId = new ParagraphStyleId() { Val = "Normal" };
                    body.Append(para);


                    var FormulaeTable = genFormulaeTable(calculation.Expressions, mainPart);
                    body.AppendChild(FormulaeTable);
                }

                if (includeOutputs)
                {
                    var para = new Paragraph(new Run(new Text("Calculated values")));
                    var paraProps = para.PrependChild<ParagraphProperties>(new ParagraphProperties());
                    paraProps.ParagraphStyleId = new ParagraphStyleId() { Val = "Heading1" };
                    body.Append(para);

                    para = new Paragraph(new Run(new Text("List of calculated values.")));
                    paraProps = para.PrependChild<ParagraphProperties>(new ParagraphProperties());
                    paraProps.ParagraphStyleId = new ParagraphStyleId() { Val = "Normal" };
                    body.Append(para);

                    var tableOfOutputs = genTable(calculation.GetOutputs());
                    body.Append(tableOfOutputs);
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
                foreach (var formula in item.Expression)
                {
                    if (formula != "")
                    {
                        var mathPara = new Paragraph();
                        var parser = new TexFormulaParser();
                        var formulaToParse = parser.Parse(formula);
                        var formulaImage = formulaToParse.RenderToPng(20, 20, 20, "Franklin Gothic Book");
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
                            var paraImage = AddImageToBody(mainPart.GetIdOfPart(imagePart), img.Width, img.Height);
                            cell2.AppendChild(paraImage);
                        }
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
                    cell2.AppendChild(paraImage);
                }

                TableCell cell3 = new TableCell();
                cell3.Append(new Paragraph(new Run(new Text(item.Conclusion))));
                cell3.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "1700" }));

                row.Append(cell1, cell2, cell3);
                tableOfInputs.AppendChild(row);
            }
            return tableOfInputs;
        }

        private static Table genTable(List<CalcValueBase> calcVals)
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
                var myMath = new M.OfficeMath(new M.Run(new M.Text(item.Symbol) { Space = SpaceProcessingModeValues.Preserve }));
                para1.AppendChild(myMath);
                para1.AppendChild(new Run(new Text(" ") { Space = SpaceProcessingModeValues.Preserve }));
                TableCell cell1 = new TableCell();
                cell1.Append(para1);
                cell1.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "1200" }));
                var para2 = new Paragraph(new Run(new Text(item.Name)));
                TableCell cell2 = new TableCell();
                cell2.AppendChild(para2);
                cell2.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "6100" }));
                var para3 = new Paragraph();
                if (item.Type == CalcValueType.DOUBLE)
                {
                    myMath = new M.OfficeMath(new M.Run(new M.Text(item.ValueAsString + item.Unit) { Space = SpaceProcessingModeValues.Preserve }));
                    para3.AppendChild(myMath);
                    para3.AppendChild(new Run(new Text(" ") { Space = SpaceProcessingModeValues.Preserve }));
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

        private static Paragraph AddImageToBody(string relationshipId, double width, double height)
        {
            var cx = (int)(width * 7000);
            var cy = (int)(height * 7000);

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
            return new Paragraph(new Run(element));
        }
    }
}
