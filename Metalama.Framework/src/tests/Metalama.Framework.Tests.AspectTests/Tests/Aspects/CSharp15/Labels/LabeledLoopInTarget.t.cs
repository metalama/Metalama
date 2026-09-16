internal class TargetType
{
  [OuterAspect]
  [InnerAspect]
  public int Method(int limit)
  {
    global::System.Console.WriteLine("Inner aspect.");
    global::System.Console.WriteLine("Outer aspect.");
    var total = 0;
    outer:
      for (var i = 0; i < 3; i++)
      {
        for (var j = 0; j < 3; j++)
        {
          if (j == 1)
          {
            continue outer;
          }
          total += i + j;
          if (total > limit)
          {
            break outer;
          }
        }
      }
    return total;
  }
}