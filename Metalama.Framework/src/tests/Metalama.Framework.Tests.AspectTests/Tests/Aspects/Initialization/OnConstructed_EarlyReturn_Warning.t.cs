// Warning LAMA0552 on `TargetCode`: `The aspect 'TheAspect' applies 'AfterLastInstanceConstructor' to a type whose constructor 'TargetCode.TargetCode(int, InitializationContext)' contains an early 'return' statement; 'OnConstructed' will not be called on that path. Refactor the constructor to avoid early returns.`
[TheAspect]
public class TargetCode
{
  public TargetCode(int value, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default(global::Metalama.Framework.RunTime.Initialization.InitializationContext))
  {
    if (value < 0)
    {
      return;
    }
    Console.WriteLine(value);
    this.OnConstructed(context);
  }
  public virtual void OnConstructed(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("OnConstructed!");
  }
}