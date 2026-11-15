# OmmCMS GitHub baseline — direct integration

Date: 2026-11-15 (UTC)
Repository: https://github.com/OmmLabs/OmmCMS
Upstream: https://github.com/smartstore/Smartstore
License: GNU AGPL-3.0 (verified in GitHub fork)
Default branch: main (left unchanged)
Pinned initial fork commit: b57c2d2c1b01009615335a5b99bd5a0778702efa
Development branch: https://github.com/OmmLabs/OmmCMS/tree/cms-baseline
Baseline workflow: https://github.com/OmmLabs/OmmCMS/blob/cms-baseline/.github/workflows/ommcms-baseline.yml
Baseline workflow commit: 0e0e0bffd9048d4a033589a43ae96e7f18496e07
Initial CI run: https://github.com/OmmLabs/OmmCMS/actions/runs/43673929687

## Completed
- Verified real, public Smartstore fork and inherited source history.
- Verified GitHub write permission and AGPL-3.0 license.
- Created cms-baseline branch without modifying main.
- Added scoped GitHub Actions workflow to restore, build and test Smartstore.sln on .NET 10.
- Workflow has no deployment, package upload, publishing or database write actions.

## Not yet established
- Clean build and tests: the initial GitHub Actions run was still executing during verification; check its final result.
- No CMS code has been removed, no CMS-specific schema applied.
- Existing Smartstore migration and production OmmRudraksha remain untouched.

## Next
- Inspect CI success or failure and fix only baseline build issues in cms-baseline.
- Record the final passing baseline before introducing cms-only runtime mode.
- Review imported Smartstore workflows before enabling any scheduled/release workflows in this fork, because they retain upstream package publishing references.
