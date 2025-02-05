internal abstract class Base
{
  public abstract void M([NotNull] string m);
}
internal class Target : Base
{
  public override void M(string m)
  {
    if (m == null)
    {
      throw new global::System.ArgumentNullException("m");
    }
  }
}