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
    /// Interaction logic for UCClusteredDesigns.xaml
    /// </summary>
    public partial class UCClusteredDesigns : UserControl
    {
        public UCClusteredDesigns()
        {
            InitializeComponent();
        }

        public void HighlightGroup(object sender, MouseEventArgs e)
        {
            BatchDesignView view = this.DataContext as BatchDesignView;
            int index = Convert.ToInt32((sender as StackPanel).Tag);
            view.DisplayOneDesignCluster(index);
        }

        public void EndHighlight(object sender, MouseEventArgs e)
        {
            BatchDesignView view = this.DataContext as BatchDesignView;
            view.DisplayDesignClusters(false,false);
        }
    }
}
