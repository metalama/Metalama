[Observable]
public partial class InheritsExistingInpcImplWithValidOpcMethodNamedRaisePropertyChanged : ExistingInpcImplWithValidOpcMethodNamedRaisePropertyChanged
{
  protected override void RaisePropertyChanged(string propertyName)
  {
    base.RaisePropertyChanged(propertyName);
  }
}