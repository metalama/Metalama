[TheAspect]
public struct ExplicitParameterless
{
  public ExplicitParameterless(InitializationContext context)
  {
    this.Value = 1;
    this.OnConstructed(context);
  }
  public int Value { get; }
  public ExplicitParameterless() : this(context: default)
  {
  }
  private void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed ExplicitParameterless");
  }
}
[TheAspect]
public struct ImplicitParameterless
{
  public ImplicitParameterless(int value, InitializationContext context = default)
  {
    this.Value = value;
    this.OnConstructed(context);
  }
  public int Value { get; }
  public ImplicitParameterless(InitializationContext context = default)
  {
    this.OnConstructed(context);
  }
  private void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed ImplicitParameterless");
  }
}