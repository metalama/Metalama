// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Advising;

// This class is used in tests.
[CompileTime]
public static class _ImplementInterfaceAdviceResultExtensions
{
    public static IReadOnlyCollection<IInterfaceMemberImplementationResult> GetObsoleteInterfaceMembers( this IImplementInterfaceAdviceResult result )
#pragma warning disable CS0618 // Type or member is obsolete
        => result.InterfaceMembers;
#pragma warning restore CS0618 // Type or member is obsolete
}