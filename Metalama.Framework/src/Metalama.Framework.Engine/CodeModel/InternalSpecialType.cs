// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.CodeModel;

internal enum InternalSpecialType
{
    // ReSharper disable once InconsistentNaming
    ITemplateAttribute,

    /// <summary>
    /// <see cref="System.ValueType"/>, which is the base that the language gives to a struct and to an enum.
    /// </summary>
    ValueType,

    /// <summary>
    /// <see cref="System.Enum"/>, which is the base that the language gives to an enum.
    /// </summary>
    Enum,

    /// <summary>
    /// <see cref="System.MulticastDelegate"/>, which is the base that the language gives to a delegate.
    /// </summary>
    MulticastDelegate,
    Count
}