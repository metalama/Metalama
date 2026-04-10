[TheAspect]
public class TargetCode
{
  public TargetCode(int value, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default(global::Metalama.Framework.RunTime.Initialization.InitializationContext))
  {
    _ = value;
    this.OnConstructed(context);
  }
  // Hand-authored OnConstructed method — the aspect should inject its template into this existing method
  // rather than introducing a new one.
  public virtual void OnConstructed(InitializationContext ctx)
  {
    global::System.Console.WriteLine("Injected!");
    Console.WriteLine("User-authored.");
  }
}