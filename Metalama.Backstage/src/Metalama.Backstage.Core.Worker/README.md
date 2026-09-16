![Metalama by PostSharp](https://raw.githubusercontent.com/metalama/.github/HEAD/images/metalama.svg)

The `Metalama.Backstage.Core.Worker` package is not meant to be directly referenced in user projects.

It contains the worker application of Metalama.Backstage as a library: the local setup web server with its pages (license key, privacy consents, news, exception review) and the telemetry upload command. Each product hosts it in a small executable that registers its own Backstage services; the executable of Metalama is `Metalama.Backstage.Worker`.
