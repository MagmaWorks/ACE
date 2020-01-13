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
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace ColumnDesign
{
    /// <summary>
    /// Interaction logic for UCChecks.xaml
    /// </summary>
    public partial class UCChecks : UserControl
    {
        public UCChecks()
        {
            InitializeComponent();
        }

        public void Save(object sender, RoutedEventArgs e)
        {
            ViewModel vm = (sender as Button).DataContext as ViewModel;
            vm.Save();
        }

        public void SaveAs(object sender, RoutedEventArgs e)
        {
            ViewModel vm = (sender as Button).DataContext as ViewModel;
            vm.SaveAs();
        }

        public void SaveAll(object sender, RoutedEventArgs e)
        {
            ViewModel vm = (sender as Button).DataContext as ViewModel;
            vm.SaveAll();
        }

        public void Open(object sender, RoutedEventArgs e)
        {
            ViewModel vm = (sender as Button).DataContext as ViewModel;
            vm.Open();
        }

        public void OpenAdd(object sender, RoutedEventArgs e)
        {
            ViewModel vm = (sender as Button).DataContext as ViewModel;
            vm.OpenAdd();
        }

        public void ShowCredits(object sender, RoutedEventArgs e)
        {
            ViewModel vm = (sender as Button).DataContext as ViewModel;
            Credits win = new Credits()
            {
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            win.ShowDialog();
        }

        public void ShowFireInfo(object sender, RoutedEventArgs e)
        {
            ViewModel vm = (sender as Button).DataContext as ViewModel;
            FireInfo win = new FireInfo()
            {
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            win.ShowDialog();
        }

        public void ToWord(object sender, RoutedEventArgs e)
        {
            ViewModel vm = (sender as Button).DataContext as ViewModel;
            vm.ExportToWord();
        }

        private void OptimiseDesign(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            ViewModel vm = this.DataContext as ViewModel;
            /*DesignOptimisation designO = new DesignOptimisation(vm.SelectedColumn);
            if (designO.newDesign)
                vm.SelectedColumn = designO.column;*/

            AutoDesign AD = new AutoDesign()
            {
                SizeToContent = SizeToContent.WidthAndHeight,
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                DataContext = vm
            };
            AD.Show();
        }

        private void OptimiseGroup(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            ViewModel vm = this.DataContext as ViewModel;

            AutoGroupDesign AGD = new AutoGroupDesign()
            {
                //SizeToContent = SizeToContent.WidthAndHeight,
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                DataContext = vm
            };
            AGD.Show();
        }
    }
}
