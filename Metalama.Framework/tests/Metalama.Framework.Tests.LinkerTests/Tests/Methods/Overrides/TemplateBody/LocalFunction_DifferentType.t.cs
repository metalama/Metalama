class TargetClass
{
  int IntMethod_VoidLocalFunction(int x)
  {
    LocalFunction();
    LocalFunction();
    return 42;
    void LocalFunction()
    {
      Console.WriteLine("Override");
      Console.WriteLine("Original");
      _ = (global::System.Int32)x;
    }
  }
  void VoidMethod_IntLocalFunction()
  {
    _ = LocalFunction();
    _ = LocalFunction();
    int LocalFunction()
    {
      Console.WriteLine("Override");
      Console.WriteLine("Original");
      return 42;
    }
  }
  int IntMethod_StringLocalFunction(int x)
  {
    return LocalFunction().Length + LocalFunction().Length;
    string LocalFunction()
    {
      Console.WriteLine("Override");
      global::System.Int32 r;
      Console.WriteLine("Original");
      r = x;
      return $"{r}";
    }
  }
}