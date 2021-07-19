using BatchDesign;
using ColumnDesignCalc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ColumnDesign
{
    /// <summary>
    /// Class to save column in a format compatible with the SCaFFOLD column calc
    /// </summary>
    public class CalcsColumn
    {
        public List<CalcsValue> Inputs = new List<CalcsValue>();
        public string InstanceName = "";
        public string ClassName = "TestCalcs.ColumnDesign";
        public string TypeName = "TestCalcs.ColumnDesign";
        
        public CalcsColumn(Column c)
        {
            Inputs.Add(new CalcsValue("Lx", c.LX.ToString()));
            Inputs.Add(new CalcsValue("Ly", c.LY.ToString()));
            Inputs.Add(new CalcsValue("Length", c.Length.ToString()));
            Inputs.Add(new CalcsValue("Angle", c.Angle.ToString()));
            Inputs.Add(new CalcsValue("Concrete grade", c.ConcreteGrade.Name));
            Inputs.Add(new CalcsValue("Max agg. size", c.MaxAggSize.ToString()));
            Inputs.Add(new CalcsValue("MxTop", c.SelectedLoad.MxTop.ToString()));
            Inputs.Add(new CalcsValue("MxBot", c.SelectedLoad.MxBot.ToString()));
            Inputs.Add(new CalcsValue("MyTop", c.SelectedLoad.MyTop.ToString()));
            Inputs.Add(new CalcsValue("MyBot", c.SelectedLoad.MyBot.ToString()));
            Inputs.Add(new CalcsValue("AxialLoad", c.SelectedLoad.P.ToString()));
            Inputs.Add(new CalcsValue("Effective length", c.EffectiveLength.ToString()));
            Inputs.Add(new CalcsValue("Cover to links", c.CoverToLinks.ToString()));
            Inputs.Add(new CalcsValue("Bar diameter", c.BarDiameter.ToString()));
            Inputs.Add(new CalcsValue("Link diameter", c.LinkDiameter.ToString()));
            Inputs.Add(new CalcsValue("NRebarX", c.NRebarX.ToString()));
            Inputs.Add(new CalcsValue("NRebarY", c.NRebarY.ToString()));
            Inputs.Add(new CalcsValue("R", c.R.ToString()));
        }
    }

    public class CalcsValue
    {
        public string Name { get; set; }
        public string ValueAsString { get; set; }

        public CalcsValue(string name, string val)
        {
            Name = name;
            ValueAsString = val;
        }
    }

    public class ProjectPlaceholder
    {
        public List<Column> Columns { get; set; }
        public Column SelectedColumn { get; set; }
        public Settings Settings { get; set; }
        public List<RebarPosition> AdvancedRebarPos { get; set; }
        public BatchColumnDesign BatchDesign { get; set; }
        public List<string> ColumnsInReport { get; set; }
    }
}
