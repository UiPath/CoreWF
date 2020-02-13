using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TestCases.Xaml.Common;

namespace TestCases.Xaml.Types.Class
{
    public class ClassType
    {
        ClassType2[] array = new ClassType2[3];

        public object Array
        {
            get
            {
                return this.array;
            }
            set
            {
                if (value is ClassType2[]) array = value as ClassType2[];
            }
        }

        #region Test Implementation
        public static List<TestCaseInfo> GetTestCases()
        {
            string instanceIDPrefix = Helper.GetInstanceIDPrefix(MethodInfo.GetCurrentMethod().DeclaringType);
            List<TestCaseInfo> testCases = new List<TestCaseInfo>();

            // bug 10194
            ClassType instance1 = new ClassType();
            testCases.Add(new TestCaseInfo { Target = instance1, TestID = instanceIDPrefix + 1 });

            return testCases;
        }
        #endregion
    }

    public struct ClassType1
    {

        string category;

        public string Category
        {
            get { return this.category; }
            set { this.category = value; }
        }

        #region Test Implementation
        public static List<TestCaseInfo> GetTestCases()
        {
            string instanceIDPrefix = Helper.GetInstanceIDPrefix(MethodInfo.GetCurrentMethod().DeclaringType);
            List<TestCaseInfo> testCases = new List<TestCaseInfo>();

            // property field initinalized to null, bug 9060
            ClassType1 instance1 = new ClassType1();
            instance1.Category = null;
            testCases.Add(new TestCaseInfo { Target = instance1, TestID = instanceIDPrefix + 1 });

            return testCases;
        }
        #endregion
    }

    public class ClassType2
    {
        ClassType1 category;

        public ClassType1 Category
        {
            get { return this.category; }
            set { this.category = value; }
        }

        #region Test Implementation
        public static List<TestCaseInfo> GetTestCases()
        {
            string instanceIDPrefix = Helper.GetInstanceIDPrefix(MethodInfo.GetCurrentMethod().DeclaringType);
            List<TestCaseInfo> testCases = new List<TestCaseInfo>();

            ClassType2 instance1 = new ClassType2();
            instance1.Category = new ClassType1();
            instance1.category.Category = "";
            testCases.Add(new TestCaseInfo { Target = instance1, TestID = instanceIDPrefix + 1 });

            return testCases;
        }
        #endregion
    }

    //empty class
    public class ClassType3
    {
        #region Test Implementation
        public static List<TestCaseInfo> GetTestCases()
        {
            string instanceIDPrefix = Helper.GetInstanceIDPrefix(MethodInfo.GetCurrentMethod().DeclaringType);
            List<TestCaseInfo> testCases = new List<TestCaseInfo>();

            ClassType3 instance1 = new ClassType3();
            testCases.Add(new TestCaseInfo { Target = instance1, TestID = instanceIDPrefix + 1 });

            return testCases;
        }
        #endregion
    }

    //empty struct
    public struct ClassType4
    {
        #region Test Implementation
        public static List<TestCaseInfo> GetTestCases()
        {
            string instanceIDPrefix = Helper.GetInstanceIDPrefix(MethodInfo.GetCurrentMethod().DeclaringType);
            List<TestCaseInfo> testCases = new List<TestCaseInfo>();

            ClassType4 instance1 = new ClassType4();
            testCases.Add(new TestCaseInfo { Target = instance1, TestID = instanceIDPrefix + 1 });

            return testCases;
        }
        #endregion
    }

    // mix
    public class ClassType5
    {
        ClassType1 field1;
        ClassType2 field2;
        ClassType3 field3;
        ClassType4 field4;

        public ClassType5()
        {
            this.field1 = new ClassType1();
            this.field1.Category = "Next Gen";

            this.field2 = new ClassType2();
            this.field2.Category = new ClassType1();
            //this.field2.Category.Category = "multiple/r/nlines";

            this.field3 = new ClassType3();
            this.field4 = new ClassType4();
        }

        public ClassType1 Field1
        {
            get { return field1; }
            set { field1 = value; }
        }

        public ClassType2 Field2
        {
            get { return field2; }
            set { field2 = value; }
        }

        public ClassType3 Field3
        {
            get { return field3; }
            set { field3 = value; }
        }

        public ClassType4 Field4
        {
            get { return field4; }
            set { field4 = value; }
        }

        #region Test Implementation
        public static List<TestCaseInfo> GetTestCases()
        {
            string instanceIDPrefix = Helper.GetInstanceIDPrefix(MethodInfo.GetCurrentMethod().DeclaringType);
            List<TestCaseInfo> testCases = new List<TestCaseInfo>();

            ClassType5 instance1 = new ClassType5();
            instance1.field1 = new ClassType1();
            instance1.field1.Category = "<Category>";
            instance1.field2 = new ClassType2();
            instance1.field2.Category = new ClassType1();
            instance1.field3 = new ClassType3();
            instance1.field4 = new ClassType4();
            testCases.Add(new TestCaseInfo { Target = instance1, TestID = instanceIDPrefix + 1 });

            return testCases;
        }
        #endregion
    }
}
