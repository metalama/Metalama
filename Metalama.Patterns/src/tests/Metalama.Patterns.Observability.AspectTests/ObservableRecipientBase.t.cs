using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
namespace Metalama.Patterns.Observability.AspectTests.ObservableRecipientBase;
public class A : ObservableRecipient
{
  private string _p;
  public string P { get => this._p; set => this.SetProperty(ref this._p, value); }
}
[Observable]
public class B : A
{
  public string Q => this.P;
  protected override void OnPropertyChanged(PropertyChangedEventArgs args)
  {
    switch (args.PropertyName)
    {
      case "P":
        OnPropertyChanged("Q");
        break;
    }
    base.OnPropertyChanged(args);
  }
}