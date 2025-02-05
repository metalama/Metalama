// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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