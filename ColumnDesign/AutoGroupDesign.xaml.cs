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
using System.Windows.Shapes;

namespace ColumnDesigner
{
    /// <summary>
    /// Interaction logic for AutoGroupDesign.xaml
    /// </summary>
    public partial class AutoGroupDesign : Window
    {
        public AutoGroupDesign()
        {
            InitializeComponent();
        }

        private void RunGroupDesign(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;

            bool[] Shapes = new bool[]
            {
                RectangularCB.IsChecked ?? false,
                CircularCB.IsChecked ?? false,
                PolygonalCB.IsChecked ?? false,
            };

            bool[] Activations = new bool[]
            {
                LXCB.IsChecked ?? false,
                LYCB.IsChecked ?? false,
                NXCB.IsChecked ?? false,
                NYCB.IsChecked ?? false,
                DCB.IsChecked ?? false,
                NCircCB.IsChecked ?? false,
                RadiusCB.IsChecked ?? false,
                EdgesCB.IsChecked ?? false,
                BarDCB.IsChecked ?? false,
                LinkDCB.IsChecked ?? false,
                CGCB.IsChecked ?? false
            };

            string[] Mins = new string[]
            {
                MinXDim?.Text ?? "",
                MinYDim?.Text ?? "",
                MinNX?.SelectedValue?.ToString() ?? "",
                MinNY?.SelectedValue?.ToString() ?? "",
                MinD?.Text ?? "",
                MinNCirc?.SelectedValue?.ToString() ?? "",
                MinRadius?.Text ?? "",
                MinEdges?.Text ?? "",
                MinBarDiameter?.SelectedValue?.ToString() ?? "",
                MinLinkDiameter?.SelectedValue?.ToString() ?? "",
                MinCG?.SelectedValue?.ToString() ?? "",
            };

            string[] Maxs = new string[]
            {
                MaxXDim?.Text ?? "",
                MaxYDim?.Text ?? "",
                MaxNX?.SelectedValue?.ToString() ?? "",
                MaxNY?.SelectedValue?.ToString() ?? "",
                MaxD?.Text ?? "",
                MaxNCirc?.SelectedValue?.ToString() ?? "",
                MaxRadius?.Text ?? "",
                MaxEdges?.Text ?? "",
                MaxBarDiameter?.SelectedValue?.ToString() ?? "",
                MaxLinkDiameter?.SelectedValue?.ToString() ?? "",
                MaxCG.SelectedValue?.ToString() ?? "",
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
                int globmaxiter = Convert.ToInt32(GlobMaxIter.Text);
                int indmaxiter = Convert.ToInt32(MaxIter.Text);

                double globalpha = Convert.ToDouble(GlobAlpha.Text);
                double indalpha = Convert.ToDouble(Alpha.Text);

                double globvari = Convert.ToDouble(GlobVariance.Text);
                double indvari = Convert.ToDouble(Variance.Text);

                double globTinit = Convert.ToDouble(GlobTinit.Text);
                double indTinit = Convert.ToDouble(Tinit.Text);

                double[] driversWeight = new double[2];
                driversWeight[0] = CostCB.IsChecked ?? false ? 1 : 0;
                driversWeight[1] = CarbonCB.IsChecked ?? false ? 1 : 0;

                double[] drivers = new double[2];
                drivers[0] = Convert.ToDouble(CostWeightTB.Text);
                drivers[1] = Convert.ToDouble(CarbonWeightTB.Text);

                string[] Incrs = new string[]
                {
                    IncrXDim.Text,
                    IncrYDim.Text,
                    IncrD.Text,
                    IncrRadius.Text
                };

                int Ndmin = Convert.ToInt32(GlobNdmin.Text);
                int Ndmax = Convert.ToInt32(GlobNdmax.Text);

                double sample = Convert.ToDouble(Sample.Text) / 100.0;

                this.Close();
                GroupDesignOptimisation GDO = new GroupDesignOptimisation(vm, Shapes, Activations, Mins, Maxs, Incrs, globmaxiter, indmaxiter, globalpha, indalpha,
                    globvari, indvari, globTinit, indTinit, driversWeight, drivers, Ndmin, Ndmax, sample, UseJSON_CB.IsChecked ?? false, true, AllLoadsCB.IsChecked ?? false);
            }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LXCB_Checked(object sender, RoutedEventArgs e)
        {
            bool val = LXCB.IsChecked ?? false;
            MinXDim.IsEnabled = val;
            MaxXDim.IsEnabled = val;
            IncrXDim.IsEnabled = val;
            SquareCB.IsEnabled = val && (LYCB.IsChecked ?? false);
            MinYDim.IsEnabled = !val && (LYCB.IsChecked ?? false);
            MaxYDim.IsEnabled = !val && (LYCB.IsChecked ?? false);
            IncrYDim.IsEnabled = !val && (LYCB.IsChecked ?? false);
        }

        private void LYCB_Checked(object sender, RoutedEventArgs e)
        {
            bool val = LYCB.IsChecked ?? false;
            MinYDim.IsEnabled = val && ((LXCB.IsChecked ?? false) ? !(SquareCB.IsChecked ?? false) : true);
            MaxYDim.IsEnabled = val && ((LXCB.IsChecked ?? false) ? !(SquareCB.IsChecked ?? false) : true);
            IncrYDim.IsEnabled = val && ((LXCB.IsChecked ?? false) ? !(SquareCB.IsChecked ?? false) : true);
            SquareCB.IsEnabled = val && (LXCB.IsChecked ?? false);
        }

        private void NXCB_Checked(object sender, RoutedEventArgs e)
        {
            bool val = NXCB.IsChecked ?? false;
            MinNX.IsEnabled = val;
            MaxNX.IsEnabled = val;
        }

        private void NYCB_Checked(object sender, RoutedEventArgs e)
        {
            bool val = NYCB.IsChecked ?? false;
            MinNY.IsEnabled = val;
            MaxNY.IsEnabled = val;
        }

        private void BarCB_Checked(object sender, RoutedEventArgs e)
        {
            bool val = BarDCB.IsChecked ?? false;
            MinBarDiameter.IsEnabled = val;
            MaxBarDiameter.IsEnabled = val;
        }

        private void LinkCB_Checked(object sender, RoutedEventArgs e)
        {
            bool val = LinkDCB.IsChecked ?? false;
            MinLinkDiameter.IsEnabled = val;
            MaxLinkDiameter.IsEnabled = val;
        }

        private void CGCB_Checked(object sender, RoutedEventArgs e)
        {
            MinCG.IsEnabled = (sender as CheckBox).IsChecked ?? false;
            MaxCG.IsEnabled = (sender as CheckBox).IsChecked ?? false;
        }

        private void SquareCB_Checked(object sender, RoutedEventArgs e)
        {
            MinYDim.IsEnabled = !(sender as CheckBox).IsChecked ?? false;
            MaxYDim.IsEnabled = !(sender as CheckBox).IsChecked ?? false;
            IncrYDim.IsEnabled = !(sender as CheckBox).IsChecked ?? false;
        }

        private void DCB_Checked(object sender, RoutedEventArgs e)
        {
            bool val = DCB.IsChecked ?? false;
            MinD.IsEnabled = val;
            MaxD.IsEnabled = val;
            IncrD.IsEnabled = val;
        }

        private void NCircCB_Checked(object sender, RoutedEventArgs e)
        {
            bool val = NCircCB.IsChecked ?? false;
            MinNCirc.IsEnabled = val;
            MaxNCirc.IsEnabled = val;
        }

        private void RadiusCB_Checked(object sender, RoutedEventArgs e)
        {
            bool val = RadiusCB.IsChecked ?? false;
            MinRadius.IsEnabled = val;
            MaxRadius.IsEnabled = val;
        }

        private void EdgesCB_Checked(object sender, RoutedEventArgs e)
        {
            bool val = EdgesCB.IsChecked ?? false;
            MinEdges.IsEnabled = val;
            MaxEdges.IsEnabled = val;
        }

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
                SquareCB.IsEnabled = val;
            }
            else
            {
                LXCB_Checked(sender, e);
                LYCB_Checked(sender, e);
                NXCB_Checked(sender, e);
                NYCB_Checked(sender, e);
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
                DCB_Checked(sender, e);
                NCircCB_Checked(sender, e);
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
                RadiusCB_Checked(sender, e);
                EdgesCB_Checked(sender, e);
            }
        }
    }
}
