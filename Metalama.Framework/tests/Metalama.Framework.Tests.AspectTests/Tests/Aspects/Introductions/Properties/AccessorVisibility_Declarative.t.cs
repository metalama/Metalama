[Introduction]
internal class TargetClass
{
  public global::System.Int32 AutoPropertyWithRestrictedGet { private get; set; }
  public global::System.Int32 AutoPropertyWithRestrictedInit { get; private init; }
  public global::System.Int32 AutoPropertyWithRestrictedSet { get; private set; }
  public global::System.Int32 PropertyWithRestrictedGet
  {
    private get
    {
      return (global::System.Int32)42;
    }
    set
    {
    }
  }
  public global::System.Int32 PropertyWithRestrictedInit
  {
    get
    {
      return (global::System.Int32)42;
    }
    private init
    {
    }
  }
  public global::System.Int32 PropertyWithRestrictedSet
  {
    get
    {
      return (global::System.Int32)42;
    }
    private set
    {
    }
  }
  protected global::System.Int32 ProtectedAutoPropertyWithPrivateProtectedSetter { get; private protected set; }
  protected internal global::System.Int32 ProtectedInternalAutoPropertyWithProtectedSetter { get; protected set; }
}