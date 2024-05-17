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

    protected override Task<Stream> GetLoadStream(Guid instanceId)
    {
        return Task.FromResult<Stream>(File.OpenRead(GetFilePath(instanceId)));
    }

    protected override Task<Stream> GetSaveStream(Guid instanceId)
    {
        string filePath = GetFilePath(instanceId);
        File.Delete(filePath);
        return Task.FromResult<Stream>(File.OpenWrite(filePath));
    }

    private string GetFilePath(Guid instanceId)
    {
        return _storeDirectoryPath + "\\" + instanceId + "-InstanceData";
    }
}
