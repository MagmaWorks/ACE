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
    /// Interaction logic for UCClusteredColumns.xaml
    /// </summary>
    public partial class UCClusteredColumns : UserControl
    {
        public UCClusteredColumns()
        {
            InitializeComponent();
        }

        public void HighlightGroup(object sender, MouseEventArgs e)
        {
            BatchDesignView view = this.DataContext as BatchDesignView;
            int index = Convert.ToInt32((sender as StackPanel).Tag);
            view.DisplayOneCluster(index);
        }

        public void EndHighlight(object sender, MouseEventArgs e)
        {
            BatchDesignView view = this.DataContext as BatchDesignView;
            if(view.ActiveCluster < 0)
                view.DisplayAllClusters(compute : false, updateKeys: false);
            else
                view.DisplayOneCluster(view.ActiveCluster);
        }
    }
}
