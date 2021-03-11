using ColumnDesignCalc;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ColumnDesign
{
    public class ProjectView : ViewModelBase
    {
        public List<Column> AllColumns { get; set; }

        public List<string> SelectedColumns { get; set; }

        Model3D columnsView;
        public Model3D ColumnsView
        {
            get { return columnsView; }
            set { columnsView = value; RaisePropertyChanged(nameof(ColumnsView)); }
        }

        public int NumberOfColumns { get => SelectedColumns.Count; }

        public bool DisplayView { get => AllColumns.Count > 1; }
        
        public void UpdateProjectView(List<Column> allColumns, Column selectedCol)
        {
            AllColumns = allColumns;
            if (selectedCol.IsCluster)
                SelectedColumns = selectedCol.ColsInCluster;
            else
                SelectedColumns = new List<string>() { selectedCol.Name };

            UpdateColumnsView();
        }

        private void UpdateColumnsView()
        {
            List<Column> selected = AllColumns.Where(c => SelectedColumns.Contains(c.Name)).ToList();
            List<Column> unselected = AllColumns.Except(selected).ToList();
            var modelGroup = new Model3DGroup();

            MeshBuilder meshBuilder1 = new MeshBuilder(false, true);
            foreach (var c in selected)
            {
                Point3D center = new Point3D(c.Point1.X * 1e-3, c.Point1.Y * 1e-3, c.Point1.Z * 1e-3);
                double xlength = 0.2;
                double ylength = 0.2;
                double zlength = (c.Point2.Z - c.Point1.Z) * 1e-3;
                meshBuilder1.AddBox(center, xlength, ylength, zlength);
            }
            Color color1 = Color.FromArgb(150, 255, 50, 50);
            var mesh1 = new GeometryModel3D(meshBuilder1.ToMesh(), MaterialHelper.CreateMaterial(color1));
            mesh1.BackMaterial = mesh1.Material;
            modelGroup.Children.Add(mesh1);

            MeshBuilder meshBuilder2 = new MeshBuilder(false, true);
            foreach (var c in unselected)
            {
                Point3D center = new Point3D(c.Point1.X * 1e-3, c.Point1.Y * 1e-3, c.Point1.Z * 1e-3);
                double xlength = 0.2;
                double ylength = 0.2;
                double zlength = (c.Point2.Z - c.Point1.Z) * 1e-3;
                meshBuilder2.AddBox(center, xlength, ylength, zlength);
            }
            Color color2 = Color.FromArgb(20, 150, 150, 150);
            var mesh2 = new GeometryModel3D(meshBuilder2.ToMesh(), MaterialHelper.CreateMaterial(color2));
            mesh2.BackMaterial = mesh2.Material;
            modelGroup.Children.Add(mesh2);

            ColumnsView = modelGroup;
        }
    }
}
