// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.LinqPad.Tests.Assets;

public interface IBase
{
    [UsedImplicitly]
    int Property { get; }
}