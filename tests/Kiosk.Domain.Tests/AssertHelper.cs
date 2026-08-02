using Kiosk.Domain.Common;

namespace Kiosk.Domain.Tests;

public static class AssertHelper
{
    public static DomainException ThrowsDomain(string codigo, Action action)
    {
        var ex = Assert.Throws<DomainException>(action);
        Assert.Equal(codigo, ex.Code);
        return ex;
    }
}
