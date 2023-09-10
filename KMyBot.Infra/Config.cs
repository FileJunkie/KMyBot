using Pulumi;

namespace KMyBot.Infra;

public class Config
{
    private readonly Pulumi.Config _config = new("KMyBot.Infra");
    public string DomainName => _config.Require(nameof(DomainName));
    public string DomainCertificateArn => _config.Require(nameof(DomainCertificateArn));
    public string DomainZoneId => _config.Require(nameof(DomainZoneId));
}