using ArchitecturePatterns.SharedKernel;

namespace Hexagonal.Application;

public interface ICheckoutUseCase
{
    Task<ScenarioReport> Run(CancellationToken cancellationToken);
}

public sealed class HexagonalCheckoutUseCase(CommerceService service) : ICheckoutUseCase
{
    public Task<ScenarioReport> Run(CancellationToken cancellationToken) => service.RunCheckout(cancellationToken);
}
