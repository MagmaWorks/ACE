using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
//using FireDesign;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using Column = ColumnDesignCalc.Column;
using ColumnDesignCalc;
using CalcCore;
using BatchDesign;

namespace ColumnDesign
{
    public class ViewModel : ViewModelBase
    {
        public bool initializing = true;

        string projectName = "New Project";
        public string ProjectName
        {
            get { return projectName; }
            set { projectName = value; RaisePropertyChanged(nameof(ProjectName)); }
        }

        List<Column> myColumns = new List<Column>();
        public List<Column> MyColumns
        {
            get { return myColumns; }
            set
            {
                myColumns = value;
                RaisePropertyChanged(nameof(MyColumns));
            }
        }

        IDView myIDView = new IDView();
        public IDView MyIDView
        {
            get { return myIDView; }
            set { myIDView = value; RaisePropertyChanged(nameof(MyIDView)); }
        }

        LayoutView myLayoutView = new LayoutView();
        public LayoutView MyLayoutView
        {
            get { return myLayoutView; }
            set { myLayoutView = value; RaisePropertyChanged(nameof(MyLayoutView)); }
        }

        ProjectView myProjectView = new ProjectView();
        public ProjectView MyProjectView
        {
            get { return myProjectView; }
            set { myProjectView = value; RaisePropertyChanged(nameof(MyProjectView)); }
        }

        Calculations columnCalcs = new Calculations();
        public Calculations ColumnCalcs
        {
            get { return columnCalcs; }
            set { columnCalcs = value; RaisePropertyChanged(nameof(ColumnCalcs)); }
        }

        public List<Formula> CalcExpressions { get => columnCalcs.Expressions; }

        
        // Design checks
        bool sectionCheck = false;
        public bool SectionCheck
        {
            get{return sectionCheck;}
            set { sectionCheck = value; RaisePropertyChanged(nameof(SectionCheck)); }
        }
        
        bool fireCheck = false;
        public bool FireCheck
        {
            get { return fireCheck; }
            set { fireCheck = value; RaisePropertyChanged(nameof(FireCheck)); }
        }

        bool spacingCheck = false;
        public bool SpacingCheck
        {
            get { return spacingCheck; }
            set { spacingCheck = value; RaisePropertyChanged(nameof(SpacingCheck)); }
        }

        bool minMaxSteelCheck = false;
        public bool MinMaxSteelCheck
        {
            get { return minMaxSteelCheck; }
            set { minMaxSteelCheck = value; RaisePropertyChanged(nameof(MinMaxSteelCheck)); }
        }

        bool minRebarCheck = false;
        public bool MinRebarCheck
        {
            get { return minRebarCheck; }
            set { minRebarCheck = value; RaisePropertyChanged(nameof(MinRebarCheck)); }
        }

        double concreteCarbon = 0;
        public double ConcreteCarbon
        {
            get { return concreteCarbon; }
            set { concreteCarbon = value; RaisePropertyChanged(nameof(ConcreteCarbon)); }
        }

        double rebarCarbon = 0;
        public double RebarCarbon
        {
            get { return rebarCarbon; }
            set { rebarCarbon = value; RaisePropertyChanged(nameof(RebarCarbon)); }
        }

        double totalCarbon = 0;
        public double TotalCarbon
        {
            get { return totalCarbon; }
            set { totalCarbon = value; RaisePropertyChanged(nameof(TotalCarbon)); }
        }

        Column selectedColumn;
        public Column SelectedColumn
        {
            get { return selectedColumn ; }
            set { selectedColumn = value ?? myColumns.FirstOrDefault(c => c!= null);
                  UpdateDesign();
                  UpdateProjectView();
                  RaisePropertyChanged(nameof(SelectedColumn));
            }
        }

        public List<string> ConcreteNames { get => columnCalcs.ConcreteGrades.Select(c => c.Name).ToList(); }

        public List<string> SteelNames { get => columnCalcs.SteelGrades.Select(c => c.Name).ToList(); }

        public List<int> NoBars { get; set; } = new List<int> { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        public List<int> BarDiameters { get; set; } = new List<int> { 10, 12, 16, 20, 25, 32, 40 };
        public List<int> LinkDiameters { get; set; } = new List<int> { 10, 12, 16, 20, 25, 32, 40 };

        public List<int> FireResistances { get; set; } = new List<int> { 30, 60, 90, 120, 180, 240 };
        List<string> fireDesignMethods = Enum.GetNames(typeof(FDesignMethod)).ToList(); //new List<string> { "Table", "Isotherm 500", "Zone method", "Advanced" };
        public List<string> FireDesignMethods
        {
            get { return fireDesignMethods; }
            set { fireDesignMethods = value; RaisePropertyChanged(nameof(FireDesignMethods)); }
        }
        public List<string> FireCurves { get; set; } = Enum.GetNames(typeof(FCurve)).ToList(); //new List<string> { "Standard curve", "Hydrocarbon" };

        //public List<string> IDReductions { get; set; } = new List<string> { "100 %", "95 %", "90 %", "85 %", "80 %" };
        public List<int> IDReductions { get; set; } = new List<int> { 100, 95, 90, 85, 80 };

        public Dictionary<double, double> CarbonData = new Dictionary<double, double>();

        Settings mySettings = new Settings();
        public Settings MySettings
        {
            get { return mySettings; }
            set { mySettings = value; RaisePropertyChanged(nameof(MySettings)); }
        }

        Column globalColumn;
        public Column GlobalColumn
        {
            get { return globalColumn; }
            set { globalColumn = value; RaisePropertyChanged(nameof(GlobalColumn)); }
        }

        bool isConcreteCustom = false;
        public bool IsConcreteCustom
        {
            get { return isConcreteCustom; }
            set { isConcreteCustom = value; RaisePropertyChanged(nameof(IsConcreteCustom)); }
        }

        bool isSteelCustom = false;
        public bool IsSteelCustom
        {
            get { return isSteelCustom; }
            set { isSteelCustom = value; RaisePropertyChanged(nameof(IsSteelCustom)); }
        }

        List<RebarPosition> advancedRebarPos = new List<RebarPosition>();
        public List<RebarPosition> AdvancedRebarPos
        {
            get { return advancedRebarPos; }
            set { advancedRebarPos = value; RaisePropertyChanged(nameof(AdvancedRebarPos)); }
        }

        BatchDesignView myBatchDesignView = new BatchDesignView();
        public BatchDesignView MyBatchDesignView
        {
            get { return myBatchDesignView; }
            set { myBatchDesignView = value; RaisePropertyChanged(nameof(MyBatchDesignView)); }
        }

        List<string> columnsInReport;
        public List<string> ColumnsInReport
        {
            get { return columnsInReport; }
            set { columnsInReport = value; RaisePropertyChanged(nameof(ColumnsInReport)); }
        }

        WordReportProgress reportProgress;
        public WordReportProgress ReportProgress
        {
            get { return reportProgress; }
            set { reportProgress = value; RaisePropertyChanged(nameof(ReportProgress)); }
        }

        bool displayOpenButtons = false;
        public bool DisplayOpenButtons
        {
            get { return displayOpenButtons; }
            set { displayOpenButtons = value; RaisePropertyChanged(nameof(DisplayOpenButtons)); }
        }

        public ViewModel()
        {
            MyLayoutView.myViewModel = this;
            //ValidationTests2 vt = new ValidationTests2(); 
        }

        public void UpdateColumn()
        {
            RaisePropertyChanged(nameof(SelectedColumn));
        }

        public void RefreshColumns()
        {
            RaisePropertyChanged(nameof(SelectedColumn));
            RaisePropertyChanged(nameof(GlobalColumn));
        }

        public void UpdateDesign(bool updateFire = true)
        {
            if (initializing) return;
            this.SelectedColumn.GetInteractionDiagram();
            if(updateFire) UpdateFire();
            UpdateCalculation();
            this.myLayoutView.UpdateLayout(this.selectedColumn);
            this.myIDView.UpdateIDHull(this.selectedColumn);
            RaisePropertyChanged(nameof(SectionCheck));
            RaisePropertyChanged(nameof(MyIDView));
        }

        public void UpdateProjectView()
        {
            myProjectView.UpdateProjectView(myColumns, selectedColumn);
            RaisePropertyChanged(nameof(MyProjectView));
        }

        public void UpdateLoad()
        {
            if (initializing) return;
            UpdateCalculation();
            this.myIDView.UpdateIDHull(this.selectedColumn);
            RaisePropertyChanged(nameof(MyIDView));
            RaisePropertyChanged(nameof(SectionCheck));
            RaisePropertyChanged(nameof(SelectedColumn));
        }

        public void UpdateFire(bool updateContours = true)
        {
            if (initializing) return;
            Column col = this.SelectedColumn;
            updateContours = this.SelectedColumn?.TP?.ContourPts == null ? true : updateContours;
            switch (col.FireDesignMethod)
            {
                case (FDesignMethod.Isotherm_500):
                    if(updateContours) col.UpdateTP();
                    MyLayoutView.LoadGraph(col);
                    break;
                case (FDesignMethod.Zone_Method):
                    if (updateContours) col.UpdateTP();
                    MyLayoutView.LoadGraph(col);
                    break;
                case (FDesignMethod.Advanced):
                    columnCalcs.UpdateFireID(updateContours);
                    MyLayoutView.LoadGraph(col);
                    break;
            }
        }

        public void UpdateCalculation(bool all = false)
        {
            if (initializing) return;
            if (this.selectedColumn != null)
            {
                columnCalcs.Column = this.selectedColumn;
                columnCalcs.InitExpressions();
                MinMaxSteelCheck = columnCalcs.CheckMinMaxSteel();
                SpacingCheck = columnCalcs.UpdateSecondOrderCheck();
                SectionCheck = SelectedColumn.isInsideCapacity();
                columnCalcs.GetLinkSpacing();
                FireCheck = columnCalcs.UpdateFireDesign();
                columnCalcs.AddInteractionDiagramFormulae();
                MinRebarCheck = SelectedColumn.CheckMinRebarNo();
                SelectedColumn.CheckGuidances();
                GetEmbodiedCarbon();
                this.SelectedColumn.GetUtilisation();
                this.SelectedColumn.Get2DMaps();
                //RaisePropertyChanged(nameof(SelectedColumn));
                RaisePropertyChanged(nameof(CalcExpressions));
            }
        
        }

        private void GetEmbodiedCarbon()
        {
            var carbon = columnCalcs.GetEmbodiedCarbon();

            ConcreteCarbon = carbon[0];
            RebarCarbon = carbon[1];
            TotalCarbon = carbon[2];

        }


        public void SaveAs()
        {
            CalcsColumn cc = new CalcsColumn(SelectedColumn);

            var saveObj = Newtonsoft.Json.JsonConvert.SerializeObject(SelectedColumn, Newtonsoft.Json.Formatting.Indented);
            var saveObj_Calcs = Newtonsoft.Json.JsonConvert.SerializeObject(cc, Newtonsoft.Json.Formatting.Indented);
            try
            {
                var saveDialog = new SaveFileDialog();
                saveDialog.Filter = @"ACE files |*.col";
                saveDialog.FileName = SelectedColumn.Name + @".col";
                Properties.Settings.Default.Reload();
                if (saveDialog.ShowDialog() != DialogResult.OK) return;

                var filePath = saveDialog.FileName;
                var filePath_Calcs = saveDialog.FileName.Replace(".col",".json");
                System.IO.File.WriteAllText(filePath, saveObj);
                System.IO.File.WriteAllText(filePath_Calcs, saveObj_Calcs);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Oops..." + Environment.NewLine + ex.Message);
                return;
            }
            
        }

        public void SaveAll()
        {
            var saveFolder = new FolderBrowserDialog();
            DialogResult result = saveFolder.ShowDialog();
            Properties.Settings.Default.Reload();
            if (result != DialogResult.OK) return;
            try
            {
                string folderName = saveFolder.SelectedPath;
                for (int i = 0; i < MyColumns.Count; i++)
                {
                    CalcsColumn cc = new CalcsColumn(MyColumns[i]);
                    var saveObj = Newtonsoft.Json.JsonConvert.SerializeObject(MyColumns[i], Newtonsoft.Json.Formatting.Indented);
                    var saveObj_Calcs = Newtonsoft.Json.JsonConvert.SerializeObject(cc, Newtonsoft.Json.Formatting.Indented);
                    string filePath = folderName + @"\\" + myColumns[i].Name + ".col";
                    string filePath_Calcs = filePath.Replace(".col", ".json");
                    System.IO.File.WriteAllText(filePath, saveObj);
                    System.IO.File.WriteAllText(filePath_Calcs, saveObj_Calcs);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Oops..." + Environment.NewLine + ex.Message);
                return;
            }
        }

        public void SaveProject()
        {
            var placeholder = CreatePH();
            var saveObj = Newtonsoft.Json.JsonConvert.SerializeObject(placeholder, Newtonsoft.Json.Formatting.Indented);

            var saveDialog = new SaveFileDialog();
            saveDialog.Filter = @"ACE files |*.ace";
            saveDialog.FileName = SelectedColumn.Name + @".ace";
            Properties.Settings.Default.Reload();
            if (saveDialog.ShowDialog() != DialogResult.OK) return;

            var filePath = saveDialog.FileName;
            System.IO.File.WriteAllText(filePath, saveObj);
        }

        public ProjectPlaceholder CreatePH()
        {
            return new ProjectPlaceholder()
            {
                Columns = myColumns,
                SelectedColumn = selectedColumn,
                Settings = mySettings,
                AdvancedRebarPos = advancedRebarPos,
                BatchDesign = myBatchDesignView.BatchDesign,
                ColumnsInReport = columnsInReport
            };
        }

        public void Open()
        {
            var openDialog = new OpenFileDialog();
            openDialog.Filter = @"ACE files |*.col| All files (*.*)|*.*";
            openDialog.Multiselect = true;
            Properties.Settings.Default.Reload();
            if (openDialog.ShowDialog() != DialogResult.OK) return;
            List<Column> newCols = new List<Column>();
            newCols.AddRange(OpenDesigns(openDialog.FileNames).ToList());
            MyColumns = newCols;
            Column c = MyColumns[Convert.ToInt32(Math.Min(newCols.Count - 1, 1))];
            c.FDMStr = "Table";
            SelectedColumn = c; 
        }

        public void OpenProject()
        {
            var openDialog = new OpenFileDialog();
            openDialog.Filter = @"ACE files |*.ace| All files (*.*)|*.*";
            openDialog.Multiselect = false;
            Properties.Settings.Default.Reload();
            if (openDialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                string openObj = System.IO.File.ReadAllText(openDialog.FileName);
                ProjectPlaceholder ph = Newtonsoft.Json.JsonConvert.DeserializeObject<ProjectPlaceholder>(openObj);

                MyColumns = ph.Columns;
                SelectedColumn = ph.SelectedColumn;
                mySettings = ph.Settings;
                advancedRebarPos = ph.AdvancedRebarPos;
                MyBatchDesignView.BatchDesign = ph.BatchDesign;
                columnsInReport = ph.ColumnsInReport;
            }
            catch(Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }

        public void OpenAdd()
        {
            var openDialog = new OpenFileDialog();
            openDialog.Filter = @"ACE files |*.col| All files (*.*)|*.*";
            openDialog.Multiselect = true;
            Properties.Settings.Default.Reload();
            if (openDialog.ShowDialog() != DialogResult.OK) return;
            List<Column> newCols = new List<Column>(); 
            newCols.AddRange(OpenDesigns(openDialog.FileNames).ToList());
            MyColumns = newCols;
            SelectedColumn = MyColumns[MyColumns.Count - 1];
        }

        public IEnumerable<Column> OpenDesigns(string[] fileNames)
        {
            for(int i = 0; i < fileNames.Length; i++)
            {
                string openObj = System.IO.File.ReadAllText(fileNames[i]);

                Column newCol = new Column();

                string[] splits = fileNames[i].Split('\\');
                string name = splits[splits.Length - 1];
                if (name.ToLower().Contains(".json"))
                {
                    var deserialiseType = new { InstanceName = "", TypeName = "", ClassName = "", Inputs = new List<CalcsValue>() };
                    var dObj = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(openObj, deserialiseType);
                    
                    newCol = new Column()
                    {
                        Name = name.Substring(0,name.Length-5),
                        LX = Convert.ToDouble(dObj.Inputs.First(x => x.Name == "Lx").ValueAsString),
                        LY = Convert.ToDouble(dObj.Inputs.First(x => x.Name == "Ly").ValueAsString),
                        Length = Convert.ToDouble(dObj.Inputs.First(x => x.Name == "Length").ValueAsString),
                        Angle = Convert.ToDouble(dObj.Inputs.First(x => x.Name == "Angle").ValueAsString),
                        Diameter = Convert.ToDouble(dObj.Inputs.First(x => x.Name == "Diameter").ValueAsString),
                        ConcreteGrade = columnCalcs.ConcreteGrades.First(c => c.Name == (dObj.Inputs.First(x => x.Name == "Concrete grade").ValueAsString)),
                        MaxAggSize = Convert.ToDouble(dObj.Inputs.First(x => x.Name == "Max agg. size").ValueAsString),
                        EffectiveLength = Convert.ToDouble(dObj.Inputs.First(x => x.Name == "Effective length").ValueAsString),
                        CoverToLinks = Convert.ToDouble(dObj.Inputs.First(x => x.Name == "Cover to links").ValueAsString),
                        BarDiameter = Convert.ToInt32(dObj.Inputs.First(x => x.Name == "Bar diameter").ValueAsString),
                        LinkDiameter = Convert.ToInt32(dObj.Inputs.First(x => x.Name == "Link diameter").ValueAsString),
                        NRebarX = Convert.ToInt32(dObj.Inputs.First(x => x.Name == "NRebarX").ValueAsString),
                        NRebarY = Convert.ToInt32(dObj.Inputs.First(x => x.Name == "NRebarY").ValueAsString),
                        R = Convert.ToInt32(dObj.Inputs.First(x => x.Name == "R").ValueAsString),
                    };
                    // shape
                    if (Convert.ToBoolean(dObj.Inputs.First(x => x.Name == "isRectangular").ValueAsString))
                        newCol.Shape = GeoShape.Rectangular;
                    else if(Convert.ToBoolean(dObj.Inputs.First(x => x.Name == "isCircular").ValueAsString))
                        newCol.Shape = GeoShape.Circular;

                    newCol.Loads.Add( new Load()
                    {
                        Name = "LOAD",
                        MxTop = Convert.ToDouble(dObj.Inputs.First(x => x.Name == "MxTop").ValueAsString),
                        MxBot = Convert.ToDouble(dObj.Inputs.First(x => x.Name == "MxBot").ValueAsString),
                        MyTop = Convert.ToDouble(dObj.Inputs.First(x => x.Name == "MyTop").ValueAsString),
                        MyBot = Convert.ToDouble(dObj.Inputs.First(x => x.Name == "MyBot").ValueAsString),
                        P = Convert.ToDouble(dObj.Inputs.First(x => x.Name == "AxialLoad").ValueAsString),
                    });
                    newCol.SelectedLoad = newCol.Loads[0];
                }
                else
                {
                    newCol = Newtonsoft.Json.JsonConvert.DeserializeObject<Column>(openObj);
                    newCol.Name = name.Substring(0, name.Length - 4);
                }

                yield return newCol;
            }
        }

        public void ExportToWord()
        {
            //Progress<WordReportProgress> progress = new Progress<WordReportProgress>(AsyncReportProgress);

            //ExportToWordAsync();
            //await Task.Run(() => ExportToWordAsync(progress));
            ExportToWordAsync();
        }

        //public async void ExportToWordAsync(IProgress<WordReportProgress> progress)
        public void ExportToWordAsync()
        {
            List<ICalc> calcs = new List<ICalc>();
            if (columnsInReport == null) columnsInReport = new List<string> { SelectedColumn.Name };
            List<Column> cols = columnsInReport.Select(n => myColumns.First(c => c.Name == n)).ToList();

            int k = 0;

            for (int n = 0; n < cols.Count; n++)
            {
                Column c = cols[n];
                if(mySettings.ExprtdLoads == ExportedLoads.Current)
                {
                    //int nloadtot = cols.Count;
                    k++;
                    Calculations calc = new Calculations();
                    calc.Column = c;
                    calc.UpdateInputOuput();
                    calc.UpdateCalc();
                    IDView.Generate2DIDs(c);
                    calc.AddInteractionDiagrams();
                    calcs.Add(calc);
                    //progress.Report(new WordReportProgress()
                    //{
                    //    Progress = Convert.ToInt32(k * 1.0 / nloadtot) * 100,
                    //    Message = string.Format("Preparing report for column {0} - load {1}", c.Name, c.SelectedLoad.Name)
                    //});
                }
                else if(mySettings.ExprtdLoads == ExportedLoads.DesigningLoads)
                {
                    //int nloadtot = cols.SelectMany(x => x.DesigningLoads).Count();
                    c.GetDesigningLoads(mySettings.NumLoads);
                    for(int i = 0; i < c.DesigningLoads.Count; i++)
                    {
                        k++;
                        c.SelectedLoad = c.DesigningLoads[i];
                        Calculations calc = new Calculations();
                        calc.Column = c;
                        calc.UpdateInputOuput();
                        calc.InstanceName = string.Format("{0} - {1}", c.Name, c.DesigningLoads[i].Name);
                        calc.UpdateCalc();
                        IDView.Generate2DIDs(c);
                        calc.AddInteractionDiagrams();
                        calcs.Add(calc);
                        //progress.Report(new WordReportProgress()
                        //{
                        //    Progress = Convert.ToInt32(k * 1.0 / nloadtot) * 100,
                        //    Message = string.Format("Preparing report for column {0} - load {1}", c.Name, c.DesigningLoads[i].Name)
                        //});
                    }
                }
            }

            if (mySettings.CombinedReport || mySettings.ExprtdCols == ExportedColumns.Current)
            {
                OutputToODT.WriteToODT(calcs, true, true, true, mySettings);
                //progress.Report(new WordReportProgress()
                //{
                //    Progress = 100,
                //    Message = "Writting report...",
                //});
            }
            else
            {
                OutputToODT.WriteToODT2(calcs, true, true, true, mySettings);
                //progress.Report(new WordReportProgress()
                //{
                //    Progress = 100,
                //    Message = "Writting report...",
                //});
            }
            //}
            //else
            //    OutputToODT.WriteToODT(columnCalcs, true, true, true, mySettings);
        }


        void AsyncReportProgress(WordReportProgress value)
        {
            ReportProgress = value;
        }

    }

    public class RebarPosition
    {
        public double X { get; set; }
        public double Y { get; set; }

        public RebarPosition(double x, double y)
        {
            X = x;
            Y = y;
        }

        public RebarPosition()
        {
            X = 0;
            Y = 0;
        }
    }
}
