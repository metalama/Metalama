namespace Metalama.Open.Virtuosity.TestApp
{
  [Virtualize]
  internal class C
  {
    // Not transformed.
    private void ImplicitPrivate()
    {
    }
    // Not transformed.
    private void ExplicitPrivate()
    {
    }
    // Transformed.
    public virtual void Public()
    {
    }
    // Not transformed (already virtual).
    public virtual void PublicVirtual()
    {
    }
    // Transformed.
    protected async virtual void Protected()
    {
    }
    // Transformed.
    private protected virtual void PrivateProtected()
    {
    }
    // Transformed (should not be sealed).
    public override string ToString()
    {
      return null;
    }
    // Not transformed.
    public override int GetHashCode()
    {
      return 0;
    }
    // Not transformed.
    public static void PublicStatic()
    {
    }
    public int Property { get; }
  }
  internal sealed partial class SC
  {
    public void M()
    {
    }
  }
}