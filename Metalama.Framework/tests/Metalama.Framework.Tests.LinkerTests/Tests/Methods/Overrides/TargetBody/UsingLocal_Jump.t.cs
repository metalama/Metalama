class Target
{
  void Foo(int x)
  {
    using var z71 = new Disposable();
    using var z72 = new Disposable();
    Console.WriteLine($"Before aspect2 {z71} {z72}");
    if (x == 0)
    {
      return;
    }
    // z81 should not be transformed
    using var z81 = new Disposable();
    using var z82 = new Disposable();
    Console.WriteLine($"Mid aspect2 {z81} {z82}");
    using var z41 = new Disposable();
    using var z42 = new Disposable();
    Console.WriteLine($"Before aspect1 {z41} {z42}");
    if (x == 0)
    {
      goto __aspect_return_1;
    }
    // z51 should be transformed
    using (var z51 = new Disposable())
    {
      using var z52 = new Disposable();
      Console.WriteLine($"Mid aspect1 {z51} {z52}");
      if (x >= 1)
      {
        if (x == 1)
        {
          goto myLabel;
        }
        myLabel:
          ;
      }
      // z11 should not be transformed
      using var z11 = new Disposable();
      using var z12 = new Disposable();
      Console.WriteLine($"First {z11} {z12}");
      if (x == 0)
      {
        goto __aspect_return_2;
      }
      // z21 should be transformed
      using (var z21 = new Disposable())
      {
        using var z22 = new Disposable();
        Console.WriteLine($"Second {z21} {z22}");
        if (x == 2)
        {
          goto __aspect_return_2;
        }
        // z31 shoudl be transformed
        using (var z31 = new Disposable())
        {
          using var z32 = new Disposable();
          Console.WriteLine($"After dispose {z31} {z32}");
        }
      }
      __aspect_return_2:
        if (x == 2)
        {
          goto __aspect_return_1;
        }
      //z61 should be transformed
      using (var z61 = new Disposable())
      {
        using var z62 = new Disposable();
        Console.WriteLine($"After aspect1 {z61} {z62}");
      }
    }
    __aspect_return_1:
      if (x == 2)
      {
        return;
      }
    // z91 should be transformed
    using var z91 = new Disposable();
    using var z92 = new Disposable();
    Console.WriteLine($"After aspect2 {z91} {z92}");
  }
}