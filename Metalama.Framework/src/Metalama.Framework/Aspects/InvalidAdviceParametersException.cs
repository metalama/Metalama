// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// An exception thrown by <see cref="IAdviceFactory"/> methods when the parameters passed to an advice method
/// are invalid or incompatible with the target declaration.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown at compile time when an aspect's <see cref="IAspect{T}.BuildAspect"/> method
/// calls an advice factory method (such as <see cref="AdviserExtensions.Override(IAdviser{Code.IFieldOrProperty}, string, object)">Override</see>
/// or <see cref="AdviserExtensions.IntroduceMethod">IntroduceMethod</see>)
/// with parameters that are not valid for the target declaration.
/// </para>
/// <para>
/// Common causes include:
/// </para>
/// <list type="bullet">
/// <item><description>Specifying a template name that does not exist in the aspect class.</description></item>
/// <item><description>Passing incompatible tag values or compile-time arguments.</description></item>
/// <item><description>Using an <see cref="OverrideStrategy"/> that is not applicable to the target.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IAdviceFactory"/>
/// <seealso cref="InvalidTemplateSignatureException"/>
/// <seealso href="@advising-code"/>
[CompileTime]
public sealed class InvalidAdviceParametersException : Exception
{
    internal InvalidAdviceParametersException( string message ) : base( message ) { }
}