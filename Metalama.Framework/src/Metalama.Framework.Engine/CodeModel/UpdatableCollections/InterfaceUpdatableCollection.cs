// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.InterfaceImplementation;
using Metalama.Framework.Engine.CodeModel.References;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

internal sealed class InterfaceUpdatableCollection : DeclarationUpdatableCollection<INamedType>
{
    private readonly IRef<INamedType> _declaringType;

    public ImmutableArray<IntroduceInterfaceTransformation> Introductions { get; private set; } = ImmutableArray<IntroduceInterfaceTransformation>.Empty;

    public InterfaceUpdatableCollection( CompilationModel compilation, IRef<INamedType> declaringType ) : base( compilation )
    {
        this._declaringType = declaringType;
    }

    public void Add( IntroduceInterfaceTransformation introduction )
    {
        this.EnsureComplete();
        this.AddItem( introduction.InterfaceType );

        this.Introductions = this.Introductions.Add( introduction );
    }

    protected override void PopulateAllItems( Action<IFullRef<INamedType>> action )
    {
        this._declaringType.AsFullRef().EnumerateImplementedInterfaces( action );
    }
}