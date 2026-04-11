public class Caller
{
  public void Method()
  {
    var r1 = new TargetRecord(1).WithInitialize();
    var r2 = (r1 with
    {
      Value = 2
    }
    ).WithInitialize(InitializationMetadata.Modify);
  }
}