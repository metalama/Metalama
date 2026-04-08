public class Caller
{
  public void Method()
  {
    var r1 = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new TargetRecord(1));
    var r2 = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize((r1 with { Value = 2 }), global::Metalama.Framework.RunTime.Initialization.InitializationMetadata.Modify);
  }
}