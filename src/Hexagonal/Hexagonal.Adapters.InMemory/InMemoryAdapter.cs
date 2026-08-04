using ArchitecturePatterns.SharedKernel;

namespace Hexagonal.Adapters.InMemory;

public static class InMemoryAdapter
{
    public static CommerceService CreateService() => new(new CommerceState(), "hexagonal");
}
