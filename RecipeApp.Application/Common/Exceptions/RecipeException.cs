using System;
using System.Collections.Generic;
using System.Text;

namespace RecipeApp.Application.Common.Exceptions
{
    public class RecipeNotFoundException : Exception
    {
        public RecipeNotFoundException() : base("Receita não encontrada.") { }
    }

    public class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException() : base("Você não tem permissão para acessar este recurso.") { }
    }
}
