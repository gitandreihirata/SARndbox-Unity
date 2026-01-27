public static class S_LocalizationManagerAccessor
{
    private static S_LocalizationManager localizationManager;

    public static S_LocalizationManager GetLocalizationManager()
    {
        if (localizationManager == null)
        {
            localizationManager = UnityEngine.Object.FindObjectOfType<S_LocalizationManager>();
        }
        return localizationManager;
    }
}
