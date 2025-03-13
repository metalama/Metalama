{
    var a_1 = new global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.ObjectInitializers.Entity1 { Property1 = 1, Property2 = { new global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.ObjectInitializers.Entity2 { Property1 = 2 }, new global::Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.ObjectInitializers.Entity2 { Property1 = 3 } } };
    var b = a_1 with
    {
        Property1 = 2
    };
    global::System.Object result;
    result = this.Method(a);
    return (object)result;
}