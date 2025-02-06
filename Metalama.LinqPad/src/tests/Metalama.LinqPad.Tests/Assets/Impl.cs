// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.LinqPad.Tests.Assets;

internal sealed class Impl : IDerived
{
    int IBase.Property => throw new NotImplementedException();

    string IDerived.Property => throw new NotImplementedException();
}