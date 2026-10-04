using UnityEngine;
using UnityEngine.UI;

public class Credit_Scripts : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 60f;
    private RectTransform creditsRectTransform;
    void Start()
    {
        creditsRectTransform = GetComponent<RectTransform>();
    }
    void Update()
    {
        creditsRectTransform.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;
    }
}
