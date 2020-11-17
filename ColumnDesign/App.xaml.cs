using ColumnDesignCalc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ColumnDesign
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void App_StartUp(object sender, StartupEventArgs e)
        {
            Console.WriteLine("Checkpoint 0");
            TextReader reader = null;
            string filePath = "";
            List<Column> cols = new List<Column>();
            foreach (var a in e.Args)
            {
                filePath += a + " ";
            }
            //if (filePath == "")
            //{
            //    DirectoryInfo di = new DirectoryInfo(Path.GetTempPath());

            //    filePath = di.FullName + "\\columnData.json";
            //}
            if(File.Exists(filePath))
            {
                filePath.Substring(0, filePath.Length - 1);
                try
                {
                    var settings = new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    };
                    reader = new StreamReader(filePath);
                    var fileContents = reader.ReadToEnd();
                    cols = JsonConvert.DeserializeObject<List<ETABSColumnDesign_Plugin.Column>>(fileContents, settings).Select(c => new Column(c)).ToList();
                }
                catch (Exception ex)
                {
                    try
                    {
                        var settings = new JsonSerializerSettings()
                        {
                            TypeNameHandling = TypeNameHandling.Objects,
                            PreserveReferencesHandling = PreserveReferencesHandling.Objects
                        };
                        reader = new StreamReader(filePath);
                        var fileContents = reader.ReadToEnd();
                        cols = new List<Column> { JsonConvert.DeserializeObject<Column>(fileContents, settings) };
                    }
                    catch { }
                    //var settings = new JsonSerializerSettings()
                    //{
                    //    TypeNameHandling = TypeNameHandling.Objects,
                    //    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    //};
                    //reader = new StreamReader(filePath);
                    //var fileContents = reader.ReadToEnd();
                    //cols = JsonConvert.DeserializeObject<List<Column>>(fileContents, settings);
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
            }
            Console.WriteLine("Checkpoint 1");

            MainWindow mainWindow = new MainWindow();

            Column customCol = new Column()
            {
                Name = "default",
                LX = 350,
                LY = 350,
                Length = 3000,
                CustomConcreteGrade = new Concrete("Custom", 50, 37),
                ConcreteGrade = new Concrete("50/60", 50, 37),
                SelectedLoad = new Load() { Name = "default", P = 5000 },
                Loads = new List<Load>() { new Load() { Name = "default", P = 5000 } },
                FireLoad = new Load() { Name = "default", P = 5000 } 
                //P = 5000
            };

            if(cols.Where(c => c.Name == "default").Count() == 0) cols.Insert(0, customCol);

            cols.ForEach(c => c.FireLoad = c.SelectedLoad.Clone());
            cols.ForEach(c => c.FDMStr = "Table");

            mainWindow.myViewModel.MyColumns = cols;

            mainWindow.myViewModel.SelectedColumn = mainWindow.myViewModel.MyColumns[0];

            mainWindow.Show();
            mainWindow.myViewModel.initializing = false;
            mainWindow.myViewModel.UpdateDesign();
            mainWindow.myViewModel.UpdateLoad();
        }
    }
}
