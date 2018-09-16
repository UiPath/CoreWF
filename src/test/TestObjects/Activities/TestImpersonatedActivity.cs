// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Test.Common.TestObjects.Activities
{
    public class TestImpersonatedActivity : TestActivity
    {
        private TestActivity _body;

        public TestImpersonatedActivity(string impersonatedName, string impersonatedDomain, string impersonatedPwd, string actualUser)
        {
            this.ProductActivity = new ImpersonatedActivity(impersonatedName, impersonatedDomain, impersonatedPwd, actualUser);
            this.DisplayName = Guid.NewGuid().ToString();
        }

        public TestActivity Child
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
                ((ImpersonatedActivity)this.ProductActivity).Child = (value == null) ? null : value.ProductActivity;
            }
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            yield return _body;
        }
    }

    [DataContract]
    public class ImpersonationExecutionProperty : IExecutionProperty
    {
        [DataMember]
        private readonly string _actualId;
        [DataMember]

        private readonly string _impersonatedUserName;
        [DataMember]

        private readonly string _impersonatedUserPassword;
        [DataMember]

        private readonly string _impersonatedUserDomain;

        //ImpersonationDemo demo;

        public ImpersonationExecutionProperty(string name, string domain, string pwd, string id)
        {
            _impersonatedUserDomain = domain;
            _impersonatedUserName = name;
            _impersonatedUserPassword = pwd;
            _actualId = id;
        }

        public void CleanupWorkflowThread()
        {
            //Log.Trace("CleanupWorkflowThread");
            //demo.Revert();
        }

        public void SetupWorkflowThread()
        {
            //Log.Trace("SetupWorkflowThread");
            //demo = new ImpersonationDemo(impersonatedUserName, impersonatedUserDomain, impersonatedUserPassword, actualId);
            //demo.Impersonate();
        }
    }

    [DataContract]
    public class ImpersonatedActivity : NativeActivity
    {
        [DataMember]
        public Activity Child
        {
            get;
            set;
        }

        [DataMember]
        public string ActualId { get; set; }

        [DataMember]
        public string ImpersonatedUserName { get; set; }

        [DataMember]
        public string ImpersonatedUserPassword { get; set; }

        [DataMember]
        public string ImpersonatedUserDomain { get; set; }

        public ImpersonatedActivity()
        {
        }

        public ImpersonatedActivity(string impersonatedName, string impersonatedDomain, string impersonatedPwd, string actualUser)
        {
            this.ImpersonatedUserName = impersonatedName;
            this.ImpersonatedUserDomain = impersonatedDomain;
            this.ImpersonatedUserPassword = impersonatedPwd;
            this.ActualId = actualUser;
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            // None
        }

        protected override void Execute(NativeActivityContext context)
        {
            ImpersonationExecutionProperty iep = new ImpersonationExecutionProperty(ImpersonatedUserName, ImpersonatedUserDomain, ImpersonatedUserPassword, ActualId);
            context.Properties.Add("ImpersonationProperty", iep);
            context.ScheduleActivity(this.Child);
        }
    }

    //public class ImpersonationDemo
    //{
    //    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    //    public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
    //        int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

    //    // [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    //    public extern static bool CloseHandle(IntPtr handle);

    //    // [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    //    public extern static bool DuplicateToken(IntPtr ExistingTokenHandle,
    //        int SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);

    //    IntPtr tokenHandle = new IntPtr(0);
    //    WindowsIdentity newId;
    //    // WindowsImpersonationContext impUser;

    //    string actualId;
    //    string impersonatedId;

    //    public ImpersonationDemo(string name, string domain, string pwd, string actualUser)
    //    {
    //        this.actualId = actualUser;
    //        this.impersonatedId = domain + '\\' + name;

    //        try
    //        {
    //            tokenHandle = IntPtr.Zero;

    //            const int LOGON32_PROVIDER_DEFAULT = 0;
    //            //This parameter causes LogonUser to create a primary token.
    //            const int LOGON32_LOGON_INTERACTIVE = 2;

    //            // Call LogonUser to obtain a handle to an access token.
    //            bool returnValue = LogonUser(name, domain, pwd,
    //                LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT,
    //                ref tokenHandle);

    //            if (false == returnValue)
    //            {
    //                int ret = Marshal.GetLastWin32Error();
    //                //Log.Trace(String.Format("LogonUser failed for user {1} {2} {3} with error code : {0}", ret, name, domain, pwd));
    //                throw new System.ComponentModel.Win32Exception(ret);
    //            }
    //            else
    //            {
    //                //Log.Trace("Logon User Passed");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            //Log.Trace("Exception occurred. " + ex.Message);
    //        }
    //    }

    //    public void Impersonate()
    //    {
    //        //VERIFY INITIAL IDENTITY
    //        if (WindowsIdentity.GetCurrent().Name != actualId)
    //        {
    //            string message = String.Format("Expected user: {0}, user returned: {1}", actualId, WindowsIdentity.GetCurrent().Name);
    //            throw new Exception(message);
    //        }
    //        //Log.Trace("Before impersonation: " + WindowsIdentity.GetCurrent().Name);

    //        newId = new WindowsIdentity(tokenHandle);
    //        // impUser = newId.Impersonate();

    //        //VERIFY IMPERSONATED IDENTITY
    //        if (WindowsIdentity.GetCurrent().Name.ToUpper() != impersonatedId.ToUpper())
    //        {
    //            string message = String.Format("Expected user: {0}, user returned: {1}", impersonatedId, WindowsIdentity.GetCurrent().Name);
    //            throw new Exception(message);
    //        }
    //        //Log.Trace("After impersonation: " + WindowsIdentity.GetCurrent().Name);
    //    }

    //    public void Revert()
    //    {
    //        //// Stop impersonating the user.
    //        // impUser.Undo();

    //        //// Check the identity.
    //        if (WindowsIdentity.GetCurrent().Name != actualId)
    //        {
    //            string message = String.Format("Expected user: {0}, user returned: {1}", actualId, WindowsIdentity.GetCurrent().Name);
    //            throw new Exception(message);
    //        }
    //        //Log.Trace("After Undo: " + WindowsIdentity.GetCurrent().Name);
    //    }

    //    public void Dispose()
    //    {
    //        if (tokenHandle != IntPtr.Zero)
    //        {
    //            CloseHandle(tokenHandle);
    //        }
    //    }
    //}
}
