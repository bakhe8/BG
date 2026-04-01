using BG.Application.Contracts.Services;
using BG.Application.Models;

namespace BG.Application.Services;

internal sealed class ArchitectureProfileService : IArchitectureProfileService
{
    public ArchitectureProfileDto GetCurrent()
    {
        return new ArchitectureProfileDto(
            "BG",
            "ASP.NET Core 8",
            "Razor Pages",
            "REST API Controllers",
            "PostgreSQL",
            "IIS",
            "Internal Integration Layer");
    }
}
