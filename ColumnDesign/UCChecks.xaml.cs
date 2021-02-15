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

        //public void Save(object sender, RoutedEventArgs e)
        //{
        //    ViewModel vm = (sender as Button).DataContext as ViewModel;
        //    vm.Save();
        //}

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
            vm.OpenAdd();
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
            //vm.MyIDView.IsUpdated = !vm.MyIDView.IsUpdated;
            //vm.UpdateDesign();
            vm.ColumnCalcs.UpdateInputOuput();
            vm.ColumnCalcs.AddInteractionDiagrams();
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
            vm.MyBatchDesignView.BatchDesign.Columns = vm.MyColumns;

            UCBatchDesign batchDesign = new UCBatchDesign();
            //batchDesign.DataContext = vm;

            Window win = new Window()
            {
                Content = batchDesign,
                Owner = System.Windows.Application.Current.MainWindow,
                Title = "Batch Design",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                DataContext = vm,
                WindowState = WindowState.Maximized,
            };
            batchDesign.StartBatchDesign();
            win.ShowDialog();
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            ViewModel vm = this.DataContext as ViewModel;
            Button b = sender as Button;
            Point position = b.PointToScreen(new Point(0d, 0d));
            Window w = new Window()
            {
                Title = "Edit settings",
                Content = new UCSettings(),
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                DataContext = vm
            };
            w.ShowDialog();
        }
    }
}
