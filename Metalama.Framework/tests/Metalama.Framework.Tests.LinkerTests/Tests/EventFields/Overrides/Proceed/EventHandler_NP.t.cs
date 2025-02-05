class Target
{
  event EventHandler? Foo
  {
    add
    {
      Console.WriteLine("Override");
    }
    remove
    {
      Console.WriteLine("Override");
    }
  }
}