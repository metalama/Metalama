[Introduction]
internal class TargetClass : BaseClass
{
  public int ExistingMethod()
  {
    // Return a constant
    return 27;
  }
  public void ExistingMethod_Void()
  {
  // Do nothing.
  }
  public override global::System.Int32 ExistingBaseMethod()
  {
    // Call the base method of the same name
    return base.ExistingBaseMethod();
  }
  public override void ExistingBaseMethod_Void()
  {
    // Call the base method of the same name
    base.ExistingBaseMethod_Void();
  }
  public global::System.Int32 NotExistingMethod()
  {
    // Return default value
    return default(global::System.Int32);
  }
  public void NotExistingMethod_Void()
  {
  // Do nothing
  }
}