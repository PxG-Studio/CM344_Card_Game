using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI
{
    /// <summary>
    /// Handles the animated Persona-style victory banner with optional SFX/VFX.
    /// </summary>
    public class VictoryCutInController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform cutInRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text mainText;
        [SerializeField] private TMP_Text shadowText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image accentPulseImage;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool autoStyleDefaults = true;

        [Header("Audio")]
        [SerializeField] private AudioClip enterSfx;
        [SerializeField] private AudioClip impactSfx;
        [SerializeField] private AudioClip exitSfx;

        [Header("Timing")]
        [SerializeField] private float offscreenOffset = 2000f;
        [SerializeField] private float enterDuration = 0.45f;
        [SerializeField] private float holdDuration = 1.75f;
        [SerializeField] private float exitDuration = 0.45f;

        [Header("Easing")]
        [SerializeField] private AnimationCurve enterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve exitCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Accent Pulse")]
        [SerializeField] private float pulseDuration = 0.35f;
        [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Screen Shake")]
        [SerializeField] private bool enableScreenShake = true;
        [SerializeField] private float screenShakeDuration = 0.35f;
        [SerializeField] private float screenShakeStrength = 22f;
        [SerializeField] private Transform screenShakeTarget;

        private Vector2 anchoredOnScreen;
        private Coroutine playRoutine;
        private Coroutine pulseRoutine;
        private bool impactPlayedThisRun;
        private Coroutine shakeRoutine;
        private static Sprite cachedHalftoneSprite;
        private static AudioClip cachedChimeClip;
        private static AudioClip cachedWhooshClip;
        private static AudioClip cachedImpactClip;

        private void Awake()
        {
            if (cutInRoot == null)
            {
                cutInRoot = GetComponent<RectTransform>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            anchoredOnScreen = cutInRoot != null ? cutInRoot.anchoredPosition : Vector2.zero;
            ApplyDefaultLook();
            HideImmediate();
        }

        private void OnDisable()
        {
            HideImmediate();
        }

        private void HideImmediate()
        {
            if (cutInRoot != null)
            {
                cutInRoot.anchoredPosition = anchoredOnScreen + Vector2.left * offscreenOffset;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// Plays the cut-in animation with the supplied message and accent color.
        /// </summary>
        public void Play(string message, Color accentColor)
        {
            if (cutInRoot == null)
            {
                Debug.LogWarning("VictoryCutInController: Missing cutInRoot reference.");
                return;
            }

            if (mainText != null)
            {
                mainText.text = message;
            }

            if (shadowText != null)
            {
                shadowText.text = message;
            }

            Color backgroundColor = accentColor;
            backgroundColor.a = 0.95f;
            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
            }

            if (accentPulseImage != null)
            {
                accentPulseImage.color = accentColor;
            }

            gameObject.SetActive(true);

            impactPlayedThisRun = false;

            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
            }

            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
            }
            playRoutine = StartCoroutine(PlayRoutine());

            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
            }
            if (accentPulseImage != null)
            {
                pulseRoutine = StartCoroutine(PulseRoutine());
            }
        }

        private IEnumerator PlayRoutine()
        {
            Vector2 leftOffscreen = anchoredOnScreen + Vector2.left * offscreenOffset;
            Vector2 rightOffscreen = anchoredOnScreen + Vector2.right * offscreenOffset;

            yield return AnimateCutIn(leftOffscreen, anchoredOnScreen, enterDuration, enterCurve, enterSfx, true);
            
            if (enableScreenShake)
            {
                shakeRoutine = StartCoroutine(ScreenShakeRoutine());
            }
            
            yield return new WaitForSeconds(holdDuration);
            yield return AnimateCutIn(anchoredOnScreen, rightOffscreen, exitDuration, exitCurve, exitSfx, false);

            HideImmediate();
            gameObject.SetActive(false);
            playRoutine = null;
        }

        private IEnumerator AnimateCutIn(Vector2 from, Vector2 to, float duration, AnimationCurve curve, AudioClip sfx, bool canPlayImpact)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (audioSource != null && sfx != null)
            {
                audioSource.PlayOneShot(sfx);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = curve.Evaluate(t);
                cutInRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);

                if (canPlayImpact && !impactPlayedThisRun && impactSfx != null && audioSource != null && t >= 0.65f)
                {
                    audioSource.PlayOneShot(impactSfx);
                    impactPlayedThisRun = true;
                }

                yield return null;
            }

            cutInRoot.anchoredPosition = to;
        }

        private IEnumerator PulseRoutine()
        {
            float elapsed = 0f;
            Color baseColor = accentPulseImage.color;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / pulseDuration);
                float scale = Mathf.Lerp(0.5f, 1.15f, pulseCurve.Evaluate(t));
                accentPulseImage.transform.localScale = Vector3.one * scale;
                Color c = baseColor;
                c.a = Mathf.Lerp(0.9f, 0f, t);
                accentPulseImage.color = c;
                yield return null;
            }

            accentPulseImage.transform.localScale = Vector3.one;
            Color finalColor = baseColor;
            finalColor.a = 0f;
            accentPulseImage.color = finalColor;
            pulseRoutine = null;
        }

        private IEnumerator ScreenShakeRoutine()
        {
            Transform target = screenShakeTarget;
            if (target == null)
            {
                if (Camera.main != null)
                {
                    target = Camera.main.transform;
                }
                else
                {
                    yield break;
                }
            }

            Vector3 originalPosition = target.localPosition;
            float elapsed = 0f;
            while (elapsed < screenShakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float strength = Mathf.Lerp(screenShakeStrength, 0f, elapsed / screenShakeDuration);
                Vector3 offset = UnityEngine.Random.insideUnitSphere * strength;
                offset.z = 0f;
                target.localPosition = originalPosition + offset;
                yield return null;
            }
            target.localPosition = originalPosition;
            shakeRoutine = null;
        }

        private void ApplyDefaultLook()
        {
            if (!autoStyleDefaults)
            {
                return;
            }

            if (backgroundImage != null)
            {
                if (backgroundImage.sprite == null)
                {
                    backgroundImage.sprite = GenerateHalftoneSprite();
                }
                backgroundImage.type = Image.Type.Tiled;
                backgroundImage.color = new Color(0.07f, 0.07f, 0.07f, 0.98f);
            }

            if (mainText != null)
            {
                mainText.color = new Color(0.98f, 0.95f, 0.88f, 1f);
                mainText.fontSize = Mathf.Max(mainText.fontSize, 90f);
            }

            if (shadowText != null)
            {
                shadowText.color = new Color(0f, 0f, 0f, 0.7f);
                shadowText.fontSize = mainText != null ? mainText.fontSize : 90f;
            }

            if (accentPulseImage != null)
            {
                if (accentPulseImage.sprite == null)
                {
                    accentPulseImage.sprite = GenerateHalftoneSprite();
                }
            }

            if (audioSource != null)
            {
                if (enterSfx == null)
                {
                    enterSfx = GenerateWhooshClip();
                }
                if (exitSfx == null)
                {
                    exitSfx = GenerateWhooshClip();
                }
                if (impactSfx == null)
                {
                    impactSfx = GenerateImpactClip();
                }
            }
        }

        private static Sprite GenerateHalftoneSprite()
        {
            if (cachedHalftoneSprite != null)
            {
                return cachedHalftoneSprite;
            }

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;

            Color32 bg = new Color32(200, 30, 30, 255);
            Color32 dot = new Color32(15, 15, 15, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int grid = 6;
                    bool isDot = ((x % grid == 0) && (y % grid == 0));
                    texture.SetPixel(x, y, isDot ? dot : bg);
                }
            }
            texture.Apply();

            cachedHalftoneSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            cachedHalftoneSprite.name = "GeneratedHalftone";
            return cachedHalftoneSprite;
        }

        private static AudioClip GenerateWhooshClip()
        {
            if (cachedWhooshClip != null)
            {
                return cachedWhooshClip;
            }
            cachedWhooshClip = GenerateToneClip("Whoosh", 0.25f, 220f, 0.5f);
            return cachedWhooshClip;
        }

        private static AudioClip GenerateImpactClip()
        {
            if (cachedImpactClip != null)
            {
                return cachedImpactClip;
            }
            cachedImpactClip = GenerateToneClip("Impact", 0.15f, 120f, 1.0f, true);
            return cachedImpactClip;
        }

        private static AudioClip GenerateToneClip(string name, float duration, float baseFrequency, float amplitude, bool addNoise = false)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Clamp01(1f - (i / (float)sampleCount));
                float value = Mathf.Sin(2f * Mathf.PI * baseFrequency * t) * amplitude * envelope;
                if (addNoise)
                {
                    value += UnityEngine.Random.Range(-0.3f, 0.3f) * envelope;
                }
                samples[i] = Mathf.Clamp(value, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

