using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ColumnDesign
{
    public class IDView : ViewModelBase
    {
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

        public IDView()
        {

        }
        

        public void UpdateIDHull(Column column)
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
            
            CenterXY = new Point3D(5, 5, -minZ/(maxZ-minZ)*scaleXYZ); //new Point3D(minX + LengthX/2 / 2, minY + LengthY / 2, minZ);
            CenterXZ = new Point3D(5, -minY/(maxY-minY) * scaleXYZ, 5); //new Point3D(minX + LengthX / 2, minY / 2, minZ + LengthZ / 2);
            CenterYZ = new Point3D(-minX/(maxX-minX) * scaleXYZ, 5, 5); // new Point3D(minX, minY + LengthY / 2, minZ + LengthZ / 2);

            GridLabels = new List<TextVisual3D>();

            GridLabels.Add(new TextVisual3D()
            {
                Text = string.Format("Mx = {0}", (int)maxX),
                Position = new Point3D(scaleXYZ, -minY / (maxY - minY) * scaleXYZ, -1),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(1,0,0),
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
                TextDirection = new Vector3D(0,1,0)
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
                Position = new Point3D(- 1, -minY / (maxY - minY) * scaleXYZ, scaleXYZ),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(1,0,0)
            });
            GridLabels.Add(new TextVisual3D()
            {
                Text = string.Format("N = {0}", (int)minZ),
                Position = new Point3D(- 1, -minY / (maxY - minY) * scaleXYZ, 0),
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
                Text = string.Format("N = {0}",(int)minZ),
                Position = new Point3D(-minX / (maxX - minX) * scaleXYZ, -1, 0),
                Height = 0.5,
                FontWeight = FontWeights.SemiBold,
                TextDirection = new Vector3D(0, 1, 0)
            });
            foreach (var gl in GridLabels)
                modelGroup.Children.Add(gl.Content);

            List<Load> loads = column.AllLoads ? column.Loads : new List<Load>() { column.SelectedLoad };

            
            for (int i = 0; i < loads.Count; i++)
            {
                var pointMesh = new MeshBuilder(false, true);
                Point3D center = new Point3D((loads[i].Mxd - minX) / (maxX - minX) * scaleXYZ, (loads[i].Myd - minY) / (maxY - minY) * scaleXYZ, (-loads[i].P - minZ) / (maxZ - minZ) * scaleXYZ);
                //Console.WriteLine("Center X = {0}, Y = {1}, Z = {2}", center.X, center.Y, center.Z);
                pointMesh.AddSphere(center, radius: 0.2);
                var color0 = Color.FromArgb(255, 50, 50, 255);
                GeometryModel3D mesh0;
                mesh0 = new GeometryModel3D(pointMesh.ToMesh(), MaterialHelper.CreateMaterial(color0));
                mesh0.BackMaterial = mesh0.Material;
                modelGroup.Children.Add(mesh0);

            }

            if(column.FireDesignMethod == FDesignMethod.Advanced || column.FireDesignMethod == FDesignMethod.Isotherm_500 || column.FireDesignMethod == FDesignMethod.Zone_Method)
            {
                var pointMesh = new MeshBuilder(false, true);
                var center = new Point3D((column.FireLoad.Mxd * 1.0 - minX) / (maxX - minX) * scaleXYZ, (column.FireLoad.Myd * 1.0 - minY) / (maxY - minY) * scaleXYZ, (-column.FireLoad.P * 1.0 - minZ) / (maxZ - minZ) * scaleXYZ);
                pointMesh.AddSphere(center, radius: 0.2);
                var color0 = Color.FromArgb(255, 100, 100, 100);
                var mesh0 = new GeometryModel3D(pointMesh.ToMesh(), MaterialHelper.CreateMaterial(color0));
                mesh0.BackMaterial = mesh0.Material;
                fireModelGroup.Children.Add(mesh0);
            }
            
            List<Point3D>  normalPoints = column.diagramVertices.Select(x => new Point3D((x.X - minX) / (maxX - minX), (x.Y - minY) / (maxY - minY), (x.Z - minZ) / (maxZ - minZ))).ToList();
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
    }

}
