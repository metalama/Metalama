[TheAspect]
public record BaseRecord(int X) : global::Metalama.Framework.RunTime.Initialization.IInitializable
{
  public virtual void Initialize(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("Initialized!");
  }
}
public record DerivedRecord(int X, int Y) : BaseRecord(X);
public class Caller
{
  public void Method()
  {
    var d = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new DerivedRecord(1, 2));
    var d2 = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize((d with { Y = 5 }), global::Metalama.Framework.RunTime.Initialization.InitializationMetadata.Modify);
  }
}