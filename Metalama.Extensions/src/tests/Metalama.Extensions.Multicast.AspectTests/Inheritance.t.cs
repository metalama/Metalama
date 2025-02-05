[AddTagInherited("Tagged")]
[Tag("Tagged")]
public class C
{
  [Tag("Tagged")]
  protected virtual void M()
  {
  }
}
[Tag("Tagged")]
public class D : C
{
  [Tag("Tagged")]
  protected override void M()
  {
  }
  protected void N()
  {
  }
}