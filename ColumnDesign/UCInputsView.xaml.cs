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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ColumnDesigner
{
    /// <summary>
    /// Interaction logic for UCInputsView.xaml
    /// </summary>
    public partial class UCInputsView : UserControl
    {
        public UCInputsView()
        {
            InitializeComponent();
        }

        private void ConcreteGradeChanged(object sender, RoutedEventArgs e)
        {
            ComboBox cb = (sender as ComboBox);
            ViewModel vm = cb.DataContext as ViewModel;
            if(cb.SelectedValue as string == "Custom")
            {
                vm.SelectedColumn.ConcreteGrade = vm.SelectedColumn.CustomConcreteGrade ?? new Concrete("Custom", 32, 33);
                vm.IsCustom = true;
            }
            else
            {
                vm.SelectedColumn.ConcreteGrade = vm.ConcreteGrades.FirstOrDefault(c => c.Name == cb.SelectedValue as string);
                vm.IsCustom = false;
            }
            vm.UpdateColumn();
            vm.UpdateDesign();
        }

        private void SteelGradeChanged(object sender, RoutedEventArgs e)
        {
            ComboBox cb = (sender as ComboBox);
            ViewModel vm = cb.DataContext as ViewModel;
            if (cb.SelectedValue as string == "Custom")
            {
                vm.SelectedColumn.CustomSteelGrade = vm.SelectedColumn.CustomSteelGrade ?? new Steel("Custom", 400);
                vm.SelectedColumn.SteelGrade = vm.SelectedColumn.CustomSteelGrade;
                vm.IsCustom = true;
            }
            else
            {
                vm.SelectedColumn.SteelGrade = vm.SteelGrades.FirstOrDefault(c => c.Name == cb.SelectedValue as string);
                vm.IsCustom = false;
            }
            vm.UpdateColumn();
            vm.UpdateDesign();
        }
        
        private void DesignChanged(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            vm.UpdateDesign();
        }

        private void BarDiameterChanged(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            ComboBox cb = sender as ComboBox;
            int val = Convert.ToInt32(cb.Text);
            if(vm.BarDiameters.Contains(val))
            {
                vm.SelectedColumn.BarDiameter = val;
                vm.UpdateDesign();
            }
        }

        private void UpdateWidth(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            double w = Convert.ToDouble(tb.Text);
            if(w != vm.SelectedColumn.LX)
            {
                vm.SelectedColumn.LX = Convert.ToDouble(w);
                vm.UpdateDesign();
            }
        }

        private void UpdateDepth(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            double d = Convert.ToDouble(tb.Text);
            if(d != vm.SelectedColumn.LY)
            {
                vm.SelectedColumn.LY = d;
                vm.UpdateDesign();
            }
        }

        private void UpdateRadius(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            double d = Convert.ToDouble(tb.Text);
            if (d != vm.SelectedColumn.Radius)
            {
                vm.SelectedColumn.Radius = d;
                vm.UpdateDesign();
            }
        }

        private void UpdateDiameter(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            double d = Convert.ToDouble(tb.Text);
            if (d != vm.SelectedColumn.Diameter)
            {
                vm.SelectedColumn.Diameter = d;
                vm.UpdateDesign();
            }
        }

        private void UpdateEdges(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            int d = Convert.ToInt32(tb.Text);
            if (d != vm.SelectedColumn.Edges)
            {
                vm.SelectedColumn.Edges = d;
                vm.UpdateDesign();
            }
        }

        private void UpdateLength(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            double l = Convert.ToDouble(tb.Text);
            if(l != vm.SelectedColumn.Length)
            {
                vm.SelectedColumn.Length = l;
                vm.UpdateDesign();
            }
        }

        private void UpdateCover(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            double c = Convert.ToDouble(tb.Text);
            if(c != vm.SelectedColumn.CoverToLinks)
            {
                vm.SelectedColumn.CoverToLinks = c;
                vm.SelectedColumn.ConcreteGrade = vm.SelectedColumn.CustomConcreteGrade;
                vm.UpdateDesign();
            }
        }

        private void UpdateYoungMod(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            double val = Convert.ToDouble(tb.Text);
            if (val != vm.SelectedColumn.CustomConcreteGrade.E)
            {
                vm.SelectedColumn.CustomConcreteGrade.E = val;
                vm.SelectedColumn.ConcreteGrade = vm.SelectedColumn.CustomConcreteGrade;
                vm.UpdateDesign();
            }
        }

        private void UpdateLimitStrength(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            double val = Convert.ToDouble(tb.Text);
            if (val != vm.SelectedColumn.CustomConcreteGrade.Fc)
            {
                vm.SelectedColumn.CustomConcreteGrade.Fc = val;
                vm.UpdateDesign();
            }
        }

        private void UpdateSteelYoungMod(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            double val = Convert.ToDouble(tb.Text);
            if (val != vm.SelectedColumn.CustomSteelGrade.E)
            {
                vm.SelectedColumn.CustomSteelGrade.E = val;
                vm.SelectedColumn.SteelGrade = vm.SelectedColumn.CustomSteelGrade;
                vm.UpdateDesign();
            }
        }

        private void UpdateSteelLimitStrength(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            double val = Convert.ToDouble(tb.Text);
            if (val != vm.SelectedColumn.CustomSteelGrade.Fy)
            {
                vm.SelectedColumn.CustomSteelGrade.Fy = val;
                vm.UpdateDesign();
            }
        }

        private void UpdateP(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            Column col = vm.SelectedColumn;
            double p = Convert.ToDouble(tb.Text);
            if (p != col.SelectedLoad.P)
            {
                col.SelectedLoad.P = p;
                vm.UpdateLoad();
            }
        }

        private void UpdateM2Top(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            double m = Convert.ToDouble(tb.Text);
            if (m != vm.SelectedColumn.SelectedLoad.MxTop)
            {
                vm.SelectedColumn.SelectedLoad.MxTop = m;
                vm.UpdateLoad();
            }
        }

        private void UpdateM2Bot(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            double m = Convert.ToDouble(tb.Text);
            if(m != vm.SelectedColumn.SelectedLoad.MxBot)
            {
                vm.SelectedColumn.SelectedLoad.MxBot = m;
                vm.UpdateLoad();
            }
        }

        private void UpdateM3Top(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            double m = Convert.ToDouble(tb.Text);
            if(m != vm.SelectedColumn.SelectedLoad.MyTop)
            {
                vm.SelectedColumn.SelectedLoad.MyTop = m;
                vm.UpdateLoad();
            }
        }

        private void UpdateM3Bot(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            double m = Convert.ToDouble(tb.Text);
            if(m != vm.SelectedColumn.SelectedLoad.MyBot)
            {
                vm.SelectedColumn.SelectedLoad.MyBot = m;
                vm.UpdateLoad();
            }
        }
        
        private void OptimiseDesign(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            ViewModel vm = this.DataContext as ViewModel;
            /*DesignOptimisation designO = new DesignOptimisation(vm.SelectedColumn);
            if (designO.newDesign)
                vm.SelectedColumn = designO.column;*/

            AutoDesign AD = new AutoDesign()
            {
                SizeToContent = SizeToContent.WidthAndHeight,
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                DataContext = vm
            };
            AD.Show();
        }

        private void OptimiseGroup(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            ViewModel vm = this.DataContext as ViewModel;

            AutoGroupDesign AGD = new AutoGroupDesign()
            {
                //SizeToContent = SizeToContent.WidthAndHeight,
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                DataContext = vm
            };
            AGD.Show();
        }

        private void ShapeChanged(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            RectangularCB.IsChecked = (cb == RectangularCB) ? true : false;
            CircularCB.IsChecked = (cb == CircularCB) ? true : false;
            PolygonalCB.IsChecked = (cb == PolygonalCB) ? true : false;

            RectDepth.IsEnabled = RectangularCB.IsChecked ?? false;
            RectWidth.IsEnabled = RectangularCB.IsChecked ?? false;
            Diameter.IsEnabled = CircularCB.IsChecked ?? false;
            PolyEdges.IsEnabled = PolygonalCB.IsChecked ?? false;
            PolyRadius.IsEnabled = PolygonalCB.IsChecked ?? false;

            FireDesignMethodCB.IsEnabled = (cb == RectangularCB) ? true : false;
            FireCurveCB.IsEnabled = (cb == RectangularCB) ? true : false;

            ViewModel vm = (cb.DataContext as ViewModel);

            if (RectangularCB.IsChecked ?? false)
                vm.SelectedColumn.Shape = GeoShape.Rectangular;
            else if (CircularCB.IsChecked ?? false)
            {
                vm.SelectedColumn.Shape = GeoShape.Circular;
                FireDesignMethodCB.SelectedItem = "Table";
            }
            else if (PolygonalCB.IsChecked ?? false)
            {
                vm.SelectedColumn.Shape = GeoShape.Polygonal;
                FireDesignMethodCB.SelectedItem = "Table";
            }

            vm.UpdateDesign();

        }

        private void LoadSelectionChanged(object sender, RoutedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            ViewModel vm = cb.DataContext as ViewModel;
            Column col = vm.SelectedColumn;
            string lname = cb.SelectedValue as string ?? col.Loads[0].Name;
            col.SelectedLoad = vm.SelectedColumn.Loads.First(l => l.Name == lname);

            if (col?.FireLoad?.Name == "0.7*[selected]")
            {
                Load fl = col.SelectedLoad.Name == "ALL LOADS" ? col.Loads[1] : col.SelectedLoad;
                col.FireLoad = new Load()
                {
                    Name = "0.7*[selected]",
                    P = 0.7 * fl.P,
                    Mxd = 0.7 * fl.Mxd,
                    Myd = 0.7 * fl.Myd,
                };
                vm.UpdateFire();
            }

            vm.UpdateLoad();
        }
        
        private void FireResistanceChanged(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            Column col = vm.SelectedColumn;
            vm.UpdateFire(true);
            //vm.MyCalcView.UpdateFireDesign(col);
            vm.UpdateCalculation();
        }

        private void FireDesignMethodChanged(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            Column col = vm.SelectedColumn;
            vm.UpdateFire(false);
            //vm.MyCalcView.UpdateFireDesign(col);
            vm.UpdateCalculation();
        }

        private void FireCurveChanged(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            Column col = vm.SelectedColumn;
            vm.UpdateFire(true);
            //vm.MyCalcView.UpdateFireDesign(col);
            vm.UpdateCalculation();
        }

        private void FireLoadChanged(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            Column col = vm.SelectedColumn;
            string load = (sender as ComboBox).SelectedValue as string;
            if (load == "0.7*[selected]")
            {
                Load fl = col.SelectedLoad.Name == "ALL LOADS" ? col.Loads[1] : col.SelectedLoad;
                col.FireLoad = new Load()
                {
                    Name = "0.7*[selected]",
                    P = 0.7 * fl.P,
                    Mxd = 0.7 * fl.Mxd,
                    Myd = 0.7 * fl.Myd,
                };
            }
            else if (load != null)
                col.FireLoad = col.Loads.First(c => c.Name == load);
            else
                col.FireLoad = col.Loads[0];
            vm.UpdateLoad();
        }
        
    }
}
