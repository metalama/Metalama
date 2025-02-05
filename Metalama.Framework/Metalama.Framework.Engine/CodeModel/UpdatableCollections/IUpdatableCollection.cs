// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

internal interface IUpdatableCollection
{
    CompilationModel Compilation { get; }

    IUpdatableCollection Clone( CompilationModel compilation );
}

internal interface IUpdatableCollection<T> : IReadOnlyList<T>, IUpdatableCollection
    where T : class, IRef
{
    ImmutableArray<T> OfName( string name );
}