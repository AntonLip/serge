using CleanDistrict.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CleanDistrict.Web.Hubs;

[Authorize(Roles = AppRoles.Staff)]
public class DispatchHub : Hub
{
}
