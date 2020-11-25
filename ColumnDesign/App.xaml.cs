using ColumnDesignCalc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
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
            //AppDomain currentDomain = AppDomain.CurrentDomain;
            //currentDomain.AssemblyResolve += new ResolveEventHandler(LoadIDdll);
            
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
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
            }
            MainWindow mainWindow = new MainWindow();


            if(cols.Count == 0)
            {
                Column col = mainWindow.myViewModel.MySettings.DefaultColumn.Clone();
                cols.Add(col);
            }
            mainWindow.myViewModel.MyColumns = cols;
            mainWindow.myViewModel.SelectedColumn = mainWindow.myViewModel.MyColumns[0];

            mainWindow.Show();
            mainWindow.myViewModel.initializing = false;
            mainWindow.myViewModel.UpdateDesign();
            mainWindow.myViewModel.UpdateLoad();
        }

        //private Assembly LoadIDdll(object sender, ResolveEventArgs args)
        //{
        //    if (!args.Name.Contains("InteractionDiagram")) return null;
        //    string assemblyPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Magma Works/Scaffold/Libraries/InteractionDiagram3D.dll";
        //    if (!File.Exists(assemblyPath)) return null;
        //    Assembly assembly = Assembly.LoadFrom(assemblyPath);
        //    return assembly;
        //}
    }
}
