// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.ComponentModel;

namespace Metalama.Patterns.Observability.AspectTests.Diagnostics;

public class ExistingInpcImplWithOPCMethodWithWrongParamType : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged( int propertyIdx ) { }
}

// <target>
[Observable]
public partial class InheritsExistingInpcImplWithOpcMethodWithWrongParamType : ExistingInpcImplWithOPCMethodWithWrongParamType { }