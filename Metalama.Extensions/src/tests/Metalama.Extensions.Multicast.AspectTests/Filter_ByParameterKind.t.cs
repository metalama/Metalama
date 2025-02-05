namespace Metalama.Extensions.Multicast.AspectTests.Filter_ByParameterKind
{
  [AddTag("In", AttributeTargetElements = MulticastTargets.Parameter, AttributeTargetParameterAttributes = MulticastAttributes.InParameter)]
  [AddTag("Out", AttributeTargetElements = MulticastTargets.Parameter, AttributeTargetParameterAttributes = MulticastAttributes.OutParameter)]
  [AddTag("Ref", AttributeTargetElements = MulticastTargets.Parameter, AttributeTargetParameterAttributes = MulticastAttributes.RefParameter)]
  [AddTag("Return", AttributeTargetElements = MulticastTargets.ReturnValue)]
  public abstract class C
  {
    [return: Tag("Return")]
    public int Method([Tag("In")] int normalParam, [Tag("In")] in int inParam, [Tag("Out")] out int outParam, [Tag("Ref")] ref int refParam)
    {
      outParam = 5;
      return 5;
    }
  }
}