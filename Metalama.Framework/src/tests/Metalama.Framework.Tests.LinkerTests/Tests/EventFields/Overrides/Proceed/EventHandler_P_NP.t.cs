class Target
{
  event EventHandler? Foo
  {
    add
    {
      Console.WriteLine("Override2 Start");
      Console.WriteLine("Override1");
      Console.WriteLine("Override2 End");
    }
    remove
    {
      Console.WriteLine("Override2 Start");
      Console.WriteLine("Override1");
      Console.WriteLine("Override2 End");
    }
  }
}