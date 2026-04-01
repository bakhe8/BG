using BG.Application.Models;

namespace BG.Application.Contracts.Services;

public interface IArchitectureProfileService
{
    ArchitectureProfileDto GetCurrent();
}
