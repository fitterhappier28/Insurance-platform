# Commercial Lines Platform

The language of Commercial Property quoting, binding and premium booking, shared by every service in this repository.

## Parties and tenancy

**Tenant**:
A program organisation (an MGA) that distributes policies on the platform under its own treaties and sees only its own data.
_Avoid_: customer, client, organisation

**Broker**:
A person at a producing agency who requests quotes and binds policies on behalf of an applicant.
_Avoid_: agent, producer, user

**Account**:
The commercial business being insured, as known to the tenant; the root of the policy hierarchy.
_Avoid_: customer, insured, client

**Applicant**:
The legal entity named on the policy as the named insured.
_Avoid_: insured, policyholder, customer

**Carrier**:
A risk-bearing reinsurer that takes a share of premium under a treaty, for example Renaissance Re or Arch Re.
_Avoid_: insurer, reinsurer, market

## Policy hierarchy

**Policy**:
The contract for an Account, identified by a policy number once bound, made of a sequence of Policy Revisions.
_Avoid_: contract, submission

**Policy Revision**:
One effective-dated version of a Policy's full schedule and terms; a bound revision never changes.
_Avoid_: version, endorsement, policy version

**Quote**:
A Policy Revision that has a Rules decision and a price but is not yet bound.
_Avoid_: proposal, quotation, offer

**Coverage**:
A type of protection on a Policy Revision, such as Building, with its deductible.
_Avoid_: cover, line, peril

**Location**:
A physical address on the schedule, carrying the fire protection class.
_Avoid_: site, premises, address

**Building**:
A structure at a Location, described by construction class and year built.
_Avoid_: structure, property, risk

**Unit**:
A separately occupied part of a Building, such as a leased suite.
_Avoid_: suite, space, tenant space

**Exposure**:
The rated unit: one Coverage applied to one Building or Unit, with a limit, a NAICS code and its NAICS extension.
_Avoid_: risk, item, rated item

**NAICS Extension**:
The industry-specific attributes an Exposure must carry for its NAICS code, defined by a versioned schema.
_Avoid_: custom fields, occupancy details

## Underwriting and pricing

**Ruleset**:
A published, immutable, versioned set of eligibility rules.
_Avoid_: rules engine, guidelines

**Decision**:
The Rules outcome for a Quote: Straight-Through, Refer or Decline, with reason codes.
_Avoid_: result, verdict, disposition

**Straight-Through**:
A Decision that allows bind without human underwriting review.
_Avoid_: auto-approve, STP pass

**Refer**:
A Decision that allows a price but requires underwriter review before bind.
_Avoid_: pending, manual review

**Decline**:
A Decision that the risk is ineligible; no price is produced.
_Avoid_: reject, deny

**Rate Manual**:
A published, immutable, versioned set of base rates and rating factors.
_Avoid_: rate table, pricing config

**Rating Factor**:
One multiplier from the Rate Manual applied to an Exposure's premium, such as protection class or building age.
_Avoid_: modifier, loading

**Rate Snapshot**:
The premium and every Rating Factor recorded on a Policy Revision at quote or bind time.
_Avoid_: rating result, cached rate

**Written Premium**:
The premium charged for a bound Policy Revision, in USD to the cent.
_Avoid_: price, cost, amount

## Bind and booking

**Bind**:
The act of making a Quote a binding contract, producing a bound Policy Revision.
_Avoid_: issue, accept, purchase

**Booking**:
The Ledger's recording of a bound Policy Revision's premium as an immutable Premium Transaction.
_Avoid_: posting, accounting, settlement

**Booking Status**:
Whether a bound Policy Revision's premium is Pending or Booked in the Ledger, as seen by Policy.
_Avoid_: sync status, ledger status

**Premium Transaction**:
An immutable Ledger record of written premium for one Policy Revision, with its premium, factor and allocation lines.
_Avoid_: ledger entry, posting, journal

**Factor Line**:
A Ledger line recording one Rating Factor's effect on one Exposure's premium.
_Avoid_: rating line, breakdown row

**Allocation Line**:
A Ledger line recording one Carrier's share of one Exposure's premium.
_Avoid_: split, cession line

## Reinsurance

**Treaty**:
A tenant's quota-share arrangement for a line of business, made of Treaty Versions.
_Avoid_: contract, program, deal

**Treaty Version**:
An immutable, effective-dated set of Carrier shares under a Treaty that sum to 100%.
_Avoid_: treaty amendment, schedule

**Quota Share**:
A treaty form in which each Carrier takes a fixed percentage of every Exposure's premium.
_Avoid_: proportional split, pro rata

**Lead Carrier**:
The Carrier with the largest share in a Treaty Version, which absorbs rounding remainders.
_Avoid_: primary, main carrier
