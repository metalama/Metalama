// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using System;

namespace Metalama.Framework.Tests.ExtensionPoints.Engine;

/// <summary>
/// A <see cref="MethodBuilderRestrictions"/> defined by the test extension, which refuses to give a parameter a type of a given full name.
/// </summary>
internal sealed class TestRefusingRestrictions : MethodBuilderRestrictions
{
    private readonly string _refusedTypeFullName;

    public TestRefusingRestrictions( string refusedTypeFullName )
    {
        this._refusedTypeFullName = refusedTypeFullName;
    }

    /// <inheritdoc />
    public override void ValidateParameterType( IParameterBuilder parameter, IType type )
    {
        if ( type is INamedType { FullName: var fullName } && fullName == this._refusedTypeFullName )
        {
            throw new InvalidOperationException( $"The test restrictions refuse the type '{fullName}' for the parameter '{parameter.Name}'." );
        }
    }
}
