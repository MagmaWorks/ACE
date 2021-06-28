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
    /// Interaction logic for UCGlobalParameters.xaml
    /// </summary>
    public partial class UCGlobalParameters : UserControl
    {
        public UCGlobalParameters()
        {
            InitializeComponent();
        }

        private void GlobalParameterClick(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            ViewModel vm = (ViewModel)this.DataContext;
            Column gc = vm.GlobalColumn;
            List<Column> Columns = vm.MyColumns;
            switch(b.Name)
            {
                case ("ConcreteGradeButton"):
                    Columns.ForEach(c => c.ConcreteGrade = gc.ConcreteGrade);
                    break;
                case ("SteelGradeButton"):
                    Columns.ForEach(c => c.SteelGrade = gc.SteelGrade);
                    break;
                case ("CoverToLinksButton"):
                    Columns.ForEach(c => c.CoverToLinks = gc.CoverToLinks);
                    break;
                case ("EffLengthFactorButton"):
                    Columns.ForEach(c => c.EffectiveLength = gc.EffectiveLength);
                    break;
                case ("BarDiameterButton"):
                    Columns.ForEach(c => c.BarDiameter = gc.BarDiameter);
                    break;
                case ("LinkDiameterButton"):
                    Columns.ForEach(c => c.LinkDiameter = gc.LinkDiameter);
                    break;
            }
            vm.RefreshColumns();
            vm.UpdateDesign();
        }

        private void GradeChanged(object sender, RoutedEventArgs e)
        {
            ((ViewModel)this.DataContext).RefreshColumns();
        }
    }
}
