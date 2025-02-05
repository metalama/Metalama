// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base class from aspects that can be applied as custom attributes. Aspects must implement a specific generic instance
    /// of the <see cref="IAspect{T}"/> interface, or derive from <see cref="TypeAspect"/>, <see cref="MethodAspect"/>,
    /// <see cref="ConstructorAspect"/>, <see cref="FieldOrPropertyAspect"/>, <see cref="EventAspect"/> or <see cref="CompilationAspect"/>.
    /// </summary>
    /// <remarks>
    /// <para>This class is a redundant helper class. The aspect framework only respects the <see cref="IAspect{T}"/> interface.</para>
    /// </remarks>
    public abstract class Aspect : Attribute, IAspect
    {
        public override string ToString() => this.GetType().Name;
    }
}