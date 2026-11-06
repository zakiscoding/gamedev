# GitHub auth blocker (cloud agent)

## Exact error

```
$ gh auth status
You are not logged into any GitHub hosts. To log in, run: gh auth login

$ gh api user
To get started with GitHub CLI, please run:  gh auth login
Alternatively, populate the GH_TOKEN environment variable with a GitHub API authentication token.
```

No `GH_TOKEN` / `GITHUB_TOKEN` was present in the environment. ConnectScm / MCP GitHub auth tools were not available in this worker’s tool catalog.

## Intended remote

Prefer creating (once authenticated):

```bash
gh repo create zakiscoding/cmis3150-project2-fps --public --source=. --remote=origin --push
```

If the `zakiscoding` user/org is wrong, use the authenticated user’s login:

```bash
gh repo create cmis3150-project2-fps --public --source=. --remote=origin --push
```

## Local artifacts while blocked

- Project path: `/agent/cmis3150-project2-fps`
- Zip: see `docs/unity-project2/` mirror / artifacts zip
