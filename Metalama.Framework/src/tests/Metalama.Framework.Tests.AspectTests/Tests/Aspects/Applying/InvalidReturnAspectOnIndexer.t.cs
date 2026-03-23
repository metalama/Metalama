internal class TargetClass
{
  [return: MyParameterAspect]
  public int this[int i] => i;
}
