public class Caller
{
  private X _x = new X
  {
    Y = new Y
    {
      Z = new Z().WithInitialize()
    }.WithInitialize()
  }.WithInitialize();
}