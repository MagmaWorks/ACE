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

namespace ColumnDesign
{
    /// <summary>
    /// Interaction logic for ColumnMap.xaml
    /// </summary>
    public partial class ColumnMap : Window
    {
        public ColumnMap()
        {
            InitializeComponent();
        }

        public void NdChanged(object sender, RoutedEventArgs e)
        {
            GroupDesignOptimisation GDO = (this.DataContext as GroupDesignOptimisation);
            //string nd = (NdComboBox.SelectedValue as string != "") ? NdComboBox.SelectedValue as string : GDO.minNd.ToString();
            int i = Convert.ToInt32(NdComboBox.SelectedValue) - GDO.minNd;
            DesignInfo.ItemsSource = GDO.Keys[i].legends;
            GDO.Columns3DView = GDO.Columns3DViews[i];
            CarbonValue.Text = GDO.CarbonCosts[i].ToString();
            CostValue.Text = GDO.CostCosts[i].ToString();
        }

    }
}
