// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Patterns.Wpf.Implementation.NamingConvention;

[CompileTime]
internal interface INamingConvention : ICompileTimeSerializable
{
    string Name { get; }
}

[CompileTime]
internal interface INamingConvention<in TDeclaration, out TMatch> : INamingConvention
    where TMatch : NamingConventionMatch
    where TDeclaration : IDeclaration
{
    TMatch Match( TDeclaration arguments );
}