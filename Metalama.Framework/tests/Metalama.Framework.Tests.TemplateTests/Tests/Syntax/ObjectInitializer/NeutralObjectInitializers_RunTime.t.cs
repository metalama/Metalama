private object Method(object a)
{
  var runTime1 = new global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.NeutralObjectInitializers_RunTime.Entity1
  {
    Property1 = 1,
    Property2 =
    {
      new global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.NeutralObjectInitializers_RunTime.Entity2
      {
        Property1 = this.Foo
      },
      new global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.NeutralObjectInitializers_RunTime.Entity2
      {
        Property1 = 3
      }
    }
  };
  var result = this.Method(a);
  return (global::System.Object)result;
}