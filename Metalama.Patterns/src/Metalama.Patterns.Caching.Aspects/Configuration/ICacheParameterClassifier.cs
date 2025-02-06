// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Patterns.Caching.Aspects.Configuration;

public interface ICacheParameterClassifier : ICompileTimeSerializable
{
    CacheParameterClassification GetClassification( IParameter parameter );
}