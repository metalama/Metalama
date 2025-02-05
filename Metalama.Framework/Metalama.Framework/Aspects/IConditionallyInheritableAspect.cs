// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Aspects;

/// <summary>
/// An interface that can be implemented by aspect that can be inheritable or non-inheritable
/// based of a field or property of the aspect. When all the instances of the aspect class are unconditionally inheritable,
/// the class must be annotated with the <see cref="InheritableAttribute"/> instead.
/// </summary>
public interface IConditionallyInheritableAspect : IAspect
{
    bool IsInheritable( IDeclaration targetDeclaration, IAspectInstance aspectInstance );
}