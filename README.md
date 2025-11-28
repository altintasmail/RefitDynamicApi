# RefitDynamicApi

[![NuGet](https://img.shields.io/nuget/v/RefitDynamicApi.svg)](https://www.nuget.org/packages/RefitDynamicApi)  
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

Dynamic Minimal API mapping for **Refit client interfaces**.  
Automatically exposes your Refit interfaces as HTTP endpoints in a **.NET 8/9 Minimal API** project.

---

## Features

- Automatically maps **Refit client interfaces** (`IRefitClient`) to Minimal API endpoints.
- Supports **GET** and **POST** methods via `[Get]` and `[Post]` attributes.
- Optional **[DisableMethod]** attribute to exclude methods from dynamic mapping.
- Single body parameter support via `[Body]` attribute.
- Works with **dependency injection** to resolve Refit clients.

---

## Installation

Install via NuGet: 

```bash
dotnet add package RefitDynamicApi
```

Download

[![GitHub Repo](https://img.shields.io/badge/GitHub-Source-blue?logo=github)](https://github.com/altintasmail/RefitDynamicApi)

- **Define your Refit clients:**

```csharp
using Refit;
public interface IUserClient : IRefitClient
{
    [Get("/youApi/User/List")]
    Task<List<UserDto>> ListUser();

    [Post("/youApi/User/Save")]
    Task<bool> SaveUser([Body] UserDto input);

    [DisableMethod] //This method is disable in dynamic api endpoints
    [Get("/youApi/User/Remove")]
    Task<bool> RemoveUser(int id);
}
```

- **List of example endpoints:**
  
Interface names starting with "I" and ending with "Client" are trimmed to form a short controller name.
```
/api/{ControllerName}/{MethodName}

GET /api/User/ListUser

POST /api/User/SaveUser
```
*Methods marked with [DisableMethod] are skipped.*

---

- **Register Refit clients in Program.cs:** (Make sure Refit clients is registered)

```csharp
builder.Services.AddRefitClient<IUserClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://your-api.example.com"));
    //.AddHttpMessageHandler<ExampleHandler>(); //Make safe your refit client with DelegatingHandler
```

- **Map dynamic api endpoints:**

```csharp
var assembly = typeof(IUserClient).Assembly;
app.MapAllRefitClientToDynamicApi("/api", assembly);//Example is starts with /api prefix
```

- **Optional: Map a single interface:** (not required)

```csharp
app.MapDynamicApi<IUserClient>("/api");//Example is starts with /api prefix
```

- **Example Parameter Binding:**

Query Parameters:
GET /api/User/GetUser?id=5

Body parameters (only one per method):
POST /api/User/SaveUser
```json
Content-Type: application/json
{
  "Username": "value1",
  "age": 123
}
```

- **Security:**
Supports standard Refit authentication via HttpClient message handlers.

```csharp
builder.Services.AddRefitClient<IUserClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://your-api.example.com"));
    .AddHttpMessageHandler(() => new AuthTokenHandler());
```

**Security Note: Dynamic API endpoints inherit the DI-resolved Refit clients, so make sure token/session handling works automatically.**
