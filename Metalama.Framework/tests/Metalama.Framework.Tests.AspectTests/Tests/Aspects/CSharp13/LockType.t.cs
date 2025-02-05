class Target
{
  Lock _lock = new();
  [TheAspect]
  void M()
  {
    lock (_aspectLock)
    {
      lock (_lock)
      {
        Console.WriteLine("Hello, World!");
      }
      return;
    }
  }
  private global::System.Threading.Lock _aspectLock = (global::System.Threading.Lock)(new());
}