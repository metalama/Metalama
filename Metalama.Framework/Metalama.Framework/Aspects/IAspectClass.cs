// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Utilities;
using System;
using System.ComponentModel;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Represents the metadata of an aspect class.
    /// </summary>
    [InternalImplement]
    [CompileTime]
    public interface IAspectClass
    {
        /// <summary>
        /// Gets the fully qualified type of the aspect.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets the name of the aspect type without the namespace and without the <c>Attribute</c> suffix.
        /// </summary>
        string ShortName { get; }

        /// <summary>
        /// Gets the name of the aspect displayed to the design-time UI. This property should not be used to report diagnostics (<see cref="ShortName"/>
        /// should be used instead). It can be set by adding the <see cref="DisplayNameAttribute"/> custom attribute to the aspect class. By default, it is equal to <see cref="ShortName"/>.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Gets the optional long description of the aspect. This property can be set by adding the <see cref="DescriptionAttribute"/> custom attribute to the aspect class. By default, it is <c>null</c>.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Gets a value indicating whether the aspect class is an abstract class.
        /// </summary>
        bool IsAbstract { get; }

        /// <summary>
        /// Gets a value indicating whether instances of this aspect class are inherited by derived declarations.
        /// This property returns <c>null</c> when the aspect class implements the <see cref="IConditionallyInheritableAspect"/>, because each aspect instance
        /// can decide whether it is inheritable or not. This property returns <c>true</c> when the aspect class is annotated with the <see cref="InheritableAttribute"/> custom attribute.
        /// </summary>
        bool? IsInheritable { get; }

        /// <summary>
        /// Gets a value indicating whether the aspect class derives from <see cref="System.Attribute" />.
        /// </summary>
        bool IsAttribute { get; }

        /// <summary>
        /// Gets the type of the aspect. 
        /// </summary>
        Type Type { get; }

        EditorExperienceOptions EditorExperienceOptions { get; }
    }
}