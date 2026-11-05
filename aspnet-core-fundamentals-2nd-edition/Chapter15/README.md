# Chapter 15

The complete code for Chapter 15: Domain/, Api/ and Tests/, grouped by AdventureWorks.slnx. Like every chapter from Chapter 7, this is its own small project: start from this folder rather than adding it to an earlier chapter's project.

## Run it

The chapter exercises the API through its tests. From this folder:

```shell
dotnet test Tests
```

Expect 12 passed tests. The tests host Api in memory through AdventureWorksFactory (Tests/Factory.cs), which gives it an in-memory SQLite database and a test sign-in. Started on its own, Api uses orders-local.db in its working folder, which nothing creates tables in, so requests that reach the database return 500.
