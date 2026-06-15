using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HypeGrid.Domain.Content;
using HypeGrid.Domain.Identity;
using HypeGrid.Domain.Marketing;
using HypeGrid.Infrastructure.Data;
using HypeGrid.Shared.Constants;
using HypeGrid.Shared.Enums;

namespace HypeGrid.Infrastructure.Data.Seed;

/// <summary>
/// Idempotent startup seeder: roles, a development-only SuperAdmin, the public
/// website's services + packages (lifted verbatim from the currently hardcoded
/// HypeGridWebsite pages), and default site settings. Safe to run on every boot
/// — each block checks for existing rows before inserting.
/// </summary>
public static class HypeGridSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<User> userManager,
        IConfiguration config,
        IHostEnvironmentAccessor env,
        ILogger logger)
    {
        await SeedRolesAsync(roleManager, logger);
        await SeedBootstrapAdminAsync(roleManager, userManager, config, logger);
        await SeedAdminUserAsync(userManager, config, env, logger);
        await SeedServicesAsync(db, logger);
        await SeedPackagesAsync(db, logger);
        await SeedSiteSettingsAsync(db, logger);
        await SeedAlertsSettingsAsync(db, logger);
        await SeedHeroPlacementsAsync(db, logger);
        await SeedDealsAsync(db, logger);
        await SeedFeaturedVideoAsync(db, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager, ILogger logger)
    {
        foreach (var role in HypeGridRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = role });
                logger.LogInformation("Seeded role {Role}", role);
            }
        }
    }

    private static async Task SeedAdminUserAsync(
        UserManager<User> userManager, IConfiguration config, IHostEnvironmentAccessor env, ILogger logger)
    {
        // Development-only convenience account. In non-Development the seed
        // user is NOT created unless Seed:ForceAdmin is explicitly true.
        var force = config.GetValue<bool>("Seed:ForceAdmin");
        if (!env.IsDevelopment && !force)
            return;

        var email = config["Seed:AdminEmail"] ?? "admin@hypegrid.co.za";
        var password = config["Seed:AdminPassword"] ?? "ChangeMe123!";

        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "HypeGrid",
            LastName = "Admin",
            IsActive = true,
            AccountStatus = AccountStatus.Active,
            CreatedOnUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, HypeGridRoles.SuperAdmin);
            logger.LogWarning("Seeded DEVELOPMENT SuperAdmin {Email} — change this password.", email);
        }
        else
        {
            logger.LogError("Failed to seed admin user: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    /// <summary>
    /// Production-safe first-admin bootstrap. Gated entirely by config and OFF by
    /// default. All values come from environment / app-pool settings — NOTHING is
    /// hardcoded here and the password is never logged. Intended flow:
    ///   1. Set BootstrapAdmin__Enabled=true + Email/Password/PhoneNumber, deploy.
    ///   2. Log in once.
    ///   3. Set BootstrapAdmin__Enabled=false and restart.
    /// Idempotent: if the user already exists it only ensures the role is assigned
    /// and never resets the password unless BootstrapAdmin__ResetPassword=true.
    /// </summary>
    private static async Task SeedBootstrapAdminAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<User> userManager,
        IConfiguration config,
        ILogger logger)
    {
        if (!config.GetValue<bool>("BootstrapAdmin:Enabled"))
            return;

        var email = config["BootstrapAdmin:Email"]?.Trim();
        var password = config["BootstrapAdmin:Password"];
        var phone = config["BootstrapAdmin:PhoneNumber"]?.Trim();
        var roleSetting = config["BootstrapAdmin:Role"]?.Trim();
        var role = string.IsNullOrWhiteSpace(roleSetting) ? HypeGridRoles.SuperAdmin : roleSetting;
        var resetPassword = config.GetValue<bool>("BootstrapAdmin:ResetPassword");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogError(
                "BootstrapAdmin is enabled but Email and/or Password are not configured. " +
                "Set BootstrapAdmin__Email and BootstrapAdmin__Password. Skipping bootstrap.");
            return;
        }

        // Ensure the target role exists. SeedRolesAsync already created the known
        // set; this guards a custom role value so the assignment cannot fail.
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = role });
            logger.LogInformation("Seeded role {Role} (requested by bootstrap admin).", role);
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is null)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone,
                PhoneNumberConfirmed = !string.IsNullOrWhiteSpace(phone),
                FirstName = "HypeGrid",
                LastName = "Admin",
                IsActive = true,
                AccountStatus = AccountStatus.Active,
                CreatedOnUtc = DateTime.UtcNow
            };

            // UserManager hashes the password and sets the security stamp / normalized
            // fields correctly — never insert Identity users via raw SQL.
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError("Bootstrap admin creation failed for {Email}: {Errors}",
                    email, string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(user, role);
            logger.LogWarning(
                "Bootstrap admin {Email} created with role {Role}. SECURITY: set " +
                "BootstrapAdmin__Enabled=false now and change the password in the portal.",
                email, role);
            return;
        }

        // User already exists — ensure the role; never silently reset the password.
        if (!await userManager.IsInRoleAsync(existing, role))
        {
            await userManager.AddToRoleAsync(existing, role);
            logger.LogInformation("Bootstrap admin {Email} already existed; ensured role {Role}.", email, role);
        }
        else
        {
            logger.LogInformation("Bootstrap admin {Email} already exists with role {Role}; no changes.", email, role);
        }

        if (resetPassword)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(existing);
            var reset = await userManager.ResetPasswordAsync(existing, token, password);
            if (reset.Succeeded)
                logger.LogWarning(
                    "Bootstrap admin {Email} password was reset via BootstrapAdmin__ResetPassword. " +
                    "Set BootstrapAdmin__ResetPassword=false and BootstrapAdmin__Enabled=false now.", email);
            else
                logger.LogError("Bootstrap admin {Email} password reset failed: {Errors}",
                    email, string.Join("; ", reset.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedServicesAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Services.AnyAsync()) return;

        // Lifted verbatim from HypeGridWebsite/src/pages/Services.jsx.
        var services = new (string Title, string[] Includes)[]
        {
            ("Business Promotion", new[] { "Promotional content", "Social media posts", "Reels/TikTok ideas", "Campaign messaging", "WhatsApp traffic campaigns", "Brand awareness push" }),
            ("Music & Artist Promotion", new[] { "Song release campaign", "TikTok/Reels sound push", "Snippet content", "Cover art motion graphics", "Fan engagement captions", "Streaming CTA" }),
            ("Influencer & Creator Campaigns", new[] { "Creator campaign brief", "Influencer selection", "Campaign content direction", "Creator posting", "Performance summary" }),
            ("Launch Campaigns", new[] { "Teaser campaign", "Launch countdown", "Launch-day push", "Content pack", "Creator/influencer activation", "Post-launch momentum" }),
            ("Event Promotion", new[] { "Event posters", "Promo videos", "Countdown content", "Influencer invitations", "Ticket CTA campaigns", "Recap content" }),
            ("Social Media Management", new[] { "Content calendar", "Page management", "Captions", "Post designs", "Reels planning", "Engagement support", "Monthly reporting" }),
            ("Content Creation", new[] { "Promo videos", "Reels", "Flyers", "Motion graphics", "Campaign visuals", "Captions", "Digital content kits" }),
            ("Paid Traffic Campaigns", new[] { "Facebook ads", "Instagram ads", "TikTok ads", "Google ads", "WhatsApp click campaigns", "Lead campaigns" })
        };

        var order = 0;
        foreach (var (title, includes) in services)
        {
            db.Services.Add(new Service
            {
                Id = Guid.NewGuid(),
                Title = title,
                Slug = Slugify(title),
                Includes = includes.ToList(),
                SortOrder = order++,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} services.", services.Length);
    }

    private static async Task SeedPackagesAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Packages.AnyAsync()) return;

        // Lifted verbatim from HypeGridWebsite/src/pages/Packages.jsx.
        var packages = new (string Name, string Desc, string Color, bool Featured, string[] Features)[]
        {
            ("Starter Hype", "For small campaigns, basic visibility, and first-time promotion.", "cyan", false,
                new[] { "Campaign concept", "3–5 social creatives", "Captions", "Basic posting plan", "Light promotional push" }),
            ("Growth Hype", "For businesses, artists, or events ready to build stronger awareness.", "green", true,
                new[] { "Campaign strategy", "Content pack", "Reels/short video direction", "Influencer/creator options", "Multi-platform promotion", "Performance summary" }),
            ("Premium Launch", "For serious launches, events, products, songs, or brands.", "cyan", false,
                new[] { "Full campaign planning", "Launch countdown", "Creator/influencer activation", "Paid traffic setup", "Branded content assets", "Campaign reporting" }),
            ("Monthly Brand Management", "For clients who need ongoing social media presence.", "green", false,
                new[] { "Content calendar", "Post designs", "Captions", "Reels planning", "Page management", "Monthly reporting" }),
            ("Music Release Push", "For artists and musicians.", "cyan", false,
                new[] { "Release-week campaign", "Song snippet content", "TikTok/Reels concept", "Creator challenge idea", "Fan engagement captions", "Streaming CTA" })
        };

        var order = 0;
        foreach (var (name, desc, color, featured, features) in packages)
        {
            db.Packages.Add(new Package
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = Slugify(name),
                Description = desc,
                Color = color,
                IsFeatured = featured,
                IsQuoteBased = true,
                PriceLabel = "Request Quote",
                Cta = "Start a Campaign",
                Features = features.ToList(),
                SortOrder = order++,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} packages.", packages.Length);
    }

    private static async Task SeedSiteSettingsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.SiteSettings.AnyAsync()) return;

        // Defaults mirror the hardcoded HypeGridAdmin/src/pages/Settings.jsx.
        var defaults = new (string Key, string? Value, string Group)[]
        {
            ("company_name", "HypeGrid", "company"),
            ("email", "hello@hypegrid.co.za", "company"),
            ("phone", "", "company"),
            ("website", "https://hypegrid.co.za", "company"),
            ("address", "", "company"),
            ("support_email", "support@hypegrid.co.za", "communication"),
            ("noreply_email", "noreply@hypegrid.co.za", "communication"),
            ("campaigns_email", "campaigns@hypegrid.co.za", "communication"),
            ("creators_email", "creators@hypegrid.co.za", "communication"),
            ("notification_email", "leads@hypegrid.co.za", "communication"),
            ("whatsapp_channel_url", "https://whatsapp.com/channel/0029VbCla7vEQIaqrNnT1E2e", "communication"),
            ("invoice_prefix", "HG-INV", "finance"),
            ("quote_prefix", "HG-Q", "finance"),
            ("tax_rate", "15", "finance"),
            ("currency", "ZAR", "finance")
        };

        foreach (var (key, value, group) in defaults)
        {
            db.SiteSettings.Add(new SiteSetting
            {
                Id = Guid.NewGuid(),
                Key = key,
                Value = value,
                Group = group,
                CreatedDate = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} site settings.", defaults.Length);
    }

    /// <summary>
    /// Ensures the public WhatsApp Channel URL setting exists even on databases
    /// seeded before HypeGrid Alerts shipped (SeedSiteSettingsAsync only runs on a
    /// fresh settings table). Idempotent: inserts the single key if it's missing.
    /// </summary>
    private static async Task SeedAlertsSettingsAsync(AppDbContext db, ILogger logger)
    {
        const string key = "whatsapp_channel_url";
        const string realUrl = "https://whatsapp.com/channel/0029VbCla7vEQIaqrNnT1E2e";
        const string placeholder = "https://whatsapp.com/channel/YOUR_CHANNEL_ID";

        var row = await db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (row is null)
        {
            db.SiteSettings.Add(new SiteSetting
            {
                Id = Guid.NewGuid(),
                Key = key,
                Value = realUrl,
                Group = "communication",
                CreatedDate = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded WhatsApp Channel URL setting ({Key}).", key);
            return;
        }

        // Heal a leftover placeholder/blank from databases seeded before the real
        // channel existed. Never overwrites a real admin-set value.
        if (string.IsNullOrWhiteSpace(row.Value) || row.Value == placeholder)
        {
            row.Value = realUrl;
            db.SiteSettings.Update(row);
            await db.SaveChangesAsync();
            logger.LogInformation("Updated placeholder WhatsApp Channel URL to the live channel.");
        }
    }

    private static async Task SeedHeroPlacementsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.HeroPlacements.AnyAsync()) return;

        var heroes = new (string Title, string Subtitle, string Badge, string Cta, string CtaUrl, string Target, int Priority)[]
        {
            ("What's hot right now on HypeGrid", "Discover featured campaigns, deals, and brands making noise across South Africa.", "Featured", "Explore the grid", "/deals", "internal", 0),
            ("Put your campaign on the grid", "Promote your business, music, event, or product to an audience that's actually paying attention.", "Advertise", "Start a campaign", "/campaigns", "internal", 1),
            ("Latest specials, worth noticing", "Hand-picked deals and discounts from shops and brands around you.", "Deals", "See all specials", "/deals", "internal", 2),
        };

        foreach (var (title, subtitle, badge, cta, ctaUrl, target, priority) in heroes)
        {
            db.HeroPlacements.Add(new HeroPlacement
            {
                Id = Guid.NewGuid(),
                Title = title,
                Subtitle = subtitle,
                Badge = badge,
                CtaText = cta,
                CtaUrl = ctaUrl,
                CtaTargetType = target,
                Priority = priority,
                IsActive = true,
                TrackingEnabled = true,
                CreatedDate = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} hero placements.", heroes.Length);
    }

    private static async Task SeedDealsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Deals.AnyAsync()) return;

        var deals = new (string Title, string Brand, string Category, string Desc, string Discount, bool Featured, int Priority)[]
        {
            ("Mid-month airtime & data bundles", "Mobile Networks", "Mobile", "Bonus data and airtime specials running across major networks this month.", "Bonus data", true, 0),
            ("Weekend grocery savings", "Local Grocers", "Grocery", "Stock up specials on everyday essentials at participating stores.", "Save up to 30%", true, 1),
            ("Fresh streetwear drop", "Local Brands", "Fashion", "New-season streetwear from South African brands, while stocks last.", "New drop", false, 2),
            ("Two-for-one meal specials", "Local Eateries", "Food", "Midweek two-for-one deals at selected restaurants and takeaways.", "2 for 1", false, 3),
            ("Tech accessory clearance", "Tech Stores", "Tech", "Clearance pricing on headphones, chargers, and accessories.", "Clearance", false, 4),
        };

        var order = 0;
        foreach (var (title, brand, category, desc, discount, featured, priority) in deals)
        {
            db.Deals.Add(new Deal
            {
                Id = Guid.NewGuid(),
                Title = title,
                Slug = $"{Slugify(title)}-{++order}",
                BrandName = brand,
                Category = category,
                ShortDescription = desc,
                DiscountLabel = discount,
                CtaText = "View deal",
                IsActive = true,
                IsFeatured = featured,
                IsSponsored = false,
                Priority = priority,
                SourceName = brand,
                ValidUntil = DateTime.UtcNow.AddDays(30),
                CreatedDate = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} deals.", deals.Length);
    }

    private static async Task SeedFeaturedVideoAsync(AppDbContext db, ILogger logger)
    {
        if (await db.FeaturedVideos.AnyAsync()) return;

        db.FeaturedVideos.Add(new FeaturedVideo
        {
            Id = Guid.NewGuid(),
            Title = "HypeGrid in motion",
            Subtitle = "See how brands, creators, and campaigns come together on the grid.",
            YouTubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            CtaText = "Start your campaign",
            CtaUrl = "/campaigns",
            IsActive = true,
            SortOrder = 0,
            CreatedDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded featured video.");
    }

    private static string Slugify(string input) =>
        new string(input.ToLowerInvariant()
            .Replace("&", "and")
            .Replace("/", " ")
            .Select(c => char.IsLetterOrDigit(c) ? c : ' ')
            .ToArray())
            .Trim()
            .Replace("  ", " ")
            .Replace(" ", "-");
}

/// <summary>
/// Tiny abstraction so the Infrastructure seeder can ask "is this Development?"
/// without taking a dependency on the ASP.NET hosting types in the API layer.
/// </summary>
public interface IHostEnvironmentAccessor
{
    bool IsDevelopment { get; }
}
