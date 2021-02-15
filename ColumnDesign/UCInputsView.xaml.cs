using ColumnDesignCalc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace ColumnDesign
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
            if (vm.initializing) return;
            //if (cb.SelectedValue as string == "Custom")
            //{
            //    vm.SelectedColumn.ConcreteGrade = vm.SelectedColumn.CustomConcreteGrade ?? new Concrete("Custom", 32, 33);
            //    vm.IsConcreteCustom = true;
            //}
            //else
            //{
            //    vm.SelectedColumn.ConcreteGrade = vm.ColumnCalcs.ConcreteGrades.FirstOrDefault(c => c.Name == cb.SelectedValue as string);
            //    vm.IsConcreteCustom = false;
            //}
            vm.IsConcreteCustom = cb.SelectedValue as string == "Custom";
            vm.SelectedColumn.ConcreteGrade = vm.ColumnCalcs.ConcreteGrades.FirstOrDefault(c => c.Name == cb.SelectedValue as string);
            vm.UpdateColumn();
            vm.UpdateDesign();
        }

        private void SteelGradeChanged(object sender, RoutedEventArgs e)
        {
            ComboBox cb = (sender as ComboBox);
            ViewModel vm = cb.DataContext as ViewModel;
            if (vm.initializing) return;
            //if (cb.SelectedValue as string == "Custom")
            //{
            //    vm.SelectedColumn.CustomSteelGrade = vm.SelectedColumn.CustomSteelGrade ?? new Steel("Custom", 400);
            //    vm.SelectedColumn.SteelGrade = vm.SelectedColumn.CustomSteelGrade;
            //    vm.IsSteelCustom = true;
            //}
            //else
            //{
            //    vm.SelectedColumn.SteelGrade = vm.ColumnCalcs.SteelGrades.FirstOrDefault(c => c.Name == cb.SelectedValue as string);
            //    vm.IsSteelCustom = false;
            //}
            vm.IsSteelCustom = cb.SelectedValue as string == "Custom";
            vm.SelectedColumn.SteelGrade = vm.ColumnCalcs.SteelGrades.FirstOrDefault(c => c.Name == cb.SelectedValue as string);
            vm.UpdateColumn();
            vm.UpdateDesign();
        }


        //private void DeleteColumn(object sender, RoutedEventArgs e)
        //{
        //    ViewModel vm = this.DataContext as ViewModel;
        //    if (vm.MyColumns.Count == 1)
        //        MessageBox.Show("You need at least one column in your list.");
        //    else
        //    {
        //        var res = MessageBox.Show(String.Format("Do you want to delete column '{0}'?", vm.SelectedColumn.Name),
        //                                  "Delete column", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        //        if(res == MessageBoxResult.Yes)
        //        {
        //            vm.DeleteColumn();
        //        }
        //    }
        //}

        //private void AddColumn(object sender, RoutedEventArgs e)
        //{
        //    ViewModel vm = this.DataContext as ViewModel;
        //    vm.AddColumn();
        //}
        
        private void DesignChangedNotFire(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            vm.UpdateDesign(updateFire: false);
        }

        private void BarDiameterChanged(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            ComboBox cb = sender as ComboBox;
            try
            {
                int val = Convert.ToInt32(cb.Text);
                if (vm.BarDiameters.Contains(val))
                {
                    vm.SelectedColumn.BarDiameter = val;
                    vm.UpdateDesign();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateWidth(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double w = Convert.ToDouble(tb.Text);
                if (w != vm.SelectedColumn.LX)
                {
                    vm.SelectedColumn.LX = Convert.ToDouble(w);
                    if (vm.SelectedColumn.TP != null) vm.SelectedColumn.TP.ContourPts = null;
                    vm.UpdateDesign();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateDepth(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double d = Convert.ToDouble(tb.Text);
                if (d != vm.SelectedColumn.LY)
                {
                    vm.SelectedColumn.LY = d;
                    if(vm.SelectedColumn.TP != null) vm.SelectedColumn.TP.ContourPts = null;
                    vm.UpdateDesign();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateRadius(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double d = Convert.ToDouble(tb.Text);
                if (d != vm.SelectedColumn.Radius)
                {
                    vm.SelectedColumn.Radius = d;
                    if(vm.SelectedColumn.TP != null) vm.SelectedColumn.TP.ContourPts = null;
                    vm.UpdateDesign();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateDiameter(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double d = Convert.ToDouble(tb.Text);
                if (d != vm.SelectedColumn.Diameter)
                {
                    vm.SelectedColumn.Diameter = d;
                    if (vm.SelectedColumn.TP != null) vm.SelectedColumn.TP.ContourPts = null;
                    vm.UpdateDesign();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateEdges(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                int d = Convert.ToInt32(tb.Text);
                if (d != vm.SelectedColumn.Edges)
                {
                    vm.SelectedColumn.Edges = d;
                    vm.UpdateDesign();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateLength(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double l = Convert.ToDouble(tb.Text);
                if (l != vm.SelectedColumn.Length)
                {
                    vm.SelectedColumn.Length = l;
                    vm.UpdateDesign();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateLTShape(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double l = Convert.ToDouble(tb.Text);
                double val = 0;
                if (tb.Name == "TShapedHX" || tb.Name == "LShapedHX")
                    val = vm.SelectedColumn.HX;
                else if (tb.Name == "TShapedHY" || tb.Name == "LShapedHY")
                    val = vm.SelectedColumn.HY;
                else if(tb.Name == "TShapedhX" || tb.Name == "LShapedhX")
                    val = vm.SelectedColumn.hX;
                else if (tb.Name == "TShapedhY" || tb.Name == "LShapedhY")
                    val = vm.SelectedColumn.hY;
                
                if (l != val)
                {
                    if (tb.Name == "TShapedHX" || tb.Name == "LShapedHX")
                    {
                        if (l > vm.SelectedColumn.hX) vm.SelectedColumn.HX = l;
                    }
                    else if (tb.Name == "TShapedHY" || tb.Name == "LShapedHY")
                    {
                        if (l > vm.SelectedColumn.hY) vm.SelectedColumn.HY = l;
                    }  
                    else if (tb.Name == "TShapedhX" || tb.Name == "LShapedhX")
                    {
                        if (l < vm.SelectedColumn.HX) vm.SelectedColumn.hX = l;
                    }
                    else if (tb.Name == "TShapedhY" || tb.Name == "LShapedhY")
                    {
                        if (l < vm.SelectedColumn.HY) vm.SelectedColumn.hY = l;
                    }
                    vm.UpdateDesign();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateCustomShape(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double l = Convert.ToDouble(tb.Text);
                double val = Convert.ToDouble(vm.SelectedColumn.GetType().GetProperty(tb.Name).GetValue(vm.SelectedColumn));
                if(l != val)
                {
                    vm.SelectedColumn.GetType().GetProperty(tb.Name).SetValue(vm.SelectedColumn, l);
                    vm.UpdateDesign();
                }
            }
            catch(Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void UpdateEffLength(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double c = Convert.ToDouble(tb.Text);
                if (c != vm.SelectedColumn.EffectiveLength)
                {
                    vm.SelectedColumn.EffectiveLength = c;
                    vm.UpdateDesign();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void UpdateCover(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double c = Convert.ToDouble(tb.Text);
                if (c != vm.SelectedColumn.CoverToLinks)
                {
                    vm.SelectedColumn.CoverToLinks = c;
                    //vm.SelectedColumn.ConcreteGrade = vm.SelectedColumn.CustomConcreteGrade;
                    vm.UpdateDesign();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateYoungMod(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double val = Convert.ToDouble(tb.Text);
                if (val != vm.SelectedColumn.ConcreteGrade.E)
                {
                    vm.SelectedColumn.ConcreteGrade.E = val;
                    vm.SelectedColumn.ConcreteGrade = vm.SelectedColumn.ConcreteGrade;
                    vm.UpdateDesign();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateLimitStrength(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double val = Convert.ToDouble(tb.Text);
                if (val != vm.SelectedColumn.ConcreteGrade.Fc)
                {
                    vm.SelectedColumn.ConcreteGrade.Fc = val;
                    vm.UpdateDesign();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateSteelYoungMod(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double val = Convert.ToDouble(tb.Text);
                if (val != vm.SelectedColumn.SteelGrade.E)
                {
                    vm.SelectedColumn.SteelGrade.E = val;
                    vm.UpdateDesign();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateSteelLimitStrength(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double val = Convert.ToDouble(tb.Text);
                if (val != vm.SelectedColumn.SteelGrade.Fy)
                {
                    vm.SelectedColumn.SteelGrade.Fy = val;
                    vm.UpdateDesign();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateP(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            Column col = vm.SelectedColumn;
            try
            {
                double p = Convert.ToDouble(tb.Text);
                if (p != col.SelectedLoad.P)
                {
                    col.SelectedLoad.P = p;
                    vm.UpdateLoad();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateM2Top(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double m = Convert.ToDouble(tb.Text);
                if (m != vm.SelectedColumn.SelectedLoad.MxTop)
                {
                    vm.SelectedColumn.SelectedLoad.MxTop = m;
                    vm.UpdateLoad();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateM2Bot(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double m = Convert.ToDouble(tb.Text);
                if (m != vm.SelectedColumn.SelectedLoad.MxBot)
                {
                    vm.SelectedColumn.SelectedLoad.MxBot = m;
                    vm.UpdateLoad();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateM3Top(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double m = Convert.ToDouble(tb.Text);
                if (m != vm.SelectedColumn.SelectedLoad.MyTop)
                {
                    vm.SelectedColumn.SelectedLoad.MyTop = m;
                    vm.UpdateLoad();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateM3Bot(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ViewModel vm = this.DataContext as ViewModel;
            try
            {
                double m = Convert.ToDouble(tb.Text);
                if (m != vm.SelectedColumn.SelectedLoad.MyBot)
                {
                    vm.SelectedColumn.SelectedLoad.MyBot = m;
                    vm.UpdateLoad();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
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

        //private void OptimiseGroup(object sender, RoutedEventArgs e)
        //{
        //    Button b = sender as Button;
        //    ViewModel vm = this.DataContext as ViewModel;

        //    AutoGroupDesign AGD = new AutoGroupDesign()
        //    {
        //        //SizeToContent = SizeToContent.WidthAndHeight,
        //        Owner = Application.Current.MainWindow,
        //        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        //        DataContext = vm
        //    };
        //    AGD.Show();
        //}

        private void ShapeChanged(object sender, RoutedEventArgs e)
        {
            
            CheckBox cb = sender as CheckBox;
            RectangularCB.IsChecked = (cb == RectangularCB) ? true : false;
            CircularCB.IsChecked = (cb == CircularCB) ? true : false;
            PolygonalCB.IsChecked = (cb == PolygonalCB) ? true : false;
            LShapedCB.IsChecked = (cb == LShapedCB) ? true : false;
            TShapedCB.IsChecked = (cb == TShapedCB) ? true : false;
            CustomShapeCB.IsChecked = (cb == CustomShapeCB) ? true : false;

            RectDepth.IsEnabled = RectangularCB.IsChecked ?? false;
            RectWidth.IsEnabled = RectangularCB.IsChecked ?? false;
            Diameter.IsEnabled = CircularCB.IsChecked ?? false;
            PolyEdges.IsEnabled = PolygonalCB.IsChecked ?? false;
            PolyRadius.IsEnabled = PolygonalCB.IsChecked ?? false;
            LShapedHX.IsEnabled = LShapedCB.IsChecked ?? false;
            LShapedhX.IsEnabled = LShapedCB.IsChecked ?? false;
            LShapedHY.IsEnabled = LShapedCB.IsChecked ?? false;
            LShapedhY.IsEnabled = LShapedCB.IsChecked ?? false;
            TShapedHX.IsEnabled = TShapedCB.IsChecked ?? false;
            TShapedhX.IsEnabled = TShapedCB.IsChecked ?? false;
            TShapedHY.IsEnabled = TShapedCB.IsChecked ?? false;
            TShapedhY.IsEnabled = TShapedCB.IsChecked ?? false;

            RectangularSection.Visibility = (RectangularCB.IsChecked ?? false)? Visibility.Visible : Visibility.Collapsed;
            CircularSection.Visibility = (CircularCB.IsChecked ?? false)? Visibility.Visible : Visibility.Collapsed;
            PolygonalSection.Visibility = (PolygonalCB.IsChecked ?? false)? Visibility.Visible : Visibility.Collapsed;
            LShapedSection.Visibility = (LShapedCB.IsChecked ?? false)? Visibility.Visible : Visibility.Collapsed;
            TShapedSection.Visibility = (TShapedCB.IsChecked ?? false)? Visibility.Visible : Visibility.Collapsed;
            CustomShapeSection.Visibility = (CustomShapeCB.IsChecked ?? false) ? Visibility.Visible : Visibility.Collapsed;

            //FireDesignMethodCB.IsEnabled = (cb == RectangularCB || cb == LShapedCB) ? true : false;
            FireCurveCB.IsEnabled = (cb == RectangularCB || cb == LShapedCB) ? true : false;

            ViewModel vm = (cb.DataContext as ViewModel);
            Column c = vm.SelectedColumn;
            
            if (RectangularCB.IsChecked ?? false)
            {
                c.Shape = GeoShape.Rectangular;
            }
            else if (CircularCB.IsChecked ?? false)
            {
                c.Shape = GeoShape.Circular;
                FireDesignMethodCB.SelectedItem = "Table";
                vm.MyLayoutView.DisplayFire = false;
            }
            else if (PolygonalCB.IsChecked ?? false)
            {
                c.Shape = GeoShape.Polygonal;
                FireDesignMethodCB.SelectedItem = "Table";
                vm.MyLayoutView.DisplayFire = false;
            }
            else if (LShapedCB.IsChecked ?? false)
            {
                c.Shape = GeoShape.LShaped;
                if (c.FireDesignMethod == FDesignMethod.Isotherm_500 || c.FireDesignMethod == FDesignMethod.Zone_Method)
                    FireDesignMethodCB.SelectedItem = "Table";
            }
            else if (TShapedCB.IsChecked ?? false)
            {
                c.Shape = GeoShape.TShaped;
                if (c.FireDesignMethod == FDesignMethod.Isotherm_500 || c.FireDesignMethod == FDesignMethod.Zone_Method)
                    FireDesignMethodCB.SelectedItem = "Table";
            }
            else if (CustomShapeCB.IsChecked ?? false)
            {
                c.Shape = GeoShape.CustomShape;
                if (c.FireDesignMethod == FDesignMethod.Isotherm_500 || c.FireDesignMethod == FDesignMethod.Zone_Method)
                    FireDesignMethodCB.SelectedItem = "Table";
            }

            // Update the fire design methods

            switch (c.Shape)
            {
                case (GeoShape.Rectangular):
                    vm.FireDesignMethods = Enum.GetNames(typeof(FDesignMethod)).ToList();
                    break;
                case (GeoShape.Circular):
                    vm.FireDesignMethods = new List<string> { FDesignMethod.Table.ToString() };
                    break;
                case (GeoShape.Polygonal):
                    vm.FireDesignMethods = new List<string> { FDesignMethod.Table.ToString() };
                    break;
                case (GeoShape.LShaped):
                    vm.FireDesignMethods = new List<string> { FDesignMethod.Table.ToString(), FDesignMethod.Advanced.ToString() };
                    break;
                case (GeoShape.TShaped):
                    vm.FireDesignMethods = new List<string> { FDesignMethod.Table.ToString(), FDesignMethod.Advanced.ToString() };
                    break;
                case (GeoShape.CustomShape):
                    vm.FireDesignMethods = new List<string> { FDesignMethod.Table.ToString(), FDesignMethod.Advanced.ToString() };
                    break;

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
                //Load fl = col.SelectedLoad.Name == "ALL LOADS" ? col.Loads[1] : col.SelectedLoad;
                Load fl = col.SelectedLoad;
                col.FireLoad = new Load()
                {
                    Name = "0.7*[selected]",
                    P = 0.7 * fl.P,
                    MEdx = 0.7 * fl.MEdx,
                    MEdy = 0.7 * fl.MEdy,
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
            vm.UpdateCalculation();
            vm.MyIDView.UpdateIDHull(col);
        }

        private void FireDesignMethodChanged(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            Column col = vm.SelectedColumn;
            vm.UpdateFire(false);
            vm.UpdateCalculation();
            vm.MyIDView.UpdateIDHull(col);
        }

        private void FireCurveChanged(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            Column col = vm.SelectedColumn;
            vm.UpdateFire(true);
            vm.UpdateCalculation();
            vm.MyIDView.UpdateIDHull(col);
        }

        private void FireLoadChanged(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            Column col = vm.SelectedColumn;
            string load = (sender as ComboBox).SelectedValue as string;
            if (load == "0.7*[selected]")
            {
                //Load fl = col.SelectedLoad.Name == "ALL LOADS" ? col.Loads[1] : col.SelectedLoad;
                Load fl = col.SelectedLoad;
                col.FireLoad = new Load()
                {
                    Name = "0.7*[selected]",
                    P = 0.7 * fl.P,
                    MEdx = 0.7 * fl.MEdx,
                    MEdy = 0.7 * fl.MEdy,
                };
            }
            else if (load != null)
                col.FireLoad = col.Loads.First(c => c.Name == load);
            else
                col.FireLoad = col.Loads[0];
            vm.UpdateLoad();
        }

        private void ChangeGeometryVisibility(object sender, RoutedEventArgs e)
        {
            if (GeometrySection.Visibility == Visibility.Visible)
            {
                GeometrySection.Visibility = Visibility.Collapsed;
                //var converter = new ImageSourceConverter();
                //GeometryEye.Source = (ImageSource)converter.ConvertFromString("/Resources/ClosedEye.png");
            }
            else if (GeometrySection.Visibility == Visibility.Collapsed)
            {
                GeometrySection.Visibility = Visibility.Visible;
                //var converter = new ImageSourceConverter();
                //GeometryEye.Source = (ImageSource)converter.ConvertFromString("/Resources/Eye.png");
            }
        }

        private void ChangeMaterialVisibility(object sender, RoutedEventArgs e)
        {
            if (MaterialSection.Visibility == Visibility.Visible)
                MaterialSection.Visibility = Visibility.Collapsed;
            else if (MaterialSection.Visibility == Visibility.Collapsed)
                MaterialSection.Visibility = Visibility.Visible;
        }

        private void ChangeLoadsVisibility(object sender, RoutedEventArgs e)
        {
            if (LoadsSection.Visibility == Visibility.Visible)
                LoadsSection.Visibility = Visibility.Collapsed;
            else if (LoadsSection.Visibility == Visibility.Collapsed)
                LoadsSection.Visibility = Visibility.Visible;
        }

        private void ChangeDesignVisibility(object sender, RoutedEventArgs e)
        {
            if (DesignSection.Visibility == Visibility.Visible)
                DesignSection.Visibility = Visibility.Collapsed;
            else if (DesignSection.Visibility == Visibility.Collapsed)
                DesignSection.Visibility = Visibility.Visible;
        }

        private void ShowLoads(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            Button b = sender as Button;
            Point position = b.PointToScreen(new Point(0d, 0d));
            Window w = new Window()
            {
                Title = "Edit loads",
                Owner = Application.Current.MainWindow,
                Content = new UCLoads(),
                DataContext = vm,
                SizeToContent = SizeToContent.WidthAndHeight,
                Left = position.X,
                Top = position.Y,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ResizeMode = ResizeMode.NoResize
            };
            w.ShowDialog();

            vm.UpdateColumn();
        }

        private void AllLoadsClicked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            ViewModel vm = this.DataContext as ViewModel;
            if(cb.IsChecked ?? false)
            {
                vm.SelectedColumn.AllLoads = true;
                vm.UpdateColumn();
                Loadcb.IsEnabled = false;
                Ptb.IsEnabled = false;
                Mxtoptb.IsEnabled = false;
                Mxbottb.IsEnabled = false;
                Mytoptb.IsEnabled = false;
                Mybottb.IsEnabled = false;
            }
            else
            {
                vm.SelectedColumn.AllLoads = false;
                vm.UpdateColumn();
                Loadcb.IsEnabled = true;
                Ptb.IsEnabled = true;
                Mxtoptb.IsEnabled = true;
                Mxbottb.IsEnabled = true;
                Mytoptb.IsEnabled = true;
                Mybottb.IsEnabled = true;
            }
            vm.UpdateLoad();
        }

        private void EditColumns(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            Button b = sender as Button;
            Point position = b.PointToScreen(new Point(0d, 0d));
            Window w = new Window()
            {
                Title = "Edit columns",
                Owner = Application.Current.MainWindow,
                Content = new UCColumns(),
                DataContext = vm,
                SizeToContent = SizeToContent.WidthAndHeight,
                Left = position.X,
                Top = position.Y,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ResizeMode = ResizeMode.NoResize
            };
            w.ShowDialog();
            PropertyInfo[] colProperties = Type.GetType(typeof(Column).AssemblyQualifiedName).GetProperties();

            Column defColClone = vm.MySettings.DefaultColumn.Clone();
            foreach (var c in vm.MyColumns)
            {
                foreach(var i in colProperties)
                {
                    if(i.GetValue(c) == null)
                    {
                        var def = i.GetValue(defColClone);
                        i.SetValue(c, def);
                    }
                }
            }
            //vm.NameSelectedColumn = vm.MyColumns[vm.MyColumns.Count - 1].Name;
            //vm.UpdateColumnNames();
        }

        private void EditAdvancedRebars(object sender, RoutedEventArgs e)
        {
            
            ViewModel vm = this.DataContext as ViewModel;
            vm.AdvancedRebarPos = vm.SelectedColumn.AdvancedRebarsPos.Select(r => new RebarPosition(r.X, r.Y)).ToList();
            Button b = sender as Button;
            Point position = b.PointToScreen(new Point(0d, 0d));
            Window w = new Window()
            {
                Title = "Edit rebars",
                Owner = Application.Current.MainWindow,
                Content = new UCAdvancedRebars(),
                DataContext = vm,
                SizeToContent = SizeToContent.WidthAndHeight,
                Left = position.X,
                Top = position.Y-200,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ResizeMode = ResizeMode.NoResize
            };
            w.ShowDialog();

            vm.SelectedColumn.AdvancedRebarsPos = vm.AdvancedRebarPos.Select(r => new MWGeometry.MWPoint2D(r.X, r.Y)).ToList();
            vm.UpdateDesign();
        }

        private void SelectAdvancedRebarFile(object sender, RoutedEventArgs e)
        {
            string filePath;
            try
            {
                var openDialog = new System.Windows.Forms.OpenFileDialog();
                openDialog.Filter = @"csv files |*.csv";
                //saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (openDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                filePath = openDialog.FileName;
                Properties.Settings.Default.Reload();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Oops..." + Environment.NewLine + ex.Message);
                return;
            }
            ViewModel vm = this.DataContext as ViewModel;
            vm.SelectedColumn.AdvancedRebarFile = filePath;
        }
    }
}
