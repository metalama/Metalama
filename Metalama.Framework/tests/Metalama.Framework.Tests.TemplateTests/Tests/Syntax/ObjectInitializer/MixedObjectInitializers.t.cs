private object Method(object a)
{
  var runTime1 = new global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.MixedObjectInitializers.Entity1{Property1 = 1, Property2 = {new global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.MixedObjectInitializers.Entity2{Property1 = this.Foo}, new global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.MixedObjectInitializers.Entity2{Property1 = 3}}};
  var runTime2 = runTime1 with {Property1 = 2};
  var result = this.Method(a);
  return (global::System.Object)result;
}