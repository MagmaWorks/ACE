using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
    /// Interaction logic for UC2DID.xaml
    /// </summary>
    public partial class UC2DID : UserControl
    {
        public UC2DID()
        {
            InitializeComponent();
        }

        public void Go3D(object sender, RoutedEventArgs e)
        {
            IDView view = (this.DataContext as ViewModel).MyIDView;
            view.Dimension = IDDimension.dim3D;
        }

        private void DiagramDiscChanged(object sender, RoutedEventArgs e)
        {
            //Slider s = sender as Slider;
            ViewModel vm = this.DataContext as ViewModel;
            if (vm != null)
            {
                //vm.SelectedColumn.DiagramDisc = Convert.ToInt32(s.Value);
                vm.UpdateDesign();
            }
        }

        private void IDReductionChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            if ((e as SelectionChangedEventArgs).RemovedItems.Count == 0) return;
            ComboBox cb = sender as ComboBox;
            ViewModel vm = this.DataContext as ViewModel;
            //string s = cb.SelectedValue as string;
            //vm.SelectedColumn.IDReduction = Convert.ToInt32(s.Substring(0, s.Length - 1));
            vm.SelectedColumn.IDReduction = Convert.ToInt32(cb.SelectedValue);
            vm.UpdateDesign();
        }

        private void PrintIDs(object sender, DependencyPropertyChangedEventArgs e)
        {
            //PrintID(MxMy);
            //PrintID(MxN);
            //PrintID(MyN);
        }

        private void PrintID(Control control)
        {
            System.Windows.Size size = control.RenderSize;
            if (size.Width == 0 || size.Height == 0) return;

            Rect bounds = VisualTreeHelper.GetDescendantBounds(control);
            RenderTargetBitmap bmpRen = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, 96, 96, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext ctx = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(control);
                ctx.DrawRectangle(vb, null, new Rect(new System.Windows.Point(), bounds.Size));
            }
            bmpRen.Render(dv);

            MemoryStream stream = new MemoryStream();
            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmpRen));
            encoder.Save(stream);

            Bitmap bmp = new Bitmap(stream);

            ImageConverter converter = new ImageConverter();
            byte[] output = (byte[])converter.ConvertTo(bmp, typeof(byte[]));

            string path = System.IO.Path.GetTempPath() + control.Name + ".tmp";
            File.WriteAllBytes(path, output);

            // prints png
            var encoderPng = new PngBitmapEncoder();
            encoderPng.Frames.Add(BitmapFrame.Create(bmpRen));
            string pathPng = System.IO.Path.GetTempPath() + control.Name + ".png";
            using (Stream stm = File.Create(pathPng))
                encoderPng.Save(stm);
        }
    }
}
