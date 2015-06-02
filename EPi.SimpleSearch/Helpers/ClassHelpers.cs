using System;
using System.Collections.Generic;
using System.Linq;

namespace DC.EPi.SimpleSearch.Helpers
{
    public static class ClassHelpers
    {
        public static List<string> GetBaseTypes(this Type type)
        {
            var typeFullNames = new List<string>();

            if (type.BaseType == null)
            {
                typeFullNames.AddRange(type.GetInterfaces().Select(x => x.FullName));
            }

            else
            {
                typeFullNames.AddRange(Enumerable.Repeat(type.BaseType, 1)
                                                 .Concat(type.GetInterfaces()).Select(x => x.FullName).ToList()
                                                 .Concat(type.GetInterfaces().SelectMany(GetBaseTypes))
                                                 .Concat(type.BaseType.GetBaseTypes()));
            }

            var result = typeFullNames.Distinct().ToList();
            result.Sort();

            return result;
        }
    }
}