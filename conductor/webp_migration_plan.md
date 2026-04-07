# Plan: Full WebP Migration and Optimization

## Objective
Convert all image assets in the project (marketing, user guides, and website) to WebP format and update all references in the codebase to improve performance and load times.

## Background
We have already optimized large marketing and guide images, reducing their size significantly (e.g., 9MB to <600KB). The user wants to go further and replace *all* `.png` and `.jpg` references with the `.webp` version across all file types (`.razor`, `.html`, `.md`, `.css`, etc.).

## Key Files & Context
- **Image Directories:**
  - `src/MotoRent.Server/wwwroot/images/marketing/`
  - `user.guides/images/`
  - `website/images/features/`
- **Reference Files:**
  - `.razor` files in `src/`
  - `.md` files in `user.guides/`
  - `.html` files in `website/`
  - `.css` files throughout the project

## Implementation Steps

### 1. Identify and Convert All Images
- List all PNG, JPG, and JPEG files that do not have a corresponding WebP version.
- Use a Node.js script with `sharp` to convert *all* remaining images to WebP (not just the large ones).
- Verify every image now has a WebP counterpart.

### 2. Global Reference Update
- Search for all occurrences of `.png`, `.jpg`, and `.jpeg` in the codebase.
- Replace them with `.webp`.
- Ensure the replacement is case-insensitive (e.g., `.PNG` -> `.webp`).
- Handle various reference types:
  - `src="image.webp"`
  - `![alt](image.webp)`
  - `url('image.webp')`

### 3. Cleanup and Verification
- Verify that all WebP files exist and are referenced correctly.
- Check for any broken links using a simple grep or script.
- Optionally remove the original PNG/JPG files to reduce repository size (or keep them as backup if requested, but for performance, WebP will be the default).

## Verification & Testing
- Run a grep search to ensure no `.png` or `.jpg` references remain in the target file types.
- Manually check the landing page (`PublicLanding.razor`) and a few user guides.
- Verify image loading in the browser (if possible) or via file existence checks.
