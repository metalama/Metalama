// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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