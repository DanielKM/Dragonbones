using UnityEngine;

using RTSEngine.Logging;

namespace RTSEngine.Faction
{
    [System.Serializable]
    public struct ColoredRenderer
    {
        public Renderer renderer;
        public int materialID;

        [Range(0.0f, 1.0f), Tooltip("How transparent would the color? The higher this value, the more transparent the color would be.")]
        public float transparency;

        [Range(0.0f, 1.0f), Tooltip("Adjust the darkness of the color, the higher this value, the darker the color would be.")]
        public float darkness;

        public void UpdateColor (Color color, IGameLoggingService logger)
        {
            if (!logger.RequireValid(renderer,
                $"[{GetType().Name}] The 'Renderer' field must be assigned or removed!")
                || !logger.RequireTrue(materialID >= 0 && materialID < renderer.materials.Length,
                $"[{GetType().Name}] The 'Material ID' {materialID} assigned is invalid. Please make sure that the target renderer supports that material."))
                return;

            // Adjust brightness:
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            color = Color.HSVToRGB(hue, saturation, 1 - darkness);

            // Adjust transparency:
            color.a = 1.0f - transparency;

            renderer.materials[materialID].color = color;
        }
    }
}
