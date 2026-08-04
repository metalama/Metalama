# Fabric retention diagnostic, disabled

The companion of the `FabricRetention` scenario, containing the same fabric with the same retention, but without the
`MetalamaDiagnoseMemoryLeaks` property.

The analysis walks the whole object graph reachable from what the fabrics registered, which is expensive. That cost
must never be paid by a user who did not ask for it, and a diagnostic that appeared by default would report the
retentions of Metalama itself on every project that has a fabric. Its absence here is therefore as much a requirement
as its presence in the other scenario, and the two together are what makes the property meaningful.

## Expected outcome

The build succeeds and emits neither `LAMA0085` nor `LAMA0086`.
