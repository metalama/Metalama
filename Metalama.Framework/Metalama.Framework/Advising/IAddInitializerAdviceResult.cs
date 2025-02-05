// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Advising;

/// <summary>
/// Represents the result of the <see cref="IAdviceFactory.AddInitializer(Metalama.Framework.Code.INamedType,string,InitializerKind,object?,object?)"/>
/// method.
/// </summary>
public interface IAddInitializerAdviceResult : IAdviceResult;