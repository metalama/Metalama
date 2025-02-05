// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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