internal class Target
{
  [Aspect]
  private void M(object obj)
  {
    global::System.Console.WriteLine("this: {0}, {1}: {2}", this, "obj", obj);
    return;
  }
}