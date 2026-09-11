internal class Target
{
  private void Foo(int x)
  {
    outer:
      for (var i = 0; i < 2; i++)
      {
        for (var j = 0; j < 2; j++)
        {
          if (j == x)
          {
            continue outer;
          }
          Console.WriteLine($"Aspect {i} {j}");
          if (i == x)
          {
            break outer;
          }
        }
      }
    outer_2:
      for (var i = 0; i < 2; i++)
      {
        for (var j = 0; j < 2; j++)
        {
          if (j == x)
          {
            continue outer_2;
          }
          Console.WriteLine($"Original {i} {j}");
          if (i == x)
          {
            break outer_2;
          }
        }
      }
  }
}