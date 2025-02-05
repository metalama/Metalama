internal class Program
{
  [Aspect]
  private void M(int arg)
  {
    _ = arg;
  }
}