private object Method(object a)
{
  var a_1 = new global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.RunTimeObjectInitializers.Entity1
  {
    Property1 = 1,
    Property2 =
    {
      new global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.RunTimeObjectInitializers.Entity2
      {
        Property1 = 2
      },
      new global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.RunTimeObjectInitializers.Entity2
      {
        Property1 = 3
      }
    }
  };
  var b = a_1 with
  {
    Property1 = 2
  };
  var result = this.Method(a);
  return (global::System.Object)result;
}