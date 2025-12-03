# Check TeamCity Build Status

Check the status of a TeamCity build.

## Instructions

1. Get the build ID from $ARGUMENTS (required)
2. Query TeamCity API for build status
3. Based on mode:
   - **once**: Report status once and exit
   - **continuous**: Check every 30 seconds until build completes, report errors immediately

## TeamCity API

**Server:** https://postsharp.teamcity.com
**Endpoint:** GET `/app/rest/builds/id:<buildId>`
**Auth:** Bearer token from `$TEAMCITY_CLOUD_TOKEN`

```bash
curl -s \
  -H "Authorization: Bearer $TEAMCITY_CLOUD_TOKEN" \
  -H "Accept: application/json" \
  https://postsharp.teamcity.com/app/rest/builds/id:<buildId>
```

## Response Fields

- `state`: queued, running, finished
- `status`: SUCCESS, FAILURE, UNKNOWN
- `statusText`: Human-readable status
- `percentageComplete`: Progress percentage (when running)
- `webUrl`: Link to build in TeamCity

## Arguments

$ARGUMENTS

Format: `<buildId> [mode]`
- `buildId`: Required. The TeamCity build ID (e.g., 310921)
- `mode`: Optional. `once` (default) or `continuous`

## Examples

- `/tc-check-build 310921` - Check once
- `/tc-check-build 310921 continuous` - Monitor until completion

## Continuous Mode

When in continuous mode:
1. Check status every 30 seconds using Bash with `sleep 30`
2. Report progress updates (percentage, status changes)
3. If build fails, report the failure details and stop
4. If build succeeds, report success and stop
5. Use the BashOutput tool to monitor long-running checks
