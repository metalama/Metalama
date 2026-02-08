[TestAspect1]
[TestAspect2]
internal class Target : Base
{
  public U Foo<U>(U item)
  {
    return default(U);
  }
}
