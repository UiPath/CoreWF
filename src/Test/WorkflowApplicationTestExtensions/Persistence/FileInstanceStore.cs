using System;
using System.IO;
using System.Threading.Tasks;

namespace WorkflowApplicationTestExtensions.Persistence;

public class FileInstanceStore : AbstractInstanceStore
{
    private readonly string _storeDirectoryPath;

    public FileInstanceStore(string storeDirectoryPath) : this(new JsonWorkflowSerializer(), storeDirectoryPath) { }

    public FileInstanceStore(IWorkflowSerializer workflowSerializer, string storeDirectoryPath) : base(workflowSerializer)
    {
        _storeDirectoryPath = storeDirectoryPath;
        Directory.CreateDirectory(storeDirectoryPath);
    }

    protected override Task<Stream> GetReadStream(Guid instanceId)
    {
        return Task.FromResult<Stream>(File.OpenRead(_storeDirectoryPath + "\\" + instanceId + "-InstanceData"));
    }

    protected override Task<Stream> GetWriteStream(Guid instanceId)
    {
        var filePath = _storeDirectoryPath + "\\" + instanceId + "-InstanceData";
        File.Delete(filePath);
        return Task.FromResult<Stream>(File.OpenWrite(filePath));
    }

}
