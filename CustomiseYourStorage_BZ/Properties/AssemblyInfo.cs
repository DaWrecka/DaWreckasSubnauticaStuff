using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if SN1
[assembly: AssemblyTitle("CustomiseYourStorage")]
[assembly: AssemblyProduct("CustomiseYourStorage")]
#elif BELOWZERO
[assembly: AssemblyTitle("CustomiseYourStorage_BZ")]
[assembly: AssemblyProduct("CustomiseYourStorage_BZ")]
#endif
[assembly: AssemblyCopyright("Copyright ©  2021")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("8d0989f2-9427-454c-af7a-e24e73ea9d44")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion(CustomiseYourStorage.CustomiseStoragePlugin.version)]
[assembly: AssemblyFileVersion(CustomiseYourStorage.CustomiseStoragePlugin.version)]
