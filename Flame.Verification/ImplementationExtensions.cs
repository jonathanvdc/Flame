using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public class ImplementationExtensions
    {
        public static bool IsImplementationOf(IMethod Method, IMethod BaseMethod)
        {
            foreach (var item in Method.GetBaseMethods())
            {
                if (item.Equals(BaseMethod))
                {
                    return true;
                }
                else if (item.IsImplementationOf(BaseMethod))
                {
                    return true;
                }
            }
            return false;
        }

        public static IMethod GetImplementation(IMethod Method, IType Target)
        {
            if (Method is IAccessor)
            {
                var accessor = (IAccessor)Method;
                var type = accessor.AccessorType;
                var property = accessor.DeclaringProperty;
                foreach (var item in Target.GetAllProperties())
                    if (!item.get_IsAbstract() && !item.Equals(property))
                {
                    var accImpl = item.GetAccessor(type);

                    if (accImpl != null)
                        if (!accImpl.get_IsAbstract() && IsImplementationOf(accImpl, accessor))
                        {
                            return accImpl;
                        }
                }
            }
            else
            {
                foreach (var item in Target.GetAllMethods())
                {
                    if (!item.get_IsAbstract() && !item.Equals(Method))
                    {
                        if (IsImplementationOf(item, Method))
                        {
                            return item;
                        }
                    }
                }
            }
            return null;
        }
    }
}
