// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.CodeModel.References;

internal interface IDurableRef : IRefImpl
{
    string Id { get; }

    IFullRef ToFullRef( RefFactory refFactory );
}