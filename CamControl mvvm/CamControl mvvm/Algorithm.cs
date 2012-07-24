using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CamControl_mvvm
{
    class Algorithm
    {
        //公式
//        %Coefficients (with 95% confidence bounds):
//       a =       484.4;%  (312.5, 656.3)
//       b =      -1.294;%  (-1.554, -1.035)
//       c =      -103.9;%  (-271.8, 63.91)
       

//      %General model Power2:
        //        y = a.*x.^b+c

        public int DistanceToLens28mm(Double dist, int offset) 
        {
            Double x = dist;
            Double a = 484.4;
            Double b = -1.294;
            Double c = -103.9;
            return (int)(a * Math.Pow(x, b) + c)+offset;
        }


//        General model Power2:   Rolling Counter
//       f(x) = a*x^b+c
//Coefficients (with 95% confidence bounds):
//       a =        1404  (1276, 1532)
//       b =      -1.165  (-1.257, -1.073)
//       c =      -50.67  (-151.1, 49.79)
        //public int DistanceToLens50mm(Double dist, int offset)
        //{
        //    Double x = dist;
        //    Double a = 1404;
        //    Double b = -1.165;
        //    Double c = -50.67;
        //    return (int)(a * Math.Pow(x, b) + c) + offset;
        //}

//        General model Power2:  Coder Counter
//       f(x) = a*x^b+c
//Coefficients (with 95% confidence bounds):
//       a =          57  (53.42, 60.59)
//       b =      -1.242  (-1.307, -1.177)
//       c =      -2.067  (-4.854, 0.7209)

//Goodness of fit:
//  SSE: 7.41
//  R-square: 0.9997
//  Adjusted R-square: 0.9996
//  RMSE: 1.111
        public int DistanceToLens50mm(Double dist, int offset)
        {
            Double x = dist;
            Double a = 57;
            Double b = -1.242;
            Double c = -2.067;
            return (int)(a * Math.Pow(x, b) + c) + offset;
        }
    }
}
