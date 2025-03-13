// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Transformations;

internal enum InsertedStatementKind
{
    /// <summary>
    /// Insert statement into the beginning of the final version of the declaration, in transformation order. 
    /// </summary>
    Initializer = -200,

    /// <summary>
    /// Insert statement into the beginning of the current version of the target declaration (source, introduction or latest override). 
    /// Statements added by one layer have their order preserved.
    /// </summary>
    InputContract = -100,

    /// <summary>
    /// Insert statement into the end of an auxiliary declaration for the current version of the target declaration (source, introduction or latest override). 
    /// Statements added by one layer have their order preserved.
    /// </summary>
    OutputContract = 100
}