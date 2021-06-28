using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColumnDesignCalc
{
    public class Concrete
    {
        public static Concrete DefaultConcrete { get; set; } = new Concrete("C32/40", 32, 33);
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

        public Concrete Clone()
        {
            return new Concrete()
            {
                Name = this.Name,
                Fc = this.Fc,
                E = this.E,
                Density = this.Density,
            };
        }
    }

    public class Steel
    {
        public static Steel DefaultSteel { get; set; } = new Steel("500B", 500);
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

        public Steel Clone()
        {
            return new Steel()
            {
                Name = this.Name,
                Fy = this.Fy,
                E = this.E,
            };
        }
    }
}
