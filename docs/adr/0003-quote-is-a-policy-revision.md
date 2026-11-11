---
status: accepted
---

# A Quote is a Policy Revision, not a separate aggregate

The schedule that is quoted is the schedule that is bound, so a Quote is Policy Revision 1 with status `Quoted`, `Referred` or `Declined`, and Bind changes that same revision to `Bound`. A separate quote model would need a translation into policy objects at bind time, which is where limits and exposures get lost. Bound revisions are immutable; later changes will be new revisions.

## Consequences

- The Policy service is larger: it owns the whole Account to Exposure hierarchy.
- Declined and referred quotes create Policy rows without a policy number; the number is assigned at bind.
