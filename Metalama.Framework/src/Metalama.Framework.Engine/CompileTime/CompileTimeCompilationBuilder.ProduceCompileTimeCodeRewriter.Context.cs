// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed partial class CompileTimeCompilationBuilder
{
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1001  // Types that own disposable fields should be disposable
    private sealed partial class ProduceCompileTimeCodeRewriter
#pragma warning restore // Types that own disposable fields should be disposable
#pragma warning restore // Remove unnecessary suppression
    {
        private sealed class Context : IDisposable
        {
            private readonly ProduceCompileTimeCodeRewriter _parent;
            private readonly Context _oldContext;

            public Context(
                TemplatingScope scope,
                INamedTypeSymbol? nestedType,
                string? nestedTypeNewName,
                int nestingLevel,
                ProduceCompileTimeCodeRewriter parent )
            {
                this.Scope = scope;
                this.NestedTypeNewName = nestedTypeNewName;
                this.NestingLevel = nestingLevel;
                this.NestedType = nestedType;
                this._parent = parent;

                // This will be null for the root context.
                this._oldContext = parent._currentContext;
            }

            public TemplatingScope Scope { get; }

            public string? NestedTypeNewName { get; }

            public int NestingLevel { get; }

            public INamedTypeSymbol? NestedType { get; }

            public void Dispose() => this._parent._currentContext = this._oldContext;
        }
    }
}