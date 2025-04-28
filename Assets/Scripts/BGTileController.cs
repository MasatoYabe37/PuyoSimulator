using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BGTileController : MonoBehaviour
{
    private Button tileButton = null;
    public Vector2Int position;

    void Start()
    {
        tileButton = GetComponent<Button>();
        tileButton.onClick.AddListener(OnClickTileButton);

        var rect = transform as RectTransform;
        position.x = (int)(rect.anchoredPosition.x / GameManager.TILE_SIZE);
        var parent_rect = transform.parent as RectTransform;
        position.y = (int)(parent_rect.anchoredPosition.y / GameManager.TILE_SIZE);
    }

    private void OnClickTileButton()
    {
        GameManager.Instance.SetPalletePuyo(position);
    }
}
