// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Markup;

[assembly: System.Runtime.InteropServices.ComVisible(false)]
[assembly: AssemblyCompany("ic#code")]
[assembly: AssemblyProduct("SharpDevelop")]
[assembly: AssemblyCopyright("2000-2012 AlphaSierraPapa for the SharpDevelop Team")]
[assembly: AssemblyVersion(RevisionClass.Major + "." + RevisionClass.Minor + "." + RevisionClass.Build + "." + RevisionClass.Revision)]
[assembly: AssemblyInformationalVersion(RevisionClass.FullVersion + "-ca8a8e28")]
[assembly: NeutralResourcesLanguage("en-US")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2243:AttributeStringLiteralsShouldParseCorrectly",
	Justification = "AssemblyInformationalVersion does not need to be a parsable version")]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ICSharpCode.TreeView")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

//In order to begin building localizable applications, set 
//<UICulture>CultureYouAreCodingWith</UICulture> in your .csproj file
//inside a <PropertyGroup>.  For example, if you are using US english
//in your source files, set the <UICulture> to en-US.  Then uncomment
//the NeutralResourceLanguage attribute below.  Update the "en-US" in
//the line below to match the UICulture setting in the project file.

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]

[assembly: ThemeInfo(
	ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
									 //(used if a resource is not found in the page, 
									 // or application resource dictionaries)
	ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
											  //(used if a resource is not found in the page, 
											  // app, or any theme specific resource dictionaries)
)]

[assembly: XmlnsPrefix("http://icsharpcode.net/sharpdevelop/treeview", "treeview")]
[assembly: XmlnsDefinition("http://icsharpcode.net/sharpdevelop/treeview", "ICSharpCode.TreeView")]

internal static class RevisionClass
{
	public const string Major = "4";
	public const string Minor = "2";
	public const string Build = "0";
	public const string Revision = "8752";
	public const string VersionName = "Beta 2";

	public const string FullVersion = Major + "." + Minor + "." + Build + ".8752-Beta 2";
}
