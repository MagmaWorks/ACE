﻿using HelixToolkit.Wpf;
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

namespace ColumnDesign
{
    public class ViewModel : ViewModelBase
    {
        public bool initializing = true;
        
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

        public bool CanColumnBeDeleted { get { return MyColumns.Count > 1; } }

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

        //CalcuationView myCalcView = new CalcuationView();
        //public CalcuationView MyCalcView
        //{
        //    get { return myCalcView; }
        //    set { myCalcView = value; RaisePropertyChanged(nameof(MyCalcView)); }
        //}

        Calculations columnCalcs = new Calculations();
        public Calculations ColumnCalcs
        {
            get { return columnCalcs; }
            set { columnCalcs = value; RaisePropertyChanged(nameof(ColumnCalcs)); }
        }

        public List<Formula> CalcExpressions { get => columnCalcs.Expressions; }


        FireDesignView myFireDesignView = new FireDesignView();
        public FireDesignView MyFireDesignView
        {
            get { return myFireDesignView; }
            set { myFireDesignView = value; RaisePropertyChanged(nameof(MyFireDesignView)); }
        }

        // Design checks
        bool sectionCheck = false;
        public bool SectionCheck
        {
            get
            {
                //sectionCheck = SelectedColumn?.isInsideCapacity() ?? false;
                //return SelectedColumn?.isInsideCapacity() ?? false;
                return sectionCheck;
            }
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

        string nameSelectedColumn = "";
        public string NameSelectedColumn
        {
            get { return nameSelectedColumn; }
            set
            {
                nameSelectedColumn = value;
                RaisePropertyChanged(nameof(NameSelectedColumn));
                SelectedColumn = MyColumns.FirstOrDefault(c => c.Name == nameSelectedColumn);
            }
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


        public ViewModel()
        {
            MyLayoutView.myViewModel = this;
            //ValidationTests2 vt = new ValidationTests2(); 
        }

        public void UpdateColumn()
        {
            RaisePropertyChanged(nameof(SelectedColumn));
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
                //myCalcView.column = this.selectedColumn;
                //myCalcView.Formulae = new List<FormulaeVM>();
                ////myFireDesignView.LoadGraph(this.selectedColumn);
                //MinMaxSteelCheck = myCalcView.UpdateMinMaxSteelCheck(this.SelectedColumn);
                //SpacingCheck = myCalcView.UpdateSecondOrderCheck();
                //FireCheck = myCalcView.UpdateFireDesign(this.SelectedColumn);
                //MinRebarCheck = SelectedColumn.CheckMinRebarNo();
                //SelectedColumn.CheckGuidances();
                //GetEmbodiedCarbon();
                //this.SelectedColumn.GetUtilisation();
                //RaisePropertyChanged(nameof(SelectedColumn));

                columnCalcs.Column = this.selectedColumn;
                columnCalcs.InitExpressions();
                //myFireDesignView.LoadGraph(this.selectedColumn);
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
                RaisePropertyChanged(nameof(SelectedColumn));
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

        public void Save()
        {
            var saveObj = Newtonsoft.Json.JsonConvert.SerializeObject(myColumns, Newtonsoft.Json.Formatting.Indented);
            try
            {
                var filePath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\ColumnDesign\Temp\columnData.JSON";
                System.IO.File.WriteAllText(filePath, saveObj);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Oops..." + Environment.NewLine + ex.Message);
                return;
            }
        }

        public void SaveAs()
        {
            CalcsColumn cc = new CalcsColumn(SelectedColumn);

            var saveObj = Newtonsoft.Json.JsonConvert.SerializeObject(SelectedColumn, Newtonsoft.Json.Formatting.Indented);
            var saveObj_Calcs = Newtonsoft.Json.JsonConvert.SerializeObject(cc, Newtonsoft.Json.Formatting.Indented);
            try
            {
                var saveDialog = new SaveFileDialog();
                //saveDialog.Filter = @"JSON files |*.JSON";
                saveDialog.Filter = @"ACE files |*.col";
                //saveDialog.FileName = "Col_" + SelectedColumn.Name + @".JSON";
                saveDialog.FileName = SelectedColumn.Name + @".col";

                //saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
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
            if (result != DialogResult.OK) return;
            try
            {
                string folderName = saveFolder.SelectedPath;
                for (int i = 0; i < MyColumns.Count; i++)
                {
                    CalcsColumn cc = new CalcsColumn(MyColumns[i]);
                    var saveObj = Newtonsoft.Json.JsonConvert.SerializeObject(MyColumns[i], Newtonsoft.Json.Formatting.Indented);
                    var saveObj_Calcs = Newtonsoft.Json.JsonConvert.SerializeObject(cc, Newtonsoft.Json.Formatting.Indented);
                    //string filePath = folderName + @"\\Col_" + myColumns[i].Name + ".JSON";
                    string filePath = folderName + @"\\" + myColumns[i].Name + ".col";
                    //string filePath_Calcs = folderName + @"\\Col_" + myColumns[i].Name + "_Calcs.JSON";
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

        public void Open()
        {
            var openDialog = new OpenFileDialog();
            openDialog.Filter = @"ACE files |*.JSON;*.col";
            openDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            openDialog.Multiselect = true;
            if (openDialog.ShowDialog() != DialogResult.OK) return;
            List<Column> newCols = new List<Column>();
            newCols.AddRange(OpenDesigns(openDialog.FileNames).ToList());
            //if (!newCols.Select(x => x.Name).Contains(customCol.Name)) newCols.Insert(0, customCol);
            MyColumns = newCols;
            Column c = MyColumns[Convert.ToInt32(Math.Min(newCols.Count - 1, 1))];
            c.FDMStr = "Table";
            SelectedColumn = c; // MyColumns[Convert.ToInt32(Math.Min(newCols.Count-1,1))];
            //NameSelectedColumn = SelectedColumn.Name;
        }

        public void OpenAdd()
        {
            var openDialog = new OpenFileDialog();
            openDialog.Filter = @"ACE files |*.JSON;*.col";
            openDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            openDialog.Multiselect = true;
            if (openDialog.ShowDialog() != DialogResult.OK) return;
            List<Column> newCols = myColumns.Select(c => c.Clone()).ToList();
            newCols.AddRange(OpenDesigns(openDialog.FileNames).ToList());
            MyColumns = newCols;
            SelectedColumn = MyColumns[MyColumns.Count - 1];
            //NameSelectedColumn = SelectedColumn.Name;
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
            if(mySettings.AllCalcs)
            {
                List<ICalc> calcs = new List<ICalc>();
                foreach (var c in myColumns)
                {
                    Calculations calc = new Calculations();
                    calc.Column = c;
                    calc.UpdateInputOuput();
                    IDView.Generate2DIDs(c);
                    calc.UpdateCalc();
                    calcs.Add(calc);
                }
                
                if (mySettings.CombinedReport)
                    OutputToODT.WriteToODT(calcs, true, true, true, mySettings);
                else
                    OutputToODT.WriteToODT2(calcs, true, true, true, mySettings);
            }
            else
                OutputToODT.WriteToODT(columnCalcs, true, true, true, mySettings);
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
