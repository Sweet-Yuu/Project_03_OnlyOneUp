using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Dùng lại hình tròn của loading; chạy bằng thời gian thực kể cả game đang tạm dừng.
public sealed class OnlineRoundTransition : MonoBehaviour
{
    private CircularRevealGraphic cover;

    public static OnlineRoundTransition Create()
    {
        var root = new GameObject("Online round transition", typeof(Canvas), typeof(GraphicRaycaster));
        DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32766;
        var transition = root.AddComponent<OnlineRoundTransition>();
        var graphic = new GameObject("Circle", typeof(RectTransform), typeof(CanvasRenderer), typeof(CircularRevealGraphic));
        graphic.transform.SetParent(root.transform, false);
        transition.cover = graphic.GetComponent<CircularRevealGraphic>();
        var rect = transition.cover.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        transition.cover.color = new Color(1f, .65f, 0f, 1f);
        transition.cover.raycastTarget = true;
        transition.cover.SetProgress(1f);
        return transition;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Khi scene mới (ví dụ màn 2) được nạp thành công, tự hủy overlay để giao diện GameLoadingPanel của scene mới tiếp quản.
        Destroy(gameObject);
    }

    public IEnumerator Cover() => Animate(1f, 0f, .35f);

    private IEnumerator Animate(float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            cover.SetProgress(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration)));
            yield return null;
        }
        cover.SetProgress(to);
    }

    public void Reveal() => StartCoroutine(RevealAndDestroy());

    private IEnumerator RevealAndDestroy()
    {
        yield return Animate(0f, 1f, 1.2f);
        Destroy(gameObject);
    }

    public void ReturnToMenu()
    {
        ReturnToMenuTransition.BeginCovered();
        Destroy(gameObject);
    }
}
