using System.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.InheritFromAbstractBase;
public partial class C11 : C10
{
  /// <summary>
  /// Ref to <see cref = "C10.C10P1"/>.
  /// </summary>
  public int C11P1 => this.C10P1;
  protected override void OnPropertyChanged(string propertyName)
  {
    switch (propertyName)
    {
      case "C10P1":
        OnPropertyChanged("C11P1");
        break;
    }
    base.OnPropertyChanged(propertyName);
  }
}
[Observable]
public abstract partial class C10 : INotifyPropertyChanged
{
  private int _c10P1;
  /// <summary>
  /// Auto
  /// </summary>
  public int C10P1
  {
    get
    {
      return _c10P1;
    }
    set
    {
      if (_c10P1 != value)
      {
        _c10P1 = value;
        OnPropertyChanged("C10P1");
      }
    }
  }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}