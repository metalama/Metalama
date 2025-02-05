// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if DEBUG && NETSTANDARD2_0
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

[AttributeUsage( AttributeTargets.Parameter )]
internal sealed class CallerArgumentExpressionAttribute : Attribute
{
    public CallerArgumentExpressionAttribute( string parameterName )
    {
        this.ParameterName = parameterName;
    }

    [UsedImplicitly]
    public string ParameterName { get; }
}

#endif