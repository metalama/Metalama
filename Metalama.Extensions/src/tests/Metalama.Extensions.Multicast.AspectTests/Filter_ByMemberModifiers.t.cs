namespace Metalama.Extensions.Multicast.AspectTests.Filter_ByMemberModifiers
{
  [AddTag("Abstract", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.Abstract)]
  [AddTag("NonAbstract", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.NonAbstract)]
  public abstract class ByAbstraction
  {
    [Tag("Abstract")]
    public abstract void AbstractMethod();
    [Tag("NonAbstract")]
    public void NonAbstract()
    {
    }
  }
  [AddTag("Virtual", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.Virtual)]
  [AddTag("NonVirtual", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.NonVirtual)]
  public class ByVirtuality
  {
    [Tag("Virtual")]
    public virtual void VirtualMethod()
    {
    }
    [Tag("NonVirtual")]
    public void NonVirtualMethod()
    {
    }
  }
  [AddTag("Static", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.Static)]
  [AddTag("Instance", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.Instance)]
  public class ByScope
  {
    [Tag("Instance")]
    public void InstanceMethod()
    {
    }
    [Tag("Static")]
    public static void StaticMethod()
    {
    }
  }
  [AddTag("Managed", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.Managed)]
  [AddTag("NonManaged", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.NonManaged)]
  public class ByImplementation
  {
    [DllImport("foo")]
    [Tag("NonManaged")]
    public static extern void ExternMethod();
    [Tag("Managed")]
    public static void ManagedMethod()
    {
    }
  }
  [AddTag("CompilerGenerated", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.CompilerGenerated)]
  [AddTag("UserGenerated", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.UserGenerated)]
  public class ByGeneration
  {
    [CompilerGenerated]
    [Tag("CompilerGenerated")]
    public static void CompilerGeneratedMethod()
    {
    }
    [Tag("UserGenerated")]
    public void UserGenerated()
    {
    }
  }
  [AddTag("Literal", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.Literal)]
  [AddTag("NonLiteral", AttributeTargetElements = MulticastTargets.AnyMember, AttributeTargetMemberAttributes = MulticastAttributes.NonLiteral)]
  public class ByLiteral
  {
    [Tag("Literal")]
    public const int ConstField = 5;
    [Tag("NonLiteral")]
    public int NonConstField;
  }
}