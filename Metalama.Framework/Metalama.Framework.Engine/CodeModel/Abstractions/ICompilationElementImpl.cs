// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.CodeModel.Abstractions;

internal interface ICompilationElementImpl : ICompilationElement
{
    new CompilationModel Compilation { get; }

    ICompilationElement? Translate(
        CompilationModel newCompilation,
        IGenericContext? genericContext = null,
        Type? interfaceType = null );
}