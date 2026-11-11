# Commercial Lines Platform

Business language for the Commercial Property quote, bind, and premium-booking learning flow.

## Parties

**Tenant**: A program organisation (MGA) whose accounts and policies are isolated from other tenants. _Avoid_: customer, client, organisation.

**Broker**: A person at a producing agency who requests quotes and binds policies on behalf of an applicant. _Avoid_: agent, producer, user.

**Account**: The commercial business being insured, as known to the tenant; root of the policy hierarchy. _Avoid_: customer, insured, client.

**Applicant**: The legal entity named on a Policy as the named insured. _Avoid_: insured, policyholder, customer.

## Policy hierarchy

**Policy**: The contract for an Account, identified by a policy number once bound, with a sequence of Policy Revisions. _Avoid_: submission.

**Policy Revision**: One effective-dated version of a Policy's schedule and terms. A bound revision never changes. _Avoid_: version, policy version.

**Quote**: A Policy Revision with a Decision and, unless Declined, a Rate Snapshot that is not yet bound. _Avoid_: proposal, quotation, offer.

**Coverage**: A type of protection on a Policy Revision, such as Building coverage, with its deductible. _Avoid_: cover, peril.

**Location**: A physical address on the schedule, including its protection class. _Avoid_: site, premises.

**Building**: A structure at a Location, described by construction class and year built. _Avoid_: property, risk.

**Unit**: A separately occupied part of a Building, such as a leased suite. _Avoid_: suite, tenant space.

**Exposure**: The rated unit: one Coverage applied to one Building or Unit, with a limit, NAICS code, and NAICS Extension. _Avoid_: risk, item, rated item.

**NAICS Extension**: Industry-specific attributes an Exposure carries for its NAICS code, defined by a versioned schema. _Avoid_: custom fields, occupancy details.

## Eligibility and pricing

**Ruleset**: A published, immutable, versioned set of eligibility rules. _Avoid_: guidelines.

**Decision**: The Rules outcome for a Quote: Straight-Through, Refer, or Decline, with reason codes. _Avoid_: result, verdict, disposition.

**Straight-Through**: A Decision that allows bind without human underwriting review. _Avoid_: auto-approve.

**Refer**: A Decision that requires underwriter review before bind. _Avoid_: pending.

**Decline**: A Decision that the risk is ineligible; no price is produced. _Avoid_: reject, deny.

**Rate Manual**: A published, immutable, versioned set of base rates and rating factors. _Avoid_: rate table, pricing config.

**Rating Factor**: A multiplier applied to an Exposure's premium, such as protection class or building age. _Avoid_: modifier, loading.

**Rate Snapshot**: The premium and factor steps recorded on a Policy Revision when it is quoted and bound. _Avoid_: cached rate.

**Written Premium**: The premium charged for a bound Policy Revision, in USD to the cent. _Avoid_: price, cost, amount.

## Bind and booking

**Bind**: The act of making an eligible Quote a binding contract, producing a bound Policy Revision. _Avoid_: issue, accept, purchase.

**Booking**: The Ledger's recording of a bound Policy Revision's premium as an immutable Premium Transaction. _Avoid_: posting, settlement.

**Booking Status**: Whether the bound revision's premium is Pending or Booked in the Ledger, as shown by Policy. _Avoid_: sync status.

**Premium Transaction**: An immutable Ledger record of written premium for one Policy Revision, with premium and factor lines. _Avoid_: ledger entry, journal.

**Factor Line**: A Ledger line recording one Rating Factor's effect on one Exposure's premium. _Avoid_: rating line, breakdown row.
