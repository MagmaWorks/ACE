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
            Console.WriteLine("filePath : {0}", filePath);
            //MessageBox.Show(String.Format("filePath = {0}", filePath));
            //filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)+ "/tmp43E6.tmp";
            if (File.Exists(filePath))
            {
                filePath.Substring(0, filePath.Length - 1);
                if(filePath.Contains(".col"))
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
                        var newcol = JsonConvert.DeserializeObject<Column>(fileContents, settings);
                        string[] splits = filePath.Split('\\');
                        string name = splits[splits.Length - 1];
                        newcol.Name = name.Replace(".col", "");
                        cols.Add(newcol);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        try
                        {
                            var settings = new JsonSerializerSettings()
                            {
                                TypeNameHandling = TypeNameHandling.Objects,
                                PreserveReferencesHandling = PreserveReferencesHandling.Objects
                            };
                            reader = new StreamReader(filePath);
                            var fileContents = reader.ReadToEnd();
                            var newcol = JsonConvert.DeserializeObject<Column>(fileContents, settings);
                            string[] splits = filePath.Split('\\');
                            string name = splits[splits.Length - 1];
                            newcol.Name = name.Replace(".col","");
                            cols.Add(newcol);
                        }
                        catch (Exception ex2) { MessageBox.Show(ex2.Message); }
                    }
                    finally
                    {
                        if (reader != null)
                            reader.Close();
                    }
                }
                else 
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
                        var tempCols = JsonConvert.DeserializeObject<List<ETABSv18_To_ACE.Column>>(fileContents, settings);
                        cols = tempCols.Select(c => new Column(c)).ToList();
                        //var newcol = JsonConvert.DeserializeObject<ETABSv18_To_ACE.Column>(fileContents, settings);
                        //cols.Add(new Column(newcol));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        try
                        {
                            var settings = new JsonSerializerSettings()
                            {
                                TypeNameHandling = TypeNameHandling.Objects,
                                PreserveReferencesHandling = PreserveReferencesHandling.Objects
                            };
                            reader = new StreamReader(filePath);
                            var fileContents = reader.ReadToEnd();
                            var tempCols = JsonConvert.DeserializeObject<List<ETABSv17_To_ACE.Column>>(fileContents, settings);
                            cols = tempCols.Select(c => new Column(c)).ToList();
                            //var newcol = JsonConvert.DeserializeObject<ETABSv17_To_ACE.Column>(fileContents, settings);
                            //cols.Add(new Column(newcol));
                        }
                        catch (Exception ex2) { MessageBox.Show(ex2.Message); }
                    }
                    finally
                    {
                        if (reader != null)
                            reader.Close();
                    }
                }
                
            }
            MainWindow mainWindow = new MainWindow();
            //MessageBox.Show(String.Format("cols count : {0}", cols.Count));

            if (cols.Count == 0)
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
