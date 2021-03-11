using ColumnDesignCalc;
using HelixToolkit.Wpf;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using FontWeights = System.Windows.FontWeights;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using System.Threading;
using OxyPlot.Axes;

namespace ColumnDesign
{
    public enum IDDimension { dim2D , dim3D }
    public class IDView : ViewModelBase
    {
        IDDimension dimension = IDDimension.dim3D;
        public IDDimension Dimension
        {
            get { return dimension; }
            set { dimension = value; RaisePropertyChanged(nameof(Dimension)); }
        }
        // 3D Interaction Diagram
        Model3D iDHull;
        public Model3D IDHull
        {
            get { return iDHull; }
            set { iDHull = value; RaisePropertyChanged(nameof(IDHull)); }
        }

        Model3D fireDDHull;
        public Model3D FireIDHull
        {
            get { return fireDDHull; }
            set { fireDDHull = value; RaisePropertyChanged(nameof(FireIDHull)); }
        }

        List<Point3D> intersectionXZ;
        public List<Point3D> IntersectionXZ
        {
            get { return intersectionXZ; }
            set { intersectionXZ = value; RaisePropertyChanged(nameof(IntersectionXZ)); }
        }

        List<Point3D> intersectionYZ;
        public List<Point3D> IntersectionYZ
        {
            get { return intersectionYZ; }
            set { intersectionYZ = value; RaisePropertyChanged(nameof(IntersectionYZ)); }
        }

        public List<TextVisual3D> GridLabels { get; set; }

        Point3D centerXY;
        public Point3D CenterXY
        {
            get { return centerXY; }
            set { centerXY = value; RaisePropertyChanged(nameof(CenterXY)); }
        }
        Point3D centerXZ;
        public Point3D CenterXZ
        {
            get { return centerXZ; }
            set { centerXZ = value; RaisePropertyChanged(nameof(CenterXZ)); }
        }
        Point3D centerYZ;
        public Point3D CenterYZ
        {
            get { return centerYZ; }
            set { centerYZ = value; RaisePropertyChanged(nameof(CenterYZ)); }
        }

        //int interval = 10;
        double scaleXYZ = 10;
        bool isUpdated = false;
        public bool IsUpdated
        {
            get { return isUpdated; }
            set { isUpdated = value; RaisePropertyChanged(nameof(IsUpdated)); }
        }

        // 2D Interaction Diagram
        PlotModel mxmyID;
        public PlotModel MxMyID
        {
            get { return mxmyID; }
            set { mxmyID = value; RaisePropertyChanged(nameof(MxMyID)); }
        }

        PlotModel mxnID;
        public PlotModel MxNID
        {
            get { return mxnID; }
            set { mxnID = value; RaisePropertyChanged(nameof(MxNID)); }
        }

        PlotModel myNID;
        public PlotModel MyNID
        {
            get { return myNID; }
            set { myNID = value; RaisePropertyChanged(nameof(MyNID)); }
        }

        public IDView()
        {

        }

        public void UpdateIDHull(Column column)
        {
            if (column.diagramVertices.Count == 0) return;
            Update3DID(column);
            Update2DIDs(column);
        }
        public void Update3DID(Column column)
        {
            var modelGroup = new Model3DGroup();
            var meshBuilder = new MeshBuilder(false, true);
            var fireModelGroup = new Model3DGroup();
            var fireMeshBuilder = new MeshBuilder(false, true);

            double maxX = column.diagramVertices.Max(pt => pt.X);
            double maxY = column.diagramVertices.Max(pt => pt.Y);
            double maxZ = column.diagramVertices.Max(pt => pt.Z);

            double minX = column.diagramVertices.Min(pt => pt.X);
            double minY = column.diagramVertices.Min(pt => pt.Y);
            double minZ = column.diagramVertices.Min(pt => pt.Z);

            CenterXY = new Point3D(5, 5, -minZ / (maxZ - minZ) * scaleXYZ); //new Point3D(minX + LengthX/2 / 2, minY + LengthY / 2, minZ);
            CenterXZ = new Point3D(5, -minY / (maxY - minY) * scaleXYZ, 5); //new Point3D(minX + LengthX / 2, minY / 2, minZ + LengthZ / 2);
            CenterYZ = new Point3D(-minX / (maxX - minX) * scaleXYZ, 5, 5); // new Point3D(minX, minY + LengthY / 2, minZ + LengthZ / 2);

            GridLabels = new List<TextVisual3D>();

            GridLabels.Add(new TextVisual3D()
            {
                Text = string.Format("Mx = {0}", (int)maxX),
                Position = new Point3D(scaleXYZ, -minY / (maxY - minY) * scaleXYZ, -1),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(1, 0, 0),
            });
            GridLabels.Add(new TextVisual3D()
            {
                Text = string.Format("Mx = {0}", (int)minX),
                Position = new Point3D(0, -minY / (maxY - minY) * scaleXYZ, -1),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(1, 0, 0)
            });
            GridLabels.Add(new TextVisual3D()
            {
                Text = string.Format("Mx = {0}", (int)maxX),
                Position = new Point3D(scaleXYZ, -minY / (maxY - minY) * scaleXYZ, scaleXYZ + 1),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(1, 0, 0),
            });
            GridLabels.Add(new TextVisual3D()
            {
                Text = string.Format("Mx = {0}", (int)minX),
                Position = new Point3D(0, -minY / (maxY - minY) * scaleXYZ, scaleXYZ + 1),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(1, 0, 0)
            });
            GridLabels.Add(new TextVisual3D()
            {
                Text = string.Format("My = {0}", (int)maxY),
                Position = new Point3D(-minX / (maxX - minX) * scaleXYZ, scaleXYZ, -1),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(0, 1, 0)
            });
            GridLabels.Add(new TextVisual3D()
            {
                Text = string.Format("My = {0}", (int)minY),
                Position = new Point3D(-minX / (maxX - minX) * scaleXYZ, 0, -1),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(0, 1, 0)
            });
            GridLabels.Add(new TextVisual3D()
            {
                Text = string.Format("My = {0}", (int)maxY),
                Position = new Point3D(-minX / (maxX - minX) * scaleXYZ, scaleXYZ, scaleXYZ + 1),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(0, 1, 0)
            });
            GridLabels.Add(new TextVisual3D()
            {
                Text = string.Format("My = {0}", (int)minY),
                Position = new Point3D(-minX / (maxX - minX) * scaleXYZ, 0, scaleXYZ + 1),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(0, 1, 0)
            });
            GridLabels.Add(new TextVisual3D()
            {
                Text = string.Format("N = {0}", (int)maxZ),
                Position = new Point3D(-1, -minY / (maxY - minY) * scaleXYZ, scaleXYZ),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(1, 0, 0)
            });
            GridLabels.Add(new TextVisual3D()
            {
                Text = string.Format("N = {0}", (int)minZ),
                Position = new Point3D(-1, -minY / (maxY - minY) * scaleXYZ, 0),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(1, 0, 0)
            });
            GridLabels.Add(new TextVisual3D()
            {
                Text = string.Format("N = {0}", (int)maxZ),
                Position = new Point3D(-minX / (maxX - minX) * scaleXYZ, -1, scaleXYZ),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(0, 1, 0)
            });
            GridLabels.Add(new TextVisual3D()
            {
                Text = string.Format("N = {0}", (int)minZ),
                Position = new Point3D(-minX / (maxX - minX) * scaleXYZ, -1, 0),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(0, 1, 0)
            });
            foreach (var gl in GridLabels)
                modelGroup.Children.Add(gl.Content);

            List<Load> loads = column.AllLoads ? column.Loads : new List<Load>() { column.SelectedLoad };

            var pointMesh = new MeshBuilder(false, true);
            for (int i = 0; i < loads.Count; i++)
            {
                Point3D center = new Point3D((loads[i].MEdx - minX) / (maxX - minX) * scaleXYZ, (loads[i].MEdy - minY) / (maxY - minY) * scaleXYZ, (-loads[i].P - minZ) / (maxZ - minZ) * scaleXYZ);
                pointMesh.AddSphere(center, radius: 0.2, thetaDiv: 8, phiDiv: 8);
            }
            var color0 = Color.FromArgb(255, 50, 50, 255);
            GeometryModel3D mesh0;
            mesh0 = new GeometryModel3D(pointMesh.ToMesh(), MaterialHelper.CreateMaterial(color0));
            mesh0.BackMaterial = mesh0.Material;
            modelGroup.Children.Add(mesh0);

            if (column.FireDesignMethod == FDesignMethod.Advanced || column.FireDesignMethod == FDesignMethod.Isotherm_500 || column.FireDesignMethod == FDesignMethod.Zone_Method)
            {
                var pointMeshFire = new MeshBuilder(false, true);
                var center = new Point3D((column.FireLoad.MEdx * 1.0 - minX) / (maxX - minX) * scaleXYZ, (column.FireLoad.MEdy * 1.0 - minY) / (maxY - minY) * scaleXYZ, (-column.FireLoad.P * 1.0 - minZ) / (maxZ - minZ) * scaleXYZ);
                pointMeshFire.AddSphere(center, radius: 0.2, thetaDiv:8, phiDiv:8);
                var color0Fire = Color.FromArgb(255, 100, 100, 100);
                var mesh0Fire = new GeometryModel3D(pointMeshFire.ToMesh(), MaterialHelper.CreateMaterial(color0Fire));
                mesh0Fire.BackMaterial = mesh0.Material;
                fireModelGroup.Children.Add(mesh0Fire);
            }

            List<Point3D> normalPoints = column.diagramVertices.Select(x => new Point3D((x.X - minX) / (maxX - minX), (x.Y - minY) / (maxY - minY), (x.Z - minZ) / (maxZ - minZ))).ToList();
            normalPoints = normalPoints.Select(x => new Point3D(scaleXYZ * x.X, scaleXYZ * x.Y, scaleXYZ * x.Z)).ToList();

            List<Point3D> normalFirePoints = column.fireDiagramVertices.Select(x => new Point3D((x.X - minX) / (maxX - minX), (x.Y - minY) / (maxY - minY), (x.Z - minZ) / (maxZ - minZ))).ToList();
            normalFirePoints = normalFirePoints.Select(x => new Point3D(scaleXYZ * x.X, scaleXYZ * x.Y, scaleXYZ * x.Z)).ToList();

            // column interaction diagram
            for (int i = 0; i < normalPoints.Count; i++)
            {
                Point3D pt = normalPoints[i];
                meshBuilder.Positions.Add(pt);
                meshBuilder.TextureCoordinates.Add(new Point());
            }

            for (int i = 0; i < column.diagramFaces.Count; i++)
            {
                var t = column.diagramFaces[i];
                meshBuilder.AddTriangle(new List<int>{
                    column.diagramVertices.IndexOf(t.Points[0]),
                    column.diagramVertices.IndexOf(t.Points[1]),
                    column.diagramVertices.IndexOf(t.Points[2]) });
                meshBuilder.AddCylinder(normalPoints[column.diagramVertices.IndexOf(t.Points[0])],
                    normalPoints[column.diagramVertices.IndexOf(t.Points[1])], 0.05, 8);
                meshBuilder.AddCylinder(normalPoints[column.diagramVertices.IndexOf(t.Points[1])],
                    normalPoints[column.diagramVertices.IndexOf(t.Points[2])], 0.05, 8);
                meshBuilder.AddCylinder(normalPoints[column.diagramVertices.IndexOf(t.Points[0])],
                    normalPoints[column.diagramVertices.IndexOf(t.Points[2])], 0.05, 8);
            }
            //var mat = new DiffuseMaterial(GradientBrushes.BlueWhiteRed);
            var color = Color.FromArgb(150, 200, 0, 0);
            var mesh = new GeometryModel3D(meshBuilder.ToMesh(), MaterialHelper.CreateMaterial(color));
            mesh.BackMaterial = mesh.Material;
            modelGroup.Children.Add(mesh);

            // column fire interaction diagram
            if (column.FireDesignMethod == FDesignMethod.Advanced)
            {

                for (int i = 0; i < normalFirePoints.Count; i++)
                {
                    Point3D pt = normalFirePoints[i];
                    fireMeshBuilder.Positions.Add(pt);
                    fireMeshBuilder.TextureCoordinates.Add(new Point());
                }

                for (int i = 0; i < column?.fireDiagramFaces.Count; i++)
                {
                    var t = column.fireDiagramFaces[i];
                    fireMeshBuilder.AddTriangle(new List<int>{
                        column.fireDiagramVertices.IndexOf(t.Points[0]),
                        column.fireDiagramVertices.IndexOf(t.Points[1]),
                        column.fireDiagramVertices.IndexOf(t.Points[2]) });
                    fireMeshBuilder.AddCylinder(normalFirePoints[column.fireDiagramVertices.IndexOf(t.Points[0])],
                        normalFirePoints[column.fireDiagramVertices.IndexOf(t.Points[1])], 0.05, 8);
                    fireMeshBuilder.AddCylinder(normalFirePoints[column.fireDiagramVertices.IndexOf(t.Points[1])],
                        normalFirePoints[column.fireDiagramVertices.IndexOf(t.Points[2])], 0.05, 8);
                    fireMeshBuilder.AddCylinder(normalFirePoints[column.fireDiagramVertices.IndexOf(t.Points[0])],
                        normalFirePoints[column.fireDiagramVertices.IndexOf(t.Points[2])], 0.05, 8);
                }
                var fireColor = Color.FromArgb(150, 0, 200, 0);
                var fireMesh = new GeometryModel3D(fireMeshBuilder.ToMesh(), MaterialHelper.CreateMaterial(fireColor));
                fireMesh.BackMaterial = fireMesh.Material;
                fireModelGroup.Children.Add(fireMesh);
            }

            this.IDHull = modelGroup;
            this.FireIDHull = fireModelGroup;
        }

        public void Update2DIDs(Column column)
        {
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
            sp1.Points.Add(new DataPoint(column.SelectedLoad.MEdx, column.SelectedLoad.MEdy));

            MxMyID = new PlotModel()
            {
                Title = "Mx-My interaction diagram",
                Subtitle = "(N = " + Math.Round(column.SelectedLoad.P) + "kN)"
            };
            MxMyID.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                Title = "Mx",
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dash,
            });
            MxMyID.Axes.Add(new LinearAxis()
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
            foreach (var p in column.MxMyPts)
                s1.Points.Add(new DataPoint(p.X, p.Y));
            MxMyID.Series.Add(s1);
            MxMyID.Series.Add(sp1);

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
            sp2.Points.Add(new DataPoint(column.SelectedLoad.MEdx, -column.SelectedLoad.P));

            MxNID = new PlotModel()
            {
                Title = "Mx-N interaction diagram",
                Subtitle = "(My = " + Math.Round(column.SelectedLoad.MEdy) + "kN.m)"
            };
            MxNID.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                Title = "Mx",
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dash,
            });
            MxNID.Axes.Add(new LinearAxis()
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
            foreach (var p in column.MxNPts)
                s2.Points.Add(new DataPoint(p.X, p.Y));
            MxNID.Series.Add(s2);
            MxNID.Series.Add(sp2);

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
            sp3.Points.Add(new DataPoint(column.SelectedLoad.MEdy, -column.SelectedLoad.P));

            MyNID = new PlotModel()
            {
                Title = "My-N interaction diagram",
                Subtitle = "(Mx = " + Math.Round(column.SelectedLoad.MEdx) + "kN.m)",
            };
            MyNID.Axes.Add(new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                Title = "My",
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dash,
            });
            MyNID.Axes.Add(new LinearAxis()
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
            foreach (var p in column.MyNPts)
                s3.Points.Add(new DataPoint(p.X, p.Y));
            MyNID.Series.Add(s3);
            MyNID.Series.Add(sp3);

            if (column.FireDesignMethod == FDesignMethod.Advanced)
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
                spf1.Points.Add(new DataPoint(column.FireLoad.MEdx, column.FireLoad.MEdy));

                LineSeries sf1 = new LineSeries()
                {
                    Color = OxyColors.DarkRed,
                    MarkerType = MarkerType.None,
                    StrokeThickness = 1
                };
                foreach (var p in column.fireMxMyPts)
                    sf1.Points.Add(new DataPoint(p.X, p.Y));
                MxMyID.Series.Add(sf1);
                MxMyID.Series.Add(spf1);

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
                spf2.Points.Add(new DataPoint(column.FireLoad.MEdx, -column.FireLoad.P));

                LineSeries sf2 = new LineSeries()
                {
                    Color = OxyColors.DarkBlue,
                    MarkerType = MarkerType.None,
                    StrokeThickness = 1
                };
                foreach (var p in column.fireMxNPts)
                    sf2.Points.Add(new DataPoint(p.X, p.Y));
                MxNID.Series.Add(sf2);
                MxNID.Series.Add(spf2);

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
                spf3.Points.Add(new DataPoint(column.FireLoad.MEdy, -column.FireLoad.P));
                LineSeries sf3 = new LineSeries()
                {
                    Color = OxyColors.DarkGreen,
                    MarkerType = MarkerType.None,
                    StrokeThickness = 1
                };
                foreach (var p in column.fireMyNPts)
                    sf3.Points.Add(new DataPoint(p.X, p.Y));
                MyNID.Series.Add(sf3);
                MyNID.Series.Add(spf3);
            }

            MxMyID.InvalidatePlot(true);
            MxNID.InvalidatePlot(true);
            MyNID.InvalidatePlot(true);
            RaisePropertyChanged(nameof(MxMyID));
            RaisePropertyChanged(nameof(MxNID));
            RaisePropertyChanged(nameof(MyNID));

            Print2DIDs();
        }

        private void Print2DIDs()
        {
            string[] plots = new string[] { "MxMy", "MxN", "MyN" };
            foreach (var p in plots)
            {
                var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 600, Height = 400, Background = OxyColors.White };
                var bitmap = pngExporter.ExportToBitmap(this.GetType().GetProperty(p + "ID").GetValue(this) as PlotModel);

                MemoryStream stream = new MemoryStream();
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);

                Bitmap bmp = new Bitmap(stream);

                ImageConverter converter = new ImageConverter();
                byte[] output = (byte[])converter.ConvertTo(bmp, typeof(byte[]));

                string path = System.IO.Path.GetTempPath() + p + ".tmp";
                File.WriteAllBytes(path, output);
            }

        }

        public static void Generate2DIDs(Column col)
        {
            if (col.diagramVertices.Count == 0) col.GetInteractionDiagram();
            if (col.MxMyPts.Count == 0) col.Get2DMaps();

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
                var bitmap = pngExporter.ExportToBitmap(plots[i]);

                MemoryStream stream = new MemoryStream();
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);

                Bitmap bmp = new Bitmap(stream);

                ImageConverter converter = new ImageConverter();
                byte[] output = (byte[])converter.ConvertTo(bmp, typeof(byte[]));

                string path = System.IO.Path.GetTempPath() + plotNames[i] + ".tmp";
                File.WriteAllBytes(path, output);
            }
        }
    }
}
