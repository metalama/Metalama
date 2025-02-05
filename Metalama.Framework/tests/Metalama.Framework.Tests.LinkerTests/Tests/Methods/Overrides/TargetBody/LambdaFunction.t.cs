class Target
{
  int IntMethod()
  {
    Console.WriteLine("Before");
    global::System.Int32 y;
    if (new Random().Next() == 0)
    {
      y = 0;
      goto __aspect_return_1;
    }
    Action foo = () =>
    {
      return;
    };
    Func<int> bar = () =>
    {
      Func<int> quz = () => 42;
      return quz();
    };
    foo();
    var x = bar();
    Console.WriteLine("Original");
    y = x;
    goto __aspect_return_1;
    __aspect_return_1:
      Console.WriteLine("After");
    return y;
  }
  void VoidMethod()
  {
    Console.WriteLine("Before");
    if (new Random().Next() == 0)
    {
      goto __aspect_return_1;
    }
    Action foo = () =>
    {
      return;
    };
    Func<int> bar = () =>
    {
      Func<int> quz = () => 42;
      return quz();
    };
    foo();
    _ = bar();
    Console.WriteLine("Original");
    __aspect_return_1:
      Console.WriteLine("After");
  }
}