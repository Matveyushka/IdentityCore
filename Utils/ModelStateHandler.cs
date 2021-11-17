using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public static class ModelStateHandler
{
    static public List<string> GetErrorList(ModelStateDictionary state)
    {
        return state.Select(stateElement => stateElement.Value.Errors)
            .Where(errorList => errorList.Count > 0)
            .SelectMany(errorList => errorList.Select(error => error.ErrorMessage))
            .ToList();
    }
}