// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

#if NET5_0_OR_GREATER
using System;
#endif

namespace Metalama.Framework.Engine.CodeModel.References;

internal abstract class DurableRef<T> : BaseRef<T>, IDurableRef<T>
    where T : class, ICompilationElement
{
    public string Id { get; }

    public abstract IFullRef ToFullRef( RefFactory refFactory );

    protected DurableRef( string id )
    {
        this.Id = id;
    }

    protected override IDurableRef<T> ToDurable() => this;

    public override bool IsDurable => true;

    public override bool Equals( IRef? other, RefComparison comparison )
    {
        if ( other == null )
        {
            return false;
        }

        if ( other is not IDurableRef stringRef )
        {
            if ( comparison is RefComparison.Structural or RefComparison.StructuralIncludeNullability )
            {
                return this.Equals( other.ToDurable(), comparison );
            }
            else
            {
                return false;
            }
        }

        // String comparisons are always portable and null-sensitive, so we ignore all flags.

        return stringRef.Id == this.Id;
    }

    public override int GetHashCode( RefComparison comparison )
    {
#if NET5_0_OR_GREATER
        return this.Id.GetHashCode( StringComparison.Ordinal );
#else
        return this.Id.GetHashCode();
#endif
    }

    public override string ToString() => this.Id;
}