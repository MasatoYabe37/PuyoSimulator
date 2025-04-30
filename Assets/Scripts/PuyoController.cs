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

    public enum ePuyoRotate
    {
        Up,
        Right,
        Down,
        Left,
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

    /// <summary> canvas group </summary>
    private CanvasGroup canvasGroup = null;

    /// <summary> 消える予約 </summary>
    public bool isReqVanish { private set; get; } = false;

    /// <summary> 親ぷよ </summary>
    public PuyoController parentPuyo = null;

    /// <summary> 子ぷよ </summary>
    public PuyoController childPuyo = null;

    /// <summary> ぷよの回転（親の一を基準でどこにいるか） </summary>
    private ePuyoRotate puyoRotate = ePuyoRotate.Up;

    private float putTimer = 0.0f;
    private const float PUT_TIME = 0.5f;

    private bool isPuted = false;

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
        if (parentPuyo != null)
        {
            UpdateRotate();
        }
    }

    /// <summary>
    /// 初期化
    /// </summary>
    private void Initialize()
    {
        rectTransform = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
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
        return isOvered(position);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool isOvered(Vector2Int pos)
    {
        // 地面に埋没
        if (pos.y < 0)
        {
            return true;
        }
        // 横壁に埋没
        if (pos.x < 0 || pos.x > 5)
        {
            return true;
        }
        // ほかのぷよと重なっている
        var puyo = GameManager.Instance.GetPuyo(pos);
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
        // 下まで移動させる
        while(true)
        {
            if (isFallable() == false)
            {
                break;
            }
            if (childPuyo != null && childPuyo.isFallable() == false)
            {
                break;
            }
            Vector2Int under_position = position;
            under_position.y -= 1;
            position = under_position;
            if (childPuyo != null)
            {
                Vector2Int under_position_child = childPuyo.position;
                under_position_child.y -= 1;
                childPuyo.position = under_position_child;
            }
        }
        // 動けないようにする
        SetPut();
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

    /// <summary>
    /// 親を保存
    /// </summary>
    /// <param name="parent"></param>
    public void SetParent(PuyoController parent)
    {
        parentPuyo = parent;
        if (parent != null)
        {
            parent.SetChild(this);
        }
        puyoRotate = ePuyoRotate.Up;
        UpdateRotate();
    }

    /// <summary>
    /// 子を保存
    /// </summary>
    /// <param name="child"></param>
    public void SetChild(PuyoController child)
    {
        childPuyo = child;
    }

    /// <summary>
    /// 子を保存
    /// </summary>
    /// <param name="child"></param>
    public PuyoController GetChild()
    {
        return childPuyo;
    }

    /// <summary>
    /// 移動できるかチェック
    /// </summary>
    /// <returns></returns>
    public bool IsMove(Vector2Int move)
    {
        var pos = position + move;
        if (isOvered(pos))
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// 移動させる
    /// </summary>
    /// <param name="move"></param>
    public void Move(Vector2Int move)
    {
        position += move;
    }

    /// <summary>
    /// 回転できるかどうかチェック
    /// </summary>
    /// <param name="rot"></param>
    /// <returns></returns>
    public bool CheckRotate(ePuyoRotate rot)
    {
        // 親がいるなら位置を移動する
        if (parentPuyo != null)
        {
            var pos = parentPuyo.position;
            switch (puyoRotate)
            {
                // 親の上下は移動可能
                case ePuyoRotate.Up:
                case ePuyoRotate.Down:
                    return true;

                // 親の左右は、親の左右に壁やプヨがある場合回転不可
                case ePuyoRotate.Left:
                case ePuyoRotate.Right:
                    var pos_right = pos;
                    pos_right.x += 1;
                    var pos_left = pos;
                    pos_left.x -= 1;
                    return !(isOvered(pos_left) && isOvered(pos_right));
            }
        }
        else
        if (childPuyo != null)
        {
            return childPuyo.CheckRotate(rot);
        }

        return true;
    }

    /// <summary>
    /// 回転を更新
    /// </summary>
    public void UpdateRotate()
    {
        // 親がいるなら位置を移動する
        if (parentPuyo != null)
        {
            var pos = parentPuyo.position;
            switch (puyoRotate)
            {
                // 親の上に移動
                case ePuyoRotate.Up:
                    pos.y += 1;
                    break;
                // 親の下に移動
                case ePuyoRotate.Down:
                    pos.y -= 1;
                    break;
                // 親の左に移動
                case ePuyoRotate.Left:
                    pos.x -= 1;
                    break;
                // 親の右に移動
                case ePuyoRotate.Right:
                    pos.x += 1;
                    break;
            }
            position = pos;
            var move = ValidatePosition();
            position = pos + move;
        }
        else
        if (childPuyo != null)
        {
            childPuyo.UpdateRotate();
        }
    }

    /// <summary>
    /// 位置補正
    /// </summary>
    public Vector2Int ValidatePosition()
    {
        Vector2Int move = new Vector2Int(0, 0);
        // 親がいるなら位置を移動する
        if (parentPuyo != null)
        {
            switch (puyoRotate)
            {
                // 親の上にいる時は、親がどこかに埋もれているかチェック
                case ePuyoRotate.Up:
                    if (parentPuyo.isOvered())
                    {
                        move.y += 1;
                    }
                    break;
                // 親の下にいる時は、自分がどこかに埋もれているかチェック
                case ePuyoRotate.Down:
                    if (isOvered())
                    {
                        move.y += 1;
                    }
                    break;
                // 親の左にいる時は、
                case ePuyoRotate.Left:
                    if (isOvered())
                    {
                        move.x += 1;
                    }
                    break;
                // 親の右
                case ePuyoRotate.Right:
                    if (isOvered())
                    {
                        move.x -= 1;
                    }
                    break;
            }
        }
        return move;
    }

    /// <summary>
    /// 右回転
    /// </summary>s
    public void rotateRight()
    {
        var next_rot = (ePuyoRotate)(((int)puyoRotate + 1) % (int)ePuyoRotate.Num);
        var next_next_rot = (ePuyoRotate)(((int)puyoRotate + 2) % (int)ePuyoRotate.Num);
        if (CheckRotate(next_rot))
        {
            puyoRotate = next_rot;
            if (parentPuyo != null)
            {
                parentPuyo.puyoRotate = puyoRotate;
            }
            if (childPuyo != null)
            {
                childPuyo.puyoRotate = next_rot;
            }
            UpdateRotate();
        }
        else
        if (CheckRotate(next_next_rot))
        {
            puyoRotate = next_next_rot;
            if (parentPuyo != null)
            {
                parentPuyo.puyoRotate = puyoRotate;
            }
            if (childPuyo != null)
            {
                childPuyo.puyoRotate = next_rot;
            }
            UpdateRotate();
        }
    }

    /// <summary>
    /// 左回転
    /// </summary>
    public void rotateLeft()
    {
        var next_rot = (ePuyoRotate)(((int)puyoRotate - 1 + (int)ePuyoRotate.Num) % (int)ePuyoRotate.Num);
        var next_next_rot = (ePuyoRotate)(((int)puyoRotate - 2 + (int)ePuyoRotate.Num) % (int)ePuyoRotate.Num);
        if (CheckRotate(next_rot))
        {
            puyoRotate = next_rot;
            if (parentPuyo != null)
            {
                parentPuyo.puyoRotate = puyoRotate;
            }
            if (childPuyo != null)
            {
                childPuyo.puyoRotate = puyoRotate;
            }
            UpdateRotate();
        }
        else
        if (CheckRotate(next_next_rot))
        {
            puyoRotate = next_next_rot;
            if (parentPuyo != null)
            {
                parentPuyo.puyoRotate = puyoRotate;
            }
            if (childPuyo != null)
            {
                childPuyo.puyoRotate = puyoRotate;
            }
            UpdateRotate();
        }
    }

    /// <summary>
    /// 動けない状態にする
    /// </summary>
    public void SetPut()
    {
        GameManager.Instance.RegisterPuyo(this);
        var parent = parentPuyo;
        var child = childPuyo;
        putTimer = PUT_TIME;
        parentPuyo = null;
        childPuyo = null;
        isPuted = true;
        if (parent != null)
        {
            parent.SetPut();
        }
        if (child != null)
        {
            child.SetPut();
        }
    }

    public bool IsPuted()
    {
        return isPuted; 
    }

    public void SetInvisible()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.0f;
        }
    }

    public void SetVisible()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1.0f;
        }
    }

}
