// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Extensibility;
using System;
using System.Collections.Generic;

namespace Metalama.Testing.UnitTesting;

internal sealed class TestExtensionCollector : ITestExtensionCollector
{
    private List<Type>? _extensionTypes;
    private List<Type>? _designTimeExtensionTypes;
    private List<string>? _compileTimeAssemblies;

    public IReadOnlyList<Type> ExtensionTypes => this._extensionTypes ?? [];

    public IReadOnlyList<Type> DesignTimeExtensionTypes => this._designTimeExtensionTypes ?? [];

    public IReadOnlyList<string> CompileTimeAssemblies => this._compileTimeAssemblies ?? [];

    public void AddExtension( Type extensionType )
    {
        this._extensionTypes ??= new List<Type>();
        this._extensionTypes.Add( extensionType );
    }

    public void AddDesignTimeExtension( Type extensionType )
    {
        this._designTimeExtensionTypes ??= new List<Type>();
        this._designTimeExtensionTypes.Add( extensionType );
    }

    public void AddCompileTimeAssembly( string path )
    {
        this._compileTimeAssemblies ??= new List<string>();
        this._compileTimeAssemblies.Add( path );
    }
}