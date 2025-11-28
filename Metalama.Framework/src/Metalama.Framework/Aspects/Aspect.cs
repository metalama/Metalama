// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base class for aspects that can be applied as custom attributes. Derive from this class or a specialized
    /// aspect base class to create aspects that transform or validate code at compile time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class derives from <see cref="Attribute"/>, allowing aspects to be applied to code elements using C# attribute syntax
    /// such as <c>[MyAspect]</c>. Aspect classes must also implement a specific generic instance of the <see cref="IAspect{T}"/>
    /// interface, or derive from a specialized base class like <see cref="TypeAspect"/>, <see cref="MethodAspect"/>,
    /// <see cref="FieldOrPropertyAspect"/>, <see cref="EventAspect"/>, <see cref="ConstructorAspect"/>, or <see cref="CompilationAspect"/>.
    /// </para>
    /// <para>
    /// <b>Specialized base classes for common scenarios:</b> Instead of implementing <see cref="IAspect{T}.BuildAspect"/> yourself,
    /// consider using specialized base classes that already implement <see cref="IAspect{T}.BuildAspect"/> and provide simpler approaches based on virtual methods.
    /// For overriding members, use <see cref="OverrideMethodAspect"/>, <see cref="OverrideFieldOrPropertyAspect"/>, <see cref="OverrideEventAspect"/>,
    /// or <see cref="ContractAspect"/>. For aspects that target specific declaration kinds, use <see cref="TypeAspect"/>, <see cref="MethodAspect"/>, or similar.
    /// Derive directly from <see cref="Aspect"/> only when you need a custom attribute applicable to multiple declaration kinds (e.g., both methods and fields).
    /// </para>
    /// <para>
    /// This class is a convenience base class. The aspect framework primarily requires implementation of the <see cref="IAspect{T}"/> interface.
    /// You can create aspects that don't derive from <see cref="Attribute"/> if you only apply them programmatically via fabrics.
    /// </para>
    /// <para>
    /// <b>How to create an aspect:</b>
    /// </para>
    /// <list type="number">
    /// <item><description>Derive from a specialized aspect class (e.g., <see cref="OverrideMethodAspect"/>, <see cref="TypeAspect"/>) or this base class</description></item>
    /// <item><description>Override the <see cref="IAspect{T}.BuildAspect"/> method to add advice, child aspects, or validations (not needed for override aspects)</description></item>
    /// <item><description>Optionally override <see cref="IEligible{T}.BuildEligibility"/> to define where the aspect can be applied</description></item>
    /// <item><description>Define template methods and annotate them with <see cref="TemplateAttribute"/> for code generation</description></item>
    /// <item><description>Optionally annotate the aspect class with <see cref="LayersAttribute"/> to split <see cref="IAspect{T}.BuildAspect"/> into multiple execution layers</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IAspect{T}"/>
    /// <seealso cref="IAspectBuilder"/>
    /// <seealso cref="TypeAspect"/>
    /// <seealso cref="MethodAspect"/>
    /// <seealso cref="PropertyAspect"/>
    /// <seealso cref="FieldAspect"/>
    /// <seealso cref="EventAspect"/>
    /// <seealso cref="ConstructorAspect"/>
    /// <seealso cref="CompilationAspect"/>
    /// <seealso href="@aspects"/>
    /// <seealso href="@aspect-design"/>
    public abstract class Aspect : Attribute, IAspect
    {
        public override string ToString() => this.GetType().Name;
    }
}