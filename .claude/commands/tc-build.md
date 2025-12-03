# Trigger TeamCity Build

Trigger a build on TeamCity for the current branch.

## Instructions

1. Read `.teamcity/settings.kts` to find:
   - Available build types: `object <Name> : BuildType`
   - VCS root ID: `AbsoluteId("...")` in vcs block - extract project prefix
2. Get current branch: `git rev-parse --abbrev-ref HEAD`
3. Ask user which build configuration to trigger
4. Build type ID format: `<ProjectPrefix>_<BuildTypeName>` (e.g., if VCS root is `Metalama_Metalama20260_Metalama`, build type ID is `Metalama_Metalama20260_Metalama_DebugBuild`)

## TeamCity API

**Server:** https://postsharp.teamcity.com
**Endpoint:** POST `/app/rest/buildQueue`
**Auth:** Bearer token from `$TEAMCITY_CLOUD_TOKEN`

```bash
curl -X POST \
  -H "Authorization: Bearer $TEAMCITY_CLOUD_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"buildType":{"id":"<BuildTypeId>"},"branchName":"<branch>"}' \
  https://postsharp.teamcity.com/app/rest/buildQueue
```

## Arguments

$ARGUMENTS

If no argument, use DebugBuild.

After triggering, provide the build URL.
