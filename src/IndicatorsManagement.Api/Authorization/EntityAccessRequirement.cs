using Microsoft.AspNetCore.Authorization;

namespace IndicatorsManagement.Api.Authorization;

/// <summary>
/// Requirement: user must belong to the entity being accessed, unless they are Super_Admin or Ministry_Admin.
/// </summary>
public class EntityAccessRequirement : IAuthorizationRequirement;
