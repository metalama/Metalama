// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CompileTime;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed partial class CodeModelTests
{
    private sealed class TestClassificationService : ISymbolClassificationService
    {
        public ExecutionScope GetExecutionScope( ISymbol symbol )
            => symbol.GetAttributes().Any( a => a.AttributeClass?.Name == nameof(CompileTimeAttribute) )
                ? ExecutionScope.CompileTime
                : ExecutionScope.Default;

        public bool IsTemplate( ISymbol symbol ) => throw new NotImplementedException();

        public bool IsCompileTimeParameter( IParameterSymbol symbol ) => throw new NotImplementedException();

        public bool IsCompileTimeTypeParameter( ITypeParameterSymbol symbol ) => throw new NotImplementedException();
    }
}