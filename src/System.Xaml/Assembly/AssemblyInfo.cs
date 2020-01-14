//
// AssemblyInfo.cs
//
// Author:
//   Andreas Nahr (ClassDevelopment@A-SoftTech.com)
//
// (C) 2003 Ximian, Inc.  http://www.ximian.com
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;
using System.Resources;
using System.Security;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if !PCL
using System.Security.Permissions;
#endif

// General Information about the System.Xaml assembly


[assembly: NeutralResourcesLanguage("en-US")]
[assembly: CLSCompliant(true)]
//[assembly: ComVisible (false)]

#if !PCL

[assembly: AssemblyCompany (Consts.MonoCompany)]
[assembly: AssemblyProduct (Consts.MonoProduct)]
[assembly: AssemblyCopyright (Consts.MonoCopyright)]
[assembly: AssemblyVersion (Consts.FxVersion)]
[assembly: SatelliteContractVersion (Consts.FxVersion)]
[assembly: AssemblyInformationalVersion (Consts.FxFileVersion)]
[assembly: AssemblyFileVersion (Consts.FxFileVersion)]

[assembly: AssemblyDelaySign (true)]

#if NET_2_1
[assembly: AssemblyKeyFile ("../silverlight.pub")]
#else
[assembly: AssemblyKeyFile ("../ecma.pub")]
[assembly: AllowPartiallyTrustedCallers]
[assembly: ComCompatibleVersion (1, 0, 3300, 0)]
[assembly: SecurityCritical (SecurityCriticalScope.Explicit)]
#endif


#if !MOBILE
[assembly: TypeForwardedTo (typeof (System.Windows.Markup.ValueSerializerAttribute))]
#endif
#endif

// TODO: change tests so they don't use internals.
#if STRONGNAME
[assembly:InternalsVisibleTo("System.Xaml_test_netstandard10, PublicKey=0024000004800000940000000602000000240000525341310004000001000100279450a21d0eae9fa5612b94c99ef198bc03154426a3ecc95738bcc9be40da62fd5cb0a13016d85e1aac3466fc6b8dffbbc1633703fb338d969770df088acf8dee9a1be049bfa1b9f8225c6364eefd89051a6760f786b675c5372409ffa236d89284405b39ed1d2dcdfb10fdb5d637171263e81d9bd3babf6a550217563439b2")]
[assembly:InternalsVisibleTo("System.Xaml_test_netstandard13, PublicKey=0024000004800000940000000602000000240000525341310004000001000100279450a21d0eae9fa5612b94c99ef198bc03154426a3ecc95738bcc9be40da62fd5cb0a13016d85e1aac3466fc6b8dffbbc1633703fb338d969770df088acf8dee9a1be049bfa1b9f8225c6364eefd89051a6760f786b675c5372409ffa236d89284405b39ed1d2dcdfb10fdb5d637171263e81d9bd3babf6a550217563439b2")]
[assembly:InternalsVisibleTo("System.Xaml.Test, PublicKey=0024000004800000940000000602000000240000525341310004000001000100279450a21d0eae9fa5612b94c99ef198bc03154426a3ecc95738bcc9be40da62fd5cb0a13016d85e1aac3466fc6b8dffbbc1633703fb338d969770df088acf8dee9a1be049bfa1b9f8225c6364eefd89051a6760f786b675c5372409ffa236d89284405b39ed1d2dcdfb10fdb5d637171263e81d9bd3babf6a550217563439b2")]
[assembly:InternalsVisibleTo("System.Xaml_test_core, PublicKey=0024000004800000940000000602000000240000525341310004000001000100279450a21d0eae9fa5612b94c99ef198bc03154426a3ecc95738bcc9be40da62fd5cb0a13016d85e1aac3466fc6b8dffbbc1633703fb338d969770df088acf8dee9a1be049bfa1b9f8225c6364eefd89051a6760f786b675c5372409ffa236d89284405b39ed1d2dcdfb10fdb5d637171263e81d9bd3babf6a550217563439b2")]
#else
[assembly:InternalsVisibleTo("System.Xaml_test_netstandard10")]
[assembly:InternalsVisibleTo("System.Xaml_test_netstandard13")]
[assembly:InternalsVisibleTo("System.Xaml_test_netstandard20")]
[assembly:InternalsVisibleTo("System.Xaml.Test")]
#endif