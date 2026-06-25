namespace Voluble.PermitDesk.Web.Domain;

[Flags]
public enum DroneMaterialTypes
{
    None = 0,
    Snedbilder = 1 << 0,
    Lodbilder = 1 << 1,
    Video = 1 << 2,
    Ortofoto = 1 << 3,
    OvrigaData = 1 << 4,
}
