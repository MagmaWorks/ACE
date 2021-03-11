using BatchDesign;
using ColumnDesignCalc;
using HelixToolkit.Wpf;
using MWGeometry;
using Optimisation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ColumnDesign
{
    /// <summary>
    /// Interaction logic for UCBatchDesign.xaml
    /// </summary>
    public partial class UCBatchDesign : UserControl
    {
        public UCBatchDesign()
        {
            InitializeComponent();
        }

        private void RunBatchDesign(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            if (vm.MyBatchDesignView.BatchDesign.Designs?.Count > 0)
            {
                MessageBoxResult res = MessageBox.Show("Delete existing designs and restart optimisation?", "Restart optimisation", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                    return;
                else
                    vm.MyBatchDesignView.BatchDesign.Designs = new List<Column>();
            }

            DesignOptimiser opti = vm.MyBatchDesignView.Optimiser;
            //BatchColumnDesign batchDesign = vm.MyBatchDesignView.BatchDesign;



            opti.SteelGrades = vm.ColumnCalcs.SteelGrades;

            bool[] Shapes = new bool[]
            {
                true, //RectangularCB.IsChecked ?? false,
                false, //CircularCB.IsChecked ?? false,
                false, //PolygonalCB.IsChecked ?? false,
            };

            // Defines which parameters are optimised and which are not
            bool[] Activations = new bool[]
            {
                DomainCB.IsChecked ?? false, //RectangularCB.IsChecked ?? false, //LXCB.IsChecked ?? false,
                DomainCB.IsChecked ?? false, //RectangularCB.IsChecked ?? false, //LYCB.IsChecked ?? false,
                true, //RectangularCB.IsChecked ?? false, //NXCB.IsChecked ?? false,
                true, //RectangularCB.IsChecked ?? false, //NYCB.IsChecked ?? false,
                false, //CircularCB.IsChecked ?? false, //DCB.IsChecked ?? false,
                true, //CircularCB.IsChecked ?? false, //NCircCB.IsChecked ?? false,
                false, //PolygonalCB.IsChecked ?? false, //RadiusCB.IsChecked ?? false,
                false, //PolygonalCB.IsChecked ?? false, //EdgesCB.IsChecked ?? false,
                true, //BarDCB.IsChecked ?? false,
                false, //LinkDCB.IsChecked ?? false,
                true, //CGCB.IsChecked ?? false
            };

            string[] Mins = new string[]
            {
                MinXDim?.Text ?? "",
                MinYDim?.Text ?? "",
                MinNX?.Text?.ToString() ?? "",
                MinNY?.Text?.ToString() ?? "",
                "", //MinD?.Text ?? "",
                "8", //MinNCirc?.Text?.ToString() ?? "",
                "", //MinRadius?.Text ?? "",
                "", //MinEdges?.Text ?? "",
                MinBarDiameter?.Text?.ToString() ?? "",
                "10", //MinLinkDiameter?.Text?.ToString() ?? "",
                MinCG?.Text?.ToString() ?? "",
            };

            string[] Maxs = new string[]
            {
                MaxXDim?.Text ?? "",
                MaxYDim?.Text ?? "",
                MaxNX?.Text?.ToString() ?? "",
                MaxNY?.Text?.ToString() ?? "",
                "", //MaxD?.Text ?? "",
                "12", //MaxNCirc?.Text?.ToString() ?? "",
                "", //MaxRadius?.Text ?? "",
                "", //MaxEdges?.Text ?? "",
                MaxBarDiameter?.Text?.ToString() ?? "",
                "10", //MaxLinkDiameter?.Text?.ToString() ?? "",
                MaxCG.Text?.ToString() ?? "",
            };

            bool error = false;

            for (int i = 0; i < Activations.Length; i++)
            {
                if (Activations[i] && (Mins[i] == "" || Maxs[i] == ""))
                    error = true;
            }
            if (Activations[0] && Convert.ToDouble(Mins[0]) > Convert.ToDouble(Maxs[0])) error = true;
            if (Activations[1] && Convert.ToDouble(Mins[1]) > Convert.ToDouble(Maxs[1])) error = true;
            if (Activations[2] && Convert.ToInt32(Mins[2]) > Convert.ToInt32(Maxs[2])) error = true;
            if (Activations[3] && Convert.ToInt32(Mins[3]) > Convert.ToInt32(Maxs[3])) error = true;
            if (Activations[4] && Convert.ToDouble(Mins[4]) > Convert.ToDouble(Maxs[4])) error = true;
            if (Activations[5] && Convert.ToInt32(Mins[5]) > Convert.ToInt32(Maxs[5])) error = true;
            if (Activations[6] && Convert.ToDouble(Mins[6]) > Convert.ToDouble(Maxs[6])) error = true;
            if (Activations[7] && Convert.ToInt32(Mins[7]) > Convert.ToInt32(Maxs[7])) error = true;
            if (Activations[8] && Convert.ToInt32(Mins[8]) > Convert.ToInt32(Maxs[8])) error = true;
            if (Activations[9] && Convert.ToInt32(Mins[9]) > Convert.ToInt32(Maxs[9])) error = true;
            if (Activations[10] && Convert.ToInt32(Mins[10].Substring(0, 2)) > Convert.ToInt32(Maxs[10].Substring(0, 2))) error = true;
            if (!(CostCB.IsChecked ?? false) && !(CarbonCB.IsChecked ?? false)) error = true;

            if (!error)
            {
                opti.Shapes = Shapes;
                opti.Activations = Activations;
                opti.Mins = Mins;
                opti.Maxs = Maxs;
                opti.Maxiter = Convert.ToInt32(MaxIter.Text);
                int i0 = vm.ColumnCalcs.ConcreteGrades.IndexOf(vm.ColumnCalcs.ConcreteGrades.First(c => c.Name == MinCG.Text));
                int i1 = vm.ColumnCalcs.ConcreteGrades.IndexOf(vm.ColumnCalcs.ConcreteGrades.First(c => c.Name == MaxCG.Text));
                opti.ConcreteGrades = new List<Concrete>();
                for (int i = i0; i <= i1; i++)
                    opti.ConcreteGrades.Add(vm.ColumnCalcs.ConcreteGrades[i]);

                i0 = vm.BarDiameters.IndexOf(Convert.ToInt32(MinBarDiameter.Text));
                i1 = vm.BarDiameters.IndexOf(Convert.ToInt32(MaxBarDiameter.Text));
                opti.BarDiameters = new List<int>();
                for (int i = i0; i <= i1; i++)
                    opti.BarDiameters.Add(vm.BarDiameters[i]);

                //i0 = vm.LinkDiameters.IndexOf(Convert.ToInt32(MinLinkDiameter.Text));
                //i1 = vm.LinkDiameters.IndexOf(Convert.ToInt32(MaxLinkDiameter.Text));
                //opti.LinkDiameters = new List<int>();
                //for (int i = i0; i <= i1; i++)
                //    opti.LinkDiameters.Add(vm.LinkDiameters[i]);

                opti.DriversWeight = new double[2]
                {
                    CostCB.IsChecked ?? false ? 1 : 0,
                    CarbonCB.IsChecked ?? false ? 1 : 0
                };

                opti.Drivers = new double[2]
                {
                    Convert.ToDouble(CostWeightTB.Text),
                    Convert.ToDouble(CarbonWeightTB.Text)
                };

                opti.Alpha = Convert.ToDouble(Alpha.Text);
                opti.Variance = Convert.ToDouble(Variance.Text);

                opti.Incrs = new string[]
                {
                    IncrXDim.Text,
                    IncrYDim.Text,
                    "", //IncrD.Text,
                    "", //IncrRadius.Text
                };

                if(LocalClusteringCB.IsChecked ?? false)
                {
                    vm.MyBatchDesignView.OptimiseClusterDesigns();
                }
                else if (GlobalClusteringCB.IsChecked ?? false)
                {
                    if (ListCB.IsChecked ?? false)
                    {
                        opti.Sizes = GetSizes();
                        vm.MyBatchDesignView.OptimiseClusterDesignsDefinedSizes();
                    }
                    else if (DomainCB.IsChecked ?? false)
                    {
                        vm.MyBatchDesignView.OptimiseClusterDesigns();
                    }
                }
                
            }
        }

        private List<Tuple<double,double>> GetSizes()
        {
            string text = ColumnSizesTB.Text;
            List<Tuple<double, double>> res = new List<Tuple<double, double>>();
            string[] sizesStr = text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach(var s in sizesStr)
            {
                var s0 = s.Replace(" ", "");
                string[] dims = s0.Split('x', 'X');
                double dim1 = Convert.ToDouble(dims[0]);
                double dim2 = Convert.ToDouble(dims[1]);
                res.Add(new Tuple<double, double>(Math.Min(dim1, dim2), Math.Max(dim1, dim2)));
            }
            return res;
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            var myWindow = Window.GetWindow(this);
            myWindow.Close();
        }
        #region Checkboxes
        //private void LXCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    bool val = LXCB.IsChecked ?? false;
        //    MinXDim.IsEnabled = val;
        //    MaxXDim.IsEnabled = val;
        //    IncrXDim.IsEnabled = val;
        //    SquareCB.IsEnabled = val && (LYCB.IsChecked ?? false);
        //    MinYDim.IsEnabled = !val && (LYCB.IsChecked ?? false);
        //    MaxYDim.IsEnabled = !val && (LYCB.IsChecked ?? false);
        //    IncrYDim.IsEnabled = !val && (LYCB.IsChecked ?? false);
        //}

        //private void LYCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    bool val = LYCB.IsChecked ?? false;
        //    MinYDim.IsEnabled = val && ((LXCB.IsChecked ?? false) ? !(SquareCB.IsChecked ?? false) : true);
        //    MaxYDim.IsEnabled = val && ((LXCB.IsChecked ?? false) ? !(SquareCB.IsChecked ?? false) : true);
        //    IncrYDim.IsEnabled = val && ((LXCB.IsChecked ?? false) ? !(SquareCB.IsChecked ?? false) : true);
        //    SquareCB.IsEnabled = val && (LXCB.IsChecked ?? false);
        //}

        //private void NXCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    bool val = NXCB.IsChecked ?? false;
        //    MinNX.IsEnabled = val;
        //    MaxNX.IsEnabled = val;
        //}

        //private void NYCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    bool val = NYCB.IsChecked ?? false;
        //    MinNY.IsEnabled = val;
        //    MaxNY.IsEnabled = val;
        //}

        //private void BarCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    bool val = BarDCB.IsChecked ?? false;
        //    MinBarDiameter.IsEnabled = val;
        //    MaxBarDiameter.IsEnabled = val;
        //}

        //private void LinkCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    bool val = LinkDCB.IsChecked ?? false;
        //    MinLinkDiameter.IsEnabled = val;
        //    MaxLinkDiameter.IsEnabled = val;
        //}

        //private void CGCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    MinCG.IsEnabled = (sender as CheckBox).IsChecked ?? false;
        //    MaxCG.IsEnabled = (sender as CheckBox).IsChecked ?? false;
        //}

        //private void SquareCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    MinYDim.IsEnabled = !(sender as CheckBox).IsChecked ?? false;
        //    MaxYDim.IsEnabled = !(sender as CheckBox).IsChecked ?? false;
        //    IncrYDim.IsEnabled = !(sender as CheckBox).IsChecked ?? false;
        //}

        //private void DCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    bool val = DCB.IsChecked ?? false;
        //    MinD.IsEnabled = val;
        //    MaxD.IsEnabled = val;
        //    IncrD.IsEnabled = val;
        //}

        //private void NCircCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    bool val = NCircCB.IsChecked ?? false;
        //    MinNCirc.IsEnabled = val;
        //    MaxNCirc.IsEnabled = val;
        //}

        //private void RadiusCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    bool val = RadiusCB.IsChecked ?? false;
        //    MinRadius.IsEnabled = val;
        //    MaxRadius.IsEnabled = val;
        //}

        //private void EdgesCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    bool val = EdgesCB.IsChecked ?? false;
        //    MinEdges.IsEnabled = val;
        //    MaxEdges.IsEnabled = val;
        //}

        //private void RectangularCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    bool val = RectangularCB.IsChecked ?? false;
        //    LXCB.IsEnabled = val;
        //    LYCB.IsEnabled = val;
        //    NXCB.IsEnabled = val;
        //    NYCB.IsEnabled = val;
        //    if (!val)
        //    {
        //        MinXDim.IsEnabled = val;
        //        MaxXDim.IsEnabled = val;
        //        MinYDim.IsEnabled = val;
        //        MaxYDim.IsEnabled = val;
        //        IncrXDim.IsEnabled = val;
        //        IncrYDim.IsEnabled = val;
        //        MinNX.IsEnabled = val;
        //        MaxNX.IsEnabled = val;
        //        MinNY.IsEnabled = val;
        //        MaxNY.IsEnabled = val;
        //        //SquareCB.IsEnabled = val;
        //    }
        //    else
        //    {
        //        //LXCB_Checked(sender, e);
        //        //LYCB_Checked(sender, e);
        //        //NXCB_Checked(sender, e);
        //        //NYCB_Checked(sender, e);
        //    }
        //}

        //private void CircularCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    bool val = CircularCB.IsChecked ?? false;
        //    DCB.IsEnabled = val;
        //    NCircCB.IsEnabled = val;
        //    if (!val)
        //    {
        //        MinD.IsEnabled = val;
        //        MaxD.IsEnabled = val;
        //        IncrD.IsEnabled = val;
        //        MinNCirc.IsEnabled = val;
        //        MaxNCirc.IsEnabled = val;
        //    }
        //    else
        //    {
        //        //DCB_Checked(sender, e);
        //        //NCircCB_Checked(sender, e);
        //    }
        //}

        //private void PolygonalCB_Checked(object sender, RoutedEventArgs e)
        //{
        //    bool val = PolygonalCB.IsChecked ?? false;
        //    RadiusCB.IsEnabled = val;
        //    EdgesCB.IsEnabled = val;
        //    if (!val)
        //    {
        //        MinRadius.IsEnabled = val;
        //        MaxRadius.IsEnabled = val;
        //        MinEdges.IsEnabled = val;
        //        MaxEdges.IsEnabled = val;
        //    }
        //    else
        //    {
        //        //RadiusCB_Checked(sender, e);
        //        //EdgesCB_Checked(sender, e);
        //    }
        //}
        #endregion

        private void ShowLoadsCloud(object sender, RoutedEventArgs e)
        {
            UCLoadsCloud cloud = new UCLoadsCloud();
            StartBatchDesign();
            Window w = new Window()
            {
                Content = cloud,
                DataContext = this,
            };
            w.ShowDialog();
        }

        public void StartBatchDesign()
        {
            BatchDesignView view = (this.DataContext as ViewModel).MyBatchDesignView;
            
            view.BatchDesign.Type = (GlobalClusteringCB.IsChecked ?? false) ? ClusteringType.Global : ClusteringType.Local;
            view.DisplayDesignClusters();
            view.DisplayAllClusters();
            view.GetCurrentPerformance();
        }

        private void NClustersChanged(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            BatchColumnDesign BatchDesign = (this.DataContext as ViewModel).MyBatchDesignView.BatchDesign;
            try
            {
                int nc = Convert.ToInt32(tb.Text);
                if(nc != BatchDesign.NClusters)
                {
                    BatchDesign.NClusters = nc;
                    StartBatchDesign();
                }
            }
            catch { }
        }

        private void SigmaChanged(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            BatchColumnDesign BatchDesign = (this.DataContext as ViewModel).MyBatchDesignView.BatchDesign;
            try
            {
                double sig = Convert.ToInt32(tb.Text);
                if(sig != BatchDesign.Sigma)
                {
                    BatchDesign.Sigma = sig;
                    StartBatchDesign();
                }
            }
            catch { }
        }

        private void AddClusterDesigns(object sender, RoutedEventArgs e)
        {
            ViewModel vm = (this.DataContext as ViewModel);
            List<Column> cols = new List<Column>();
            for (int i = 0; i < vm.MyBatchDesignView.BatchDesign.Clusters.Count; i++)
            {
                Column col = vm.MyBatchDesignView.BatchDesign.Designs[i].Clone();
                col.Name = vm.MyBatchDesignView.BatchDesign.LoadClusters[i].Name;
                col.Loads = new List<Load>();
                col.IsCluster = true;
                col.ColsInCluster = new List<string>();
                for (int j = 0; j < vm.MyBatchDesignView.BatchDesign.LoadClusters[i].Loads.Count; j++)
                {
                    ClusterLoad cl = vm.MyBatchDesignView.BatchDesign.LoadClusters[i].Loads[j];
                    string name = cl.Name.Replace(cl.ParentColumn.Name, "").Replace(" - ", "");
                    Load l = cl.ParentColumn.Loads.First(m => m.Name == name).Clone();
                    l.Name = l.Name.Insert(0, cl.ParentColumn.Name + " - ");
                    col.Loads.Add(l);
                    col.ColsInCluster.Add(cl.ParentColumn.Name);
                }
                col.ColsInCluster = col.ColsInCluster.Distinct().ToList();
                MaxLoadOnX(col);
                col.SelectedLoad = col.Loads[0];
                col.AllLoads = true;
                cols.Add(col);
            }
            vm.MyColumns.AddRange(cols);

            MessageBox.Show("Cluster designs added to the current list of column designs.","",MessageBoxButton.OK);

        }

        private void ApplyClusterDesigns(object sender, RoutedEventArgs e)
        {
            ViewModel vm = (this.DataContext as ViewModel);
            List<Column> cols = new List<Column>();
            for (int i = 0; i < vm.MyBatchDesignView.BatchDesign.Clusters.Count; i++)
            {
                // look for all columns present in the cluster
                List<string> colsInCluster = new List<string>();
                for (int j = 0; j < vm.MyBatchDesignView.BatchDesign.LoadClusters[i].Loads.Count; j++)
                {
                    ClusterLoad cl = vm.MyBatchDesignView.BatchDesign.LoadClusters[i].Loads[j];
                    string name = cl.Name.Replace(cl.ParentColumn.Name, "").Replace(" - ", "");
                    Load l = cl.ParentColumn.Loads.First(m => m.Name == name).Clone();
                    l.Name = l.Name.Insert(0, cl.ParentColumn.Name + " - ");
                    colsInCluster.Add(cl.ParentColumn.Name);
                }
                colsInCluster = colsInCluster.Distinct().ToList();

                Column clusterCol = vm.MyBatchDesignView.BatchDesign.Designs[i];
                // assign the cluster design to all the columns
                for (int j = 0; j < colsInCluster.Count; j++)
                {
                    if (vm.MyColumns.Any(c => c.Name == colsInCluster[j]))
                    {
                        Column col = vm.MyColumns.Find(c => c.Name == colsInCluster[j]);
                        int idx = vm.MyColumns.IndexOf(col);
                        vm.MyColumns[idx].NRebarX = clusterCol.NRebarX;
                        vm.MyColumns[idx].NRebarY = clusterCol.NRebarY;
                        vm.MyColumns[idx].ConcreteGrade = clusterCol.ConcreteGrade;
                        vm.MyColumns[idx].LX = clusterCol.LX;
                        vm.MyColumns[idx].LY = clusterCol.LY;
                        vm.MyColumns[idx].BarDiameter = clusterCol.BarDiameter;
                    }
                }
            }

            MessageBox.Show("Cluster designs applied to corresponding columns in the current list of designs.", "", MessageBoxButton.OK);
        }

        private void MaxLoadOnX(Column c)
        {
            c.AllLoads = true;
            c.GetDesignMoments();
            foreach(var load in c.Loads)
            {
                if(load.MEdy > load.MEdx)
                {
                    double val = load.MxBot;
                    load.MxBot = load.MyBot;
                    load.MyBot = val;

                    val = load.MxTop;
                    load.MxTop = load.MyTop;
                    load.MyTop = val;
                }
            }
            c.Loads.ForEach(l => { l.MEdx = 0; l.MEdy = 0; });
        }

        private void ClusteringMethodChanged(object sender, RoutedEventArgs e)
        {
            string text = (sender as ComboBox).SelectedValue as string;
            ClusteringMethod m = (ClusteringMethod)Enum.Parse(typeof(ClusteringMethod), text);
            (this.DataContext as ViewModel).MyBatchDesignView.BatchDesign.Method = m;
            (this.DataContext as ViewModel).MyBatchDesignView.DisplayAllClusters();
        }

        public void Clustering_Clicked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            BatchDesignView view = (this.DataContext as ViewModel).MyBatchDesignView;
            if (cb.Name == "LocalClusteringCB")
            {
                view.BatchDesign.Type = ClusteringType.Local;
                if ((GlobalClusteringCB.IsChecked ?? false) && (LocalClusteringCB.IsChecked ?? false))
                {
                    view.DisplayAllClusters();
                }
                LocalClusteringCB.IsChecked = true;
                GlobalClusteringCB.IsChecked = false;
            }
            else if (cb.Name == "GlobalClusteringCB")
            {
                view.BatchDesign.Type = ClusteringType.Global;
                if ((GlobalClusteringCB.IsChecked ?? false) && (LocalClusteringCB.IsChecked ?? false))
                {
                    view.DisplayAllClusters();
                }
                GlobalClusteringCB.IsChecked = true;
                LocalClusteringCB.IsChecked = false;
            }

            bool b = GlobalClusteringCB.IsChecked ?? false;
            DomainCB.IsEnabled = b;
            MinXDim.IsEnabled = b;
            MinYDim.IsEnabled = b;
            MaxXDim.IsEnabled = b;
            MaxYDim.IsEnabled = b;
            IncrXDim.IsEnabled = b;
            IncrYDim.IsEnabled = b;
            ListCB.IsEnabled = b;
            ColumnSizesTB.IsEnabled = b;

            // update clustering
            
        }

    }
}
