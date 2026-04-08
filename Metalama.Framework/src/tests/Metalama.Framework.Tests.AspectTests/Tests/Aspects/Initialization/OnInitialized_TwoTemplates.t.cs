[TheAspect]
public class TargetCode : global::Metalama.Framework.RunTime.Initialization.IInitializable
{
  public virtual void Initialize(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("First!");
    global::System.Console.WriteLine("Second!");
  }
}