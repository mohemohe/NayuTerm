== How to use

To use ApplicationUtilities class, you need to add a GUID information to your "AssemblyInfo.cs" file(only WPF project).
You can create a GUID in Visual Studio (Refer to http://msdn.microsoft.com/en-us/library/ms241442(v=vs.80).aspx).
If you get a new GUID, put a assembly attribute	in "AssemblyInfo.cs":

[assembly: Guid("Your GUID")]

And don't forget to add reference to "StoneDot.BasicLibrary.dll" in your project (Refer to http://msdn.microsoft.com/en-us/library/wkze6zky.aspx).

== License

StoneDot BasicLibrary is released under the MIT license:

* http://www.opensource.org/licenses/MIT