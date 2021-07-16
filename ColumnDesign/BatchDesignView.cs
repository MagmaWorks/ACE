using BatchDesign;
using ColumnDesignCalc;
using GenericViewer;
using HelixToolkit.Wpf;
using MWGeometry;
using Optimisation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using KnownColor = System.Drawing.KnownColor;

namespace ColumnDesign
{
    public class BatchDesignView : ViewModelBase
    {
        
        public DesignOptimiser Optimiser = new DesignOptimiser();

        Model3D loadsCloud;
        public Model3D LoadsCloud
        {
            get { return loadsCloud; }
            set { loadsCloud = value; RaisePropertyChanged(nameof(LoadsCloud)); }
        }

        Model3D clusteredColumns;
        public Model3D ClusteredColumns
        {
            get { return clusteredColumns; }
            set { clusteredColumns = value; RaisePropertyChanged(nameof(ClusteredColumns)); }
        }

        Model3D clusteredDesigns;
        public Model3D ClusteredDesigns
        {
            get { return clusteredDesigns; }
            set { clusteredDesigns = value; RaisePropertyChanged(nameof(ClusteredDesigns)); }
        }

        List<DesignKey> designKeys;
        public List<DesignKey> DesignKeys
        {
            get { return designKeys; }
            set { designKeys = value; RaisePropertyChanged(nameof(DesignKeys)); }
        }

        List<DesignKey> columnKeys;
        public List<DesignKey> ColumnKeys
        {
            get { return columnKeys; }
            set { columnKeys = value; RaisePropertyChanged(nameof(ColumnKeys)); }
        }

        Model3D interactionDiagrams;
        public Model3D InteractionDiagrams
        {
            get { return interactionDiagrams; }
            set { interactionDiagrams = value; RaisePropertyChanged(nameof(InteractionDiagrams)); }
        }

        BatchColumnDesign batchDesign = new BatchColumnDesign();
        public BatchColumnDesign BatchDesign
        {
            get { return batchDesign; }
            set { batchDesign = value; RaisePropertyChanged(nameof(BatchDesign)); }
        }

        ObservableCollection<Design> designs = new ObservableCollection<Design>();
        public ObservableCollection<Design> Designs
        {
            get { return designs; }
            set { designs = value; RaisePropertyChanged(nameof(Designs)); }
        }

        int progressPercentageMain;
        public int ProgressPercentageMain
        {
            get { return progressPercentageMain; }
            set { progressPercentageMain = value; RaisePropertyChanged(nameof(ProgressPercentageMain)); }
        }

        int progressPercentage;
        public int ProgressPercentage
        {
            get { return progressPercentage; }
            set { progressPercentage = value; RaisePropertyChanged(nameof(ProgressPercentage)); }
        }

        Performance currentPerformance;
        public Performance CurrentPerformance
        { 
            get { return currentPerformance; }
            set { currentPerformance = value; RaisePropertyChanged(nameof(CurrentPerformance)); }
        }

        Performance optimiserPerformance;
        public Performance OptimiserPerformance
        {
            get { return optimiserPerformance; }
            set { optimiserPerformance = value; RaisePropertyChanged(nameof(OptimiserPerformance)); }
        }

        public List<string> ClusteringMethods { get; set; } = Enum.GetNames(typeof(ClusteringMethod)).ToList();

        string method = ClusteringMethod.KMeans.ToString();
        public string Method
        {
            get { return method; }
            set { method = value; RaisePropertyChanged(nameof(Method)); }
        }

        string optiMessage;
        public string OptiMessage
        {
            get { return optiMessage; }
            set { optiMessage = value; RaisePropertyChanged(nameof(OptiMessage)); }
        }

        int numberOfColumns;
        public int NumberOfColumns
        {
            get { return numberOfColumns; }
            set { numberOfColumns = value; RaisePropertyChanged(nameof(NumberOfColumns)); }
        }

        int numberOfColumns_Designs;
        public int NumberOfColumns_Designs
        {
            get { return numberOfColumns_Designs; }
            set { numberOfColumns_Designs = value; RaisePropertyChanged(nameof(NumberOfColumns_Designs)); }
        }


        public int ActiveCluster = -1;

        public List<Color> myColors = new List<Color>
        {
            Color.FromArgb(255, 255, 50, 50),
            Color.FromArgb(255, 50, 255, 50),
            Color.FromArgb(255, 50, 50, 255),
            Color.FromArgb(255, 255, 255, 50),
            Color.FromArgb(255, 50, 255, 255),
            Color.FromArgb(255, 255, 50, 255),
            Color.FromArgb(255, 255, 128, 0),
            Color.FromArgb(255, 255, 0, 128),
            Color.FromArgb(255, 0, 128, 255),
            Color.FromArgb(255, 128, 0, 255),
            Color.FromArgb(255, 128, 255, 0),
            Color.FromArgb(255, 128, 128, 0),
            Color.FromArgb(255, 128, 0, 128),
            Color.FromArgb(255, 0, 128, 128),

        };

        // CONSTRUCTOR
        public BatchDesignView()
        {
            //InitializeColors();
        }

        // METHODS
        public async Task ColumnDesignOpti_Async()
        {
            Progress<TaskAsyncProgress> progress = new Progress<TaskAsyncProgress>(ReportProgress);
            await Task.Run(() => Optimiser.Optimise_Simulated_Annealing_Async(progress));
        }

        void ReportProgress(TaskAsyncProgress value)
        {
            List<Color> colors = GetColors();
            if (value.Updated && (value.Col.CapacityCheck ?? false))
            {
                DisplayInteractionDiagram(value.Col, colors[Optimiser.Index].ChangeAlpha(50));
                designs[Optimiser.Index].Col = value.Col.Clone();
                designs[Optimiser.Index].Status = DesignStatus.DesignFound;
                GetOptimiserPerformance();
            }
            ProgressPercentage = value.ProgressPercentage;
            designs[Optimiser.Index].Progress = value.ProgressPercentage;
            RaisePropertyChanged(nameof(Designs));
        }
        
        public void DisplayDesignClusters(bool updateKeys = true, bool generate = true)
        {
            if(generate) BatchDesign.GetDesignClusters();
            var modelGroup = new Model3DGroup();
            List<List<Column>> designClusters = BatchDesign.GetDesignClustersCol();

            for (int i = 0; i < designClusters.Count; i++)
            {
                MeshBuilder meshBuilder = new MeshBuilder(false, true);
                List<Column> columns = designClusters[i];
                foreach (var c in columns)
                {
                    Point3D center = new Point3D(c.Point1.X * 1e-3, c.Point1.Y * 1e-3, c.Point1.Z * 1e-3);
                    double xlength = 0.2;
                    double ylength = 0.2;
                    double zlength = (c.Point2.Z - c.Point1.Z) * 1e-3;
                    meshBuilder.AddBox(center, xlength, ylength, zlength);
                }
                var mesh = new GeometryModel3D(meshBuilder.ToMesh(), MaterialHelper.CreateMaterial(myColors[i].ChangeAlpha(150)));
                mesh.BackMaterial = mesh.Material;
                modelGroup.Children.Add(mesh);
            }
            ClusteredDesigns = modelGroup;

            if (updateKeys) DisplayDesignClustersKeys();
            NumberOfColumns_Designs = designClusters.Sum(c => c.Count);
        }

        public void DisplayOneDesignCluster(int n)
        {
            //BatchDesign.GetDesignClusters();
            var modelGroup = new Model3DGroup();
            List<List<Column>> designClusters = BatchDesign.GetDesignClustersCol();
            for (int i = 0; i < designClusters.Count; i++)
            {
                MeshBuilder meshBuilder = new MeshBuilder(false, true);
                List<Column> columns = designClusters[i];
                foreach (var c in columns)
                {
                    Point3D center = new Point3D(c.Point1.X * 1e-3, c.Point1.Y * 1e-3, c.Point1.Z * 1e-3);
                    double xlength = 0.2;
                    double ylength = 0.2;
                    double zlength = (c.Point2.Z - c.Point1.Z) * 1e-3;
                    meshBuilder.AddBox(center, xlength, ylength, zlength);
                }
                Color col = i == n ? myColors[i].ChangeAlpha(150) : myColors[i].ChangeAlpha(20);
                var mesh = new GeometryModel3D(meshBuilder.ToMesh(), MaterialHelper.CreateMaterial(col));
                mesh.BackMaterial = mesh.Material;
                modelGroup.Children.Add(mesh);
            }
            ClusteredDesigns = modelGroup;
            NumberOfColumns_Designs = designClusters[n].Count;
        }

        public void DisplayDesignClustersKeys()
        {
            // update keys
            designKeys = new List<DesignKey>();
            List<List<Column>> designClusters = BatchDesign.GetDesignClustersCol();
            for (int i = 0; i < designClusters.Count; i++)
            {
                string name = "Unnamed";
                if (designClusters[i][0].LX != 0)
                    name = designClusters[i][0].LX + " x " + designClusters[i][0].LY;
                else
                    name = "D" + designClusters[i][0].Diameter;
                designKeys.Add(new DesignKey()
                {
                    Label = name,
                    Color = new SolidColorBrush(myColors[i]),
                    Index = i,
                });
            }
            RaisePropertyChanged(nameof(DesignKeys));
        }

        public void DisplayColumnClustersKeys()
        {
            List<Color> colors = GetColors();
            // update keys
            columnKeys = new List<DesignKey>();
            List<Cluster> clusters = BatchDesign.LoadClusters;
            for (int i = 0; i < clusters.Count; i++)
            {
                columnKeys.Add(new DesignKey()
                {
                    Label = clusters[i].Name,
                    Color = new SolidColorBrush(colors[i]),
                    Index = i,
                });
            }
            RaisePropertyChanged(nameof(ColumnKeys));
        }

        public void DisplayAllColumnsClusters(bool updateKeys = true)
        {
            var modelGroup = new Model3DGroup();
            List<Cluster> loadClusters = BatchDesign.LoadClusters;
            List<Color> colors = GetColors();
            for (int i = 0; i < loadClusters.Count; i++)
            {
                MeshBuilder meshBuilder = new MeshBuilder(false, true);
                List<Column> columns = loadClusters[i].Loads.Select(p => batchDesign.GetParentColumn(p)).Distinct().ToList();
                foreach (var c in columns)
                {
                    Point3D center = new Point3D(c.Point1.X * 1e-3, c.Point1.Y * 1e-3, c.Point1.Z * 1e-3);
                    double xlength = 0.2;
                    double ylength = 0.2;
                    double zlength = (c.Point2.Z - c.Point1.Z) * 1e-3;
                    meshBuilder.AddBox(center, xlength, ylength, zlength);
                }
                var mesh = new GeometryModel3D(meshBuilder.ToMesh(), MaterialHelper.CreateMaterial(colors[i].ChangeAlpha(150)));
                mesh.BackMaterial = mesh.Material;
                modelGroup.Children.Add(mesh);
            }
            ClusteredColumns = modelGroup;

            if (updateKeys) DisplayColumnClustersKeys();
            NumberOfColumns = loadClusters.SelectMany(c => c.Loads.Select(l => batchDesign.GetParentColumn(l))).Distinct().Count();
        }

        

        public void DisplayOneColumnsCluster(int n)
        {
            var modelGroup = new Model3DGroup();
            List<Cluster> loadClusters = BatchDesign.LoadClusters;
            List<Color> colors = GetColors();
            for (int i = 0; i < loadClusters.Count; i++)
            {
                MeshBuilder meshBuilder = new MeshBuilder(false, true);
                List<Column> columns = loadClusters[i].Loads.Select(p => batchDesign.GetParentColumn(p)).Distinct().ToList();
                foreach (var c in columns)
                {
                    Point3D center = new Point3D(c.Point1.X * 1e-3, c.Point1.Y * 1e-3, c.Point1.Z * 1e-3);
                    double xlength = 0.2;
                    double ylength = 0.2;
                    double zlength = (c.Point2.Z - c.Point1.Z) * 1e-3;
                    meshBuilder.AddBox(center, xlength, ylength, zlength);
                }
                Color color = i == n ? colors[i].ChangeAlpha(150) : colors[i].ChangeAlpha(20);
                var mesh = new GeometryModel3D(meshBuilder.ToMesh(), MaterialHelper.CreateMaterial(color));
                mesh.BackMaterial = mesh.Material;
                modelGroup.Children.Add(mesh);
            }

            ClusteredColumns = modelGroup;
            NumberOfColumns = loadClusters[n].Loads.Select(l => batchDesign.GetParentColumn(l)).Distinct().Count();
        }

        public void DisplayAllClusters(bool compute = true, bool updateKeys = true)
        {
            var modelGroup = new Model3DGroup();
            if(compute) BatchDesign.GenerateLoadsClustering();
            List<List<MWPoint3D>> clusters = BatchDesign.Clusters;

            List<Color> colors = GetColors();

            for (int i = 0; i < clusters.Count; i++)
            {
                var pointMesh = new MeshBuilder(false, true);
                foreach (var p in clusters[i])
                {
                    Point3D center = new Point3D(10 * p.X / batchDesign.MaxMx, 10 * p.Y / batchDesign.MaxMy, 10 * p.Z / batchDesign.MaxP);
                    pointMesh.AddSphere(center, radius: 0.05, thetaDiv: 8, phiDiv: 4);
                }
                var mesh0 = new GeometryModel3D(pointMesh.ToMesh(), MaterialHelper.CreateMaterial(colors[i]));
                mesh0.BackMaterial = mesh0.Material;
                modelGroup.Children.Add(mesh0);
            }

            LoadsCloud = modelGroup;
            DisplayAllColumnsClusters(updateKeys);
        }

        public void DisplayOneCluster(int n)
        {
            var modelGroup = new Model3DGroup();
            List<List<MWPoint3D>> clusters = BatchDesign.Clusters;
            Console.WriteLine("Displaying cluster number {0}", n);
            List<Color> colors = GetColors();
            for (int i = 0; i < clusters.Count; i++)
            {
                var pointMesh = new MeshBuilder(false, true);
                foreach (var p in clusters[i])
                {
                    Point3D center = new Point3D(10 * p.X / batchDesign.MaxMx, 10 * p.Y / batchDesign.MaxMy, 10 * p.Z / batchDesign.MaxP);
                    pointMesh.AddSphere(center, radius: 0.05, thetaDiv: 8, phiDiv: 4);
                }
                Color color = i == n ? colors[i] : colors[i].ChangeAlpha(20);
                var mesh0 = new GeometryModel3D(pointMesh.ToMesh(), MaterialHelper.CreateMaterial(color));
                mesh0.BackMaterial = mesh0.Material;
                modelGroup.Children.Add(mesh0);
            }

            LoadsCloud = modelGroup;
            DisplayOneColumnsCluster(n);
        }

        public List<Color> GetColors()
        {
            if (batchDesign.Type == ClusteringType.Local)
            {
                List<Color> colors = new List<Color>();
                for (int i = 0; i < myColors.Count; i++)
                {
                    for (int j = 0; j < batchDesign.NClusters; j++)
                    {
                        double factor = batchDesign.NClusters > 1 ? factor = 0.6 + j * (1.0 - 0.6) / (batchDesign.NClusters - 1) : 1;
                        colors.Add(myColors[i].ChangeIntensity(factor));
                    }
                }
                return colors;
            }
            else if (batchDesign.Type == ClusteringType.Global)
                return myColors;
            return null;
        }

        public void DisplayInteractionDiagrams(List<Column> columns)
        {
            var modelGroup = new Model3DGroup();
            List<Color> colors = GetColors();
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].diagramVertices.Count == 0) continue;
                var mesh = GenerateInteractionDiagram(columns[i], colors[i].ChangeAlpha(50));
                modelGroup.Children.Add(mesh);
            }
            InteractionDiagrams = modelGroup;
            RaisePropertyChanged(nameof(InteractionDiagrams));
        }

        public void DisplayInteractionDiagram(Column column, Color color)
        {
            if (column.diagramVertices.Count == 0) return;
            var modelGroup = new Model3DGroup();
            var mesh = GenerateInteractionDiagram(column, color);
            modelGroup.Children.Add(mesh);
            InteractionDiagrams = modelGroup;
            RaisePropertyChanged(nameof(InteractionDiagrams));
        }

        public GeometryModel3D GenerateInteractionDiagram(Column column, Color color)
        {
            double scaleXYZ = 10;

            Console.WriteLine("MaxMX = {0}, MaxMy = {1}, MaxP = {2}", batchDesign.MaxMx, batchDesign.MaxMy, batchDesign.MaxP);
            List<Point3D> normalPoints = column.diagramVertices.Select(x => new Point3D(x.X / BatchDesign.MaxMx, x.Y / BatchDesign.MaxMy, -x.Z / BatchDesign.MaxP)).ToList();
            normalPoints = normalPoints.Select(x => new Point3D(scaleXYZ * x.X, scaleXYZ * x.Y, scaleXYZ * x.Z)).ToList();

            var meshBuilder = new MeshBuilder(false, true);

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
            var mesh = new GeometryModel3D(meshBuilder.ToMesh(), MaterialHelper.CreateMaterial(color));
            mesh.BackMaterial = mesh.Material;

            return mesh;
        }

        /// <summary>
        /// Optimisation when column sizes are kept as original or when completely released
        /// </summary>
        public async void OptimiseClusterDesigns()
        {
            //for(int i = 0; i < NClusters; i++)
            if(BatchDesign.Designs == null ? true : BatchDesign.Designs.Count == 0)
            {
                BatchDesign.Designs = new List<Column>();
                int ncg = Optimiser.ConcreteGrades.Count - 1;
                designs = new ObservableCollection<Design>();
                for (int i = 0; i < BatchDesign.NClustersTot; i++)
                {
                    designs.Add(new Design() 
                    { 
                        Col = new Column()
                        {
                            Name = BatchDesign.LoadClusters[i].Name,
                            LX = 200,
                            LY = 200,
                            Diameter = 200,
                            NRebarX = 2,
                            NRebarY = 2,
                            NRebarCirc = 3,
                            ConcreteGrade = Optimiser.ConcreteGrades[ncg],
                            Loads = new List<Load>()
                        },
                        Status = DesignStatus.NotDesigned
                    });
                }
            }

            for (int i = 0; i < BatchDesign.NClustersTot; i++)
            {
                if (BatchDesign.LoadClusters[i].Loads.Count == 0) continue;
                DisplayOneCluster(i);
                ActiveCluster = i;
                InteractionDiagrams = new Model3DGroup();
                Column c = batchDesign.GetParentColumn(BatchDesign.LoadClusters[i].Loads[0]);
                Column ClusterCol = new Column()
                {
                    Name = BatchDesign.LoadClusters[i].Name,
                    LX = c.LX,
                    LY = c.LY,
                    Diameter = c.Diameter,
                    Shape = c.Shape,
                    Loads = new List<Load>(),
                    ConcreteGrade = Optimiser.ConcreteGrades[0],
                    SteelGrade = Optimiser.SteelGrades[0],
                    NRebarX = 7, //Convert.ToInt32(Mins[2]),
                    NRebarY = 7, //Convert.ToInt32(Mins[3]),
                    NRebarCirc = 8,
                    //Diameter = 20, //Convert.ToDouble(Optimiser.Mins[4]),
                    //NRebarCirc = Convert.ToInt32(Optimiser.Mins[5]),
                    //Radius = Convert.ToDouble(Optimiser.Mins[6]),
                    //Edges = Convert.ToInt32(Optimiser.Mins[7]),
                    BarDiameter = Convert.ToInt32(Optimiser.Mins[8]),
                    LinkDiameter = Convert.ToInt32(Optimiser.Mins[9]),
                    AllLoads = true
                };
                ClusterCol.Loads = BatchDesign.LoadClusters[i].Loads.Select(l => new Load()
                {
                    MEdx = l.Load.X,
                    MEdy = l.Load.Y,
                    P = l.Load.Z
                }).ToList();
                Optimiser.Index = i;

                // Initialization
                Column col = ClusterCol.Clone();
                Optimiser.column = col;
                Optimiser.Initialize();

                int k = 1;
                int maxAttemps = 10;
                designs[i].Status = DesignStatus.InProgress;
                while (designs[i].Status == DesignStatus.InProgress && k <= maxAttemps)
                {
                    OptiMessage = BatchDesign.LoadClusters[i].Name + " - run " + k;
                    await ColumnDesignOpti_Async();
                    k++;
                }
                if(k == maxAttemps + 1)
                {
                    designs[i].Status = DesignStatus.NoDesignFound;
                }
                else
                {
                    designs[i].Status = DesignStatus.Designed;
                    BatchDesign.Designs.Add(designs[i].Col.Clone());
                }
                ProgressPercentageMain = Convert.ToInt32( ( i + 1 ) * 1.0 / ( BatchDesign.NClustersTot ) * 100);
            }

            ActiveCluster = -1;
            DisplayAllClusters(false);
            InteractionDiagrams = new Model3DGroup(); // removes last interaction diagram
            //DisplayInteractionDiagrams(Designs.ToList());
        }

        /// <summary>
        /// Optimisation when column sizes to be taken from the predefined list
        /// </summary>
        public async void OptimiseClusterDesignsDefinedSizes()
        {
            //for(int i = 0; i < NClusters; i++)
            if (BatchDesign.Designs == null ? true : BatchDesign.Designs.Count == 0)
            {
                BatchDesign.Designs = new List<Column>();
                int ncg = Optimiser.ConcreteGrades.Count - 1;
                designs = new ObservableCollection<Design>();
                for (int i = 0; i < BatchDesign.NClusters; i++)
                    designs.Add(new Design()
                    {
                        Col = new Column()
                        {
                            LX = 800,
                            LY = 800,
                            NRebarX = 5,
                            NRebarY = 5,
                            ConcreteGrade = Optimiser.ConcreteGrades[ncg]
                        },
                        Status = DesignStatus.NotDesigned
                    });
            }

            for (int i = 0; i < BatchDesign.NClusters; i++)
            {
                DisplayOneCluster(i);
                InteractionDiagrams = new Model3DGroup();
                Column ClusterCol = new Column()
                {
                    Name = "Design " + i,
                    Loads = new List<Load>(),
                    ConcreteGrade = Optimiser.ConcreteGrades[0],
                    SteelGrade = Optimiser.SteelGrades[0],
                    LX = 800, //Convert.ToInt32(Mins[0]),
                    LY = 800, //Convert.ToInt32(Mins[1]),
                    NRebarX = 9, //Convert.ToInt32(Mins[2]),
                    NRebarY = 9, //Convert.ToInt32(Mins[3]),
                    Diameter = 400, //Convert.ToDouble(Optimiser.Mins[4]),
                    NRebarCirc = Convert.ToInt32(Optimiser.Mins[5]),
                    Radius = Convert.ToDouble(Optimiser.Mins[6]),
                    Edges = Convert.ToInt32(Optimiser.Mins[7]),
                    BarDiameter = Convert.ToInt32(Optimiser.Mins[8]),
                    LinkDiameter = Convert.ToInt32(Optimiser.Mins[9]),
                    AllLoads = true
                };
                ClusterCol.Loads = BatchDesign.LoadClusters[i].Loads.Select(l => new Load()
                {
                    MEdx = l.Load.X,
                    MEdy = l.Load.Y,
                    P = l.Load.Z
                }).ToList();
                Optimiser.Index = i;

                // Initialization
                Column col = ClusterCol.Clone();
                col.LX = Optimiser.Sizes[0].Item1;
                col.LY = Optimiser.Sizes[0].Item2;
                Optimiser.column = col;
                Optimiser.Initialize();

                List<Column> clusterDesigns = new List<Column>();
                for (int j = 0; j < Optimiser.Sizes.Count; j++)
                {
                    OptiMessage = Optimiser.Sizes[j].Item1 + " x " + Optimiser.Sizes[j].Item2;
                    ProgressPercentageMain = Convert.ToInt32((i * Optimiser.Sizes.Count + j) * 1.0 / (BatchDesign.NClusters * Optimiser.Sizes.Count) * 100);
                    ClusterCol.LX = Optimiser.Sizes[j].Item1;
                    ClusterCol.LY = Optimiser.Sizes[j].Item2;
                    Optimiser.column = ClusterCol;
                    await ColumnDesignOpti_Async();
                    clusterDesigns.Add(ClusterCol.Clone());
                }

                var bestCol = clusterDesigns.Aggregate(clusterDesigns[0], (best, next) => next.Cost < best.Cost ? next : best);
                BatchDesign.Designs.Add(bestCol);
            }

            DisplayAllClusters(false);
            //DisplayInteractionDiagrams(Designs.ToList());
        }

        public void GetCurrentPerformance()
        {
            
            double carb = 0;
            double vol = 0;
            foreach(var c in batchDesign.Columns)
            {
                Calculations calc = new Calculations(c);
                carb += calc.GetEmbodiedCarbon()[2] * 1e-3; //  in TCO2e
                vol += c.ConcreteVol() * 1e-9; //  in m3
            }
            CurrentPerformance = new Performance()
            {
                Carbon = carb,
                ConcreteVolume = vol
            };
        }

        public void GetOptimiserPerformance()
        {
            
            double carb = 0;
            double vol = 0;
            List<List<Column>> optiCols = batchDesign.LoadClusters.Select(l => l.Loads.Select(lc => batchDesign.GetParentColumn(lc)).Distinct().Select(c => c.Clone()).ToList()).ToList();
            int tot = optiCols.SelectMany(l => l.Select(c => c).ToList()).ToList().Count;
            Console.WriteLine("num tot columns : {0}", tot);
            for(int i = 0; i < optiCols.Count; i++)
            {
                if (optiCols[i].Count == 0) continue;
                optiCols[i].ForEach(c =>
                {
                    c.LX = designs[i].Col.LX;
                    c.LY = designs[i].Col.LY;
                    c.NRebarX = designs[i].Col.NRebarX;
                    c.NRebarY = designs[i].Col.NRebarY;
                    c.BarDiameter = designs[i].Col.BarDiameter;
                    c.ConcreteGrade = designs[i].Col.ConcreteGrade;
                });
                foreach(var c in optiCols[i])
                {
                    Calculations calc = new Calculations(c);
                    carb += calc.GetEmbodiedCarbon()[2] * 1e-3; //  in TCO2e
                    vol += c.ConcreteVol() * 1e-9; //  in m3
                }
                Calculations calc0 = new Calculations(optiCols[i][0]);
                Console.WriteLine("Design {0} carbon : {1} kgCO2", i, calc0.GetEmbodiedCarbon()[2]);
            }
            OptimiserPerformance = new Performance()
            {
                Carbon = carb,
                ConcreteVolume = vol
            };
        }
    }

    public class DesignKey
    {
        public string Label { get; set; }
        public SolidColorBrush Color { get; set; }
        public int Index { get; set; }
    }

    public class Performance
    {
        public double Carbon { get; set; }
        public double ConcreteVolume { get; set; }
    }

    public enum DesignStatus { Designed, InProgress, NotDesigned, DesignFound, NoDesignFound }
    public class Design : ViewModelBase
    {
        Column col;
        public Column Col
        {
            get { return col; }
            set { col = value; RaisePropertyChanged(nameof(Col)); }
        }

        DesignStatus status = DesignStatus.NotDesigned;
        public DesignStatus Status
        {
            get { return status; }
            set { status = value; RaisePropertyChanged(nameof(Status)); }
        }

        int progress = 0;
        public int Progress
        {
            get { return progress; }
            set { progress = value; RaisePropertyChanged(nameof(Progress)); }
        }
    }
}
