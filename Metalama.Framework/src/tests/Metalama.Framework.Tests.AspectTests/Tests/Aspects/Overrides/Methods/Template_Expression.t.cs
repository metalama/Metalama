public class Target
{
  [Test]
  public void VoidMethod()
  {
    return;
  }
  [Test]
  public int Method()
  {
    return default;
  }
  [Test]
  public T? Method<T>()
  {
    return default;
  }
}