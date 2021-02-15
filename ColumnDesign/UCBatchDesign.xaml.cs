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
            DesignOptimiser opti = vm.MyBatchDesignView.Optimiser;
            //BatchColumnDesign batchDesign = vm.MyBatchDesignView.BatchDesign;

            opti.SteelGrades = vm.ColumnCalcs.SteelGrades;

            bool[] Shapes = new bool[]
            {
                RectangularCB.IsChecked ?? false,
                CircularCB.IsChecked ?? false,
                PolygonalCB.IsChecked ?? false,
            };

            opti.Sizes = GetSizes();

            bool[] Activations = new bool[]
            {
                false, //RectangularCB.IsChecked ?? false, //LXCB.IsChecked ?? false,
                false, //RectangularCB.IsChecked ?? false, //LYCB.IsChecked ?? false,
                RectangularCB.IsChecked ?? false, //NXCB.IsChecked ?? false,
                RectangularCB.IsChecked ?? false, //NYCB.IsChecked ?? false,
                CircularCB.IsChecked ?? false, //DCB.IsChecked ?? false,
                CircularCB.IsChecked ?? false, //NCircCB.IsChecked ?? false,
                PolygonalCB.IsChecked ?? false, //RadiusCB.IsChecked ?? false,
                PolygonalCB.IsChecked ?? false, //EdgesCB.IsChecked ?? false,
                true, //BarDCB.IsChecked ?? false,
                true, //LinkDCB.IsChecked ?? false,
                true, //CGCB.IsChecked ?? false
            };

            string[] Mins = new string[]
            {
                MinXDim?.Text ?? "",
                MinYDim?.Text ?? "",
                MinNX?.Text?.ToString() ?? "",
                MinNY?.Text?.ToString() ?? "",
                MinD?.Text ?? "",
                MinNCirc?.Text?.ToString() ?? "",
                MinRadius?.Text ?? "",
                MinEdges?.Text ?? "",
                MinBarDiameter?.Text?.ToString() ?? "",
                MinLinkDiameter?.Text?.ToString() ?? "",
                MinCG?.Text?.ToString() ?? "",
            };

            string[] Maxs = new string[]
            {
                MaxXDim?.Text ?? "",
                MaxYDim?.Text ?? "",
                MaxNX?.Text?.ToString() ?? "",
                MaxNY?.Text?.ToString() ?? "",
                MaxD?.Text ?? "",
                MaxNCirc?.Text?.ToString() ?? "",
                MaxRadius?.Text ?? "",
                MaxEdges?.Text ?? "",
                MaxBarDiameter?.Text?.ToString() ?? "",
                MaxLinkDiameter?.Text?.ToString() ?? "",
                MaxCG.Text?.ToString() ?? "",
            };

            bool error = false;

            for (int i = 0; i < Activations.Length; i++)
            {
                if (Activations[i] && (Mins[i] == "" || Maxs[i] == ""))
                    error = true;
            }
            if (Activations[0] && Convert.ToDouble(Mins[0]) > Convert.ToDouble(Maxs[0]))
            {
                LXmess.Text = "Incompatible Min and Max values";
                error = true;
            }
            if (Activations[1] && Convert.ToDouble(Mins[1]) > Convert.ToDouble(Maxs[1]))
            {
                LYmess.Text = "Incompatible Min and Max values";
                error = true;
            }
            if (Activations[2] && Convert.ToInt32(Mins[2]) > Convert.ToInt32(Maxs[2]))
            {
                NXmess.Text = "Incompatible Min and Max values";
                error = true;
            }
            if (Activations[3] && Convert.ToInt32(Mins[3]) > Convert.ToInt32(Maxs[3]))
            {
                NYmess.Text = "Incompatible Min and Max values";
                error = true;
            }
            if (Activations[4] && Convert.ToDouble(Mins[4]) > Convert.ToDouble(Maxs[4]))
            {
                Dmess.Text = "Incompatible Min and Max values";
                error = true;
            }
            if (Activations[5] && Convert.ToInt32(Mins[5]) > Convert.ToInt32(Maxs[5]))
            {
                NCircmess.Text = "Incompatible Min and Max values";
                error = true;
            }
            if (Activations[6] && Convert.ToDouble(Mins[6]) > Convert.ToDouble(Maxs[6]))
            {
                Radiusmess.Text = "Incompatible Min and Max values";
                error = true;
            }
            if (Activations[7] && Convert.ToInt32(Mins[7]) > Convert.ToInt32(Maxs[7]))
            {
                Edgesmess.Text = "Incompatible Min and Max values";
                error = true;
            }

            if (Activations[8] && Convert.ToInt32(Mins[8]) > Convert.ToInt32(Maxs[8]))
            {
                Barmess.Text = "Incompatible Min and Max values";
                error = true;
            }
            if (Activations[9] && Convert.ToInt32(Mins[9]) > Convert.ToInt32(Maxs[9]))
            {
                Linkmess.Text = "Incompatible Min and Max values";
                error = true;
            }
            if (Activations[10] && Convert.ToInt32(Mins[10].Substring(0, 2)) > Convert.ToInt32(Maxs[10].Substring(0, 2)))
            {
                CGmess.Text = "Incompatible Min and Max values";
                error = true;
            }

            if (!(CostCB.IsChecked ?? false) && !(CarbonCB.IsChecked ?? false))
            {
                Drivermess.Text = "You must select at least one driver";
                error = true;
            }

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

                i0 = vm.LinkDiameters.IndexOf(Convert.ToInt32(MinLinkDiameter.Text));
                i1 = vm.LinkDiameters.IndexOf(Convert.ToInt32(MaxLinkDiameter.Text));
                opti.LinkDiameters = new List<int>();
                for (int i = i0; i <= i1; i++)
                    opti.LinkDiameters.Add(vm.LinkDiameters[i]);

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
                    IncrD.Text,
                    IncrRadius.Text
                };

                vm.MyBatchDesignView.OptimiseClusterDesigns();
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

        private void RectangularCB_Checked(object sender, RoutedEventArgs e)
        {
            bool val = RectangularCB.IsChecked ?? false;
            LXCB.IsEnabled = val;
            LYCB.IsEnabled = val;
            NXCB.IsEnabled = val;
            NYCB.IsEnabled = val;
            if (!val)
            {
                MinXDim.IsEnabled = val;
                MaxXDim.IsEnabled = val;
                MinYDim.IsEnabled = val;
                MaxYDim.IsEnabled = val;
                IncrXDim.IsEnabled = val;
                IncrYDim.IsEnabled = val;
                MinNX.IsEnabled = val;
                MaxNX.IsEnabled = val;
                MinNY.IsEnabled = val;
                MaxNY.IsEnabled = val;
                //SquareCB.IsEnabled = val;
            }
            else
            {
                //LXCB_Checked(sender, e);
                //LYCB_Checked(sender, e);
                //NXCB_Checked(sender, e);
                //NYCB_Checked(sender, e);
            }
        }

        private void CircularCB_Checked(object sender, RoutedEventArgs e)
        {
            bool val = CircularCB.IsChecked ?? false;
            DCB.IsEnabled = val;
            NCircCB.IsEnabled = val;
            if (!val)
            {
                MinD.IsEnabled = val;
                MaxD.IsEnabled = val;
                IncrD.IsEnabled = val;
                MinNCirc.IsEnabled = val;
                MaxNCirc.IsEnabled = val;
            }
            else
            {
                //DCB_Checked(sender, e);
                //NCircCB_Checked(sender, e);
            }
        }

        private void PolygonalCB_Checked(object sender, RoutedEventArgs e)
        {
            bool val = PolygonalCB.IsChecked ?? false;
            RadiusCB.IsEnabled = val;
            EdgesCB.IsEnabled = val;
            if (!val)
            {
                MinRadius.IsEnabled = val;
                MaxRadius.IsEnabled = val;
                MinEdges.IsEnabled = val;
                MaxEdges.IsEnabled = val;
            }
            else
            {
                //RadiusCB_Checked(sender, e);
                //EdgesCB_Checked(sender, e);
            }
        }
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
            (this.DataContext as ViewModel).MyBatchDesignView.DisplayDesignClusters();
            (this.DataContext as ViewModel).MyBatchDesignView.DisplayAllClusters();
            (this.DataContext as ViewModel).MyBatchDesignView.GetCurrentPerformance();
        }

        private void NClustersChanged(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            BatchColumnDesign BatchDesign = (this.DataContext as ViewModel).MyBatchDesignView.BatchDesign;
            try
            {
                int nc = Convert.ToInt32(tb.Text);
                BatchDesign.NClusters = nc;
                StartBatchDesign();
            }
            catch { }
        }

        private void SigmaChanged(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            BatchColumnDesign BatchDesign = (this.DataContext as ViewModel).MyBatchDesignView.BatchDesign;
            try
            {
                BatchDesign.Sigma = Convert.ToInt32(tb.Text);
                StartBatchDesign();
            }
            catch { }
        }

        private void ExportClusters(object sender, RoutedEventArgs e)
        {
            ViewModel vm = (this.DataContext as ViewModel);
            List<Column> cols = new List<Column>();
            for (int i = 0; i < vm.MyBatchDesignView.BatchDesign.Clusters.Count; i++)
            {
                Column col = vm.MyBatchDesignView.BatchDesign.Designs[i].Clone();
                col.Name = "Cluster_" + i;
                for(int j = 0; j < vm.MyBatchDesignView.BatchDesign.LoadClusters[i].Count; j++)
                {
                    ClusterLoad cl = vm.MyBatchDesignView.BatchDesign.LoadClusters[i][j];
                    Load l = cl.ParentColumn.Loads.First(m => m.Name == cl.Name);
                    l.Name = "load " + j;
                    col.Loads.Add(l);
                }
                col.SelectedLoad = col.Loads[0];
                cols.Add(col);
            }
            vm.MyColumns.AddRange(cols);
        }

        private void ClusteringMethodChanged(object sender, RoutedEventArgs e)
        {
            string text = (sender as ComboBox).SelectedValue as string;
            ClusteringMethod m = (ClusteringMethod)Enum.Parse(typeof(ClusteringMethod), text);
            (this.DataContext as ViewModel).MyBatchDesignView.BatchDesign.Method = m;
            (this.DataContext as ViewModel).MyBatchDesignView.DisplayAllClusters();
        }

    }
}
