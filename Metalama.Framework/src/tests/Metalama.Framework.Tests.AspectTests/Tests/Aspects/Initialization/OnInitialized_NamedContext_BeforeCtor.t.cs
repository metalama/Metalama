[TheAspect]
public class TargetCode : IInitializable
{
  public int Value { get; set; }
  // Non-default parameter name 'ctx' — the linker must use this name when appending the named argument at call sites.
  public TargetCode(int value, InitializationContext ctx = default)
  {
    Console.WriteLine($"Before ctor, intent={ctx.Intent}");
    this.Value = value;
  }
  public virtual void Initialize(InitializationContext context = default)
  {
  }
}
public class Caller
{
  public void Method()
  {
    var t = new TargetCode(42, ctx: InitializationContext.WillInitialize).WithInitialize();
  }
}