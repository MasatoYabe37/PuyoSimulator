using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuyoController : MonoBehaviour
{
    public enum ePuyoType
    {
        None,
        Red,
        Blue,
        Yellow,
        Green,
        Purple,
        Ojama,
        Num,
    }

    /// <summary> ぷよ色 </summary>
    public Color[] puyoColor = new Color[(int)ePuyoType.Num]
    {
        Color.white,
        Color.red,
        Color.blue,
        Color.yellow,
        Color.green,
        Color.magenta,
        Color.gray,
    };

    /// <summary> ぷよの種類 </summary>
    public ePuyoType puyoType = ePuyoType.None;

    /// <summary> パレット用のぷよ </summary>
    public bool isPalletePuyo = false;

    /// <summary> ぷよ画像 </summary>
    public Image puyoImage = null;

    /// <summary> ぷよ選択中 </summary>
    public GameObject puyoSelectImage = null;

    /// <summary> ぷよ選択ボタン </summary>
    public Button puyoSelectButton = null;

    /// <summary> 位置 </summary>
    public Vector2 positionRaw = new Vector2();

    /// <summary> （パレット用）選択中かどうか </summary>
    public bool isSelected = false;

    /// <summary> transform </summary>
    private RectTransform rectTransform = null;

    /// <summary> 消える予約 </summary>
    public bool isReqVanish { private set; get; } = false;

    /// <summary> 位置（インデックス） </summary>
    public Vector2Int position
    {
        get
        {
            float x = positionRaw.x / GameManager.TILE_SIZE;
            float y = positionRaw.y / GameManager.TILE_SIZE;
            return new Vector2Int((int)x, (int)y);
        }
        set
        {
            positionRaw.x = GameManager.TILE_SIZE * value.x;
            positionRaw.y = GameManager.TILE_SIZE * value.y;
            SetPosition();
        }
    }

    void Awake()
    {
        Initialize();
    }

    void Start()
    {
        if (isPalletePuyo)
        {
            Setup();
            puyoSelectButton.onClick.AddListener(OnClickPuyo);
        }
    }

    void Update()
    {
        positionRaw = rectTransform.anchoredPosition;
    }

    /// <summary>
    /// 初期化
    /// </summary>
    private void Initialize()
    {
        rectTransform = transform as RectTransform;
    }

    /// <summary>
    /// セットアップ(使えるようにする)
    /// </summary>
    public void Setup()
    {
        if(puyoImage != null)
        {
            puyoImage.color = puyoColor[(int)puyoType];
        }
        puyoSelectButton.enabled = isPalletePuyo;
        puyoSelectButton.interactable = isPalletePuyo;
        puyoSelectImage.SetActive(false);
    }

    /// <summary>
    /// 他のぷよや壁に重なっているか？
    /// </summary>
    /// <returns></returns>
    public bool isOvered()
    {
        // 地面に埋没
        if (position.y < 0)
        {
            return true;
        }
        // 横壁に埋没
        if (position.x < 0 || position.x > 5)
        {
            return true;
        }
        // ほかのぷよと重なっている
        var puyo = GameManager.Instance.GetPuyo(position);
        if (puyo != null && puyo != this)
        {
            return true;
        }
        // ○ 重なっていない
        return false;
    }

    /// <summary>
    /// 落下可能か？（下に何もないか？）
    /// </summary>
    /// <returns></returns>
    public bool isFallable()
    {
        // 重なっている
        if (isOvered())
        {
            return false;
        }

        // 下は地面
        if (position.y <= 0)
        {
            return false;
        }

        // ほかのぷよがある
        Vector2Int under_position = position;
        under_position.y -= 1;
        if (GameManager.Instance.GetPuyo(under_position) != null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// クイックドロップ
    /// </summary>
    public void quickDrop()
    {
        while(true)
        {
            if (isFallable() == false)
            {
                break;
            }
            Vector2Int under_position = position;
            under_position.y -= 1;
            position = under_position;
        }
    }

    /// <summary>
    /// ぷよの種類を切り替え
    /// </summary>
    public void Change(PuyoController pallete_puyo)
    {
        puyoType = pallete_puyo.puyoType;
        Setup();
    }

    /// <summary>
    /// ぷよのポジションをセット
    /// </summary>
    public void SetPosition()
    {
        rectTransform.anchoredPosition = positionRaw;
    }

    /// <summary>
    /// ぷよ（パレット）をクリック
    /// </summary>
    private void OnClickPuyo()
    {
        GameManager.Instance.SetSelectPuyo(this);
    }

    /// <summary>
    /// 選択された
    /// </summary>
    public void OnSelected()
    {
        puyoSelectImage.SetActive(true);
    }

    /// <summary>
    /// 選択外し
    /// </summary>
    public void OnUnselected()
    {
        puyoSelectImage.SetActive(false);
    }

    /// <summary>
    /// 消える予約
    /// </summary>
    public void RequestVanish()
    {
        isReqVanish = true;
    }
}
