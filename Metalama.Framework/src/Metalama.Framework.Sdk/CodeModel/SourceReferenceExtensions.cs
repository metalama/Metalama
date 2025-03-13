// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel;

[PublicAPI]
public static class SourceReferenceExtensions
{
    public static SyntaxNodeOrToken SyntaxNodeOrToken( this in SourceReference sourceReference )
        => sourceReference.NodeOrTokenInternal switch
        {
            SyntaxNode node => node,
            SyntaxToken token => token,
            _ => throw new ArgumentOutOfRangeException()
        };
}