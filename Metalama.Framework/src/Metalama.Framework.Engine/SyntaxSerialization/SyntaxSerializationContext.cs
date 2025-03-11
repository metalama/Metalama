// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal sealed class SyntaxSerializationContext : ISyntaxGenerationContext
{
    private int _recursionLevel;

    public SyntaxSerializationContext( CompilationModel compilation, SyntaxGenerationOptions syntaxGenerationOptions ) : this(
        compilation,
        compilation.CompilationContext.GetSyntaxGenerationContext( syntaxGenerationOptions ),
        null ) { }

    public SyntaxSerializationContext( CompilationModel compilation, SyntaxGenerationContext syntaxGenerationContext, INamedType? currentType )
    {
        this.CompilationModel = compilation;
        this.SyntaxGenerationContext = syntaxGenerationContext;
        this.CurrentType = currentType;
    }

    public CompilationContext CompilationContext => this.CompilationModel.CompilationContext;

    public ITypeSymbol GetTypeSymbol( Type type ) => this.CompilationContext.ReflectionMapper.GetTypeSymbol( type );

    public TypeSyntax GetTypeSyntax( Type type ) => this.SyntaxGenerator.TypeSyntax( this.CompilationContext.ReflectionMapper.GetTypeSymbol( type ) );

    public CompilationModel CompilationModel { get; }

    public SyntaxGenerationContext SyntaxGenerationContext { get; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public INamedType? CurrentType { get; }

    public ContextualSyntaxGenerator SyntaxGenerator => this.SyntaxGenerationContext.SyntaxGenerator;

    public DisposeAction WithSerializeObject<T>( [UsedImplicitly] T o )
    {
        this._recursionLevel++;

        if ( this._recursionLevel > 32 )
        {
            throw SerializationDiagnosticDescriptors.CycleInSerialization.CreateException( typeof(T) );
        }

        return new DisposeAction( () => this._recursionLevel-- );
    }
}