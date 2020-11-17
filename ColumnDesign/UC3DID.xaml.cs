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

namespace ColumnDesign
{
    /// <summary>
    /// Interaction logic for UC3DID.xaml
    /// </summary>
    public partial class UC3DID : UserControl
    {
        public UC3DID()
        {
            InitializeComponent();
        }

        public void Go2D (object sender, RoutedEventArgs e)
        {
            IDView view = (this.DataContext as ViewModel).MyIDView;
            view.Dimension = IDDimension.dim2D;
        }

        private void DiagramDiscChanged(object sender, RoutedEventArgs e)
        {
            Slider s = sender as Slider;
            ViewModel vm = s.DataContext as ViewModel;
            if (vm != null)
            {
                //vm.SelectedColumn.DiagramDisc = Convert.ToInt32(s.Value);
                vm.UpdateDesign();
            }
        }

        private void IDReductionChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            if ((e as SelectionChangedEventArgs).RemovedItems.Count == 0) return;
            ComboBox cb = sender as ComboBox;
            ViewModel vm = this.DataContext as ViewModel;
            //string s = cb.SelectedValue as string;
            //vm.SelectedColumn.IDReduction = Convert.ToInt32(s.Substring(0, s.Length - 1));
            vm.SelectedColumn.IDReduction = Convert.ToInt32(cb.SelectedValue);
            vm.UpdateDesign();
        }
    }
}
