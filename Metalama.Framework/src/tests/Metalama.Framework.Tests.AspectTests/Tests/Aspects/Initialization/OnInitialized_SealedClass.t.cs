[TheAspect]
public sealed class TargetCode : global::Metalama.Framework.RunTime.Initialization.IInitializable
{
  public void Initialize(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("Initialized!");
  }
}