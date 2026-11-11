# Domain Docs

How agents should consume this repo's domain documentation.

## Before exploring, read these

- **`GLOSSARY.md`** at the repo root.
- **`docs/adr/`**: ADRs that touch the area you are about to work in.
- **`specs/product/`**: the requirement ids your change touches.

## Layout

Single-context repo:

```
/
├── GLOSSARY.md
├── docs/adr/
├── specs/product/
└── backend/src/
```

## Use the glossary's vocabulary

When your output names a domain concept (ticket title, test name, class name, doc), use the term as defined in `GLOSSARY.md`. Don't drift to synonyms the glossary lists under _Avoid_.

If the concept you need isn't in the glossary, either you are inventing language the project doesn't use (reconsider) or there's a real gap (add the term, without implementation detail).

## Keep decisions aligned

Check the ADR index for decisions that still apply to the active four-context quote, bind, and booking flow. Do not reference removed decisions or scope that is no longer present in the glossary.
