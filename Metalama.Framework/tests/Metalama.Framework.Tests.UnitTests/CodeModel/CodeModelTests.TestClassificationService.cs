// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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