public class AttributeOverridesFabric : INotifyPropertyChanged
{
  public event PropertyChangedEventHandler? PropertyChanged;
  [Command]
  private void ExecuteFoo1()
  {
  }
  public bool CanExecuteFoo1 => true;
  [Command(EnableINotifyPropertyChangedIntegration = true)]
  private void ExecuteFoo2()
  {
  }
  public bool CanExecuteFoo2 => true;
  public AttributeOverridesFabric()
  {
    Foo2Command = DelegateCommandFactory.CreateDelegateCommand(ExecuteFoo2, () => CanExecuteFoo2, this, "CanExecuteFoo2");
    Foo1Command = DelegateCommandFactory.CreateDelegateCommand(ExecuteFoo1, () => CanExecuteFoo1);
  }
  public DelegateCommand Foo1Command { get; }
  public DelegateCommand Foo2Command { get; }
}