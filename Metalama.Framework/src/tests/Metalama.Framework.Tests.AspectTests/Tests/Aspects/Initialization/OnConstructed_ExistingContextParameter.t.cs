[TheAspect]
public class TargetCode
{
  // Hand-authored InitializationContext parameter with custom name 'ctx' — the epilogue call must use this name.
  public TargetCode(int value, InitializationContext ctx = default)
  {
    _ = value;
    _ = ctx;
    this.OnConstructed(ctx);
  }
  public virtual void OnConstructed(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("OnConstructed!");
  }
}