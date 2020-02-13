namespace TestCases.Xaml.Common
{
    public class Global
    {
        static object SyncRoot = new object();
        const string DefaultResultFileNamePrefix = "out";
        static int ResultFileCount = 0;
        public const string SerializationSchemaFileName = "schemas.microsoft.com.2003.10.Serialization.xsd";
        public const string SerializationSchemaTargetNamespace = "http://schemas.microsoft.com/2003/10/Serialization/";
        public const string ToConsoleEnvironmentVariable = "ToConsole";
        public const string ToFileEnvironmentVariable = "ToFile";
        public const string GetTestCasesMethodName = "GetTestCases";

        public static string UniqueResultFileName
        {
            get
            {
                lock (Global.SyncRoot)
                {
                    return Global.DefaultResultFileNamePrefix + Global.ResultFileCount++;
                }
            }
        }
    }
}
