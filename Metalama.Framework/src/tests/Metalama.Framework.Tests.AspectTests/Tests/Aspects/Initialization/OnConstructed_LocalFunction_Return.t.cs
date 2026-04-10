[TheAspect]
public class TargetCode
{
  public TargetCode(int value, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default(global::Metalama.Framework.RunTime.Initialization.InitializationContext))
  {
    // `return;` inside a nested local function belongs to the local function's own
    // control flow and must NOT be rewritten. The constructor has no top-level return
    // statement, so no epilogue label should be emitted.
    LocalCheck();
    void LocalCheck()
    {
      if (value < 0)
      {
        return;
      }
      Console.WriteLine(value);
    }
    this.OnConstructed(context);
  }
  public virtual void OnConstructed(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("OnConstructed!");
  }
}