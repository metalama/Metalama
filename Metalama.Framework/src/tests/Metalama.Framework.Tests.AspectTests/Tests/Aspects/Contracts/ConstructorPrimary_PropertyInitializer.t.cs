[Validate]
internal sealed class Target : IDisposable
{
  private readonly Action _action = default !;
  private Action Action
  {
    get
    {
      return this._action;
    }
    init
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException(nameof(value));
      }
      this._action = value;
    }
  }
  void IDisposable.Dispose()
  {
    this.Action();
  }
  public Target(Action action)
  {
    this.Action = action;
    if (action == null)
    {
      throw new global::System.ArgumentNullException("action");
    }
  }
}