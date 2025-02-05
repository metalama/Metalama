// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.CodeModel.GenericContexts;

internal static class GenericContextExtensions
{
    public static GenericContext AsGenericContext( this IGenericContext? genericContext ) => (GenericContext?) genericContext ?? GenericContext.Empty;
}