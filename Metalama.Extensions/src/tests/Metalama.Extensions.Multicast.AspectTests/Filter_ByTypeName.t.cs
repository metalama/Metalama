[assembly: AddTag("C", AttributeTargetElements = MulticastTargets.Class, AttributeTargetTypes = "Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeName.C")]
[assembly: AddTag("C+N", AttributeTargetElements = MulticastTargets.Class, AttributeTargetTypes = "Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeName.C+N")]
[assembly: AddTag("C<T>", AttributeTargetElements = MulticastTargets.Class, AttributeTargetTypes = "Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeName.C`1")]
[assembly: AddTag("C<T>+N", AttributeTargetElements = MulticastTargets.Class, AttributeTargetTypes = "Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeName.C`1+N")]
[Tag("C")]
public class C
{
  [Tag("C+N")]
  private class N
  {
  }
}
public class D
{
}
[Tag("C<T>")]
public class C<T>
{
  [Tag("C<T>+N")]
  private class N
  {
  }
}