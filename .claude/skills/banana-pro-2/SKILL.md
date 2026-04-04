---
name: banana-pro-2
description: Generate images using Google Gemini's image generation capabilities via browser automation. Use this skill when the user asks to generate, create, or make an image, illustration, icon, banner, hero image, or any visual asset -- especially when they want a specific style like flat design, teal palette, or marketing-quality visuals. Also use when the user mentions "Gemini image", "generate with Gemini", "banana pro", or needs an image for a website, app, documentation, or marketing material.
---

# Banana Pro 2 -- Gemini Image Generator

Generate images by automating Google Gemini (gemini.google.com) in a browser with saved authentication state.

## Authentication

The auth state is saved in `.claude/skills/banana-pro-2/gemini-auth.json`. This file contains cookies and localStorage that keep you signed into Google. It is gitignored since it contains session secrets.

### First-time setup (if no auth file exists)

```bash
playwright-cli -s=gemini open --headed "https://gemini.google.com"
# User signs in manually in the headed browser
playwright-cli -s=gemini state-save .claude/skills/banana-pro-2/gemini-auth.json
playwright-cli -s=gemini close
```

### Normal usage (auth file exists)

```bash
playwright-cli -s=gemini open "https://gemini.google.com"
playwright-cli -s=gemini state-load .claude/skills/banana-pro-2/gemini-auth.json
playwright-cli -s=gemini goto "https://gemini.google.com"
```

### Auth recovery (expired session)

If after loading saved auth the snapshot shows a "Sign in" button instead of the chat input, the session has expired. Recover automatically:

1. Close the headless session: `playwright-cli -s=gemini close`
2. Open a **headed** browser so the user can sign in visually:
   ```bash
   playwright-cli -s=gemini open --headed "https://gemini.google.com"
   ```
3. Tell the user: *"Your Gemini session expired. Please sign in to Google in the browser window that just opened. Let me know when you're done."*
4. Once the user confirms sign-in, save the refreshed auth state:
   ```bash
   playwright-cli -s=gemini state-save .claude/skills/banana-pro-2/gemini-auth.json
   ```
5. Continue with Step 3 (compose prompt) — the headed session stays open and is now authenticated.

## Default Style

Unless the user specifies a different style, use this default:

> **Style: minimal flat design with Tropical Teal (#00897B) and white palette, subtle shadows, no text, 16:9 aspect ratio, light gradient background.**

## Workflow

### Step 1: Open browser and load auth

```bash
playwright-cli -s=gemini open "https://gemini.google.com"
playwright-cli -s=gemini state-load .claude/skills/banana-pro-2/gemini-auth.json
playwright-cli -s=gemini goto "https://gemini.google.com"
```

### Step 2: Verify sign-in

Take a snapshot. If there's a chat input ("Enter a prompt for Gemini"), you're signed in — proceed to Step 3.

If there's a "Sign in" button, auth has expired. Follow the **Auth recovery** flow above:
1. Close headless session
2. Reopen with `--headed` so the user can sign in
3. Wait for user confirmation
4. Save refreshed auth state
5. Continue to Step 3 in the same headed session

### Step 3: Compose and submit the prompt

Build the prompt:
```
Generate an image: [user's description]. Style: [style directive].
```

Find the textbox (`Enter a prompt for Gemini`), click it, type the prompt, press Enter.

### Step 4: Wait for image generation

Wait 30 seconds, then screenshot. If "Show thinking" is still visible with no image, wait another 15 seconds. Images typically appear within 10-30 seconds.

### Step 5: Download the image

Look for the button `"Download full-sized image"` in the snapshot and click it. The image downloads to `.playwright-cli/` as `Gemini-Generated-Image-*.png`.

Copy the **most recent** downloaded file to the `download/` folder with a descriptive name:
```bash
# Find the newest Gemini image and copy to download/
cp "$(ls -t .playwright-cli/Gemini-Generated-Image-*.png | head -1)" download/<descriptive-name>.png
```

### Step 6: Show the result

Read the downloaded image file from `download/` so the user can see it inline.

### Step 7: Clean up

```bash
playwright-cli -s=gemini close
```

## Style Presets

| Preset | Style |
|--------|-------|
| **default** / **brand** | minimal flat design with Tropical Teal (#00897B) and white palette, subtle shadows, no text, 16:9 aspect ratio, light gradient background |
| **dark** | minimal flat design with dark teal (#004D40) and charcoal (#1e293b) palette, subtle glows, no text, 16:9 aspect ratio, dark gradient background |
| **playful** | colorful flat illustration with teal (#00897B) accents, rounded shapes, friendly characters, no text, 16:9 aspect ratio, white background |
| **photo** | photorealistic, professional photography style, shallow depth of field, natural lighting, 16:9 aspect ratio |

## Example

User: "Generate a feature image for the fleet management page"

Prompt sent to Gemini:
```
Generate an image: A clean, modern flat-design illustration of a motorbike fleet management dashboard. Show a grid of motorbike cards with status indicators (available, rented, maintenance), a map pin showing shop locations, and a sidebar with fleet statistics. Style: minimal flat design with Tropical Teal (#00897B) and white palette, subtle shadows, no text, 16:9 aspect ratio, light gradient background.
```

Output saved to: `download/fleet-management-feature.png`

## Troubleshooting

- **Auth expired**: The workflow handles this automatically — closes headless, reopens `--headed`, user signs in, auth is re-saved
- **Image not appearing**: Wait longer (up to 60s), or try rephrasing the prompt
- **Download fails**: Use `playwright-cli -s=gemini screenshot <element-ref>` as fallback
