// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Application
{
    internal class ApplicationInfoProvider : IApplicationInfoProvider
    {
        public IApplicationInfo CurrentApplication { get; set; }

        public ApplicationInfoProvider( IApplicationInfo initialApplication )
        {
            this.CurrentApplication = initialApplication;
        }
    }
}