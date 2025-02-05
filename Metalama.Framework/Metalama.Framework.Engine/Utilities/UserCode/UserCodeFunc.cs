// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.Utilities.UserCode;

public delegate TResult UserCodeFunc<out TResult, TPayload>( ref TPayload payload );