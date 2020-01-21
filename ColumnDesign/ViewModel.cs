using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using FireDesign;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

namespace ColumnDesign
{
    public class ViewModel : ViewModelBase
    {

        
        List<Column> myColumns = new List<Column>();
        public List<Column> MyColumns
        {
            get { return myColumns; }
            set
            {
                myColumns = value;
                RaisePropertyChanged(nameof(MyColumns));
                RaisePropertyChanged(nameof(ColumnNames));
            }
        }

        private Column customCol = new Column()
        {
            Name = "default",
            LX = 350,
            LY = 350,
            Length = 3000,
            CustomConcreteGrade = new Concrete("Custom", 50, 37),
            ConcreteGrade = new Concrete("50/60", 50, 37),
            SelectedLoad = new Load() { Name = "default", P = 5000 },
            FireLoad = new Load() { Name = "default", P = 5000 },
            Loads = new List<Load>() { new Load() { Name = "default", P = 5000 } }
            //P = 5000
        };

        public ObservableCollection<string> ColumnNames { get { return new ObservableCollection<string>(MyColumns.Select(c => c.Name)); } }

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

        CalcuationView myCalcView = new CalcuationView();
        public CalcuationView MyCalcView
        {
            get { return myCalcView; }
            set { myCalcView = value; RaisePropertyChanged(nameof(MyCalcView)); }
        }

        FireDesignView myFireDesignView = new FireDesignView();
        public FireDesignView MyFireDesignView
        {
            get { return myFireDesignView; }
            set { myFireDesignView = value; RaisePropertyChanged(nameof(MyFireDesignView)); }
        }

        // Design checks
        //bool sectionCheck = false;
        public bool SectionCheck
        {
            get
            {
                //sectionCheck = SelectedColumn?.isInsideCapacity() ?? false;
                return SelectedColumn?.isInsideCapacity() ?? false;
                //return sectionCheck;
            }
            //set { sectionCheck = value; RaisePropertyChanged(nameof(SectionCheck)); }
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
            set { nameSelectedColumn = value;
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
            set { selectedColumn = value;
                  UpdateDesign();
                  RaisePropertyChanged(nameof(SelectedColumn));
            }
        }

        public List<string> ConcreteNames { get => ConcreteGrades.Select(c => c.Name).ToList(); }

        public List<Concrete> ConcreteGrades { get; } = new List<Concrete>()
        {
            new Concrete("Custom"),
            new Concrete("32/40",32,33),
            new Concrete("35/45",35,34),
            new Concrete("40/50",40,35),
            //new Concrete("45/55",45,36),
            new Concrete("50/60",50,37),
        };

        public List<string> SteelNames { get => SteelGrades.Select(c => c.Name).ToList(); }

        public List<Steel> SteelGrades { get; } = new List<Steel>()
        {
            new Steel("Custom",400),
            new Steel("500B",500)
        };

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

        const double concreteVolMass = 2.5e3;
        const double steelVolMass = 7.5e3;

        bool isCustom = true;
        public bool IsCustom
        {
            get { return isCustom; }
            set { isCustom = value; RaisePropertyChanged(nameof(IsCustom)); }
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

        public void UpdateDesign()
        {
            this.SelectedColumn.GetInteractionDiagram();
            UpdateFire();
            this.MyIDView.UpdateIDHull(this.selectedColumn);
            UpdateCalculation();
            this.MyLayoutView.UpdateLayout(this.selectedColumn);
            RaisePropertyChanged(nameof(SectionCheck));
            RaisePropertyChanged(nameof(MyIDView));
        }

        public void UpdateLoad()
        {
            UpdateCalculation();
            this.MyIDView.UpdateIDHull(this.selectedColumn);
            RaisePropertyChanged(nameof(MyIDView));
            RaisePropertyChanged(nameof(SectionCheck));
            RaisePropertyChanged(nameof(SelectedColumn));
        }

        public void UpdateFire(bool updateContours = true)
        {
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
                    col.UpdateFireID(updateContours);
                    MyLayoutView.LoadGraph(col);
                    break;
            }
            MyIDView.UpdateIDHull(col);
        }

        public void UpdateCalculation(bool all = false)
        {
            if(this.selectedColumn != null)
            {
                myCalcView.column = this.selectedColumn;
                myCalcView.Formulae = new List<FormulaeVM>();
                //myFireDesignView.LoadGraph(this.selectedColumn);
                MinMaxSteelCheck = myCalcView.UpdateMinMaxSteelCheck(this.SelectedColumn);
                SpacingCheck = myCalcView.UpdateSecondOrderCheck();
                FireCheck = myCalcView.UpdateFireDesign(this.SelectedColumn);
                MinRebarCheck = SelectedColumn.CheckMinRebarNo();
                SelectedColumn.CheckGuidances();
                GetEmbodiedCarbon();
                this.SelectedColumn.GetUtilisation();
                RaisePropertyChanged(nameof(SelectedColumn));
            }
        
        }

        private void GetEmbodiedCarbon()
        {
            var carbon = selectedColumn.GetEmbodiedCarbon();

            ConcreteCarbon = carbon[0];
            RebarCarbon = carbon[1];
            TotalCarbon = carbon[2];

        }

        private void InitializeCarbonData()
        {
            CarbonData.Add(32, 0.163);
            CarbonData.Add(40, 0.188);
            CarbonData.Add(50, 0.205);
            CarbonData.Add(60, 0.23);
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
                saveDialog.Filter = @"JSON files |*.JSON";
                saveDialog.FileName = "Col_" + SelectedColumn.Name + @".JSON";

                saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (saveDialog.ShowDialog() != DialogResult.OK) return;

                var filePath = saveDialog.FileName;
                var filePath_Calcs = saveDialog.FileName.Insert(saveDialog.FileName.Length - 5,"_Calcs");
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
                    string filePath = folderName + @"\\Col_" + myColumns[i].Name + ".JSON";
                    string filePath_Calcs = folderName + @"\\Col_" + myColumns[i].Name + "_Calcs.JSON";
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
            openDialog.Filter = @"Calc files |*.JSON";
            openDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            openDialog.Multiselect = true;
            if (openDialog.ShowDialog() != DialogResult.OK) return;
            List<Column> newCols = new List<Column>();
            newCols.AddRange(OpenDesigns(openDialog.FileNames).ToList());
            if (!newCols.Select(x => x.Name).Contains(customCol.Name)) newCols.Insert(0, customCol);
            MyColumns = newCols;
            SelectedColumn = MyColumns[Convert.ToInt32(Math.Min(newCols.Count-1,1))];
            NameSelectedColumn = SelectedColumn.Name;
        }

        public void OpenAdd()
        {
            var openDialog = new OpenFileDialog();
            openDialog.Filter = @"Calc files |*.JSON";
            openDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            openDialog.Multiselect = true;
            if (openDialog.ShowDialog() != DialogResult.OK) return;
            List<Column> newCols = myColumns.Select(c => c.Clone()).ToList();
            newCols.AddRange(OpenDesigns(openDialog.FileNames).ToList());
            MyColumns = newCols;
            SelectedColumn = MyColumns[MyColumns.Count - 1];
            NameSelectedColumn = SelectedColumn.Name;
        }

        public IEnumerable<Column> OpenDesigns(string[] fileNames)
        {
            for(int i = 0; i < fileNames.Length; i++)
            {
                string openObj = System.IO.File.ReadAllText(fileNames[i]);

                Column newCol = new Column();

                string[] splits = fileNames[i].Split('\\');
                string name = splits[splits.Length - 1];
                if (fileNames[i].Contains("Calcs"))
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
                        ConcreteGrade = ConcreteGrades.First(c => c.Name == (dObj.Inputs.First(x => x.Name == "Concrete grade").ValueAsString)),
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
                    newCol.Name = name.Substring(0, name.Length - 5);
                }

                yield return newCol;
            }
        }

        public void ExportToWord()
        {
            OutputToODT.WriteToODT(myCalcView, true, true, true);
        }
        
    }
}
