// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

// ReSharper disable IdentifierTypo
// Resharper disable UnusedMember.Global

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets
{
    [Flags]
    internal enum WorldFeatures : ulong
    {
        Icy = 1,
        Edenlike = 2,
        Poisonous = 4,
        Volcanic = 8
    }
}