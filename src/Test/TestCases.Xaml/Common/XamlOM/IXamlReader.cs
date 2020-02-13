using System;
using System.IO;

namespace TestCases.Xaml.Common.XamlOM
{
    public interface IXamlReader
    {
        Guid TraceGuid { get; set; }
        void Init(Stream input);
        bool Read();
    }
}
