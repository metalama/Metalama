partial class Target
{
  [TheAspect]
  partial int this[int i] { get; set; }
  partial int this[int i]
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return 0;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
      throw new Exception();
    }
  }
  partial int this[string s] { get; set; }
  [TheAspect]
  partial int this[string s]
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return 0;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
      throw new Exception();
    }
  }
  [TheAspect]
  partial int this[long i] { get; }
  partial int this[long i]
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return 0;
    }
  }
}