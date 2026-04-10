public class Caller
{
  private static readonly TargetRecord _template = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize(new TargetRecord(0));
  public TargetRecord Bar { get; } = global::Metalama.Framework.RunTime.Initialization.InitializableExtensions.WithInitialize((_template with { Value = 42 }), global::Metalama.Framework.RunTime.Initialization.InitializationMetadata.Modify);
}