private string Method(MyClass1 a)
{
  var rt = typeof(global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.TypeOf.TypeOfRunTimeType.MyClass1);
  global::System.Console.WriteLine("rt=" + rt);
  global::System.Console.WriteLine("ct=Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.TypeOf.TypeOfRunTimeType.MyClass1");
  global::System.Console.WriteLine("Oops");
  global::System.Console.WriteLine(typeof(global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.TypeOf.TypeOfRunTimeType.MyClass1));
  global::System.Console.WriteLine(typeof(global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.TypeOf.TypeOfRunTimeType.MyClass1).FullName);
  return this.Method(a);
}