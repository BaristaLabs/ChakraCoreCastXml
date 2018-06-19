namespace BaristaLabs.ChakraCoreCastXml.VisualStudioSetup
{
    using System.Runtime.InteropServices;

    [Guid("42843719-DB4C-46C2-8E7C-64F1816EFD5B")]
    [CoClass(typeof(SetupConfigurationClass))]
    [ComImport]
    public interface SetupConfiguration : ISetupConfiguration2, ISetupConfiguration
    {
    }
}
