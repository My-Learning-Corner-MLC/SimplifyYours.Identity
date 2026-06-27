namespace IdentityService.Domain.Identity;

public static class Permissions
{
    public const string EventsCreate = "events.create";
    public const string EventsView = "events.view";
    public const string EventsUpdate = "events.update";
    public const string GuestsAdd = "guests.add";
    public const string TenantManageUsers = "tenant.manage_users";

    public static IReadOnlyCollection<string> All { get; } = new[]
    {
        EventsCreate,
        EventsView,
        EventsUpdate,
        GuestsAdd,
        TenantManageUsers,
    };
}
