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
    /// Interaction logic for UCColumnLayout.xaml
    /// </summary>
    public partial class UCColumnLayout : UserControl
    {
        public UCColumnLayout()
        {
            InitializeComponent();
        }

        public void LayoutSizeChanged(object sender, RoutedEventArgs e)
        {
            LayoutView lv = this.DataContext as LayoutView;
            if(lv != null) lv.LayoutSizeChanged(this.ActualHeight, this.ActualWidth);
        }

        public void FireDisplayChanged(object sender, RoutedEventArgs e)
        {
            LayoutView lv = this.DataContext as LayoutView;
            lv.DisplayFire = !lv.DisplayFire;
        }

        public void RotAntiClockWise(object sender, RoutedEventArgs e)
        {
            LayoutView lv = this.DataContext as LayoutView;
            double theta = lv.column.Theta + 90;
            if (theta == -90) theta = 270;
            if (theta == 360) theta = 0;
            lv.column.Theta = theta;
            lv.myViewModel.UpdateDesign();
        }

        public void RotClockWise(object sender, RoutedEventArgs e)
        {
            LayoutView lv = this.DataContext as LayoutView;
            double theta = lv.column.Theta - 90;
            if (theta == -90) theta = 270;
            if (theta == 360) theta = 0;
            lv.column.Theta = theta;
            lv.myViewModel.UpdateDesign();
        }
    }
}
