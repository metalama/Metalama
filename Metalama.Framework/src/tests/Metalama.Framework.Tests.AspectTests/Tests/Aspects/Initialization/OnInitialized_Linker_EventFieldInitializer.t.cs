public class Caller
{
  private event Func<TargetCode> _factory = () => new TargetCode().WithInitialize();
}