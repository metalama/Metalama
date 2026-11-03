public class Caller
{
  [PropertyAspect]
  public TargetCode BlockBodied
  {
    get
    {
      return new TargetCode().WithInitialize();
    }
  }
  [PropertyAspect]
  public TargetCode ExpressionBodied
  {
    get
    {
      return new TargetCode().WithInitialize();
    }
  }
}