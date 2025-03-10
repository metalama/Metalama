// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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