// Warning BUG780 on `TargetClass`: `Method 'PartialWithImpl' HasImplementation: True`
// Warning BUG780 on `TargetClass`: `Method 'PartialWithoutImpl' HasImplementation: True`
[OverridePartialAspect]
[CheckHasImplementationAspect]
internal partial class TargetClass
{
  partial void PartialWithoutImpl()
  {
    global::System.Console.WriteLine("Overridden.");
    return;
  }
  partial void PartialWithImpl();
}
internal partial class TargetClass
{
  partial void PartialWithImpl()
  {
    global::System.Console.WriteLine("Overridden.");
    Console.WriteLine("Original implementation.");
    return;
  }
}
