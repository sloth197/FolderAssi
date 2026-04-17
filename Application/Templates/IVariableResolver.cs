using System.Collections.Generic;

namespace FolderAssi.Application.Templates;

public interface IVariableResolver
{
    string Resolve(string input, IReadOnlyDictionary<string, string> variables);
}
