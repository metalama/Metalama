[ConditionalMethodCall]
[ConditionalPropertyAccess]
private void Method()
{
  _ = (object)new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.ReturnConditionalExpressionAsVoid.RunTimeClass()?.P;
  var runTimeClass = new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.ReturnConditionalExpressionAsVoid.RunTimeClass();
  runTimeClass?.M();
  return;
}