public class Caller
{
  private static readonly TargetRecord _template = new TargetRecord(0).WithInitialize();
  public TargetRecord Bar { get; } = (_template with
  {
    Value = 42
  }
  ).WithInitialize(InitializationMetadata.Modify);
}