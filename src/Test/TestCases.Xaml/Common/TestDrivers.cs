namespace TestCases.Xaml.Common
{
    public enum TestDrivers
    {
        Default = XamlSerializationDeserializationDoubleRoundtripDriver,
        XamlSerializationDeserializationDoubleRoundtripDriver = 0,
        XamlDeserializationSerializationDoubleRoundtripDriver,
        DataContractSerializationDeserializationRoundtripDriver,
        XamlReaderWriterDriver,
        UseRunDelegate
    }
}
