[TheAspect]
public record struct TargetRecordStruct(int Value) : global::Metalama.Framework.RunTime.Initialization.IInitializable
{
  public void Initialize(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::System.Console.WriteLine("Initialized!");
  }
}
public class Caller
{
  public void Method()
  {
    var r1 = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new TargetRecordStruct(1));
    var r2 = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize((r1 with { Value = 2 }), global::Metalama.Framework.RunTime.Initialization.InitializationMetadata.Modify);
  }
}