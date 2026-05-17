using System;
using System.Data;

namespace FFLiteGUI.Utils
{
    public static class ExpressionEvaluator
    {
        public static int Evaluate(string expression, int mainW, int mainH, int boxW, int boxH)
        {
            string expr = expression.Replace("W", mainW.ToString())
                                    .Replace("H", mainH.ToString())
                                    .Replace("w", boxW.ToString())
                                    .Replace("h", boxH.ToString());
            var result = new DataTable().Compute(expr, null);
            return Convert.ToInt32(result);
        }
    }
}
