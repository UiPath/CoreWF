// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Markup;

namespace System.Activities.Statements;

[ContentProperty("Text")]
public sealed class WriteLine : CodeActivity
{
    public WriteLine() { }

    [DefaultValue(null)]
    public InArgument<TextWriter> TextWriter { get; set; }

    [DefaultValue(null)]
    public InArgument<string> Text { get; set; }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        RuntimeArgument textArgument = new("Text", typeof(string), ArgumentDirection.In);
        metadata.Bind(Text, textArgument);

        RuntimeArgument textWriterArgument = new("TextWriter", typeof(TextWriter), ArgumentDirection.In);
        metadata.Bind(TextWriter, textWriterArgument);

        metadata.SetArgumentsCollection(
            new Collection<RuntimeArgument>
            {
                textArgument,
                textWriterArgument
            });
    }

    protected override void Execute(CodeActivityContext context)
    {
        TextWriter writer = TextWriter.Get(context);
        writer ??= context.GetExtension<TextWriter>() ?? Console.Out;
        Fx.Assert(writer != null, "Writer should fallback to Console.Out and never be null");
        writer.WriteLine(Text.Get(context));
    }
}
