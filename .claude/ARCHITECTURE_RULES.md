# IPMan Architecture Rules

Claude Code must follow these rules in addition to `CLAUDE.md`.

1. Do not merge Domain/Application/Infrastructure/App into one project.
2. No `System.Management` reference outside `IPMan.Infrastructure`.
3. No WPF reference outside `IPMan.App`.
4. Adapter identity must not be display-name based.
5. Every network mutation must be followed by a fresh read/verification.
6. Do not implement aggressive polling.
7. Do not use `netsh`, PowerShell or CMD for normal network configuration.
8. Do not delete all routes/all IPv4 addresses as a shortcut.
9. Do not treat failed ping as proof an IPv4 address is unused.
10. Do not hard-code Turkish user messages in infrastructure/application code.
11. Do not add a new NuGet package without documenting why it is necessary.
12. Preserve rollback/recovery information before network mutation.
