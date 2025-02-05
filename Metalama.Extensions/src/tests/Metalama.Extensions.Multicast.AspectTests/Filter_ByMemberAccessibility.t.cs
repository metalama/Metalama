[assembly: AddTag("PublicMember", AttributeTargetElements = MulticastTargets.Method | MulticastTargets.Property | MulticastTargets.Event | MulticastTargets.Field, AttributeTargetTypes = "Metalama.Extensions.Multicast.AspectTests.Filter_ByMemberAccessibility.*", AttributeTargetMemberAttributes = MulticastAttributes.Public)]
namespace Metalama.Extensions.Multicast.AspectTests.Filter_ByMemberAccessibility
{
  public class PublicClass
  {
    [Tag("PublicMember")]
    public void PublicMethod()
    {
    }
    internal void InternalMethod()
    {
    }
    [Tag("PublicMember")]
    public int PublicProperty {[Tag("PublicMember")]
      get; private set; }
    internal int InternalProperty { get; private set; }
    [Tag("PublicMember")]
    public int PublicField;
    internal int InternalField;
    [Tag("PublicMember")]
    public event Action PublicEvent;
    internal event Action InternalEvent;
  }
  internal class InternalClass
  {
    [Tag("PublicMember")]
    public void PublicMethod()
    {
    }
    internal void InternalMethod()
    {
    }
    [Tag("PublicMember")]
    public int PublicProperty {[Tag("PublicMember")]
      get; private set; }
    internal int InternalProperty { get; private set; }
    [Tag("PublicMember")]
    public int PublicField;
    internal int InternalField;
    [Tag("PublicMember")]
    public event Action PublicEvent;
    internal event Action InternalEvent;
  }
}