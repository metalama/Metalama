private void Method()
{
  var value = this.a;
  global::System.Console.WriteLine($"a={value}");
  var value_1 = global::Metalama.Framework.Tests.AspectTests.Tests.Templating.Dynamic.Issue28742.TargetCode.c;
  global::System.Console.WriteLine($"c={value_1}");
  var value_2 = this.B;
  global::System.Console.WriteLine($"B={value_2}");
  return;
}