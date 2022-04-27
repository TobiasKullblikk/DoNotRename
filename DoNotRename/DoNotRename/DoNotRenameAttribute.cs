using System;

namespace DoNotRename
{
    // TODO: support fields etc
    /// <summary>
    /// Attribute for generating analyzer errors if name of class does not match
    /// 'className' constructor argument value or if attribute argument value
    /// is not a string literal
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DoNotRenameClassAttribute : Attribute
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public DoNotRenameClassAttribute(string className) { }
#pragma warning restore IDE0060 // Remove unused parameter
    }
}

