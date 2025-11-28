# Refit.DynamicApi

[![NuGet](https://img.shields.io/nuget/v/Refit.DynamicApi.svg)](https://www.nuget.org/packages/Refit.DynamicApi)  
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

Dynamic Minimal API mapping for **Refit client interfaces**.  
Automatically exposes your Refit interfaces as HTTP endpoints in a **.NET 7/8/9 Minimal API** project.

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
dotnet add package Refit.DynamicApi
