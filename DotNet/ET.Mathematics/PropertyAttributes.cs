namespace ET;

public class PropertyAttributes
{
    public class PropertyAttribute : Attribute
    {
    }
    
    /// <summary>
    /// Used by property drawers when vectors should be post normalized.
    /// </summary>
    public class PostNormalizeAttribute : PropertyAttribute {}

    /// <summary>
    /// Used by property drawers when vectors should not be normalized.
    /// </summary>
    public class DoNotNormalizeAttribute : PropertyAttribute {}
}

