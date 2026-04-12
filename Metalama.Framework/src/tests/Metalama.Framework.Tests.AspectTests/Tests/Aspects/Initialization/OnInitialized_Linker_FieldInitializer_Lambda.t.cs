public class Caller
{
  private Func<TargetCode> _factory = () => new TargetCode().WithInitialize();
}