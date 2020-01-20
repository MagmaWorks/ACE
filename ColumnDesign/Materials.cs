using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColumnDesign
{
    public class Concrete
    {
        public string Name { get; set; }
        public double Fc { get; set; }
        public double E { get; set; }
        public double Density { get; set; } = 2500;

        public Concrete(string n, double f, double e)
        {
            Name = n;
            Fc = f;
            E = e;
        }

        public Concrete(string n)
        {
            Name = n;
        }

        public Concrete()
        {

        }
    }

    public class Steel
    {
        public string Name { get; set; }
        public double Fy { get; set; }
        public double E { get; set; }

        public Steel(string n, double f, double e = 200)
        {
            Name = n;
            Fy = f;
            E = e;
        }

        public Steel(string n)
        {
            Name = n;
        }

        public Steel()
        {

        }
    }
}
