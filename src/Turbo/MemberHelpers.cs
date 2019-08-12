using System;
using System.Collections.Generic;
using System.Reflection;

namespace Turbo
{
    /// <summary>
    /// Defines helper methods for retrieving the members required to represent an object graph.
    /// </summary>
    internal static class MemberHelpers
    {
        /// <summary>
        /// Gets the set of all types and methods touched by a particular object graph.
        /// </summary>
        /// <param name="root">The root of an object graph.</param>
        /// <returns>A set of types and methods.</returns>
        public static HashSet<MemberInfo> GetMembers(object root)
        {
            var visited = new HashSet<object>();
            var members = new HashSet<MemberInfo>();
            AddMembersToSet(root, visited, members);
            return members;
        }

        private static void AddMembersToSet(object obj, HashSet<object> visited, HashSet<MemberInfo> members)
        {
            if (obj == null || !visited.Add(obj))
            {
                return;
            }

            if (obj is Delegate)
            {
                var deleg = (Delegate)obj;
                members.Add(deleg.Method);
                AddMembersToSet(deleg.Target, visited, members);
            }

            var t = obj.GetType();
            members.Add(t);

            if (obj is Array)
            {
                var arr = (Array)obj;
                foreach (var elem in arr)
                {
                    AddMembersToSet(elem, visited, members);
                }
            }
            else
            {
                foreach (var field in t.GetFields())
                {
                    AddMembersToSet(field.GetValue(obj), visited, members);
                }
            }
        }
    }
}
