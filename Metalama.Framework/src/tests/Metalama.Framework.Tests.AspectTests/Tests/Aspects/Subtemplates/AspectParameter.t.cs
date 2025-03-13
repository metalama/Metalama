[Aspect(42)]
private void Method()
{
  var aspect = new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.AspectParameter.Aspect(42);
  global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.AspectParameter.Aspect aspect_1 = aspect;
  global::System.Console.WriteLine($"run-time i={aspect_1.I}");
  global::System.Console.WriteLine("compile-time i=42");
  return;
}