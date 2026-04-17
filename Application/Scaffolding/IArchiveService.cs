namespace FolderAssi.Application.Scaffolding;

public interface IArchiveService
{
    string CreateZip(string sourceDirectoryPath, string destinationZipPath);
}
