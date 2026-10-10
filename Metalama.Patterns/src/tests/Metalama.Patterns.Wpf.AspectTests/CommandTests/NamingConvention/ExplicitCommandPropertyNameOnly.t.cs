public class ExplicitCommandPropertyNameOnly
{
  // The explicit name must be used even when no CanExecute member is specified.
  [Command(CommandPropertyName = "TheFooCommand")]
  private void ExecuteFoo()
  {
  }
  // Found by the default CanExecute name, which still applies.
  private bool CanExecuteFoo() => true;
  // Conflicts with the name determined by the default naming convention.
  private int FooCommand;
  public ExplicitCommandPropertyNameOnly()
  {
    TheFooCommand = DelegateCommandFactory.CreateDelegateCommand(ExecuteFoo, CanExecuteFoo);
  }
  public DelegateCommand TheFooCommand { get; }
}