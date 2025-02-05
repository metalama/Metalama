[assembly: AddTag("PublicType", AttributeTargetElements = MulticastTargets.Class, AttributeTargetTypeAttributes = MulticastAttributes.Public)]
[assembly: AddTag("MethodOfPublicType", AttributeTargetElements = MulticastTargets.Method, AttributeTargetTypeAttributes = MulticastAttributes.Public)]
[Tag("PublicType")]
public class PublicClass
{
  [Tag("MethodOfPublicType")]
  public void PublicMethod()
  {
  }
  [Tag("MethodOfPublicType")]
  internal void InternalMethod()
  {
  }
}
internal class InternalClass
{
  public void PublicMethod()
  {
  }
  internal void InternalMethod()
  {
  }
}