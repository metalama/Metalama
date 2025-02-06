[Observable]
public partial class InheritsExistingInpcImplWithValidOpcMethodNamedNotifyOfPropertyChange : ExistingInpcImplWithValidOpcMethodNamedNotifyOfPropertyChange
{
  protected override void NotifyOfPropertyChange(string propertyName)
  {
    base.NotifyOfPropertyChange(propertyName);
  }
}