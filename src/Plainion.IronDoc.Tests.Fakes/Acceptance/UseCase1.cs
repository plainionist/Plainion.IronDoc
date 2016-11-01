
namespace Plainion.IronDoc.Tests.Fakes.Acceptance
{
    /// <summary>
    /// This is use case number one.
    /// <para>A dedicated paragraph</para>
    /// 
    /// <seealso cref="System.IO.TextWriter"/>
    /// </summary>
    /// <remarks>
    /// And here are some remarks
    /// </remarks>
    public class UseCase1
    {
        public static readonly UseCase1 Empty = new UseCase1();

        /// <summary>
        /// Creates default intance of this use case.
        /// Use <see cref="Factory"/> to call it from outside.
        /// </summary>
        private UseCase1() { }

        // remain undocumented!
        public UseCase1( string s ) { }

        /// <summary>
        /// Accessor for the member S
        /// </summary>
        public string S { get; private set; }

        // remain undocumented!
        public string Name { get; private set; }

        /// <summary>
        /// Simple member
        /// </summary>
        /// <param name="s">some string</param>
        public void Run( string s ) { }

        /// <summary>
        /// Factory method
        /// </summary>
        /// <returns>an initialized instance of this class</returns>
        public static UseCase1 Factory() { return null; }
    }
}
