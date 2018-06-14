using System;

namespace ExpressionCompilation.Tests.TestData
{
    public partial class SamplesForCopy
    {
        public bool TryCatch()
        {
            try
            {
                Helpers.DoNothing();
            }
            catch (Exception)
            {
                throw;
            }

            return false;
        }

        public bool TryFinnaly()
        {
            try
            {
                Helpers.DoNothing();

                return true;
            }
            finally
            {
                Helpers.DoNothing();
            }
        }

        public bool Boxing()
        {
            return ((object)4).GetType().Name == "Int32";
        }

        public bool LdToken()
        {
            Helpers.DoNothing(typeof(int));
            return true;
        }

        public bool Unboxing()
        {
            return (int)Helpers.BoxedIntValue == 4;
        }

        public bool Cast()
        {
            object val = new int[5];
            var array = (Array)val;
            return array.GetLength(0) == 5;
        }

        public bool RaieException()
        {
            throw new ExpectedException();
        }
    }
}