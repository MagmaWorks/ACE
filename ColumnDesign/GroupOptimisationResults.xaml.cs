using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace ColumnDesign
{
    /// <summary>
    /// Interaction logic for GroupOptimisationResults.xaml
    /// </summary>
    public partial class GroupOptimisationResults : Window
    {
        public GroupOptimisationResults()
        {
            InitializeComponent();

            GroupOptiChart.AxisX.Add(new LogarithmicAxis
            {
                LabelFormatter = value => Math.Pow(10, value).ToString("N"),
                Base = 10,
                Separator = new LiveCharts.Wpf.Separator
                {
                    Stroke = Brushes.LightGray
                }
            });
        }

        public void ViewMap(object sender, RoutedEventArgs e)
        {
            GroupDesignOptimisation GDO = this.DataContext as GroupDesignOptimisation;

            GDO.SetColumn3DView();

            ColumnMap CM = new ColumnMap()
            {
                DataContext = GDO,
                Owner = this,
            };

            GDO.Columns3DView = GDO.Columns3DViews[0];
            CM.DesignInfo.ItemsSource = GDO.Keys[0].legends;
            CM.CarbonValue.Text = GDO.CarbonCosts[0].ToString();
            CM.CostValue.Text = GDO.CostCosts[0].ToString();
            List<int> Nds = new List<int>();
            for (int i = GDO.minNd; i <= GDO.maxNd; i++)
                Nds.Add(i);
            CM.NdComboBox.ItemsSource = Nds;

            CM.Show();
        }

        public void RepeatGroupOpti(object sender, RoutedEventArgs e)
        {
            (this.DataContext as GroupDesignOptimisation).RepeatOptimisation();
        }

        public void Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void sourceUpdated(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("toto");
        }

        private void GroupOptiChart_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
