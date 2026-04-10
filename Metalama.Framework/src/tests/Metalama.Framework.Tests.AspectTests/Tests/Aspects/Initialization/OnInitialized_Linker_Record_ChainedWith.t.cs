[TheAspect]
public record TargetRecord(int A, int B) : global::Metalama.Framework.RunTime.Initialization.IInitializable
{
  public virtual void Initialize(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("Initialized!");
  }
}
public class Caller
{
  public void Method()
  {
    var r1 = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new TargetRecord(1, 2));
    var r2 = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize((global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize((r1 with { A = 10 }), global::Metalama.Framework.RunTime.Initialization.InitializationMetadata.Modify)with { B = 20 }), global::Metalama.Framework.RunTime.Initialization.InitializationMetadata.Modify);
  }
}