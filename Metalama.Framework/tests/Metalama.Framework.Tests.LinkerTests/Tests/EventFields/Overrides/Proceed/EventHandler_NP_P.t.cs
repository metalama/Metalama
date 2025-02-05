class Target
{
  event EventHandler? Foo
  {
    add
    {
      Console.WriteLine("Override2");
    }
    remove
    {
      Console.WriteLine("Override2");
    }
  }
}