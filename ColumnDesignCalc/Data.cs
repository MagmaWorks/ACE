using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColumnDesignCalc
{
    public class FireData
    {
        public int R;
        public double mu;
        public FireExposition sidesExposed;
        public int minDimension;
        public int axisDistance;

        public FireData(int r, double m, int mindim, int a, FireExposition e = FireExposition.MoreThanOneSide)
        {
            R = r;
            mu = m;
            sidesExposed = e;
            minDimension = mindim;
            axisDistance = a;
        }
    }

    public class ConcreteData
    {
        public double Temp;
        public AggregateType type;
        public double k;
        public double Ec1;
        public double Ecu1;
        public double density;

        public ConcreteData(double temp, double kk, double ec1, double ecu1, AggregateType t = AggregateType.Siliceous, double dens = 2500)
        {
            Temp = temp;
            type = t;
            k = kk;
            Ec1 = ec1;
            Ecu1 = ecu1;
            density = dens;
        }
    }

    public class SteelData
    {
        public double Temp;
        public double kf;
        public double kE;

        public SteelData(double t, double f, double E)
        {
            Temp = t;
            kf = f;
            kE = E;
        }
    }
}
