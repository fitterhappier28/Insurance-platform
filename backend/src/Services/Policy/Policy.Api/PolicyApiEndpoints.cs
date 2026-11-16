using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Net.Http.Json;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Platform.Tenancy;
using Policy.Application;
using Policy.Domain;
using Policy.Infrastructure;

namespace Policy.Api;

public static class PolicyApiEndpoints
{
    private const string AccountOperation = "create-account";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void MapPolicyApi(this WebApplication app)
    {
        app.MapPost("/v1/accounts", CreateAccount);
        app.MapGet("/v1/accounts/{accountId:guid}", GetAccount);
        app.MapPost("/v1/quotes", CreateQuote);
        app.MapGet("/v1/quotes/{quoteId:guid}", GetQuote);
        app.MapPost("/v1/quotes/{quoteId:guid}/bind", BindQuote);
        app.MapGet("/v1/policies/{policyId:guid}", GetPolicy);
    }

    private static async Task<IResult> CreateAccount(
        CreateAccountRequest? request,
        HttpContext httpContext,
        ITenantContext tenant,
        PolicyDbContext db,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 64)
        {
            return Problem("idempotency-key-required", "A valid Idempotency-Key is required.", StatusCodes.Status400BadRequest);
        }
        if (tenant.TenantId is null || request?.Applicant?.MailingAddress is null)
        {
            return Problem("request-invalid", "Account and Applicant fields are required.", StatusCodes.Status422UnprocessableEntity);
        }

        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requestJson)));
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var existing = await db.Idempotency.AsNoTracking()
            .SingleOrDefaultAsync(row => row.TenantId == tenant.TenantId
                && row.Operation == AccountOperation && row.Key == idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                return Problem("idempotency-key-reused", "The Idempotency-Key was already used with a different request.",
                    StatusCodes.Status422UnprocessableEntity);
            }
            httpContext.Response.Headers["Idempotent-Replayed"] = "true";
            httpContext.Response.Headers["Location"] = existing.ResourcePath;
            return Results.Content(existing.ResponseJson, "application/json", Encoding.UTF8, existing.ResponseStatus);
        }

        Account account;
        try
        {
            account = Account.Create(request.Name ?? "", new ApplicantInput(
                request.Applicant.LegalName ?? "", request.Applicant.Fein ?? "", request.Applicant.MailingAddress),
                tenant.TenantId, DateTimeOffset.UtcNow);
        }
        catch (ArgumentException exception)
        {
            return Problem("request-invalid", exception.Message, StatusCodes.Status422UnprocessableEntity);
        }

        var response = ToAccountResponse(account);
        var responseJson = JsonSerializer.Serialize(response, JsonOptions);
        var resourcePath = $"/v1/accounts/{account.AccountId:D}";
        db.Accounts.Add(new PolicyAccountEntity
        {
            AccountId = account.AccountId,
            ApplicantId = account.Applicant.ApplicantId,
            TenantId = account.TenantId,
            Name = account.Name,
            ApplicantLegalName = account.Applicant.LegalName,
            ApplicantFein = account.Applicant.Fein,
            MailingAddressJson = JsonSerializer.Serialize(account.Applicant.MailingAddress, JsonOptions),
            CreatedAt = account.CreatedAt
        });
        db.Idempotency.Add(new PolicyIdempotencyEntity
        {
            TenantId = tenant.TenantId,
            Operation = AccountOperation,
            Key = idempotencyKey,
            RequestHash = requestHash,
            ResponseStatus = StatusCodes.Status201Created,
            ResponseJson = responseJson,
            ResourcePath = resourcePath,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        httpContext.Response.Headers["Location"] = resourcePath;
        return Results.Content(responseJson, "application/json", Encoding.UTF8, StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetAccount(Guid accountId, ITenantContext tenant, PolicyDbContext db,
        CancellationToken cancellationToken)
    {
        var row = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(
            item => item.TenantId == tenant.TenantId && item.AccountId == accountId, cancellationToken);
        if (row is null)
        {
            return Problem("not-found", "Account was not found.", StatusCodes.Status404NotFound);
        }

        var address = JsonSerializer.Deserialize<Address>(row.MailingAddressJson, JsonOptions)!;
        var result = new AccountResponse(row.AccountId, row.Name,
            new ApplicantResponse(row.ApplicantId, row.ApplicantLegalName, row.ApplicantFein, address), row.CreatedAt);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateQuote(
        CreateQuoteRequest? request,
        HttpContext httpContext,
        ITenantContext tenant,
        PolicyDbContext db,
        IHttpClientFactory clientFactory,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 64)
        {
            return Problem("idempotency-key-required", "A valid Idempotency-Key is required.", StatusCodes.Status400BadRequest);
        }
        if (tenant.TenantId is null || request?.EffectiveFrom is null || request.EffectiveTo is null
            || request.EffectiveTo <= request.EffectiveFrom
            || request.Coverages is not { Count: > 0 }
            || request.Locations is not { Count: > 0 }
            || request.Exposures is not { Count: > 0 })
        {
            return Problem("request-invalid", "Quote term, Coverages, Locations and Exposures are required.", StatusCodes.Status422UnprocessableEntity);
        }

        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requestJson)));
        var existing = await FindIdempotency(db, tenant.TenantId, "create-quote", idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Replay(httpContext, existing, requestHash);
        }

        var accountExists = await db.Accounts.AsNoTracking().AnyAsync(
            account => account.TenantId == tenant.TenantId && account.AccountId == request.AccountId,
            cancellationToken);
        if (!accountExists)
        {
            return Problem("not-found", "Account was not found.", StatusCodes.Status404NotFound);
        }

        Schedule schedule;
        try
        {
            schedule = Schedule.Create(
                request.Coverages.Select(item => new CoverageInput(item.Code ?? "", item.Deductible ?? "")).ToArray(),
                request.Locations.Select(location => new LocationInput(location.Ref ?? "", location.Address!, location.ProtectionClass,
                    (location.Buildings ?? []).Select(building => new BuildingInput(building.Ref ?? "", building.Construction ?? "",
                        building.YearBuilt, building.SquareFeet, (building.Units ?? []).Select(unit => new UnitInput(
                            unit.Ref ?? "", unit.UnitNumber ?? "", unit.SquareFeet, unit.Description)).ToArray())).ToArray())).ToArray(),
                request.Exposures.Select(exposure => new ExposureInput(exposure.Ref ?? "", exposure.CoverageCode ?? "",
                    exposure.SubjectRef ?? "", ParseLimit(exposure.Limit), exposure.NaicsCode ?? "")).ToArray(),
                request.EffectiveFrom.Value);
        }
        catch (Exception exception) when (exception is ArgumentException or ScheduleValidationException or FormatException)
        {
            return Problem("request-invalid", exception.Message, StatusCodes.Status422UnprocessableEntity);
        }

        PolicyEvaluation evaluation;
        try
        {
            var correlationId = CorrelationId(httpContext);
            evaluation = await PolicyQuoteEvaluator.EvaluateAsync(schedule, request.EffectiveFrom.Value,
                tenant.TenantId, correlationId, null, null, clientFactory, cancellationToken);
        }
        catch (PolicyDependencyException exception)
        {
            return Problem(exception.Code, exception.Message,
                exception.Unavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status422UnprocessableEntity);
        }

        var now = DateTimeOffset.UtcNow;
        var quoteId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var status = evaluation.Outcome switch
        {
            "StraightThrough" => "Quoted",
            "Refer" => "Referred",
            "Decline" => "Declined",
            _ => "Declined"
        };
        var scheduleJson = JsonSerializer.Serialize(schedule, JsonOptions);
        var response = ToQuoteResponse(quoteId, policyId, request.AccountId, 1, status,
            request.EffectiveFrom.Value, request.EffectiveTo.Value, now, now.AddDays(30),
            evaluation.DecisionJson, evaluation.RatingJson, schedule);
        var responseJson = JsonSerializer.Serialize(response, JsonOptions);
        var resourcePath = $"/v1/quotes/{quoteId:D}";

        existing = await FindIdempotency(db, tenant.TenantId, "create-quote", idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Replay(httpContext, existing, requestHash);
        }

        db.Revisions.Add(new PolicyRevisionEntity
        {
            QuoteId = quoteId,
            PolicyId = policyId,
            TenantId = tenant.TenantId,
            AccountId = request.AccountId,
            RevisionNo = 1,
            Status = status,
            EffectiveFrom = request.EffectiveFrom.Value,
            EffectiveTo = request.EffectiveTo.Value,
            QuotedAt = now,
            ExpiresAt = now.AddDays(30),
            DecisionJson = evaluation.DecisionJson,
            RatingJson = evaluation.RatingJson,
            ScheduleJson = scheduleJson,
            CreatedAt = now
        });
        db.Idempotency.Add(new PolicyIdempotencyEntity
        {
            TenantId = tenant.TenantId,
            Operation = "create-quote",
            Key = idempotencyKey,
            RequestHash = requestHash,
            ResponseStatus = StatusCodes.Status201Created,
            ResponseJson = responseJson,
            ResourcePath = resourcePath,
            CreatedAt = now
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            existing = await FindIdempotency(db, tenant.TenantId, "create-quote", idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                return Replay(httpContext, existing, requestHash);
            }
            throw;
        }

        httpContext.Response.Headers["Location"] = resourcePath;
        return Results.Content(responseJson, "application/json", Encoding.UTF8, StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetQuote(Guid quoteId, ITenantContext tenant, PolicyDbContext db,
        CancellationToken cancellationToken)
    {
        var row = await db.Revisions.AsNoTracking().SingleOrDefaultAsync(
            revision => revision.TenantId == tenant.TenantId && revision.QuoteId == quoteId, cancellationToken);
        if (row is null)
        {
            return Problem("not-found", "Quote was not found.", StatusCodes.Status404NotFound);
        }
        var schedule = JsonSerializer.Deserialize<Schedule>(row.ScheduleJson, JsonOptions)!;
        return Results.Ok(ToQuoteResponse(row.QuoteId, row.PolicyId, row.AccountId, row.RevisionNo, row.Status,
            row.EffectiveFrom, row.EffectiveTo, row.QuotedAt, row.ExpiresAt,
            row.DecisionJson, row.RatingJson, schedule));
    }

    private static async Task<IResult> GetPolicy(Guid policyId, ITenantContext tenant, PolicyDbContext db,
        CancellationToken cancellationToken)
    {
        var revision = await db.Revisions.AsNoTracking()
            .Where(item => item.TenantId == tenant.TenantId && item.PolicyId == policyId && item.Status == "Bound")
            .OrderByDescending(item => item.RevisionNo)
            .FirstOrDefaultAsync(cancellationToken);
        if (revision is null || revision.RatingJson is null || revision.PolicyNumber is null)
        {
            return Problem("not-found", "Policy was not found.", StatusCodes.Status404NotFound);
        }

        var booking = await db.BookingStatuses.AsNoTracking().SingleOrDefaultAsync(item =>
            item.TenantId == tenant.TenantId && item.PolicyId == policyId && item.RevisionNo == revision.RevisionNo,
            cancellationToken);
        using var ratingDocument = JsonDocument.Parse(revision.RatingJson);
        var rating = ratingDocument.RootElement;
        return Results.Ok(new
        {
            policyId = revision.PolicyId,
            policyNumber = revision.PolicyNumber,
            accountId = revision.AccountId,
            revisionNo = revision.RevisionNo,
            status = revision.Status,
            effectiveFrom = revision.EffectiveFrom,
            effectiveTo = revision.EffectiveTo,
            writtenPremium = rating.GetProperty("writtenPremium").GetString(),
            boundAt = revision.BoundAt,
            bookingStatus = booking?.Status ?? "Pending",
            transactionId = booking?.TransactionId,
            bookedAt = booking?.BookedAt
        });
    }

    private static async Task<IResult> BindQuote(
        Guid quoteId,
        HttpContext httpContext,
        ITenantContext tenant,
        PolicyDbContext db,
        IHttpClientFactory clientFactory,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 64)
        {
            return Problem("idempotency-key-required", "A valid Idempotency-Key is required.", StatusCodes.Status400BadRequest);
        }
        if (tenant.TenantId is null)
        {
            return Problem("tenant-required", "Tenant is required.", StatusCodes.Status400BadRequest);
        }

        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{quoteId:D}|bind")));
        var existing = await FindIdempotency(db, tenant.TenantId, "bind-quote", idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Replay(httpContext, existing, requestHash);
        }

        var row = await db.Revisions.AsNoTracking().SingleOrDefaultAsync(
            item => item.TenantId == tenant.TenantId && item.QuoteId == quoteId, cancellationToken);
        if (row is null)
        {
            return Problem("not-found", "Quote was not found.", StatusCodes.Status404NotFound);
        }
        if (row.Status != "Quoted")
        {
            return Conflict("quote-not-bindable", "Only a Straight-Through Quote can bind.");
        }
        if (QuoteFreshness.HasExpired(row.ExpiresAt, DateTimeOffset.UtcNow))
        {
            return Conflict("quote-expired", "Quote has expired.");
        }
        if (row.RatingJson is null)
        {
            return Conflict("quote-stale", "Quote has no pinned Rate Snapshot.");
        }

        var schedule = JsonSerializer.Deserialize<Schedule>(row.ScheduleJson, JsonOptions)!;
        using var storedDecisionDocument = JsonDocument.Parse(row.DecisionJson);
        using var storedRatingDocument = JsonDocument.Parse(row.RatingJson);
        var pinnedRuleset = storedDecisionDocument.RootElement.GetProperty("rulesetVersion").GetString();
        var pinnedRateManual = storedRatingDocument.RootElement.GetProperty("rateManualVersion").GetString();
        PolicyEvaluation evaluation;
        try
        {
            evaluation = await PolicyQuoteEvaluator.EvaluateAsync(schedule, row.EffectiveFrom, tenant.TenantId,
                CorrelationId(httpContext), pinnedRuleset, pinnedRateManual, clientFactory, cancellationToken);
        }
        catch (PolicyDependencyException exception)
        {
            if (QuoteFreshness.IsPinnedVersionUnavailable(exception.Code)
                || !exception.Unavailable)
            {
                return Conflict("quote-stale", "Pinned Rules or Rating version is no longer available.");
            }
            return Problem(exception.Code, "Rules or Rating is unavailable.", StatusCodes.Status503ServiceUnavailable);
        }

        if (QuoteFreshness.IsStale(row.DecisionJson, row.RatingJson, evaluation))
        {
            return Conflict("quote-stale", "Decision or Rate Snapshot changed since the Quote was created.");
        }

        var boundAt = DateTimeOffset.UtcNow;
        var policyNumber = await NewPolicyNumberAsync(db, row.EffectiveFrom.Year, cancellationToken);
        var result = new BindResult(row.PolicyId, policyNumber, row.RevisionNo, "Bound", boundAt,
            Money(evaluation.WrittenPremium!.Value), "Pending");
        var responseJson = JsonSerializer.Serialize(result, JsonOptions);
        var resourcePath = $"/v1/policies/{row.PolicyId:D}";
        var eventId = Guid.NewGuid();
        var eventJson = CreatePolicyBoundEvent(row, schedule, evaluation, policyNumber, boundAt,
            eventId, tenant.TenantId, CorrelationId(httpContext));

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var concurrentReplay = await FindIdempotency(db, tenant.TenantId, "bind-quote", idempotencyKey, cancellationToken);
        if (concurrentReplay is not null)
        {
            return Replay(httpContext, concurrentReplay, requestHash);
        }
        var current = await db.Revisions.SingleOrDefaultAsync(
            item => item.TenantId == tenant.TenantId && item.QuoteId == quoteId, cancellationToken);
        if (current is null)
        {
            return Problem("not-found", "Quote was not found.", StatusCodes.Status404NotFound);
        }
        if (!current.RowVersion.SequenceEqual(row.RowVersion))
        {
            return Conflict("quote-not-bindable", "Quote changed while Bind was being evaluated.");
        }
        if (current.Status != "Quoted" || current.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Conflict(current.ExpiresAt <= DateTimeOffset.UtcNow ? "quote-expired" : "quote-not-bindable",
                "Quote can no longer bind.");
        }

        current.Status = "Bound";
        current.PolicyNumber = policyNumber;
        current.BoundAt = boundAt;
        db.BookingStatuses.Add(new PolicyBookingStatusEntity
        {
            TenantId = tenant.TenantId,
            PolicyId = current.PolicyId,
            RevisionNo = current.RevisionNo,
            Status = "Pending",
            CreatedAt = boundAt,
            UpdatedAt = boundAt
        });
        db.Idempotency.Add(new PolicyIdempotencyEntity
        {
            TenantId = tenant.TenantId,
            Operation = "bind-quote",
            Key = idempotencyKey,
            RequestHash = requestHash,
            ResponseStatus = StatusCodes.Status200OK,
            ResponseJson = responseJson,
            ResourcePath = resourcePath,
            CreatedAt = boundAt
        });
        db.Outbox.Add(new PolicyOutboxEntity
        {
            MessageId = eventId,
            TenantId = tenant.TenantId,
            Type = "clp.policy.policy-bound.v1",
            PayloadJson = eventJson,
            CreatedAt = boundAt
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        httpContext.Response.Headers["Location"] = resourcePath;
        return Results.Content(responseJson, "application/json", Encoding.UTF8, StatusCodes.Status200OK);
    }

    private static async Task<string> NewPolicyNumberAsync(PolicyDbContext db, int year, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var number = $"CP-{year:D4}-{RandomNumberGenerator.GetInt32(100000, 1000000):D6}";
            if (!await db.Revisions.AsNoTracking().AnyAsync(row => row.PolicyNumber == number, cancellationToken))
            {
                return number;
            }
        }
        throw new InvalidOperationException("Unable to allocate a unique Policy number.");
    }

    private static string CreatePolicyBoundEvent(PolicyRevisionEntity revision, Schedule schedule,
        PolicyEvaluation evaluation, string policyNumber, DateTimeOffset boundAt, Guid eventId,
        string tenantId, string correlationId)
    {
        using var ratingDocument = JsonDocument.Parse(evaluation.RatingJson!);
        var rating = ratingDocument.RootElement;
        var ratedById = rating.GetProperty("exposures").EnumerateArray()
            .ToDictionary(item => item.GetProperty("exposureId").GetGuid());
        var payload = new
        {
            specversion = "1.0",
            id = eventId,
            source = "urn:clp:policy",
            type = "clp.policy.policy-bound.v1",
            subject = $"policy/{revision.PolicyId:D}",
            time = boundAt,
            datacontenttype = "application/json",
            dataschema = "https://clp.example/schemas/events/policy-bound.v1.json",
            tenantid = tenantId,
            correlationid = correlationId,
            causationid = revision.QuoteId,
            traceparent = System.Diagnostics.Activity.Current?.Id,
            data = new
            {
                aggregateSeq = revision.RevisionNo,
                policyId = revision.PolicyId,
                policyNumber,
                accountId = revision.AccountId,
                revisionNo = revision.RevisionNo,
                lineOfBusiness = "CP",
                effectiveFrom = revision.EffectiveFrom,
                effectiveTo = revision.EffectiveTo,
                writtenPremium = rating.GetProperty("writtenPremium").GetString(),
                currency = rating.GetProperty("currency").GetString(),
                rulesetVersion = evaluation.RulesetVersion,
                rateManualVersion = evaluation.RateManualVersion,
                boundAt,
                exposures = schedule.Exposures.Select(exposure =>
                {
                    var rated = ratedById[exposure.ExposureId];
                    return new
                    {
                        exposureId = exposure.ExposureId,
                        coverageCode = exposure.CoverageCode,
                        locationId = exposure.LocationId,
                        buildingId = exposure.BuildingId,
                        unitId = exposure.UnitId,
                        naicsCode = exposure.NaicsCode,
                        limit = Money(exposure.Limit),
                        baseRate = rated.GetProperty("baseRate").GetString(),
                        premium = rated.GetProperty("premium").GetString(),
                        factors = rated.GetProperty("factors").Clone()
                    };
                })
            }
        };
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static object ToQuoteResponse(Guid quoteId, Guid policyId, Guid accountId, int revisionNo, string status,
        DateOnly effectiveFrom, DateOnly effectiveTo, DateTimeOffset quotedAt, DateTimeOffset expiresAt,
        string decisionJson, string? ratingJson, Schedule schedule)
    {
        using var decisionDocument = JsonDocument.Parse(decisionJson);
        var decision = decisionDocument.RootElement;
        JsonElement? rating = null;
        if (ratingJson is not null)
        {
            using var ratingDocument = JsonDocument.Parse(ratingJson);
            rating = ratingDocument.RootElement.Clone();
        }
        return new
        {
            quoteId,
            policyId,
            accountId,
            revisionNo,
            status,
            effectiveFrom,
            effectiveTo,
            quotedAt,
            expiresAt,
            decision = new
            {
                outcome = decision.GetProperty("outcome").GetString(),
                rulesetVersion = decision.GetProperty("rulesetVersion").GetString(),
                reasons = decision.GetProperty("reasons").Clone()
            },
            rating,
            schedule = new
            {
                coverages = schedule.Coverages.Select(item => new { coverageId = item.CoverageId, code = item.Code, deductible = Money(item.Deductible) }),
                locations = schedule.Locations.Select(location => new
                {
                    locationId = location.LocationId,
                    @ref = location.Ref,
                    address = location.Address,
                    protectionClass = location.ProtectionClass,
                    buildings = location.Buildings.Select(building => new
                    {
                        buildingId = building.BuildingId,
                        @ref = building.Ref,
                        construction = building.Construction,
                        yearBuilt = building.YearBuilt,
                        squareFeet = building.SquareFeet,
                        units = building.Units.Select(unit => new
                        {
                            unitId = unit.UnitId,
                            @ref = unit.Ref,
                            unitNumber = unit.UnitNumber,
                            squareFeet = unit.SquareFeet,
                            description = unit.Description
                        })
                    })
                }),
                exposures = schedule.Exposures.Select(exposure => new
                {
                    exposureId = exposure.ExposureId,
                    @ref = exposure.Ref,
                    coverageCode = exposure.CoverageCode,
                    subjectRef = exposure.SubjectRef,
                    locationId = exposure.LocationId,
                    buildingId = exposure.BuildingId,
                    unitId = exposure.UnitId,
                    limit = Money(exposure.Limit),
                    naicsCode = exposure.NaicsCode
                })
            }
        };
    }

    private static decimal ParseLimit(string? value) => decimal.TryParse(value, NumberStyles.AllowDecimalPoint,
        CultureInfo.InvariantCulture, out var limit)
        ? limit
        : throw new FormatException("Exposure limit must be a decimal string.");

    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string CorrelationId(HttpContext context) =>
        Guid.TryParse(context.Request.Headers["X-Correlation-Id"], out var correlationId)
            ? correlationId.ToString("D")
            : Guid.NewGuid().ToString("D");

    private static Task<PolicyIdempotencyEntity?> FindIdempotency(PolicyDbContext db, string tenantId,
        string operation, string key, CancellationToken cancellationToken) =>
        db.Idempotency.AsNoTracking().SingleOrDefaultAsync(row => row.TenantId == tenantId
            && row.Operation == operation && row.Key == key, cancellationToken);

    private static IResult Replay(HttpContext context, PolicyIdempotencyEntity existing, string requestHash)
    {
        if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return Problem("idempotency-key-reused", "The Idempotency-Key was already used with a different request.",
                StatusCodes.Status422UnprocessableEntity);
        }
        context.Response.Headers["Idempotent-Replayed"] = "true";
        context.Response.Headers["Location"] = existing.ResourcePath;
        return Results.Content(existing.ResponseJson, "application/json", Encoding.UTF8, existing.ResponseStatus);
    }

    private static AccountResponse ToAccountResponse(Account account) => new(
        account.AccountId, account.Name,
        new ApplicantResponse(account.Applicant.ApplicantId, account.Applicant.LegalName,
            account.Applicant.Fein, account.Applicant.MailingAddress), account.CreatedAt);

    private static IResult Problem(string code, string title, int status) => new ProblemResult(code, title, status);
    private static IResult Conflict(string code, string title) =>
        Problem(code, title, StatusCodes.Status409Conflict);

    private sealed class ProblemResult(string code, string title, int status) : IResult
    {
        public Task ExecuteAsync(HttpContext context) => ProblemWriter.WriteAsync(context, status, code, title);
    }

    private sealed record CreateAccountRequest(string? Name, ApplicantRequest? Applicant);
    private sealed record ApplicantRequest(string? LegalName, string? Fein, Address? MailingAddress);
    private sealed record AccountResponse(Guid AccountId, string Name, ApplicantResponse Applicant, DateTimeOffset CreatedAt);
    private sealed record ApplicantResponse(Guid ApplicantId, string LegalName, string Fein, Address MailingAddress);
    private sealed record CreateQuoteRequest(Guid AccountId, DateOnly? EffectiveFrom, DateOnly? EffectiveTo,
        List<CoverageRequest>? Coverages, List<LocationRequest>? Locations, List<ExposureRequest>? Exposures);
    private sealed record CoverageRequest(string? Code, string? Deductible);
    private sealed record LocationRequest(string? Ref, Address? Address, int ProtectionClass, List<BuildingRequest>? Buildings);
    private sealed record BuildingRequest(string? Ref, string? Construction, int YearBuilt, int SquareFeet, List<UnitRequest>? Units);
    private sealed record UnitRequest(string? Ref, string? UnitNumber, int SquareFeet, string? Description);
    private sealed record ExposureRequest(string? Ref, string? CoverageCode, string? SubjectRef, string? Limit, string? NaicsCode);
    private sealed record BindResult(Guid PolicyId, string PolicyNumber, int RevisionNo, string Status,
        DateTimeOffset BoundAt, string WrittenPremium, string BookingStatus);
}
