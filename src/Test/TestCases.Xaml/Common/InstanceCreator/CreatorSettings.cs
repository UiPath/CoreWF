namespace TestCases.Xaml.Common.InstanceCreator
{
    public static class CreatorSettings
    {
        public static int MaxArrayLength = 10;
        public static int MaxListLength = 10;
        public static int MaxStringLength = 100;
        public static bool CreateOnlyAsciiChars = false;
        public static bool DontCreateSurrogateChars = false;
        public static bool CreateDateTimeWithSubMilliseconds = true;
        public static bool NormalizeEndOfLineOnXmlNodes = false;
    }
}
