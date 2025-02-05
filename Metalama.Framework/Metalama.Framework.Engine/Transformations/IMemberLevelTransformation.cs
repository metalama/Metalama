// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.Transformations;

internal interface IMemberLevelTransformation : ISyntaxTreeTransformation
{
    /// <summary>
    /// Gets a target method base of this code transformation.
    /// </summary>
    IFullRef<IMember> TargetMember { get; }
}