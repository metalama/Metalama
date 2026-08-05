[MyAspect]
public partial class TargetClass
{
  public TargetClass()
  {
  }
  // Chains to the constructor above, so the pull runs and the type of the introduced parameter has to be resolved.
  public TargetClass(int x) : this()
  {
  }
}