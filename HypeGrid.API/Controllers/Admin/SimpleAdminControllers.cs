using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Campaigns;
using HypeGrid.Domain.Content;
using HypeGrid.Domain.Leads;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>Campaign board tasks — backs the CampaignDetail Tasks tab.</summary>
[Route("api/admin/tasks")]
public sealed class TasksController : AdminCrudController<CampaignTask>
{
    public TasksController(IRepository<CampaignTask> repo) : base(repo) { }

    protected override Expression<Func<CampaignTask, bool>> BuildSearchPredicate(string q)
        => t => t.Title.Contains(q);
}

/// <summary>Notes / internal messages — backs the admin Messages page.</summary>
[Route("api/admin/notes")]
public sealed class NotesController : AdminCrudController<Note>
{
    public NotesController(IRepository<Note> repo) : base(repo) { }
}

/// <summary>General enquiries from the website Contact form.</summary>
[Route("api/admin/enquiries")]
public sealed class EnquiriesController : AdminCrudController<Enquiry>
{
    public EnquiriesController(IRepository<Enquiry> repo) : base(repo) { }

    protected override Expression<Func<Enquiry, bool>> BuildSearchPredicate(string q)
        => e => e.FullName.Contains(q) || e.Email.Contains(q);
}

// ── Content management (admin CMS for currently-hardcoded website content) ──

[Route("api/admin/content/services")]
public sealed class ServicesAdminController : AdminCrudController<Service>
{
    public ServicesAdminController(IRepository<Service> repo) : base(repo) { }
}

[Route("api/admin/content/packages")]
public sealed class PackagesAdminController : AdminCrudController<Package>
{
    public PackagesAdminController(IRepository<Package> repo) : base(repo) { }
}

[Route("api/admin/content/testimonials")]
public sealed class TestimonialsAdminController : AdminCrudController<Testimonial>
{
    public TestimonialsAdminController(IRepository<Testimonial> repo) : base(repo) { }
}

[Route("api/admin/content/case-studies")]
public sealed class CaseStudiesAdminController : AdminCrudController<CaseStudy>
{
    public CaseStudiesAdminController(IRepository<CaseStudy> repo) : base(repo) { }
}
