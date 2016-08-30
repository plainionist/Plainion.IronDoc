
namespace Plainion.IronDoc.Tests.Fakes
{
    class OverwrittenMethods
    {
        /// <summary>
        /// Returns nicely formatted message about the state of this object
        /// </summary>
        public override string ToString()
        {
            return "silence";
        }
    }
}
