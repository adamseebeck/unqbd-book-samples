# Chapter 18

The complete code for Chapter 18: the deployment-ready API in this folder (AdventureWorks.Deployment.csproj), its Dockerfile and Dockerfile.aot, compose.yaml, kubernetes.yaml and ci.yml. Like every chapter from Chapter 7, this is its own small project: start from this folder rather than adding it to an earlier chapter's project.

## Run it

From this folder:

```shell
dotnet run
```

There is no launch profile, so the app runs in Production on http://localhost:5000 and writes JSON logs. It serves:

- http://localhost:5000/healthz/live and http://localhost:5000/healthz/ready (both return Healthy)
- http://localhost:5000/api/products

The container image needs a Linux container engine. From this folder, build it with `docker build -t adventureworks:local .` (the final dot is the build context). `docker compose up --build` runs it on http://127.0.0.1:42151.

ci.yml is a GitHub Actions workflow written for a repository whose root is this book folder, with the file in .github/workflows; adjust its paths if you copy it into another layout.
