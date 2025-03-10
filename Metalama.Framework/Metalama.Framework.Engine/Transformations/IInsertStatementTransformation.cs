// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Transformations;

/// <summary>
/// Represents a code transformation that insert statements into the target member.
/// </summary>
internal interface IInsertStatementTransformation : ISyntaxTreeTransformation
{
    /// <summary>
    /// Provides an list of inserted statements.
    /// </summary>
    /// <param name="context">Context for providing inserted statements.</param>
    /// <returns>A list of Inserted statements or empty list if an error occured.</returns>
    IReadOnlyList<InsertedStatement> GetInsertedStatements( InsertStatementTransformationContext context );

    /// <summary>
    /// Gets the member that statements will be inserted into (may differ from <see cref="ITransformationBase.TargetDeclaration"/> e.g. for builders).
    /// </summary>
    IFullRef<IMember> TargetMember { get; }
}