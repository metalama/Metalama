// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// https://github.com/metalama/Metalama/issues/426

namespace Metalama.Extensions.DependencyInjection.AspectTests.Bugs.Issue426;

// <target>
public class ClientWeb 
{
    public ClientWeb( object appSettings, bool throwOnError = true )
    {
    }

    [ExtractedResultLoggingAspect]
    public void Foo() { }
}

// <target>
public class ScriptedClient : ClientWeb
{
    protected ILogger<ScriptedClient> _logger;

    public ScriptedClient( object appSettings, ILogger<ScriptedClient> logger ) : base( appSettings )
    {
        _logger = logger;
    }

    [ExtractedResultLoggingAspect]
    public void Bar() { }
}


public class ExtractedResultLoggingAspect : OverrideMethodAspect
{
    [IntroduceDependency] private readonly ILogger _logger;

    public override dynamic? OverrideMethod()
    {
        throw new NotImplementedException();
    }
}