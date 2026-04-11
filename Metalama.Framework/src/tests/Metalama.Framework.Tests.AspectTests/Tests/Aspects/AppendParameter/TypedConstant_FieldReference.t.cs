internal class TargetCode
{
  public const int DefaultValue = 42;
  [AddParameterWithFieldReference]
  private TargetCode(string name, int arg1 = DefaultValue)
  {
  }
}