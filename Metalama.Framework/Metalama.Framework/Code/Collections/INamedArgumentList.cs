// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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