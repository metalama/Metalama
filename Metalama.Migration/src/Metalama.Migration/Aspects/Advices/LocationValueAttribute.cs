// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In a template applied to a field or property, the field or property value can be represented as a parameter named <c>value</c>. The type of this parameter must be <c>dynamic</c>
    /// or be compatible with the field or property type.
    /// </summary>
    public sealed class LocationValueAttribute : AdviceParameterAttribute { }
}