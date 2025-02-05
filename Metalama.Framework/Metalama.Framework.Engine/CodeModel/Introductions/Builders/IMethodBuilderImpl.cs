// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.Collections;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal interface IMethodBuilderImpl : IMethodBuilder, IMethodImpl, IMemberOrNamedTypeBuilderImpl
{
    new TypeParameterBuilderList TypeParameters { get; }

    new BaseParameterBuilder ReturnParameter { get; }

    IntroducedRef<IMethod> Ref { get; }
}