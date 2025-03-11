// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.ComponentModel;

namespace Metalama.Patterns.Observability.AspectTests.IntermediateInterface;

public interface IMyInterface : INotifyPropertyChanged
{
    int P { get; }
}

public class C : IMyInterface
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public int P => 0;
}

[Observable]
public class D
{
    public IMyInterface C { get; set; }

    public int P => this.C.P;
}