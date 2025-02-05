partial class Target
{
  [TheAspect]
  partial int P1 { get; set; }
  partial int P1
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return default;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
    }
  }
  [TheAspect]
  partial int P2 { get; }
  partial int P2
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return default;
    }
  }
}