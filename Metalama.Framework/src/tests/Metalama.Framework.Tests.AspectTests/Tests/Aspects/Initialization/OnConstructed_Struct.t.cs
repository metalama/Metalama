[TheAspect]
public struct TargetCode
{
  public int Value;
  public TargetCode(int value, global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default(global::Metalama.Framework.RunTime.Initialization.InitializationContext))
  {
    this.Value = value;
    this.OnConstructed(context);
  }
  public TargetCode(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
  }
  public void OnConstructed(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("OnConstructed!");
  }
}