using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColumnDesignCalc
{
    public class Load
    {
        public string Name { get; set; }
        public double MxTop { get; set; } = 0;
        public double MxBot { get; set; } = 0;
        public double MyTop { get; set; } = 0;
        public double MyBot { get; set; } = 0;
        public double P { get; set; } = 0;

        public double Mxd { get; set; } = 0;
        public double Myd { get; set; } = 0;

        public Load()
        {

        }

        public Load(ETABSColumnDesign_Plugin.Load l)
        {
            Name = l.Name;
            MxTop = Math.Round(l.MxTop);
            MxBot = Math.Round(l.MxBot);
            MyTop = Math.Round(l.MyTop);
            MyBot = Math.Round(l.MyBot);
            P = -Math.Round(l.P);
        }

        public Load Clone()
        {
            return new Load()
            {
                Name = this.Name,
                MxTop = this.MxTop,
                MxBot = this.MxBot,
                MyTop = this.MyTop,
                MyBot = this.MyBot,
                P = this.P
            };
        }

    }
}
