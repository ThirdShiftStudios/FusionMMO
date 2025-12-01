#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class DisableSplashScreen
{
    static DisableSplashScreen()
    {
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
    }
}
#endif
