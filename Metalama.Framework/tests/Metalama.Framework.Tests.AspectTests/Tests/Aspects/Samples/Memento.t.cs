[Memento]
internal class TargetClass : IOriginator
{
  private int _state1;
  private int State2 { get; set; }
  public void Restore(IMemento memento)
  {
    _state1 = ((Memento)memento)._state1;
    State2 = ((Memento)memento).State2;
  }
  public IMemento Save()
  {
    return new Memento(_state1, State2);
  }
  public class Memento : IMemento
  {
    public readonly int State2;
    public readonly int _state1;
    public Memento(int _state1, int State2)
    {
      this._state1 = _state1;
      this.State2 = State2;
    }
  }
}