namespace Buzzard.Services;

public interface IPathValidator
{
    bool IsPathAllowed(string path);
    bool IsPathDenied(string path);
    bool IsPathBlocked(string path);
}
