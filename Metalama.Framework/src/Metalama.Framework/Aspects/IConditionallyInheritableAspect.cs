// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Aspects;

/// <summary>
/// An interface that can be implemented by aspects that determine their inheritability dynamically
/// based on aspect properties or the target declaration. When all the instances of the aspect class are unconditionally inheritable,
/// use the <see cref="InheritableAttribute"/> instead.
/// </summary>
/// <remarks>
/// <para>
/// Implementing this interface allows an aspect to decide at runtime whether it should be inherited by derived declarations.
/// The <see cref="IsInheritable"/> method is called to determine whether a specific aspect instance should propagate to:
/// </para>
/// <list type="bullet">
/// <item><description>Derived classes (from a base class)</description></item>
/// <item><description>Derived interfaces (from a base interface)</description></item>
/// <item><description>Implementing types (from an interface)</description></item>
/// <item><description>Override members (from a virtual or abstract member)</description></item>
/// <item><description>Interface implementations (from an interface member)</description></item>
/// </list>
/// <para>
/// <b>Cross-project inheritance:</b> When the base declaration is in a referenced assembly, the <see cref="IAspect"/>
/// object itself and its <see cref="IAspectState"/> (if set) are serialized into that assembly and deserialized
/// when compiling the derived project. Ensure your aspect class and any custom state are compile-time serializable.
/// </para>
/// <para>
/// <b>Note:</b> When this interface is implemented, the IDE refactoring menu will always suggest adding the aspect
/// to a declaration, even if the aspect is eligible for inheritance only on the target declaration.
/// </para>
/// </remarks>
/// <seealso cref="InheritableAttribute"/>
/// <seealso href="@aspect-inheritance"/>
public interface IConditionallyInheritableAspect : IAspect
{
    /// <summary>
    /// Determines whether this aspect instance should be inherited by derived declarations.
    /// </summary>
    /// <param name="targetDeclaration">The declaration to which the aspect is applied.</param>
    /// <param name="aspectInstance">The aspect instance being evaluated.</param>
    /// <returns><c>true</c> if the aspect should be inherited by derived declarations; otherwise, <c>false</c>.</returns>
    bool IsInheritable( IDeclaration targetDeclaration, IAspectInstance aspectInstance );
}