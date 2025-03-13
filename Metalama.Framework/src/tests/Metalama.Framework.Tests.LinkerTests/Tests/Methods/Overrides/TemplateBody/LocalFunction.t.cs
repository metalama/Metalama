class TargetClass
{
  int IntMethod(int x)
  {
    return LocalFunction() + LocalFunction();
    int LocalFunction()
    {
      Console.WriteLine("Override");
      global::System.Int32 z;
      Console.WriteLine("Original");
      z = x;
      return z;
    }
  }
  string? StringMethod(string x)
  {
    return ToUpper();
    string? ToUpper()
    {
      Console.WriteLine("Override");
      return this.StringMethod_Source(x)?.ToUpper();
    }
  }
  private string? StringMethod_Source(string x)
  {
    Console.WriteLine("Original");
    return x;
  }
  void VoidMethod()
  {
    LocalFunction();
    LocalFunction();
    void LocalFunction()
    {
      Console.WriteLine("Override");
      Console.WriteLine("Original");
    }
  }
}