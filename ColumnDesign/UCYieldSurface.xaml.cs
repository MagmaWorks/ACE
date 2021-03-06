﻿using System;
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
    /// Interaction logic for UCYieldSurface.xaml
    /// </summary>
    public partial class UCYieldSurface : UserControl
    {
        public UCYieldSurface()
        {
            InitializeComponent();
        }

        private void DiagramDiscChanged(object sender, RoutedEventArgs e)
        {
            Slider s = sender as Slider;
            ViewModel vm = s.DataContext as ViewModel;
            if(vm != null)
            {
                //vm.SelectedColumn.DiagramDisc = Convert.ToInt32(s.Value);
                vm.UpdateDesign();
            }
        }

        private void IDReductionChanged(object sender, RoutedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            ViewModel vm = this.DataContext as ViewModel;
            //string s = cb.SelectedValue as string;
            //vm.SelectedColumn.IDReduction = Convert.ToInt32(s.Substring(0, s.Length - 1));
            vm.SelectedColumn.IDReduction = Convert.ToInt32(cb.SelectedValue);
            vm.UpdateDesign();
        }

        public void Change3D2D(object sender, RoutedEventArgs e)
        {
            IDView view = (this.DataContext as ViewModel).MyIDView;
            if(buttonText.Text == "2D")
            {
                view.Dimension = IDDimension.dim2D;
                buttonText.Text = "3D";
            }
            else if(buttonText.Text == "3D")
            {
                view.Dimension = IDDimension.dim3D;
                buttonText.Text = "2D";
            }
        }
    }
}
