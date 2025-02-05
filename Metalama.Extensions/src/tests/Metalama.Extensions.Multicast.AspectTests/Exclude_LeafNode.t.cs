[assembly: MyAspect("1")]
public class C
{
  [MyAspect("2", AttributeExclude = true)]
  public void M()
  {
  }
}