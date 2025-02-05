partial class Target
{
  [TheAspect]
  partial int this[int i] { get; set; }
  partial int this[int i]
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
  partial int this[long i] { get; }
  partial int this[long i]
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return default;
    }
  }
}