using Microsoft.EntityFrameworkCore;

namespace Policy.Infrastructure;

public sealed class PolicyDbContext(DbContextOptions<PolicyDbContext> options) : DbContext(options);
