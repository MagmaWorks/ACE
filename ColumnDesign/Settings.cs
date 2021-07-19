using ColumnDesignCalc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColumnDesign
{
    public enum ExportedColumns { All, Current, AllClusterCols, Selection}
    public enum ExportedLoads { Current, DesigningLoads }
    public class Settings
    {
        public string ReportBy { get; set; } = Environment.UserName;
        public string ReportCheckedBy { get; set; } = "";
        public bool CombinedReport { get; set; } = false;
        public int NumLoads { get; set; } = 3;
        public ExportedColumns ExprtdCols { get; set; } = ExportedColumns.Current;
        public ExportedLoads ExprtdLoads { get; set; } = ExportedLoads.Current;

        public bool IsAllCols { get => ExprtdCols == ExportedColumns.All; }
        public bool IsCurrentCol { get => ExprtdCols == ExportedColumns.Current; }
        public bool IsAllClusterCols { get => ExprtdCols == ExportedColumns.AllClusterCols; }
        public bool IsSelectionCols { get => ExprtdCols == ExportedColumns.Selection; }
        public bool IsCurrentLoad { get => ExprtdLoads == ExportedLoads.Current; }
        public bool IsDesigningLoads { get => ExprtdLoads == ExportedLoads.DesigningLoads; }
        public Column DefaultColumn { get; set; } = new Column()
        {
            Name = "Col 350x350",
            LX = 350,
            LY = 350,
            Length = 3000,
            ConcreteGrade = new Concrete("50/60", 50, 37),
            SteelGrade = new Steel("500B", 500),
            SelectedLoad = new Load() { Name = "default", P = 500 },
            FireLoad = new Load() { Name = "default", P = 500 },
            Loads = new List<Load>() { new Load() { Name = "default", P = 500 } }
        };
    }
}
