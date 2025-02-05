// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Diagnostics;

[PublicAPI]
public static class LocationExtensions
{
    public static IDiagnosticLocation ToDiagnosticLocation( this Location? location ) => new LocationWrapper( location );

    public static Location? GetDiagnosticLocation( this IDiagnosticLocation? location )
        => location switch
        {
            ISdkDeclaration sdkDeclaration => sdkDeclaration.DiagnosticLocation,
            LocationWrapper wrapper => wrapper.DiagnosticLocation,
            SourceReference sourceReference => sourceReference.NodeOrTokenInternal switch
            {
                SyntaxNode node => node.GetDiagnosticLocation(),
                SyntaxToken token => token.GetLocation(),
                SyntaxNodeOrToken { IsNode: true } nodeOrToken => nodeOrToken.AsNode().GetDiagnosticLocation(),
                SyntaxNodeOrToken { IsToken: true } nodeOrToken => nodeOrToken.AsToken().GetLocation(),
                _ => throw new ArgumentOutOfRangeException()
            },
            _ => throw new NotImplementedException( $"Type {location.GetType()} not supported." )
        };
}