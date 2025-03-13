Metalama.Framework.Sdk offers direct access to Metalama's underlying code-modifying capabilities through Roslyn-based APIs.

Unlike Metalama.Framework, our high-level API, aspects built with Metalama.Framework.Sdk must be in their own project, separate from the code they transform. Metalama.Framework.Sdk is much more complex and less safe than Metalama.Framework, and does not allow for a good design-time experience. You should use Metalama.Framework.Sdk only when necessary.

To learn more visit Metalama documentation page:
https://doc.metalama.net/sdk/sdk
