class TargetClass
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
    Foo();
    var x = Bar();
    Console.WriteLine("Original");
    y = x;
    goto __aspect_return_1;
    void Foo()
    {
      return;
    }
    int Bar()
    {
      int Quz() => 42;
      return Quz();
    }
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
    Foo();
    _ = Bar();
    Console.WriteLine("Original");
    void Foo()
    {
      return;
    }
    int Bar()
    {
      int Quz() => 42;
      return Quz();
    }
    __aspect_return_1:
      Console.WriteLine("After");
  }
}