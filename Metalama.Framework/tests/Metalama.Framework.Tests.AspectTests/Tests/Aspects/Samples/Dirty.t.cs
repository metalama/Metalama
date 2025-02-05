[Dirty]
public class TargetClass : IDirty
{
  private int _a;
  public int A
  {
    get
    {
      return _a;
    }
    set
    {
      _a = value;
      if (this.DirtyState == DirtyState.Clean)
      {
        this.DirtyState = DirtyState.Dirty;
      }
    }
  }
  public DirtyState DirtyState { get; protected set; }
}