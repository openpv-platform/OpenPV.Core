# Download BSP Info

If the build of this project if failing due to it not being able to find the `secrets.json` file indicated in the error.
You will need to follow the instructions laid out in the
[microsoft documentation](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows#enable-secret-storage).

The resulting JSON should look at follows:

```json
{
    "SparkLogin": "<USER_EMAIL>",
    "SparkPassword": "<USER_PASSWORD>"
}
```
