[Introduction]
internal class TargetClass
{
  public global::System.Int32 Property_NameConflict
  {
    get
    {
      return (global::System.Int32)42;
    }
    set
    {
      var z = value;
    }
  }
}