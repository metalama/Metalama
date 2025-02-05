class Target
{
  private event EventHandler? _foo;
  event EventHandler? Foo
  {
    add
    {
      Console.WriteLine("Before2");
      Console.WriteLine("Before1");
      if (new Random().Next() == 0)
      {
        goto __aspect_return_1;
      }
      this._foo += value;
      Console.WriteLine("After1");
      __aspect_return_1:
        Console.WriteLine("After2");
    }
    remove
    {
      Console.WriteLine("Before2");
      Console.WriteLine("Before1");
      if (new Random().Next() == 0)
      {
        goto __aspect_return_1;
      }
      this._foo += value;
      Console.WriteLine("After1");
      __aspect_return_1:
        Console.WriteLine("After2");
    }
  }
}