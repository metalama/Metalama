private void Method()
{
  var clone1 = this;
  var clone2 = this;
  var clone3 = this;
  ((global::Metalama.Framework.Tests.AspectTests.Tests.Templating.Dynamic.Issue28811.TargetCode)clone1).a = clone1;
  ((global::Metalama.Framework.Tests.AspectTests.Tests.Templating.Dynamic.Issue28811.TargetCode)clone2).a = this.a;
  ((global::Metalama.Framework.Tests.AspectTests.Tests.Templating.Dynamic.Issue28811.TargetCode)clone3).a = this.a.Clone();
  return;
}