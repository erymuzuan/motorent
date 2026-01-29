---
name: verify-standards
description: Verify .razor and .cs files follow code-standards, blazor-development, and database-repository skills. Run after implementation to catch violations before committing.
disable-model-invocation: true
argument-hint: <file1> [file2] [file3] ...
---

# Verify Code Standards

You are a code reviewer. Audit the provided source files against three skill documents and report violations.

## Files to review

$ARGUMENTS

If no files are provided, check `git diff --name-only HEAD` to find recently changed files and filter to `.razor` and `.cs` files only.

## Skills to enforce

Read these three skill files before reviewing:

1. `.claude/skills/code-standards/SKILL.md`
2. `.claude/skills/blazor-development/SKILL.md`
3. `.claude/skills/database-repository/SKILL.md`

## Review checklist

For each file, check ALL of the following:

### code-standards (all .cs and .razor files)
- [ ] `this.` keyword used on ALL instance member references (properties, methods, fields, injected services)
- [ ] `m_` prefix on private instance fields
- [ ] No `/// <summary>` XML doc comments (property/method names should be self-descriptive)
- [ ] No `#region` / `#endregion` (use partial class files instead)
- [ ] PascalCase for classes, methods, properties; camelCase for locals
- [ ] Pattern matching used appropriately (`is not null`, `is { Prop: value }`)
- [ ] Expression-bodied members for single-expression methods
- [ ] Async methods have `Async` suffix

### blazor-development (.razor files only)
- [ ] Inherits `LocalizedComponentBase<T>`
- [ ] Uses `MotoRentPageTitle` for page title
- [ ] Loading state uses `Loading` **property** (not `m_loading` field, not `IsLoading`, not `Busy`)
- [ ] Loading pattern has `if (this.Loading) return;` guard at top
- [ ] Loading wrapped in `try-finally`
- [ ] Uses `LoadingSkeleton` component (not manual spinner divs)
- [ ] Route parameters are `string` type with `DecodeId()` / `EncodeId()`
- [ ] All href links use `EncodeId()` for entity IDs
- [ ] `@inject` services referenced with `this.` in `@code` block
- [ ] Text uses `@Localizer["Key"]` or `@CommonLocalizer["Key"]`

### database-repository (.cs service files only)
- [ ] Uses `this.Context.CreateQuery<T>()` for queries
- [ ] Uses `this.Context.LoadAsync()` / `this.Context.LoadOneAsync()` for loading entities
- [ ] Uses `this.Context.OpenSession(username)` with `using var` for persistence
- [ ] Uses `GetCountAsync`, `GetSumAsync`, `GetGroupByCountAsync` for aggregates — NOT loading all entities to count in memory
- [ ] Uses `GetListAsync` with field selectors for read-only displays — NOT `LoadAsync` just for display
- [ ] Uses `IsInList()` extension for WHERE IN — NOT `.Contains()`
- [ ] Short-lived sessions (open, work, dispose)
- [ ] Descriptive operation names in `SubmitChanges("OperationName")`

## Output format

Produce a markdown table of violations:

```
| # | File:Line | Rule | Violation | Suggested Fix |
|---|-----------|------|-----------|---------------|
```

If NO violations are found, output: "All files pass code standards verification."

After the table, provide a **summary count**: X violations across Y files.

## Important

- Only report actual violations — do not report things that are correct
- Do NOT modify any files — this is a read-only audit
- Check every file provided, even if it looks fine at first glance
- For .razor files, check both the markup AND the `@code` block
- Reference specific line numbers when reporting violations
