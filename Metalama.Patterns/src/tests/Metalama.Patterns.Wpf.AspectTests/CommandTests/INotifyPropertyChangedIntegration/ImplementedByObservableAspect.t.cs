using System.ComponentModel;
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.
// TODO: #34044 Can't use RequireOrderedAspects yet. If not fixed, re-enable when DependencyPropertyAttribute aspect is also ordered.
// __RequireOrderedAspects__
using Metalama.Patterns.Observability;
namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.INotifyPropertyChangedIntegration.ImplementedByObservableAspect;
[Observable]
public class ImplementedByObservableAspect : INotifyPropertyChanged
{
  [Command]
  private void ExecuteFoo1()
  {
  }
  private bool _canExecuteFoo1;
  public bool CanExecuteFoo1
  {
    get
    {
      return _canExecuteFoo1;
    }
    set
    {
      if (_canExecuteFoo1 != value)
      {
        _canExecuteFoo1 = value;
        OnPropertyChanged("CanExecuteFoo1");
      }
    }
  }
  public ImplementedByObservableAspect()
  {
    Foo1Command = DelegateCommandFactory.CreateDelegateCommand(ExecuteFoo1, () => CanExecuteFoo1, this, "CanExecuteFoo1");
  }
  public DelegateCommand Foo1Command { get; }
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
  public event PropertyChangedEventHandler? PropertyChanged;
}
public class ImplementedByBase : ImplementedByObservableAspect
{
  [Command]
  private void ExecuteFoo2()
  {
  }
  private bool _canExecuteFoo2;
  public bool CanExecuteFoo2
  {
    get
    {
      return _canExecuteFoo2;
    }
    set
    {
      if (_canExecuteFoo2 != value)
      {
        _canExecuteFoo2 = value;
        OnPropertyChanged("CanExecuteFoo2");
      }
    }
  }
  public ImplementedByBase()
  {
    Foo2Command = DelegateCommandFactory.CreateDelegateCommand(ExecuteFoo2, () => CanExecuteFoo2, this, "CanExecuteFoo2");
  }
  public DelegateCommand Foo2Command { get; }
  protected override void OnPropertyChanged(string propertyName)
  {
    base.OnPropertyChanged(propertyName);
  }
}