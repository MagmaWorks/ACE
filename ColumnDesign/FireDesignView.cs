using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColumnDesignCalc;
//using FireDesign;
using OxyPlot;
using OxyPlot.Series;

namespace ColumnDesign
{
    public class FireDesignView : ViewModelBase
    {
        public TemperatureProfile TP;
        PlotModel plotModel;
        public PlotModel PlotModel
        {
            get { return plotModel; }
            set { plotModel = value; RaisePropertyChanged(nameof(PlotModel)); }
        }

        public void LoadGraph(Column c)
        {
            plotModel = new PlotModel();
            TP = new TemperatureProfile(c.LX / 1000, c.LY / 1000, c.R * 60, c.FireCurve);
            //generate values
            var cs = new ContourSeries
            {
                Color = OxyColors.Black,
                LabelBackground = OxyColors.White,
                ColumnCoordinates = TP.Y.AsArray(),
                RowCoordinates = TP.X.AsArray(),
                Data = TP.Temp.ToArray()
            };
            cs.RenderInLegend = false;
            cs.ContourLevelStep = 100;
            plotModel.Series.Add(cs);
            RaisePropertyChanged(nameof(PlotModel));

        }

        public void AdvancedFireDesign(Column c)
        {
            LoadGraph(c);

        }
    }
}
