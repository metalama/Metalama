[assembly: MyAspect("1", AttributePriority = 1)]
[assembly: MyAspect("2", AttributePriority = 2, AttributeExclude = true)]
public class C
{
  public void M()
  {
  }
}