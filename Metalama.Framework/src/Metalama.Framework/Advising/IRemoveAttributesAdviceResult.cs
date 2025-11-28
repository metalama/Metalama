// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Advising;

/// <summary>
/// Represents the result of an advice that removes attributes, returned by <see cref="AdviserExtensions.RemoveAttributes"/>.
/// </summary>
/// <seealso cref="IAdviceResult"/>
/// <seealso cref="AdviserExtensions.RemoveAttributes"/>
/// <seealso href="@adding-attributes"/>
public interface IRemoveAttributesAdviceResult : IAdviceResult;