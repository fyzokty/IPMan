---
title: Solution and Dependency Rules
version: 1.0.0
status: Approved
---

# Project dependency matrix

| Project | Domain | Application | Infrastructure | WPF |
|---|---:|---:|---:|---:|
| Domain | - | No | No | No |
| Application | Yes | - | No | No |
| Infrastructure | Yes | Yes | - | No |
| App | Yes | Yes | Yes | Yes |

# Namespace policy

- `IPMan.Domain.*`
- `IPMan.Application.*`
- `IPMan.Infrastructure.*`
- `IPMan.App.*`

# Rules

1. No business/network mutation logic in XAML code-behind.
2. No `System.Management` outside Infrastructure.
3. No profile filesystem access outside Infrastructure.
4. No direct WPF `Dispatcher` use in Domain/Application.
5. Infrastructure events must be marshalled to UI thread by App/ViewModel
   boundary when required.
6. Concrete infrastructure services are registered in App composition root or
   infrastructure DI extension methods.
7. One interface should represent one coherent capability; avoid "god services".
