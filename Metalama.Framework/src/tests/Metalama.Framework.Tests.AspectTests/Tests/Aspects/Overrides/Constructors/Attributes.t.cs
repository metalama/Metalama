[Override]
internal class TargetClass
{
  [ConstructorOnly]
  [method: ExplicitConstructorOnly]
  public TargetClass([ParamOnly] int x)
  {
    global::System.Console.WriteLine("This is the overridden constructor.");
  }
}