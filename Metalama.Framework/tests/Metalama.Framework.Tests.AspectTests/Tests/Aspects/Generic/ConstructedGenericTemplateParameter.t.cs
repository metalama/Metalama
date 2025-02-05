class TargetCode
{
  [Aspect]
  public int M(int[] arg)
  {
    global::System.Console.WriteLine(arg[0]);
    return 0;
  }
}