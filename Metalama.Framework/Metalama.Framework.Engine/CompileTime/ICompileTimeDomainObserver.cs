// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.CompileTime;

[UsedImplicitly( ImplicitUseTargetFlags.Members )]
public interface ICompileTimeDomainObserver : IGlobalService
{
    void OnDomainCreated( CompileTimeDomain domain );

    void OnDomainUnloaded( CompileTimeDomain domain );
}