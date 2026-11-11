using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure;

public sealed class LedgerDbContext(DbContextOptions<LedgerDbContext> options) : DbContext(options);
