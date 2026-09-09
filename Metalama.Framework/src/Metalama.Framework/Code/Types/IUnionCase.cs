// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code.Types;

/// <summary>
/// Represents one case of a union, reached through <see cref="IUnionFacet.Cases"/>.
/// </summary>
/// <remarks>
/// <para>
/// A case is more than its type. The compiler reports the case types of a union as a set, so two cases never have the
/// same type and <see cref="Index"/> cannot be recovered from <see cref="Type"/>. The member that creates a value of
/// the case is a constructor for one authoring form and a static method for another, so it cannot be derived from the
/// type either.
/// </para>
/// </remarks>
/// <seealso cref="IUnionFacet.Cases"/>
[CompileTime]
[InternalImplement]
public interface IUnionCase
{
    /// <summary>
    /// Gets the type of the case, which is an ordinary type declared elsewhere. A union declaration names it in its
    /// header, and no type is declared by the case itself.
    /// </summary>
    IType Type { get; }

    /// <summary>
    /// Gets the zero-based index of the case, in the order in which <see cref="IUnionFacet.Cases"/> reports the cases.
    /// </summary>
    int Index { get; }

    /// <summary>
    /// Gets the member that creates a value of the case: the constructor that the compiler synthesizes for a union
    /// declaration, the public single-parameter constructor of the attribute form, or the static <c>Create</c> method
    /// of the <c>IUnionMembers</c> interface of the attribute form, or of an interface that this interface inherits.
    /// </summary>
    /// <remarks>
    /// The member is typed as <see cref="IMethodBase"/> because it is a constructor for one authoring form and a
    /// method for another. A consumer that invokes it tests whether it is an <see cref="IMethod"/> or an
    /// <see cref="IConstructor"/>, because <see cref="IMethodBase"/> declares no invoker of its own.
    /// </remarks>
    IMethodBase CreationMember { get; }
}
