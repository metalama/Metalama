internal class C
{
  [TheAspect]
  public void M(Node? node)
  {
    var node_1 = (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.NullConditionalAssignment_Template.Node? )node;
    node_1?.Value = 1;
    node_1?.Next?.Value = 2;
    return;
  }
}
