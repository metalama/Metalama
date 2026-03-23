internal class TargetClass
{
  [return: MyParameterAspect]
  public int Value { get; set; }
}
