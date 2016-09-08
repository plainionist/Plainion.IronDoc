namespace Plainion.IronDoc.Tests.Fakes

type internal OverwrittenMethods() =
    /// <summary>
    /// Returns nicely formatted message about the state of this object
    /// </summary>
    override this.ToString() =
        "silence"

/// <summary>
/// This is a summary
/// </summary>
/// <remarks>
/// And here are some remarks
/// </remarks>
type internal SimplePublicClass() = class end
