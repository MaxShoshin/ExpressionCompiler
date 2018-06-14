# ExpressionCompiler

Create DynamicMethod based on text expression. Supports full C# expression syntax.

Roslyn Scripting provide awesome compilation of any C# expression. Roslyn.Scripting has one disadwantage now: for any single script parse it creates separate assembly in current AppDomain. Now there is no ability to unload such assemblies even they are not used anymore.

ExpressionCompiler creates DynamicMethod based on your expression without loading additional assemblies in the app domain. DynamicMethod can be garbage collected as usual class when it no longer used.

Usage:
```C#
Func<int> calculator = new ExpressionCompiler("1 + 1").Returns(typeof(int)).Compile<Func<int>>();
Console.WriteLine(calculator.Invoke()); // Prints 2
```