using TMPro;

public static class TextUtilities
{
    /// <summary>
    /// Configures a TMP_Text component to show ellipsis (...) when text overflows.
    /// </summary>
    /// <param name="textComponent">The TextMeshPro text component to configure</param>
    public static void ConfigureEllipsisOverflow(TMP_Text textComponent)
    {
        if (textComponent == null) return;

        textComponent.overflowMode = TextOverflowModes.Ellipsis;
        textComponent.textWrappingMode = TextWrappingModes.NoWrap;
    }

    /// <summary>
    /// Configures multiple TMP_Text components to show ellipsis on overflow.
    /// </summary>
    /// <param name="textComponents">Array of text components to configure</param>
    public static void ConfigureEllipsisOverflow(params TMP_Text[] textComponents)
    {
        foreach (var textComponent in textComponents)
        {
            ConfigureEllipsisOverflow(textComponent);
        }
    }
}
