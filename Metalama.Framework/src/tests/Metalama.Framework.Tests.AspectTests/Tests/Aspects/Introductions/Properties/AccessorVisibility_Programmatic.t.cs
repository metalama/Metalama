[Introduction]
internal class TargetClass
{
  public global::System.Int32 PrivateGetPublicSet
  {
    private get
    {
      return default(global::System.Int32);
    }
    set
    {
    }
  }
  protected global::System.Int32 ProtectedGetPrivateSet
  {
    get
    {
      return default(global::System.Int32);
    }
    private set
    {
    }
  }
  public global::System.Int32 PublicGetPrivateSet
  {
    get
    {
      return default(global::System.Int32);
    }
    private set
    {
    }
  }
}
