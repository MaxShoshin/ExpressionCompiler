using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Mono.Cecil;
using Xunit;

namespace ExpressionCompilation.Tests
{
    public sealed class ILCopierTests
    {
        [Theory]
        [MemberData(nameof(GetMethodsForCopy))]
        public void Should_Copy_Method(string methodName)
        {
            var module = ModuleDefinition.ReadModule(Assembly.GetExecutingAssembly().Location);

            var methodDefinition = module.Types
                .Single(type => type.Name == nameof(ILCopierTests))
                .Methods.First(method => method.Name == methodName);

            var dynMethod = ILCopier.CopyToDynamicMethod(methodDefinition);
            var copied = (Func<bool>)dynMethod.CreateDelegate(typeof(Func<bool>));

            copied();
        }

        [UsedImplicitly]
        public static IEnumerable<object[]> GetMethodsForCopy()
        {
            return typeof(ILCopierTests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.Name != nameof(Should_Copy_Method))
                .Where(method => method.GetCustomAttributes(typeof(FactAttribute), false).Length == 0)
                .Select(method => new object[] {method.Name});
        }

        [UsedImplicitly]
        public bool Generics_ReturnsTypeParam()
        {
            var foo = new Foo<Another<Dictionary<string, int>>>(new Another<Dictionary<string, int>>());
            var item = foo.ReturnsTypeParam<bool?>(new Another<Dictionary<string, int>>(), true);
            return item == null;
        }

        [UsedImplicitly]
        public bool Generics_GenericMethodReturnsMethodParam()
        {
            var foo = new Foo<Another<Dictionary<string, int>>>(new Another<Dictionary<string, int>>());
            var item = foo.GenericMethodReturnsMethodParam<int?>();

            return item == null;
        }

        [UsedImplicitly]
        public bool Generics_GenericMethodReturnsTypeParam()
        {
            var foo = new Foo<Another<Dictionary<string, int>>>(new Another<Dictionary<string, int>>());
            var item = foo.GenericMethodReturnsTypeParam<int?>();

            return item == null;
        }

        [UsedImplicitly]
        public bool Generics_SimpleMethodReturnsTypeParam()
        {
            var foo = new Foo<Another<Dictionary<string, int>>>(new Another<Dictionary<string, int>>());
            var item = foo.SimpleMethodReturnsTypeParam();

            return item == null;
        }

        [UsedImplicitly]
        public bool Generics_SimpleMethodInGenericClass()
        {
            var foo = new Foo<Another<Dictionary<string, int>>>(new Another<Dictionary<string, int>>());
            var item = foo.SimpleMethodInGenericClass();
            return item;
        }

        [UsedImplicitly]
        public bool Generics_ReturnsAnotherGenericOnMethodParam()
        {
            var foo = new Foo<Another<Dictionary<string, int>>>(new Another<Dictionary<string, int>>());
            var item = foo.ReturnsAnotherGenericOnMethodParam<DateTime?>(new Another<Dictionary<string, int>>(), DateTime.UtcNow);
            return item != null;
        }

        [UsedImplicitly]
        public bool Generics_AnotherGenericOnTypeParamRetTypeGen()
        {
            var foo = new Foo<Another<Dictionary<string, int>>>(new Another<Dictionary<string, int>>());
            var item = foo.AnotherGenericOnTypeParamRetTypeGen<bool?>(new Another<Dictionary<string, int>>[1], null);
            return item == null;
        }

        [UsedImplicitly]
        public bool Generics_AnotherGenericOnMethodParam()
        {
            var foo = new Foo<Another<Dictionary<string, int>>>(new Another<Dictionary<string, int>>());

            var nullableIntArray = new int?[3];
            nullableIntArray[0] =6;
            nullableIntArray[1] = null;
            nullableIntArray[2] = 5;

            var item = foo.AnotherGenericOnMethodParam(new Another<Dictionary<string, int>>(), nullableIntArray);
            return item == null;
        }

        [UsedImplicitly]
        public bool Generics_AnotherGenericOnTypeParam()
        {
            var foo = new Foo<Another<Dictionary<string, int>>>(new Another<Dictionary<string, int>>());
            var item = foo.AnotherGenericOnTypeParam<int?>(new Another<Dictionary<string, int>>[0], 6);
            return item == null;
        }

        [UsedImplicitly]
        public bool Generics_ReturnsMethodParam()
        {
            var foo = new Foo<Another<Dictionary<string, int>>>(new Another<Dictionary<string, int>>());
            var item = foo.ReturnsMethodParam<int?>(new Another<Dictionary<string, int>>(), 5);
            return item == null;
        }

        [UsedImplicitly]
        public bool Generics_AnotherGenericWithOneArgumentParam()
        {
            var foo = new Foo<Another<Dictionary<string, int>>>(new Another<Dictionary<string, int>>());
            foo.AnotherGenericWithOneArgumentParam(new Dictionary<string, Another<Dictionary<string, int>>>());
            return true;
        }

        [UsedImplicitly]
        public bool Generics_AnotherGenericWithOneArgumentParamOverloaded()
        {
            var foo = new Foo<Another<Dictionary<string, int>>>(new Another<Dictionary<string, int>>());
            foo.AnotherGenericWithOneArgumentParamOverloaded(new Dictionary<Another<Dictionary<string, int>>, string>());
            return true;
        }

        [UsedImplicitly]
        public bool Create_Generic_Multidimensional_Arrays()
        {
            var nullableIntArray = new int?[2,1];
            nullableIntArray[0,0] =6;
            nullableIntArray[1,0] = null;

            return true;
        }

        [UsedImplicitly]
        public bool Create_Generic_Arrays_Of_Arrays()
        {
            var nullableIntArray = new int?[2][];
            nullableIntArray[0] = new int?[1];
            nullableIntArray[1] = new int?[2];
            nullableIntArray[1][1] = 4;

            return true;
        }

        [UsedImplicitly]
        public bool Create_Generic_Arrays()
        {
            var nullableIntArray = new int?[3];
            nullableIntArray[0] =6;
            nullableIntArray[1] = null;
            nullableIntArray[2] = 5;

            return true;
        }

        [UsedImplicitly]
        public bool Overloaded_Constructors()
        {
            return new DateTime(2017, 01, 03) < DateTime.UtcNow;
        }

        [UsedImplicitly]
        public bool Nullable_Ints()
        {
            Bar bar = new Bar();

            return bar.B?.B?.Val != 4;
        }

        [UsedImplicitly]
        public bool Nested_Generics()
        {
            var obj = new Foo<Dictionary<int, object>>.Nested<int?>();

            return obj.ToString() != null;
        }

        [Fact(Skip = "Not supported try-catch")]
        public bool Try_Catch()
        {
            try
            {
                Nop();
            }
            catch (IOException ex)
            {
                Nop();
                ex.ToString();
            }

            return true;
        }

        [Fact(Skip = "Not supported try-finally")]
        public bool Try_Finally()
        {
            try
            {
                Nop();
            }
            finally
            {
                Nop();
            }

            return true;
        }



        [Fact(Skip = "Not supported try-catch-finally")]
        public bool Try_Catch_Finally()
        {
            try
            {
                Nop();
            }
            catch (Exception)
            {
                Nop();
            }
            finally
            {
                Nop();
            }

            return true;
        }

        [UsedImplicitly]
        public bool Linq()
        {
            var arrayOfInts = new int[5];
            for (int i = 0; i < arrayOfInts.Length; i++)
            {
                arrayOfInts[i] = i + 1;
            }

            return arrayOfInts.Contains(3);
        }

        [Fact(Skip = "Not supported ldtoken instruction yet")]
        public bool Array_Initializers()
        {
            var arrayOfInts = new[] {1, 2, 3, 4, 5, 6, 7};

            return arrayOfInts.Contains(3);
        }

#pragma warning disable xUnit1013
        [UsedImplicitly]
        public static void Nop()
        {
        }
#pragma warning restore xUnit1013

        [UsedImplicitly]
        public class Foo<TTypeArg>
        {
            public Foo(TTypeArg arg)
            {
            }


            public bool SimpleMethodInGenericClass()
            {
                return true;
            }

            public TTypeArg SimpleMethodReturnsTypeParam()
            {
                return default;
            }

            [UsedImplicitly]
            public TTypeArg GenericMethodReturnsTypeParam<TMethodArg>()
            {
                return default;
            }

            [UsedImplicitly]
            public TMethodArg GenericMethodReturnsMethodParam<TMethodArg>()
            {
                return default;
            }

            [UsedImplicitly]
            public TMethodArg ReturnsMethodParam<TMethodArg>(TTypeArg arg, TMethodArg methodArg)
            {
                return default;
            }

            [UsedImplicitly]
            public TTypeArg ReturnsTypeParam<TMethodArg>(TTypeArg arg, TMethodArg methodArg)
            {
                return default;
            }

            [UsedImplicitly]
            public void AnotherGenericWithOneArgumentParam(Dictionary<string, TTypeArg> dict)
            {
            }

            [UsedImplicitly]
            public void AnotherGenericWithOneArgumentParamOverloaded(Dictionary<TTypeArg, string> dict)
            {
            }

            [UsedImplicitly]
            public TMethodArg AnotherGenericOnMethodParam<TMethodArg>(TTypeArg arg, IEnumerable<TMethodArg> methodArg)
            {
                return default;
            }

            [UsedImplicitly]
            public TMethodArg AnotherGenericOnTypeParam<TMethodArg>(IEnumerable<TTypeArg> arg, TMethodArg methodArg)
            {
                return default;
            }

            [UsedImplicitly]
            public TTypeArg AnotherGenericOnTypeParamRetTypeGen<TMethodArg>(IEnumerable<TTypeArg> arg, TMethodArg methodArg)
            {
                return default;
            }

            [UsedImplicitly]
            public IEnumerable<TMethodArg> ReturnsAnotherGenericOnMethodParam<TMethodArg>(TTypeArg arg, TMethodArg methodArg)
            {
                return new TMethodArg[0];
            }

            public class Nested<TAnother>
            {
            }
        }

        [UsedImplicitly]
        public class Another<TArg>
        {
        }

        [UsedImplicitly]
        public class Bar
        {
            public Bar B { get; } = null;

            public int Val { get; } = 5;
        }
    }
}