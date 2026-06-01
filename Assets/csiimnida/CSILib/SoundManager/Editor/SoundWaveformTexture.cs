using UnityEngine;

namespace csiimnida.CSILib.SoundManager.Editor
{
    /// <summary>
    /// AudioClip 의 샘플 데이터를 읽어 파형(waveform)을 그린 Texture2D 를 생성합니다.
    /// 고급 설정 창에서 시작 지점을 시각적으로 보여주는 용도로 사용합니다.
    /// </summary>
    internal static class SoundWaveformTexture
    {
        /// <summary>
        /// 지정한 클립의 파형 텍스처를 생성합니다.
        /// </summary>
        /// <param name="clip">대상 AudioClip</param>
        /// <param name="width">텍스처 가로 픽셀</param>
        /// <param name="height">텍스처 세로 픽셀</param>
        /// <param name="waveColor">파형 색</param>
        /// <param name="bgColor">배경 색</param>
        public static Texture2D Build(AudioClip clip, int width, int height, Color waveColor, Color bgColor)
        {
            width  = Mathf.Max(8, width);
            height = Mathf.Max(8, height);

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            // 배경 채우기
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = bgColor;

            if (clip == null || clip.samples <= 0 || clip.channels <= 0)
            {
                tex.SetPixels(pixels);
                tex.Apply();
                return tex;
            }

            int channels = clip.channels;
            int totalSamples = clip.samples * channels;
            float[] samples = new float[totalSamples];

            bool ok;
            try
            {
                // 압축/스트리밍 클립은 읽지 못할 수 있음 → 평평한 파형으로 폴백
                ok = clip.GetData(samples, 0);
            }
            catch
            {
                ok = false;
            }

            if (!ok)
            {
                tex.SetPixels(pixels);
                tex.Apply();
                return tex;
            }

            int half = height / 2;
            int frames = clip.samples;
            int framesPerColumn = Mathf.Max(1, frames / width);

            for (int x = 0; x < width; x++)
            {
                int startFrame = x * framesPerColumn;
                int endFrame = Mathf.Min(startFrame + framesPerColumn, frames);

                float peak = 0f;
                for (int f = startFrame; f < endFrame; f++)
                {
                    // 다채널은 첫 채널만 샘플링 (성능 + 충분히 대표적)
                    float v = Mathf.Abs(samples[f * channels]);
                    if (v > peak) peak = v;
                }

                int barHeight = Mathf.Clamp(Mathf.RoundToInt(peak * half), 1, half);
                for (int y = half - barHeight; y <= half + barHeight && y < height; y++)
                {
                    if (y < 0) continue;
                    pixels[y * width + x] = waveColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
