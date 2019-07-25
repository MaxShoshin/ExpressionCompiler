namespace NeedfulThings.ExpressionCompilation.Tests.TestData
{
    public partial class SamplesForCopy
    {
        public bool Bne_Un_S()
        {
            if (Helpers.IntValue == 4)
            {
                return true;
            }

            Helpers.DoNothing();

            return false;
        }

        public bool Beq_s()
        {
            if (Helpers.UIntValue != 4)
            {
                Helpers.DoNothing();
                return true;
            }

            return false;
        }
    }
}