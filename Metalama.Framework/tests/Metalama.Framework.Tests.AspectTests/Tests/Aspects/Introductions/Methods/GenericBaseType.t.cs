[Aspect]
internal class TargetCode : Base<int>
{
  public new global::System.Int32 ExistingBaseHiddenMethod(global::System.Int32 value)
  {
    global::System.Console.WriteLine("This is the hiding method.");
    return base.ExistingBaseHiddenMethod(value);
  }
  public new void ExistingBaseHiddenMethod_Void(global::System.Int32 value)
  {
    global::System.Console.WriteLine("This is the hiding method.");
    base.ExistingBaseHiddenMethod_Void(value);
  }
  public override global::System.Int32 ExistingBaseOverriddenMethod(global::System.Int32 value)
  {
    global::System.Console.WriteLine("This is the overriding method.");
    return base.ExistingBaseOverriddenMethod(value);
  }
  public override void ExistingBaseOverriddenMethod_Void(global::System.Int32 value)
  {
    global::System.Console.WriteLine("This is the overriding method.");
    base.ExistingBaseOverriddenMethod_Void(value);
  }
}