using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ViewModel myViewModel { get; set; } //Test

        public string Version
        {
            get
            {
                return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileMajorPart.ToString() +
                    "." +
                    FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileMinorPart.ToString() +
                    "."
                    +
                    FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileBuildPart.ToString()
                    ;
            }
        }

        public string Build
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public MainWindow()
        {
            myViewModel = new ViewModel();
            this.DataContext = myViewModel;
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var save = new SaveOnQuitVM();
            ClosingWindow myWin = new ClosingWindow(save)
            {
                SizeToContent = SizeToContent.WidthAndHeight,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            myWin.ShowDialog();
            if (save.Save)
            {
                myViewModel.Save();
            }
            else if (save.SaveAll)
            {
                myViewModel.SaveAll();
            }
            if (save.Cancel)
            {
                e.Cancel = true;
            }
            base.OnClosing(e);
        }
    }
}
