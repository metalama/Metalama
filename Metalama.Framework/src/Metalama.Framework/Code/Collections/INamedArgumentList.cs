// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Collections;

/// <summary>
/// Represents a list of names arguments (i.e. setting of field or property values) in an <see cref="IAttributeData"/>. The primary interface
/// is an <see cref="IReadOnlyList{T}"/> because the order of arguments may be important if property setters have a side effect.
/// </summary>
[CompileTime]
[InternalImplement]
public interface INamedArgumentList : IReadOnlyList<KeyValuePair<string, TypedConstant>>, IReadOnlyDictionary<string, TypedConstant>;