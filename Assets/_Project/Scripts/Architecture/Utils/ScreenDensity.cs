using System.Runtime.InteropServices;
using UnityEngine;

namespace _Project.Scripts.Architecture.Utils
{
    public static class ScreenDensity
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern float UI_GetCanvasPixelRatio();
#endif

        public static float GetPixelRatio()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            float ratio = UI_GetCanvasPixelRatio();
            if (ratio > 0f)
            {
                return ratio;
            }
#endif
            return 1f;
        }

        public static float GetScreenHeightDp()
        {
            return Screen.height / GetPixelRatio();
        }
    }
}
