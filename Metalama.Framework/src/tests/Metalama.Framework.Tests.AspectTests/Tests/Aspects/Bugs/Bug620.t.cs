internal class Target
{
  [TheAspect]
  internal void M1(int x)
  {
    global::System.Console.WriteLine(true);
    global::System.Console.WriteLine(true);
    global::System.Console.WriteLine(true);
    throw new NotImplementedException();
    return;
  }
}
