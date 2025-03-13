// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;
using System.Collections.Immutable;
using System.Linq;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// A compilation-independent version of <see cref="Code.TypedConstant"/>.
/// </summary>
internal readonly struct TypedConstantRef
{
    public object? RawValue { get; }

    // This property may be null if the type can be assumed from the value.
    // It's not necesseraly a IFulLRef since it could be deserialized.
    public IRef<IType>? Type { get; }

    public TypedConstantRef( object? value, IRef<IType>? type )
    {
        if ( value is Array )
        {
            throw new ArgumentOutOfRangeException( nameof(value), "An ImmutableArray<TypedConstantRef> was expected." );
        }

        this.RawValue = value;
        this.Type = type;
    }

    public TypedConstant ToTypedConstant( CompilationModel compilation )
    {
        var type = this.Type?.GetTargetOrNull( compilation );

        if ( this.RawValue == null && type == null )
        {
            return default;
        }

        return this.RawValue switch
        {
            null => TypedConstant.Default( type! ),
            ImmutableArray<TypedConstantRef> array => TypedConstant.Create( array.SelectAsImmutableArray( x => x.ToTypedConstant( compilation ) ), type! ),
            IRef<IType> valueAsType => TypedConstant.Create( valueAsType.GetTarget( compilation ), type! ),
            _ => TypedConstant.Create( this.RawValue, type! )
        };
    }
}