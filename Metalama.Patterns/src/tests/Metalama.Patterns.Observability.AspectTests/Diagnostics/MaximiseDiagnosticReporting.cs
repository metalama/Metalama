// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.ComponentModel;

namespace Metalama.Patterns.Observability.AspectTests.Diagnostics.MaximiseErrorReporting;

// A class with multiple errors covering different concerns. The aspect should report all of these (ie, it must not give up on error reporting early).

// Base with no "OnPropertyChanged" method (base validation concern)
public class Base : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
}

// <target>
[Observable]
public partial class Test : Base
{
    // Root property validation concern
    public virtual int VirtualProperty { get; set; }

    public string? B { get; set; }

    // Dependency analysis concern
    public string CallsInstanceMethod => this.B ?? this.Method();

    // ReSharper disable once MemberCanBeMadeStatic.Local
    private string Method()
    {
        return "hello";
    }
}