// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code;

/// <summary>
/// Options for the <see cref="ICompilation.GetDerivedTypes(Metalama.Framework.Code.INamedType,Metalama.Framework.Code.DerivedTypesOptions)"/> method.
/// </summary>
[CompileTime]
public enum DerivedTypesOptions
{
    /// <summary>
    /// Equivalent to <see cref="All"/>.
    /// </summary>
    Default,

    /// <summary>
    /// Returns all types declared in the current compilation that derive from the given type, directly or indirectly.
    /// </summary>
    All = Default,

    /// <summary>
    /// Only returns types declared in the current compilation that directly derive from the given type.
    /// </summary>
    DirectOnly,

    /// <summary>
    /// Only returns types of the current compilation that derive from the given type or from an intermediate derived type of the given type, only
    /// if the derived type is an external type. That is, does not return types of the current compilation that derive from another type in
    /// the current compilation that derives from the given type.
    /// </summary>
    FirstLevelWithinCompilationOnly,

    /// <summary>
    /// Returns types of the current compilation and of any referenced project or assembly. This setting is dangerous because not all types referenced
    /// by the current compilation are returned: only types <i>declared</i> or <i>used as attributes</i> in the current compilation and all their base
    /// types are indexed. 
    /// </summary>
    IncludingExternalTypesDangerous
}