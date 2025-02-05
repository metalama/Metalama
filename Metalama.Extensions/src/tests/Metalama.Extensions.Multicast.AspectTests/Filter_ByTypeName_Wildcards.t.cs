[assembly: AddTag("Prefixed", AttributeTargetElements = MulticastTargets.Class, AttributeTargetTypes = "Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeName_Wildcards.Prefixed*")]
[assembly: AddTag("Ns", AttributeTargetElements = MulticastTargets.Class, AttributeTargetTypes = "Metalama.Extensions.Multicast.AspectTests.Filter_ByTypeName_Wildcards.Ns.*")]
[Tag("Prefixed")]
public class PrefixedA
{
}
[Tag("Prefixed")]
public class PrefixedB
{
}
public class NonPrefixed
{
}
namespace Ns
{
  [Tag("Ns")]
  public class InNamespace
  {
  }
}