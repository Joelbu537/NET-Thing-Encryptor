using System.Reflection;

namespace NET_Thing_Encryptor.Tests;

public sealed class VersionTests
{
    [Fact]
    public void ProgramVersion_ComesFromAssemblyInformationalVersion()
    {
        string informationalVersion = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion
            .Split('+', 2)[0];

        Assert.Equal(Version.Parse(informationalVersion), Program.Version);
    }
}
