namespace BaristaLabs.ChakraCoreCastXml
{
    using System;
    using System.Diagnostics;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor)]
    [Conditional("DEBUG")]
    [ExcludeFromCodeCoverage]
    internal sealed class ExcludeFromCodeCoverageAttribute : Attribute
    {
        public string Reason { get; set; }
    }
}
