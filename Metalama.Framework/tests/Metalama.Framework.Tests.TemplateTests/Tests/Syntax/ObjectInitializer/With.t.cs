private object Method(object a)
{
  var runTime1 = new global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.With.Entity1
  {
    Property1 = 1
  };
  var runTime2 = runTime1 with
  {
    Property1 = this.Foo
  };
  var result = this.Method(a);
  return (global::System.Object)result;
}