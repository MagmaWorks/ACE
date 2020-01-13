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
    /// Interaction logic for ClosingWindow.xaml
    /// </summary>
    public partial class ClosingWindow : Window
    {
        SaveOnQuitVM SaveQuit;

        public ClosingWindow(SaveOnQuitVM save)
        {
            InitializeComponent();
            SaveQuit = save;
        }

        public void Button_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b.Name == "Save")
                SaveQuit.Save = true;
            else if (b.Name == "SaveAll")
                SaveQuit.SaveAll = true;
            else if (b.Name == "Cancel")
                SaveQuit.Cancel = true;
            this.Close();
        }

    }
}
