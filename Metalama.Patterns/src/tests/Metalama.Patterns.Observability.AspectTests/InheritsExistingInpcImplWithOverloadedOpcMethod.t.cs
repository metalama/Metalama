[Observable]
public partial class InheritsExistingInpcImplWithOverloadedOpcMethod : ExistingInpcImplWithOverloadedOpcMethod
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
      if (_a != value)
      {
        _a = value;
        OnPropertyChanged("A");
      }
    }
  }
  protected override void OnPropertyChanged(string propertyName)
  {
    base.OnPropertyChanged(propertyName);
  }
}