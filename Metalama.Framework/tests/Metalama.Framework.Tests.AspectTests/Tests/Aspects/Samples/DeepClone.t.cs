[DeepClone]
internal partial class AutomaticallyCloneable : ICloneable
{
  public int A;
  public ManuallyCloneable? B;
  public AutomaticallyCloneable? C;
  public NotCloneable? D;
  public virtual AutomaticallyCloneable Clone()
  {
    var clone = (AutomaticallyCloneable)this.MemberwiseClone();
    clone.B = (ManuallyCloneable? )B?.Clone();
    clone.C = (C?.Clone());
    return clone;
  }
  object ICloneable.Clone()
  {
    return Clone();
  }
}
internal class ManuallyCloneable : ICloneable
{
  public int E;
  public object Clone()
  {
    return new ManuallyCloneable()
    {
      E = E
    };
  }
}
internal class NotCloneable
{
  public int F;
}
internal partial class Derived : AutomaticallyCloneable
{
  public ManuallyCloneable? G { get; private set; }
  public override Derived Clone()
  {
    var clone = (Derived)base.Clone();
    clone.G = (ManuallyCloneable? )G?.Clone();
    return clone;
  }
}