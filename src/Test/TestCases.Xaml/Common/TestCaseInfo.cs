using System.Collections.Generic;

namespace TestCases.Xaml.Common
{
    public delegate void RunTest(string source, TestCaseInfo info);

    public class TestCaseInfo
    {
        object target;
        bool expectedResult;
        string expectedMessage;
        TestDrivers testDriver;
        string testID;

        public TestCaseInfo()
        {
            this.expectedResult = true;
            this.expectedMessage = null;
        }

        public object Target
        {
            get { return this.target; }
            set { this.target = value; }
        }

        public bool ExpectedResult
        {
            get { return this.expectedResult; }
            set { this.expectedResult = value; }
        }

        public string ExpectedMessage
        {
            get { return this.expectedMessage; }
            set { this.expectedMessage = value; }
        }

        public string TestID
        {
            get { return this.testID; }
            set { this.testID = value; }
        }

        public TestDrivers TestDriver
        {
            get { return testDriver; }
            set { testDriver = value; }
        }

        public RunTest RunTestDelegate { get; set; }

        public bool CompareAttachedProperties { get; set; }

        HashSet<string> xpathExpressions = new HashSet<string>();
        public ICollection<string> XPathExpresions { get { return xpathExpressions; } }

        Dictionary<string, string> xpathNamespacePrefixMap = new Dictionary<string, string>
        {
            { "cttxtc", "clr-namespace:CDF.Test.TestCases.Xaml.Types.ContentProperties;assembly=CDF.Test.TestCases.Xaml"},
            { "cttxt", "clr-namespace:CDF.Test.TestCases.Xaml.Types;assembly=CDF.Test.TestCases.Xaml" },
            { "x", Constants.Namespace2006},
            { "x2", Constants.NamespaceV2 },
            { "xasl", Constants.NamespaceBuiltinTypes }
        };

        public IDictionary<string, string> XPathNamespacePrefixMap { get { return xpathNamespacePrefixMap; } }

    }
}
