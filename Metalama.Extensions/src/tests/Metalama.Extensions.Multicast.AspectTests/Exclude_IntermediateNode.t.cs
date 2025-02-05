[assembly: MyAspect("1")]
[MyAspect("2", AttributeExclude = true)]
public class C
{
  public void M()
  {
  }
}