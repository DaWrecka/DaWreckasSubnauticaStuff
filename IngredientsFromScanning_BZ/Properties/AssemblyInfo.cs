using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if SN1
[assembly: AssemblyTitle("PartsFromScanning")]
[assembly: AssemblyProduct("PartsFromScanning")]
#elif BELOWZERO
[assembly: AssemblyTitle("PartsFromScanning_BZ")]
[assembly: AssemblyProduct("PartsFromScanning_BZ")]
#endif
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyCopyright("Copyright ©  2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d05216e9-ccf3-4037-8c2f-36b571acb323")]

// Version information for an assembly consists of the following four values:
//
//	  Major Version
//	  Minor Version
//	  Build Number
//	  Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion(PartsFromScanning.PartsFromScanningPlugin.version)]
[assembly: AssemblyFileVersion(PartsFromScanning.PartsFromScanningPlugin.version)]
