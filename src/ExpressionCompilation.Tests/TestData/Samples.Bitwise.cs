namespace ExpressionCompilation.Tests.TestData
{
    public partial class SamplesForCopy
    {
        public bool And()
        {
            var boolValue = Helpers.TrueValue;
            return Helpers.TrueValue && boolValue;
        }

        public bool Not()
        {
            return !Helpers.FalseValue;
        }

        public bool Or()
        {
            return Helpers.FalseValue || Helpers.TrueValue;
        }

        public bool Xor()
        {
            return Helpers.FalseValue ^ Helpers.TrueValue;
        }
    }
}