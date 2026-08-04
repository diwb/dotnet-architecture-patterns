using ArchitecturePatterns.SharedKernel;

namespace CleanArchitecture.Application;

public sealed class CleanCommerce : CommerceService
{
    public CleanCommerce(CommerceState state) : base(state, "clean") { }
}
