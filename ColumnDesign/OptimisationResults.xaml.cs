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

namespace ColumnDesigner
{
    /// <summary>
    /// Interaction logic for OptimisationResults.xaml
    /// </summary>
    public partial class OptimisationResults : Window
    {
        public OptimisationResults()
        {
            InitializeComponent();
        }

        private void AcceptDesign(object sender, RoutedEventArgs e)
        {
            DesignOptimisation DOpti = (this.DataContext as DesignOptimisation);
            DOpti.newDesign = true;
            this.Close();
        }

        private void KeepExisting(object sender, RoutedEventArgs e)
        {
            DesignOptimisation DOpti = (this.DataContext as DesignOptimisation);
            DOpti.newDesign = false;
            this.Close();
        }
    }
}
