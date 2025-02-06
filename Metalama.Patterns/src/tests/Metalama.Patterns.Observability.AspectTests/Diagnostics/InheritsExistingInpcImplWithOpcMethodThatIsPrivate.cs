// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.ComponentModel;

namespace Metalama.Patterns.Observability.AspectTests.Diagnostics;

public class ExistingInpcImplWithOPCMethodThatIsPrivate : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged( string propertyName ) { }
}

// <target>
[Observable]
public partial class InheritsExistingInpcImplWithOpcMethodThatIsPrivate : ExistingInpcImplWithOPCMethodThatIsPrivate { }