// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Diagnostics;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Code
{
    internal interface ICompilationHelpers
    {
        IteratorInfo GetIteratorInfo( IMethod method );

        AsyncInfo GetAsyncInfo( IMethod method );

        AsyncInfo GetAsyncInfo( IType type );

        string GetMetadataName( INamedType type );

        string GetFullMetadataName( INamedType type );

        SerializableTypeId GetSerializableId( IType type );

        IExpression ToTypeOfExpression( IType type );

        bool DerivesFrom( INamedType left, INamedType right, DerivedTypesOptions options = DerivedTypesOptions.Default );

        bool TryConstructAttribute( IAttribute attribute, ScopedDiagnosticSink diagnosticSink, [NotNullWhen( true )] out Attribute? constructedAttribute );

        Attribute ConstructAttribute( IAttribute attribute );
    }
}