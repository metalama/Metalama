[Aspect]
internal class Target : ICloneable
{
  private int _field;
  private int Property { get; set; }
  private int this[int i] => 42;
  private event EventHandler? Event;
  public static Target operator -(Target target) => target;
  public object Clone() => new Target();
  ~Target()
  {
  }
}