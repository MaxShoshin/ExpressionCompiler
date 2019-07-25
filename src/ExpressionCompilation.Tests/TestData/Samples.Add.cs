using System.Globalization;

namespace NeedfulThings.ExpressionCompilation.Tests.TestData
{
    public partial class SamplesForCopy
    {
        public bool Add()
        {
            var item = Helpers.IntValue;

            item++;
            item += 5;

            return item.ToString(CultureInfo.InvariantCulture) == "10";
        }

        public bool Add_Ovf()
        {
            var item = Helpers.IntValue;

            checked
            {
                item += 7;
            }

            return item.ToString(CultureInfo.InvariantCulture) == "11";
        }

        public bool Add_Ovf_Un()
        {
            var item = Helpers.UIntValue;

            checked
            {
                item += 11;
            }

            return item.ToString(CultureInfo.InvariantCulture) == "15";
        }
    }
}