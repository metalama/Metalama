namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.DesignTimeImplicitMembers
{
  partial class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.DesignTimeImplicitMembers.IInterface
  {
    public global::System.Int32 InterfaceMethod()
    {
      return default(global::System.Int32);
    }
    public global::System.Int32 AutoProperty { get; set; }
    public global::System.Int32 Property
    {
      get
      {
        return default(global::System.Int32);
      }
      set
      {
      }
    }
    public event global::System.EventHandler EventField;
    public event global::System.EventHandler Event
    {
      add
      {
      }
      remove
      {
      }
    }
  }
}