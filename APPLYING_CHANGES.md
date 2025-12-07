# How to pull the current branch and apply the changes

The backend and frontend updates are already committed on the `work` branch in this repository. To get them into your local copy, do the following:

1. Fetch and check out the branch
   ```bash
   git fetch origin work
   git checkout work
   ```
   (Replace `origin` with your remote name if different.)

2. Install dependencies and restore solutions
   ```bash
   dotnet restore StorhaugenWebsite.slnx
   ```

3. Apply the latest EF Core migration
   ```bash
   dotnet ef database update --project StorhaugenEats.API/StorhaugenEats.API.csproj
   ```

4. Run the API and Blazor WASM app as you normally do. The public recipes, household privacy/share IDs, and household friendship endpoints are included in this branch.

If you prefer to cherry-pick these changes onto another branch, run `git cherry-pick bf84733` from that branch.
