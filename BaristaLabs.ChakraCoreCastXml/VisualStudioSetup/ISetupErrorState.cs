﻿namespace BaristaLabs.ChakraCoreCastXml.VisualStudioSetup
{
    using System;
    using System.Runtime.InteropServices;

    [Guid("46DCCD94-A287-476A-851E-DFBC2FFDBC20")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface ISetupErrorState
    {
        [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
        ISetupFailedPackageReference[] GetFailedPackages();

        [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
        ISetupPackageReference[] GetSkippedPackages();
    }
}
