using ColumnDesignCalc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColumnDesign
{
    public class Settings
    {
        public string ReportBy { get; set; } = Environment.UserName;
        public string ReportCheckedBy { get; set; } = "";
        public bool CombinedReport { get; set; } = false;
        public bool AllCalcs { get; set; } = false;
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
