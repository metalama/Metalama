// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

/// <summary>
/// Updatable collection for extension blocks within a static class.
/// Extension blocks don't have unique names (they're identified by receiver type),
/// so this collection uses a non-uniquely named collection pattern.
/// </summary>
internal sealed class ExtensionBlockUpdatableCollection : NonUniquelyNamedUpdatableCollection<IExtensionBlock>
{
    public ExtensionBlockUpdatableCollection( CompilationModel compilation, IFullRef<INamedType> declaringType )
        : base( compilation, declaringType.As<INamespaceOrNamedType>() ) { }

    protected override IEqualityComparer<IRef<IExtensionBlock>> MemberRefComparer => RefEqualityComparer<IExtensionBlock>.Default;

    protected override DeclarationKind ItemsDeclarationKind => DeclarationKind.ExtensionBlock;
}
#endif