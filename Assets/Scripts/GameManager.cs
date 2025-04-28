using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance
    {
        get => _instance;
    }
    private static GameManager _instance = null;

    public static int COLUMN_NUM = 6;
    public static int ROW_NUM = 13;
    public static float TILE_SIZE = 75f;

    public Transform puyoParent = null;
    public GameObject puyoPrefab = null;


    private List<PuyoController> puyoList = new List<PuyoController>();
    private PuyoController selectedPalletePuyo = null;


    void Awake()
    {
        if (_instance != null)
        {
            Component.Destroy(this);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    /// <summary>
    /// ぷよを登録
    /// </summary>
    public void RegisterPuyo(PuyoController puyo)
    {
        if (puyoList.Contains(puyo) == false)
        {
            puyoList.Add(puyo);
        }
    }

    /// <summary>
    /// 配置情報からぷよを取得
    /// </summary>
    /// <returns></returns>
    public PuyoController GetPuyo(Vector2Int position)
    {
        return puyoList.Find((x) => x.position == position);
    }

    /// <summary>
    /// 選択中ぷよを設定
    /// </summary>
    /// <param name="puyo"></param>
    public void SetSelectPuyo(PuyoController puyo)
    {
        if (selectedPalletePuyo != null)
        {
            selectedPalletePuyo.OnUnselected();
        }
        selectedPalletePuyo = puyo;
        selectedPalletePuyo.OnSelected();
    }

    /// <summary>
    /// 選択中ぷよを取得
    /// </summary>
    /// <returns></returns>
    public PuyoController GetSelectPuyo()
    {
        return selectedPalletePuyo;
    }

    /// <summary>
    /// パレットのぷよをフィールドに配置
    /// </summary>
    public void SetPalletePuyo(Vector2Int position)
    {
        var puyo = GetPuyo(position);
        if (selectedPalletePuyo != null)
        {
            // 既存ぷよがある場合はぷよを上書き
            if (puyo != null)
            {
                puyo.Change(selectedPalletePuyo);
            }
            // ない場合はプヨを作成
            else
            {
                RegisterPuyo(CreatePuyo(selectedPalletePuyo, position));
            }
        }
        else
        {
            if (puyo != null)
            {
                RemovePuyo(puyo);
            }
        }
    }

    public PuyoController CreatePuyo(PuyoController pallete_puyo, Vector2Int position)
    {
        var new_puyo_obj = GameObject.Instantiate(puyoPrefab, Vector3.zero, Quaternion.identity, puyoParent);
        var new_puyo = new_puyo_obj.GetComponent<PuyoController>();
        new_puyo.puyoType = pallete_puyo.puyoType;
        new_puyo.Setup();
        new_puyo.position = position;
        return new_puyo;
    }

    public void RemovePuyo(PuyoController puyo)
    {
        if (puyoList.Contains(puyo))
        {
            puyoList.Remove(puyo);
        }
        GameObject.Destroy(puyo);
    }
}
