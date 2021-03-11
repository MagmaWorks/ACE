using ColumnDesignCalc;
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
    /// Interaction logic for UCSettings.xaml
    /// </summary>
    public partial class UCSettings : UserControl
    {
        public UCSettings()
        {
            InitializeComponent();

        }
        private void OK(object sender, RoutedEventArgs e)
        {
            ViewModel view = (this.DataContext as ViewModel);

            Settings sett = view.MySettings;
            if (CurrentCB.IsChecked ?? false)
                sett.ExprtdCols = ExportedColumns.Current;
            else if (AllCB.IsChecked ?? false)
                sett.ExprtdCols = ExportedColumns.All;
            else if (AllClustersCB.IsChecked ?? false)
                sett.ExprtdCols = ExportedColumns.AllClusterCols;
            else if (SelectionCB.IsChecked ?? false)
                sett.ExprtdCols = ExportedColumns.Selection;

            switch(sett.ExprtdCols)
            {
                case (ExportedColumns.Current):
                    view.ColumnsInReport = new List<string>() { view.SelectedColumn.Name };
                    break;
                case (ExportedColumns.All):
                    view.ColumnsInReport = view.MyColumns.Select(c => c.Name).ToList();
                    break;
                case (ExportedColumns.AllClusterCols):
                    view.ColumnsInReport = view.MyColumns.Where(c => c.IsCluster).Select(x => x.Name).ToList();
                    break;
                case (ExportedColumns.Selection):
                    view.ColumnsInReport = new List<string>();
                    for (int i = 0; i < ColumnList.SelectedItems.Count; i++)
                        view.ColumnsInReport.Add((ColumnList.SelectedItems[i] as Column).Name);
                    break;
            }

            if (SelectedLoadCB.IsChecked ?? false)
                sett.ExprtdLoads = ExportedLoads.Current;
            else if (DesigningLoadsCB.IsChecked ?? false)
                sett.ExprtdLoads = ExportedLoads.DesigningLoads;

            var myWindow = Window.GetWindow(this);
            myWindow.Close();
        }

        private void CBClicked(object sender, RoutedEventArgs e)
        {
            CurrentCB.IsChecked = false;
            AllCB.IsChecked = false;
            AllClustersCB.IsChecked = false;
            SelectionCB.IsChecked = false;

            CheckBox cb = sender as CheckBox;
            cb.IsChecked = true;

            ColumnList.IsEnabled = SelectionCB.IsChecked ?? false;
            CombineCB.IsEnabled = !(CurrentCB.IsChecked ?? false);
        }

        private void LoadCBClicked(object sender, RoutedEventArgs e)
        {
            SelectedLoadCB.IsChecked = false;
            DesigningLoadsCB.IsChecked = false;

            CheckBox cb = sender as CheckBox;
            cb.IsChecked = true;

            NumLoadsTB.IsEnabled = DesigningLoadsCB.IsChecked ?? false;
        }

    }
    
}
