// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// Implemented by the test framework (Metalama.Framework.UnitTesting) to collect extensions. 
/// </summary>
public interface ITestExtensionCollector
{
    void AddExtension( Type extensionType );

    void AddDesignTimeExtension( Type extensionType );

    void AddCompileTimeAssembly( string path );
}