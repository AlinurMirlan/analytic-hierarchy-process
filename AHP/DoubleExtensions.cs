using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AHP;

internal static class DoubleExtensions
{
    internal static double SumByColumn(this double[][] doubles, int columnIndex)
    {
        double sum = 0;
        for (int i = 0; i < doubles.GetLength(0); i++)
        {
            sum += doubles[i][columnIndex];
        }

        return sum;
    }
}
